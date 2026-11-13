#!/usr/bin/env -S dotnet --
#:project $(SourceDirectory)timewarp-nuru/timewarp-nuru.csproj

#region Purpose
// Pins the JSON key binding config search order, startup profile resolution, sample configs, and REPL integration; split from repl-46-key-binding-config-loader.cs.
#endregion

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.ReplTests.KeyBindingConfigLoaderTestsSearch
{
[TestTag("REPL")]
public class KeyBindingConfigSearchTests
{
  [ModuleInitializer]
  internal static void Register() => RegisterTests<KeyBindingConfigSearchTests>();

  public static async Task Search_should_prefer_environment_then_project_then_user()
  {
    string root = Path.Combine(Path.GetTempPath(), "nuru-kb-" + Guid.NewGuid().ToString("N"));
    string project = Path.Combine(root, "project");
    string user = Path.Combine(root, "user");
    try
    {
      Directory.CreateDirectory(Path.Combine(project, ".nuru"));
      Directory.CreateDirectory(Path.Combine(user, ".nuru"));
      string projectFile = Path.Combine(project, ".nuru", "keybindings.json");
      string userFile = Path.Combine(user, ".nuru", "keybindings.json");
      string environmentFile = Path.Combine(root, "env.json");
      await File.WriteAllTextAsync(projectFile, """{"name":"Project","baseProfile":"Vi"}""");
      await File.WriteAllTextAsync(userFile, """{"name":"User","baseProfile":"Emacs"}""");
      await File.WriteAllTextAsync(environmentFile, """{"name":"Env","baseProfile":"VSCode"}""");

      IKeyBindingProfile? environmentProfile = Load(new KeyBindingConfigSearch(environmentFile, project, user));
      environmentProfile.Name.ShouldBe("Env");

      IKeyBindingProfile? projectProfile = Load
      (
        new KeyBindingConfigSearch(Path.Combine(root, "missing.json"), project, user)
      );
      projectProfile.Name.ShouldBe("Project");

      File.Delete(projectFile);
      IKeyBindingProfile? userProfile = Load(new KeyBindingConfigSearch(null, project, user));
      userProfile.Name.ShouldBe("User");

      File.Delete(userFile);
      bool found = KeyBindingConfigLoader.TryLoad
      (
        new KeyBindingConfigSearch(null, project, user),
        out IKeyBindingProfile? none
      );
      found.ShouldBeFalse();
      none.ShouldBeNull();

      Should.Throw<KeyBindingConfigException>
      (
        () => KeyBindingConfigLoader.Load(new KeyBindingConfigSearch(null, project, user))
      );

      await File.WriteAllTextAsync(environmentFile, "{");
      await File.WriteAllTextAsync(projectFile, """{"name":"Project","baseProfile":"Vi"}""");
      KeyBindingConfigException invalid = Should.Throw<KeyBindingConfigException>
      (
        () => KeyBindingConfigLoader.TryLoad
        (
          new KeyBindingConfigSearch(environmentFile, project, user),
          out IKeyBindingProfile? _
        )
      );
      invalid.Message.ShouldContain(environmentFile);
      invalid.Message.ShouldContain("Invalid key binding JSON");
    }
    finally
    {
      if (Directory.Exists(root))
      {
        Directory.Delete(root, recursive: true);
      }
    }

    await Task.CompletedTask;
  }

  public static async Task ResolveStartupProfile_should_let_explicit_choices_win()
  {
    string root = Path.Combine(Path.GetTempPath(), "nuru-kb-" + Guid.NewGuid().ToString("N"));
    try
    {
      Directory.CreateDirectory(root);
      string environmentFile = Path.Combine(root, "env.json");
      await File.WriteAllTextAsync(environmentFile, """{"name":"FromFile","baseProfile":"Emacs"}""");
      KeyBindingConfigSearch search = new(environmentFile, root, root);

      CustomKeyBindingProfile explicitProfile = new CustomKeyBindingProfile().WithName("Explicit");
      IKeyBindingProfile chosen = KeyBindingConfigLoader.ResolveStartupProfile
      (
        explicitProfile,
        "Vi",
        search
      );
      chosen.ShouldBeSameAs(explicitProfile);

      IKeyBindingProfile named = KeyBindingConfigLoader.ResolveStartupProfile(null, "Emacs", search);
      (named is EmacsKeyBindingProfile).ShouldBeTrue("An explicit profile name skips config files");
      named.Name.ShouldBe("Emacs");

      await File.WriteAllTextAsync(environmentFile, "{");
      IKeyBindingProfile stillNamed = KeyBindingConfigLoader.ResolveStartupProfile(null, "Vi", search);
      (stillNamed is ViKeyBindingProfile).ShouldBeTrue("A bad config file is not read when the name is explicit");

      await File.WriteAllTextAsync(environmentFile, """{"name":"FromFile","baseProfile":"VSCode"}""");
      IKeyBindingProfile discovered = KeyBindingConfigLoader.ResolveStartupProfile(null, "Default", search);
      discovered.Name.ShouldBe("FromFile");

      File.Delete(environmentFile);
      IKeyBindingProfile fallback = KeyBindingConfigLoader.ResolveStartupProfile(null, "Default", search);
      (fallback is DefaultKeyBindingProfile).ShouldBeTrue();
    }
    finally
    {
      if (Directory.Exists(root))
      {
        Directory.Delete(root, recursive: true);
      }
    }

    await Task.CompletedTask;
  }

  public static async Task Sample_configs_should_load()
  {
    string root = FindRepoRoot();
    string[] samples =
    [
      Path.Combine(root, "samples", "configuration", "emacs-enhanced.json"),
      Path.Combine(root, "samples", "configuration", "vi-enhanced.json"),
      Path.Combine(root, "samples", "configuration", "minimal.json")
    ];

    string[] names = ["EmacsEnhanced", "ViEnhanced", "Minimal"];
    for (int index = 0; index < samples.Length; index++)
    {
      CustomKeyBindingProfile profile = KeyBindingConfigLoader.LoadFromFile(samples[index]);
      profile.Name.ShouldBe(names[index], samples[index]);
    }

    await Task.CompletedTask;
  }

  public static async Task CreateDefaultSearch_should_use_the_process_locations()
  {
    KeyBindingConfigSearch search = KeyBindingConfigLoader.CreateDefaultSearch();
    search.ProjectDirectory.ShouldBe(Directory.GetCurrentDirectory());
    search.EnvironmentVariablePath.ShouldBe
    (
      Environment.GetEnvironmentVariable(KeyBindingConfigLoader.EnvironmentVariableName)
    );
    search.UserProfileDirectory.ShouldBe
    (
      Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)
    );
    await Task.CompletedTask;
  }

  [Timeout(5000)]
  public static async Task Loaded_profile_should_run_in_the_repl()
  {
    using TestTerminal terminal = new();
    terminal.QueueLine("exit");

    CustomKeyBindingProfile profile = KeyBindingConfigLoader.LoadFromJson
    (
      """
      {"name":"FromJson","baseProfile":"Default"}
      """
    );

    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .AddRepl(options =>
      {
        options.KeyBindingProfile = profile;
        options.WelcomeMessage = null;
      })
      .Build();

    await app.RunAsync(["--interactive"]);

    terminal.OutputContains("Goodbye!").ShouldBeTrue("A JSON profile should drive the REPL");
  }

  private static IKeyBindingProfile Load(KeyBindingConfigSearch search)
  {
    bool found = KeyBindingConfigLoader.TryLoad(search, out IKeyBindingProfile? profile);
    found.ShouldBeTrue();
    profile.ShouldNotBeNull();
    return profile!;
  }

  private static string FindRepoRoot()
  {
    string? directory = Directory.GetCurrentDirectory();
    while (directory is not null)
    {
      if (File.Exists(Path.Combine(directory, "timewarp-nuru.slnx")))
      {
        return directory;
      }

      directory = Path.GetDirectoryName(directory);
    }

    throw new InvalidOperationException("Could not find timewarp-nuru.slnx from the current directory.");
  }
}
}
