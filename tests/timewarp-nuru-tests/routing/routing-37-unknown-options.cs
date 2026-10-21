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

  // Review M1: a missing required value option skips the route, but only after the unknown
  // option is recorded, so the typo still gets a suggestion
  public static async Task Should_suggest_required_value_option_for_close_typo()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("kanban create {title} --folder {folder}")
        .WithHandler((string title, string folder) => $"created:{title}|{folder}")
        .AsCommand().Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["kanban", "create", "Title", "--foldr", "x"]);

    // Assert
    exitCode.ShouldBe(1);
    terminal.ErrorContains("Unknown option '--foldr'. Did you mean '--folder'?").ShouldBeTrue();
    terminal.OutputContains("created:").ShouldBeFalse();
  }

  // Review M1: an unbound discriminator flag (no bool parameter) must be present to match
  public static async Task Should_suggest_discriminator_flag_for_close_typo()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("commit --amend").WithHandler(() => "commit:amend")
      .AsCommand().Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["commit", "--ammend"]);

    // Assert
    exitCode.ShouldBe(1);
    terminal.ErrorContains("Unknown option '--ammend'. Did you mean '--amend'?").ShouldBeTrue();
    terminal.OutputContains("commit:").ShouldBeFalse();
  }

  public static async Task Should_reject_unknown_option_beside_present_required_option()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("kanban create {title} --folder {folder}")
        .WithHandler((string title, string folder) => $"created:{title}|{folder}")
        .AsCommand().Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["kanban", "create", "Title", "--folder", "f", "--repo", "x"]);

    // Assert
    exitCode.ShouldBe(1);
    terminal.ErrorContains("Unknown option '--repo'").ShouldBeTrue();
    terminal.OutputContains("created:").ShouldBeFalse();
  }

  public static async Task Should_still_run_route_with_required_option_present()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("kanban create {title} --folder {folder}")
        .WithHandler((string title, string folder) => $"created:{title}|{folder}")
        .AsCommand().Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["kanban", "create", "Title", "--folder", "f"]);

    // Assert
    exitCode.ShouldBe(0);
    terminal.OutputContains("created:Title|f").ShouldBeTrue();
  }

  // Review M2: a route with no leading literal records the unknown option once the rest of the
  // route matches, before a missing required flag skips it
  public static async Task Should_suggest_discriminator_flag_on_route_without_literal()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("--check-updates").WithHandler(() => "check-updates")
      .AsCommand().Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["--check-update"]);

    // Assert
    exitCode.ShouldBe(1);
    terminal.ErrorContains("Unknown option '--check-update'. Did you mean '--check-updates'?").ShouldBeTrue();
    terminal.OutputContains("check-updates").ShouldBeFalse();
  }

  // Review M2: the token after the typo is its value, so it does not count as an extra positional
  public static async Task Should_suggest_required_value_option_on_route_without_literal()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("{file} --output {output}")
        .WithHandler((string file, string output) => $"readme:{file}|{output}")
        .AsCommand().Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["readme", "--outpt", "out"]);

    // Assert
    exitCode.ShouldBe(1);
    terminal.ErrorContains("Unknown option '--outpt'. Did you mean '--output'?").ShouldBeTrue();
    terminal.OutputContains("readme:").ShouldBeFalse();
  }

  public static async Task Should_suggest_optional_value_option_on_route_without_literal()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("{file} --output? {output?}")
        .WithHandler((string file, string? output) => $"readme:{file}|{output}")
        .AsCommand().Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["readme", "--outpt", "out"]);

    // Assert
    exitCode.ShouldBe(1);
    terminal.ErrorContains("Unknown option '--outpt'. Did you mean '--output'?").ShouldBeTrue();
    terminal.OutputContains("readme:").ShouldBeFalse();
  }

  // A missing value still skips the route; with no unknown option it stays Unknown command
  public static async Task Should_keep_unknown_command_for_missing_value_on_route_without_literal()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("{file} --count {count:int}")
        .WithHandler((string file, int count) => $"readme:{file}|{count}")
        .AsCommand().Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["readme", "--count"]);

    // Assert
    exitCode.ShouldBe(1);
    terminal.ErrorContains("Unknown command").ShouldBeTrue();
    terminal.OutputContains("readme:").ShouldBeFalse();
  }

  // Review M3: a flag typo takes no value, so a stray word after it is an extra positional and the
  // route does not fit
  public static async Task Should_keep_unknown_command_for_flag_typo_with_extra_word()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("--check-updates").WithHandler(() => "check-updates")
      .AsCommand().Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["--check-update", "extra"]);

    // Assert
    exitCode.ShouldBe(1);
    terminal.ErrorContains("Unknown command").ShouldBeTrue();
    terminal.ErrorContains("Did you mean").ShouldBeFalse();
    terminal.OutputContains("check-updates").ShouldBeFalse();
  }

  public static async Task Should_keep_unknown_command_for_flag_typo_with_extra_word_after_parameter()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("{file} --check-updates")
        .WithHandler((string file) => $"readme:{file}")
        .AsCommand().Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["readme", "--check-update", "extra"]);

    // Assert
    exitCode.ShouldBe(1);
    terminal.ErrorContains("Unknown command").ShouldBeTrue();
    terminal.ErrorContains("Did you mean").ShouldBeFalse();
    terminal.OutputContains("readme:").ShouldBeFalse();
  }

  // The route has a value option too, but the typo is closest to the flag
  public static async Task Should_keep_unknown_command_for_flag_typo_beside_value_option()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("{file} --output {output} --force")
        .WithHandler((string file, string output, bool force) => $"readme:{file}|{output}|{force}")
        .AsCommand().Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["readme", "--forc", "extra"]);

    // Assert
    exitCode.ShouldBe(1);
    terminal.ErrorContains("Unknown command").ShouldBeTrue();
    terminal.ErrorContains("Did you mean").ShouldBeFalse();
    terminal.OutputContains("readme:").ShouldBeFalse();
  }

  // The value-option typo on the same route still owns its value and suggests
  public static async Task Should_suggest_value_option_typo_beside_flag()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("{file} --output {output} --force")
        .WithHandler((string file, string output, bool force) => $"readme:{file}|{output}|{force}")
        .AsCommand().Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["readme", "--outpt", "out"]);

    // Assert
    exitCode.ShouldBe(1);
    terminal.ErrorContains("Unknown option '--outpt'. Did you mean '--output'?").ShouldBeTrue();
    terminal.OutputContains("readme:").ShouldBeFalse();
  }

  // An alias block used to jump to the primary route's skip label, which is before the alias,
  // so a failed option guard on an alias looped. The alias now skips to its own label.
  public static async Task Should_reject_unknown_option_on_alias_with_failed_option_guard()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map<T515MakeCommand>()
      .Build();

    // Act - --folder has no value, so its guard fails; --bogus is unknown
    int exitCode = await app.RunAsync(["t515-mk", "--bogus", "--folder"]);

    // Assert
    exitCode.ShouldBe(1);
    terminal.ErrorContains("Unknown option '--bogus'").ShouldBeTrue();
    terminal.OutputContains("t515-make:").ShouldBeFalse();
  }

  public static async Task Should_run_alias_with_option_value()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map<T515MakeCommand>()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["t515-mk", "--folder", "f"]);

    // Assert
    exitCode.ShouldBe(0);
    terminal.OutputContains("t515-make:f").ShouldBeTrue();
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

[NuruRoute("t515-make", Description = "Alias option-guard fixture (515)")]
[NuruRouteAlias("t515-mk")]
public sealed class T515MakeCommand : ICommand<Unit>
{
  [Option("folder", Description = "Target folder")]
  public string Folder { get; set; } = null!;

  public sealed class Handler(ITerminal terminal) : ICommandHandler<T515MakeCommand, Unit>
  {
    public async Task<Unit> Handle(T515MakeCommand command, CancellationToken cancellationToken)
    {
      ArgumentNullException.ThrowIfNull(command);
      await terminal.WriteLineAsync($"t515-make:{command.Folder}").ConfigureAwait(false);
      return default;
    }
  }
}

} // namespace TimeWarp.Nuru.Tests.Routing
