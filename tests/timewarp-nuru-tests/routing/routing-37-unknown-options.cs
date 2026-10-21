#!/usr/bin/env -S dotnet --
#:project $(SourceDirectory)timewarp-nuru/timewarp-nuru.csproj

// Unknown options fail closed (task 515). An option-shaped token that no route declares must not
// reach a handler: the call exits 1 and names the option, with a suggestion when one is close.

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.Routing
{

[TestTag("Routing")]
public class UnknownOptionTests
{
  [ModuleInitializer]
  internal static void Register() => RegisterTests<UnknownOptionTests>();

  // The ganda 439 case: a mutating command with options gets an option it does not declare
  public static async Task Should_reject_unknown_option_on_route_with_options()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("kanban create {title} --folder? {folder?}")
        .WithHandler((string title, string? folder) => $"created:{title}|{folder}")
        .AsCommand().Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["kanban", "create", "Title", "--repo", "timewarp-architecture"]);

    // Assert
    exitCode.ShouldBe(1);
    terminal.ErrorContains("Unknown option '--repo'").ShouldBeTrue();
    terminal.OutputContains("created:").ShouldBeFalse();
  }

  public static async Task Should_suggest_closest_declared_option()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("build --verbose --config? {mode?}")
        .WithHandler((bool verbose, string? mode) => $"build:{verbose}|{mode}")
        .AsCommand().Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["build", "--verbos"]);

    // Assert
    exitCode.ShouldBe(1);
    terminal.ErrorContains("Unknown option '--verbos'. Did you mean '--verbose'?").ShouldBeTrue();
    terminal.OutputContains("build:").ShouldBeFalse();
  }

  public static async Task Should_not_suggest_when_no_declared_option_is_close()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("build --verbose").WithHandler((bool verbose) => $"build:{verbose}")
      .AsCommand().Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["build", "--target-framework"]);

    // Assert
    exitCode.ShouldBe(1);
    terminal.ErrorContains("Unknown option '--target-framework'").ShouldBeTrue();
    terminal.ErrorContains("Did you mean").ShouldBeFalse();
  }

  // "--key=value" is a configuration override and is peeled before routing, so the message
  // helper is checked directly for a token that carries "=value"
  public static async Task Should_name_option_without_its_equals_value()
  {
    NuruUnknownOption.FormatMessage("--confg=Debug", ["--config", "-c"])
      .ShouldBe("Unknown option '--confg'. Did you mean '--config'?");
    NuruUnknownOption.FormatMessage("-k=Debug", ["--config", "-c"])
      .ShouldBe("Unknown option '-k'. Use --help for usage.");
    await Task.CompletedTask;
  }

  public static async Task Should_treat_numbers_dash_and_separator_as_non_options()
  {
    NuruUnknownOption.IsOptionLike("-5").ShouldBeFalse();
    NuruUnknownOption.IsOptionLike("-1.5e10").ShouldBeFalse();
    NuruUnknownOption.IsOptionLike("-").ShouldBeFalse();
    NuruUnknownOption.IsOptionLike("--").ShouldBeFalse();
    NuruUnknownOption.IsOptionLike("value").ShouldBeFalse();
    NuruUnknownOption.IsOptionLike("-x").ShouldBeTrue();
    NuruUnknownOption.IsOptionLike("--repo").ShouldBeTrue();
    await Task.CompletedTask;
  }

  public static async Task Should_reject_unknown_short_option()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("deploy {env} --force,-f").WithHandler((string env, bool force) => $"deploy:{env}|{force}")
      .AsCommand().Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["deploy", "prod", "-x"]);

    // Assert
    exitCode.ShouldBe(1);
    terminal.ErrorContains("Unknown option '-x'. Use --help for usage.").ShouldBeTrue();
    terminal.OutputContains("deploy:").ShouldBeFalse();
  }

  public static async Task Should_still_accept_declared_options_and_negative_numbers()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("shift {by:int} {scale:double} --verbose")
        .WithHandler((int by, double scale, bool verbose) => $"shift:{by}|{scale}|{verbose}")
        .AsCommand().Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["shift", "-5", "-1.5e10", "--verbose"]);

    // Assert
    exitCode.ShouldBe(0);
    terminal.OutputContains("shift:-5|-15000000000|True").ShouldBeTrue();
  }

  public static async Task Should_keep_option_shaped_tokens_after_separator_as_positionals()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("run {*args} --verbose")
        .WithHandler((string[] args, bool verbose) => $"run:{string.Join(",", args)}|{verbose}")
        .AsCommand().Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["run", "--verbose", "a", "--", "--not-mine", "-x"]);

    // Assert
    exitCode.ShouldBe(0);
    terminal.OutputContains("run:a,--not-mine,-x|True").ShouldBeTrue();
  }

  public static async Task Should_accept_real_positionals_in_catch_all()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("exec {*args}").WithHandler((string[] args) => $"exec:{string.Join(",", args)}")
      .AsCommand().Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["exec", "ls", "-5", "readme.md"]);

    // Assert
    exitCode.ShouldBe(0);
    terminal.OutputContains("exec:ls,-5,readme.md").ShouldBeTrue();
  }

  public static async Task Should_reject_unknown_option_before_separator_in_catch_all()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("exec {*args}").WithHandler((string[] args) => $"exec:{string.Join(",", args)}")
      .AsCommand().Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["exec", "ls", "-la"]);

    // Assert
    exitCode.ShouldBe(1);
    terminal.ErrorContains("Unknown option '-la'").ShouldBeTrue();
    terminal.OutputContains("exec:").ShouldBeFalse();
  }

  public static async Task Should_reject_unknown_option_on_default_catch_all_route()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("{*args}").WithHandler((string[] args) => $"default:{string.Join(",", args)}")
      .AsCommand().Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["anything", "--unknown"]);

    // Assert
    exitCode.ShouldBe(1);
    terminal.ErrorContains("Unknown option '--unknown'").ShouldBeTrue();
    terminal.OutputContains("default:").ShouldBeFalse();
  }

  public static async Task Should_keep_unknown_command_for_extra_non_option_token()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("greet {name}").WithHandler((string name) => $"hello:{name}")
      .AsCommand().Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["greet", "bob", "extra"]);

    // Assert
    exitCode.ShouldBe(1);
    terminal.ErrorContains("Unknown command").ShouldBeTrue();
    terminal.ErrorContains("Unknown option").ShouldBeFalse();
  }

  public static async Task Should_reject_unknown_option_on_route_without_options()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("greet {name}").WithHandler((string name) => $"hello:{name}")
      .AsCommand().Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["greet", "bob", "--loud"]);

    // Assert
    exitCode.ShouldBe(1);
    terminal.ErrorContains("Unknown option '--loud'").ShouldBeTrue();
    terminal.OutputContains("hello:").ShouldBeFalse();
  }

  public static async Task Should_reject_extra_positional_on_route_with_options()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("build --verbose").WithHandler((bool verbose) => $"build:{verbose}")
      .AsCommand().Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["build", "stray"]);

    // Assert
    exitCode.ShouldBe(1);
    terminal.ErrorContains("Unknown command").ShouldBeTrue();
    terminal.OutputContains("build:").ShouldBeFalse();
  }

  public static async Task Should_let_a_later_route_match_when_one_route_saw_unknown_option()
  {
    // Arrange - the less specific route declares the option the more specific one lacks
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("deploy prod").WithHandler(() => "deploy:prod")
      .AsCommand().Done()
      .Map("deploy {env} --dry-run").WithHandler((string env, bool dryRun) => $"deploy:{env}|{dryRun}")
      .AsCommand().Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["deploy", "prod", "--dry-run"]);

    // Assert
    exitCode.ShouldBe(0);
    terminal.OutputContains("deploy:prod|True").ShouldBeTrue();
  }

  public static async Task Should_reject_unknown_option_in_repl()
  {
    // Arrange
    using TestTerminal terminal = new();
    terminal.QueueLine("build --verbos");
    terminal.QueueLine("exit");

    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("build --verbose").WithHandler((bool verbose) => $"build:{verbose}")
      .AsCommand().Done()
      .AddRepl(options => options.ContinueOnError = true)
      .Build();

    // Act
    await app.RunAsync(["--interactive"]);

    // Assert
    terminal.ErrorContains("Unknown option '--verbos'. Did you mean '--verbose'?").ShouldBeTrue();
    terminal.OutputContains("build:").ShouldBeFalse();
  }
}

} // namespace TimeWarp.Nuru.Tests.Routing
