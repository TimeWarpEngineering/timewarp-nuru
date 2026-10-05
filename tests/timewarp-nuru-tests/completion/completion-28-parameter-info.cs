#!/usr/bin/env -S dotnet --

// Shell Completion Parameter Detection Tests
// Tests that TryGetParameterInfo finds the parameter at the cursor (not only the first),
// matches command prefixes on a token boundary, and resolves user and keyword types
// for CompletionSourceRegistry.RegisterForType.

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.Completion.ParameterInfo
{

[TestTag("Completion")]
public class CompletionParameterInfoTests
{
  [ModuleInitializer]
  internal static void Register() => RegisterTests<CompletionParameterInfoTests>();

  public static async Task Complete_should_use_source_for_second_parameter()
  {
    // Arrange
    using TestTerminal terminal = new();

    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("copy {source} {dest}").WithHandler((string source, string dest) => $"{source}->{dest}").AsCommand().Done()
      .EnableCompletion(configure: registry =>
      {
        registry.RegisterForParameter("source", new FixedSource("from-a", "from-b"));
        registry.RegisterForParameter("dest", new FixedSource("to-a", "to-b"));
      })
      .Build();

    // Act - cursor on the second parameter: "app copy from-a <TAB>"
    int exitCode = await app.RunAsync(["__complete", "3", "app", "copy", "from-a"]);

    // Assert
    exitCode.ShouldBe(0);
    terminal.OutputContains("to-a").ShouldBeTrue();
    terminal.OutputContains("to-b").ShouldBeTrue();
    terminal.OutputContains("from-b").ShouldBeFalse();
  }

  public static async Task Complete_should_use_source_for_first_parameter_of_two()
  {
    // Arrange
    using TestTerminal terminal = new();

    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("move {source} {dest}").WithHandler((string source, string dest) => $"{source}->{dest}").AsCommand().Done()
      .EnableCompletion(configure: registry =>
      {
        registry.RegisterForParameter("source", new FixedSource("from-a", "from-b"));
        registry.RegisterForParameter("dest", new FixedSource("to-a", "to-b"));
      })
      .Build();

    // Act - cursor on the first parameter: "app move <TAB>"
    int exitCode = await app.RunAsync(["__complete", "2", "app", "move"]);

    // Assert
    exitCode.ShouldBe(0);
    terminal.OutputContains("from-a").ShouldBeTrue();
    terminal.OutputContains("to-a").ShouldBeFalse();
  }

  public static async Task Complete_should_not_treat_git_as_prefix_of_github()
  {
    // Arrange
    using TestTerminal terminal = new();

    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("git {repo}").WithHandler((string repo) => repo).AsCommand().Done()
      .Map("github {user}").WithHandler((string user) => user).AsCommand().Done()
      .EnableCompletion(configure: registry =>
      {
        registry.RegisterForParameter("repo", new FixedSource("repo-one", "repo-two"));
      })
      .Build();

    // Act - "app github <TAB>" must not resolve to git's {repo}
    int exitCode = await app.RunAsync(["__complete", "2", "app", "github"]);

    // Assert
    exitCode.ShouldBe(0);
    terminal.OutputContains("repo-one").ShouldBeFalse();
    terminal.OutputContains("repo-two").ShouldBeFalse();
  }

  public static async Task Complete_should_resolve_registered_user_type()
  {
    // Arrange
    using TestTerminal terminal = new();

    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("ship {target:ShipTarget}").WithHandler((ShipTarget target) => target.ToString()).AsCommand().Done()
      .EnableCompletion(configure: registry =>
      {
        registry.RegisterForType(typeof(ShipTarget), new FixedSource("custom-alpha", "custom-beta"));
      })
      .Build();

    // Act
    int exitCode = await app.RunAsync(["__complete", "2", "app", "ship"]);

    // Assert
    exitCode.ShouldBe(0);
    terminal.OutputContains("custom-alpha").ShouldBeTrue();
    terminal.OutputContains("custom-beta").ShouldBeTrue();
  }

  public static async Task Complete_should_resolve_registered_keyword_type()
  {
    // Arrange
    using TestTerminal terminal = new();

    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("pause {ms:int}").WithHandler((int ms) => ms).AsCommand().Done()
      .EnableCompletion(configure: registry =>
      {
        registry.RegisterForType(typeof(int), new FixedSource("100", "250"));
      })
      .Build();

    // Act
    int exitCode = await app.RunAsync(["__complete", "2", "app", "pause"]);

    // Assert
    exitCode.ShouldBe(0);
    terminal.OutputContains("100").ShouldBeTrue();
    terminal.OutputContains("250").ShouldBeTrue();
  }

  public static async Task Complete_should_resolve_registered_type_for_catch_all()
  {
    // Arrange
    using TestTerminal terminal = new();

    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("pack {*files}").WithHandler((string[] files) => files.Length).AsCommand().Done()
      .EnableCompletion(configure: registry =>
      {
        registry.RegisterForType(typeof(string[]), new FixedSource("a.txt", "b.txt"));
      })
      .Build();

    // Act - cursor on the catch-all slot: "app pack <TAB>"
    int exitCode = await app.RunAsync(["__complete", "2", "app", "pack"]);

    // Assert
    exitCode.ShouldBe(0);
    terminal.OutputContains("a.txt").ShouldBeTrue();
    terminal.OutputContains("b.txt").ShouldBeTrue();
  }

  public static async Task Complete_should_resolve_catch_all_type_past_the_first_word()
  {
    // Arrange
    using TestTerminal terminal = new();

    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("pack {*files}").WithHandler((string[] files) => files.Length).AsCommand().Done()
      .EnableCompletion(configure: registry =>
      {
        registry.RegisterForType(typeof(string[]), new FixedSource("a.txt", "b.txt"));
      })
      .Build();

    // Act - cursor past the first catch-all word: "app pack a.txt <TAB>"
    int exitCode = await app.RunAsync(["__complete", "3", "app", "pack", "a.txt"]);

    // Assert
    exitCode.ShouldBe(0);
    terminal.OutputContains("a.txt").ShouldBeTrue();
    terminal.OutputContains("b.txt").ShouldBeTrue();
  }
}

// ==========================================================================
// Test Helpers
// ==========================================================================

/// <summary>
/// A user-defined parameter type for RegisterForType resolution.
/// </summary>
public enum ShipTarget
{
  Alpha,
  Beta
}

/// <summary>
/// Completion source that returns a fixed list of values.
/// </summary>
sealed class FixedSource(params string[] values) : ICompletionSource
{
  public IEnumerable<CompletionCandidate> GetCompletions(CompletionContext context) =>
    values.Select(v => new CompletionCandidate(Value: v, Description: null, Type: CompletionType.Parameter));
}

} // namespace TimeWarp.Nuru.Tests.Completion.ParameterInfo
