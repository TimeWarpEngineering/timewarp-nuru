#!/usr/bin/env -S dotnet --
#:project $(SourceDirectory)timewarp-nuru/timewarp-nuru.csproj

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.ReplTests.KeyBindingCatalogTests
{

/// <summary>
/// The key-binding catalog stays aligned with each built-in profile, and the REPL command prints it.
/// </summary>
[TestTag("REPL")]
public class KeyBindingCatalogTests
{
  [ModuleInitializer]
  internal static void Register() => RegisterTests<KeyBindingCatalogTests>();

  private sealed class EmptyRoutes : IReplRouteProvider
  {
    public IReadOnlyList<string> GetCommandPrefixes() => [];

    public IEnumerable<CompletionCandidate> GetCompletions(string[] args, bool hasTrailingSpace) => [];

    public bool IsKnownCommand(string token) => false;
  }

  public static Task Should_match_every_built_in_profile()
  {
    AssertCatalogMatchesProfile(new DefaultKeyBindingProfile());
    AssertCatalogMatchesProfile(new EmacsKeyBindingProfile());
    AssertCatalogMatchesProfile(new ViKeyBindingProfile());
    AssertCatalogMatchesProfile(new VSCodeKeyBindingProfile());
    return Task.CompletedTask;
  }

  public static Task Should_format_default_bindings_by_category()
  {
    string text = KeyBindingCatalog.Render("Default", key: null, function: null, detailed: false);

    text.ShouldContain("Profile: Default");
    text.ShouldContain("Basic editing functions");
    text.ShouldContain(new string('=', "Basic editing functions".Length));
    text.ShouldContain("Cursor movement functions");
    text.ShouldContain("BeginningOfLine");
    text.ShouldContain("Ctrl+a");
    text.ShouldContain("Home");
    text.ShouldContain("Key");
    text.ShouldContain("Function");
    text.ShouldContain("Description");
    return Task.CompletedTask;
  }

  public static Task Should_filter_by_function_name()
  {
    IReadOnlyList<KeyBindingRow> rows = KeyBindingCatalog.List("Default", function: "BeginningOfLine");

    rows.Count.ShouldBe(2);
    rows.ShouldContain(row => row.Key == "Ctrl+a");
    rows.ShouldContain(row => row.Key == "Home");
    rows.ShouldAllBe(row => row.Function == "BeginningOfLine");

    IReadOnlyList<KeyBindingRow> folded = KeyBindingCatalog.List("Default", function: "beginningofline");
    folded.Count.ShouldBe(rows.Count);
    return Task.CompletedTask;
  }

  public static Task Should_filter_by_exact_chord_and_by_substring()
  {
    IReadOnlyList<KeyBindingRow> exact = KeyBindingCatalog.List("Default", key: "Ctrl+a");
    exact.Count.ShouldBe(1);
    exact[0].Function.ShouldBe("BeginningOfLine");

    IReadOnlyList<KeyBindingRow> folded = KeyBindingCatalog.List("Default", key: "ctrl+a");
    folded.Count.ShouldBe(1);
    folded[0].Function.ShouldBe("BeginningOfLine");

    IReadOnlyList<KeyBindingRow> arrows = KeyBindingCatalog.List("Default", key: "Arrow");
    arrows.ShouldContain(row => row.Key == "LeftArrow");
    arrows.ShouldAllBe(row => row.Key.Contains("Arrow", StringComparison.OrdinalIgnoreCase));
    return Task.CompletedTask;
  }

  public static Task Should_render_a_detailed_block()
  {
    string text = KeyBindingCatalog.Render("Default", key: "Ctrl+a", function: null, detailed: true);

    text.ShouldContain("Ctrl+a");
    text.ShouldContain("Function: BeginningOfLine");
    text.ShouldContain("Category:");
    text.ShouldContain("Description:");
    return Task.CompletedTask;
  }

  public static Task Should_reject_an_unknown_profile()
  {
    ArgumentException exception = Should.Throw<ArgumentException>(() => KeyBindingCatalog.List("Nope"));
    exception.Message.ShouldContain("Emacs");
    exception.Message.ShouldContain("Valid profiles");
    exception.Message.ShouldNotContain("Parameter");
    return Task.CompletedTask;
  }

  public static Task Should_say_when_nothing_matches()
  {
    string text = KeyBindingCatalog.Render("Default", key: null, function: "NotARealFunction", detailed: false);
    text.ShouldContain("Profile: Default");
    text.ShouldContain("No key bindings match.");
    return Task.CompletedTask;
  }

  public static async Task Should_list_the_command_in_cli_help_when_enabled()
  {
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .AddRepl()
      .ConfigureHelp(options => options.ShowReplCommandsInCli = true)
      .Build();

    int exitCode = await app.RunAsync(["--help"]);

    exitCode.ShouldBe(0);
    terminal.OutputContains("key-bindings").ShouldBeTrue();
    terminal.OutputContains("Show REPL key bindings").ShouldBeTrue();
  }

  public static Task Should_show_the_quote_key_for_possible_completions()
  {
    IReadOnlyList<KeyBindingRow> rows = KeyBindingCatalog.List("Default", key: "Alt+'");
    rows.Count.ShouldBe(1);
    rows[0].Function.ShouldBe("PossibleCompletions");
    rows[0].Key.ShouldBe("Alt+'");
    return Task.CompletedTask;
  }

  public static Task Should_round_trip_displayed_chords()
  {
    AssertRoundTrip(ConsoleKey.A, ConsoleModifiers.Control, "Ctrl+a");
    AssertRoundTrip(ConsoleKey.OemComma, ConsoleModifiers.Alt | ConsoleModifiers.Shift, "Alt+<");
    AssertRoundTrip(ConsoleKey.OemMinus, ConsoleModifiers.Control, "Ctrl+-");
    return Task.CompletedTask;
  }

  public static Task Should_describe_profile_specific_bindings()
  {
    string emacs = KeyBindingCatalog.Render("Emacs", key: null, function: null, detailed: false);
    emacs.ShouldContain("Ctrl+a");
    emacs.ShouldContain("BeginningOfLine");
    emacs.ShouldNotContain("UpArrow");

    string vi = KeyBindingCatalog.Render("Vi", key: null, function: null, detailed: false);
    vi.ShouldNotContain("PossibleCompletions");

    IReadOnlyList<KeyBindingRow> visualStudio = KeyBindingCatalog.List("VSCode", key: "Ctrl+a");
    visualStudio.Count.ShouldBe(1);
    visualStudio[0].Function.ShouldBe("SelectAll");
    return Task.CompletedTask;
  }

  public static async Task Should_list_bindings_from_the_repl()
  {
    using TestTerminal terminal = new();
    terminal.QueueLine("key-bindings");
    terminal.QueueLine("exit");

    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .AddRepl(options =>
      {
        options.EnableColors = false;
        options.PersistHistory = false;
        options.WelcomeMessage = null;
        options.ShowTiming = false;
        options.KeyBindingProfile = new DefaultKeyBindingProfile();
      })
      .Build();
    await app.RunAsync(["--interactive"]);

    terminal.OutputContains("BeginningOfLine").ShouldBeTrue();
    terminal.OutputContains("Ctrl+a").ShouldBeTrue();
    terminal.OutputContains("Basic editing functions").ShouldBeTrue();
    terminal.OutputContains("Goodbye!").ShouldBeTrue();
  }

  public static async Task Should_filter_bindings_from_the_repl()
  {
    using TestTerminal terminal = new();
    terminal.QueueLine("key-bindings --function BeginningOfLine");
    terminal.QueueLine("exit");

    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .AddRepl(options =>
      {
        options.EnableColors = false;
        options.PersistHistory = false;
        options.WelcomeMessage = null;
        options.ShowTiming = false;
        options.KeyBindingProfile = new DefaultKeyBindingProfile();
      })
      .Build();
    await app.RunAsync(["--interactive"]);

    terminal.OutputContains("BeginningOfLine").ShouldBeTrue();
    terminal.OutputContains("PreviousHistory").ShouldBeFalse();
    terminal.OutputContains("Goodbye!").ShouldBeTrue();
  }

  public static async Task Should_list_the_emacs_profile_from_the_repl()
  {
    using TestTerminal terminal = new();
    terminal.QueueLine("key-bindings");
    terminal.QueueLine("exit");

    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .AddRepl(options =>
      {
        options.EnableColors = false;
        options.PersistHistory = false;
        options.WelcomeMessage = null;
        options.ShowTiming = false;
        options.KeyBindingProfile = new EmacsKeyBindingProfile();
      })
      .Build();
    await app.RunAsync(["--interactive"]);

    terminal.OutputContains("Profile: Emacs").ShouldBeTrue();
    terminal.OutputContains("BeginningOfLine").ShouldBeTrue();
    terminal.OutputContains("UpArrow").ShouldBeFalse();
  }

  public static async Task Should_accept_a_positional_profile_name()
  {
    using TestTerminal terminal = new();
    terminal.QueueLine("key-bindings Emacs");
    terminal.QueueLine("exit");

    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .AddRepl(options =>
      {
        options.EnableColors = false;
        options.PersistHistory = false;
        options.WelcomeMessage = null;
        options.ShowTiming = false;
        options.KeyBindingProfile = new DefaultKeyBindingProfile();
      })
      .Build();
    await app.RunAsync(["--interactive"]);

    terminal.OutputContains("Profile: Emacs").ShouldBeTrue();
    terminal.OutputContains("UpArrow").ShouldBeFalse();
  }

  public static async Task Should_report_a_custom_profile_and_still_list_a_built_in()
  {
    using TestTerminal terminal = new();
    terminal.QueueLine("key-bindings");
    terminal.QueueLine("key-bindings --profile Default");
    terminal.QueueLine("exit");

    // The line editor submits on Enter, so the custom profile keeps a base
    // that binds Enter. The listing still uses the custom name, not the base.
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .AddRepl(options =>
      {
        options.EnableColors = false;
        options.PersistHistory = false;
        options.WelcomeMessage = null;
        options.ShowTiming = false;
        options.KeyBindingProfile = new CustomKeyBindingProfile(new DefaultKeyBindingProfile()).WithName("Mine");
      })
      .Build();
    await app.RunAsync(["--interactive"]);

    terminal.ErrorOutput.ShouldContain("Unknown key binding profile: 'Mine'");
    terminal.OutputContains("Profile: Default").ShouldBeTrue();
    terminal.OutputContains("BeginningOfLine").ShouldBeTrue();
    terminal.OutputContains("Goodbye!").ShouldBeTrue();
  }

  public static async Task Should_report_a_json_profile_loaded_by_the_reader()
  {
    string configFile = Path.Combine(Path.GetTempPath(), $"nuru-keybindings-{Guid.NewGuid():N}.json");
    string? previous = Environment.GetEnvironmentVariable(KeyBindingConfigLoader.EnvironmentVariableName);
    await File.WriteAllTextAsync(configFile, """{"name":"Json","baseProfile":"Default"}""");
    Environment.SetEnvironmentVariable(KeyBindingConfigLoader.EnvironmentVariableName, configFile);
    try
    {
      using TestTerminal terminal = new();
      terminal.QueueLine("key-bindings");
      terminal.QueueLine("exit");

      // No explicit profile, so the reader loads the JSON file. The listing
      // must name that profile instead of silently printing Default.
      NuruApp app = NuruApp.CreateBuilder()
        .UseTerminal(terminal)
        .AddRepl(options =>
        {
          options.EnableColors = false;
          options.PersistHistory = false;
          options.WelcomeMessage = null;
          options.ShowTiming = false;
        })
        .Build();
      await app.RunAsync(["--interactive"]);

      terminal.ErrorOutput.ShouldContain("Unknown key binding profile: 'Json'");
      terminal.OutputContains("Profile: Default").ShouldBeFalse();
      terminal.OutputContains("Goodbye!").ShouldBeTrue();
    }
    finally
    {
      Environment.SetEnvironmentVariable(KeyBindingConfigLoader.EnvironmentVariableName, previous);
      File.Delete(configFile);
    }
  }

  public static async Task Should_print_usage_for_a_bad_flag_without_leaving_the_repl()
  {
    using TestTerminal terminal = new();
    terminal.QueueLine("key-bindings --profile");
    terminal.QueueLine("key-bindings --nope");
    terminal.QueueLine("exit");

    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .AddRepl(options =>
      {
        options.EnableColors = false;
        options.PersistHistory = false;
        options.WelcomeMessage = null;
        options.ShowTiming = false;
        options.KeyBindingProfile = new DefaultKeyBindingProfile();
      })
      .Build();
    await app.RunAsync(["--interactive"]);

    terminal.ErrorOutput.ShouldContain("Missing value for --profile.");
    terminal.ErrorOutput.ShouldContain("Unknown argument '--nope'.");
    terminal.ErrorOutput.ShouldContain("Usage: key-bindings [--profile <name>] [--key <chord>] [--function <name>] [--detailed]");
    terminal.OutputContains("Goodbye!").ShouldBeTrue();
  }

  private static void AssertCatalogMatchesProfile(IKeyBindingProfile profile)
  {
    using TestTerminal terminal = new();
    ReplConsoleReader reader = CreateReader(terminal, profile);
    Dictionary<(ConsoleKey Key, ConsoleModifiers Modifiers), Func<Task>> bindings = profile.GetBindings(reader);
    IReadOnlyList<KeyBindingRow> rows = KeyBindingCatalog.List(profile.Name);

    rows.Count.ShouldBe(bindings.Count, profile.Name);

    HashSet<(ConsoleKey Key, ConsoleModifiers Modifiers)> seen = [];
    foreach (KeyBindingRow row in rows)
    {
      row.Description.ShouldNotBeNullOrWhiteSpace($"{profile.Name} {row.Key}");
      (ConsoleKey key, ConsoleModifiers modifiers) = KeyBindingComboParser.Parse(row.Key);
      seen.Add((key, modifiers)).ShouldBeTrue($"{profile.Name} displays {row.Key} twice");
      bindings.ContainsKey((key, modifiers)).ShouldBeTrue($"{profile.Name} catalog chord {row.Key} is not in the profile");

      if (SkipsMethodParity(row.Function))
      {
        continue;
      }

      Func<Task> expected = KeyBindingActionRegistry.GetAction(row.Function, reader);
      bindings[(key, modifiers)].Method.ShouldBe
      (
        expected.Method,
        $"{profile.Name} {row.Key} should run {row.Function}"
      );
    }

    foreach ((ConsoleKey key, ConsoleModifiers modifiers) in bindings.Keys)
    {
      seen.Contains((key, modifiers)).ShouldBeTrue
      (
        $"{profile.Name} is missing {KeyBindingCatalog.FormatChord(key, modifiers)}"
      );
    }
  }

  private static bool SkipsMethodParity(string function)
  {
    return function is "TabComplete" or "TabCompleteReverse"
      || function.StartsWith("DigitArgument:", StringComparison.Ordinal);
  }

  private static void AssertRoundTrip(ConsoleKey key, ConsoleModifiers modifiers, string expected)
  {
    string display = KeyBindingCatalog.FormatChord(key, modifiers);
    display.ShouldBe(expected);
    (ConsoleKey parsedKey, ConsoleModifiers parsedModifiers) = KeyBindingComboParser.Parse(display);
    parsedKey.ShouldBe(key, expected);
    parsedModifiers.ShouldBe(modifiers, expected);
  }

  private static ReplConsoleReader CreateReader(TestTerminal terminal, IKeyBindingProfile profile)
  {
    ReplOptions options = new()
    {
      PersistHistory = false,
      EnableColors = false,
      KeyBindingProfile = profile
    };

    return new ReplConsoleReader
    (
      history: [],
      routeProvider: new EmptyRoutes(),
      replOptions: options,
      loggerFactory: null,
      terminal: terminal
    );
  }

}

} // namespace TimeWarp.Nuru.Tests.ReplTests.KeyBindingCatalogTests
