#!/usr/bin/env -S dotnet --
#:project $(SourceDirectory)timewarp-nuru/timewarp-nuru.csproj

#region Purpose
// Root --help lists parameters, options, and examples of a default route, which has no command
// page of its own. Named-command details stay on that command's help.
#endregion

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.Help
{

[TestTag("Help")]
public class RootDefaultRouteParametersTests
{
  [ModuleInitializer]
  internal static void Register() => RegisterTests<RootDefaultRouteParametersTests>();

  public static async Task Should_list_parameters_options_examples_and_usage_for_a_single_default_route()
  {
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .WithName("tool")
      .Map<RootHelpConvertCommand>()
      .Build();

    int exitCode = await app.RunAsync(["--help"]);
    string output = terminal.Output;

    exitCode.ShouldBe(0);
    output.ShouldContain("tool {input} [count] [--format,-f {format}] [--verbose,-v]");
    output.ShouldContain("tool [options]");
    output.ShouldNotContain("tool [command] [options]");
    output.ShouldContain("Convert a file");
    output.ShouldContain("Parameters:");
    output.ShouldContain("input");
    output.ShouldContain("Source file");
    output.ShouldContain("count");
    output.ShouldContain("Repeat count");
    output.ShouldContain("int?");
    output.ShouldContain("Yes");
    output.ShouldContain("No");
    output.ShouldContain("--format, -f <format>");
    output.ShouldContain("Output format");
    output.ShouldContain("--verbose, -v");
    output.ShouldContain("Verbose output");
    output.ShouldContain("Examples:");
    output.ShouldContain("in.json --format json");
    output.ShouldContain("Write json");
    output.ShouldNotContain("Commands:");
    int parametersAt = output.IndexOf("Parameters:", StringComparison.Ordinal);
    int optionsAt = output.IndexOf("Options:", StringComparison.Ordinal);
    int examplesAt = output.IndexOf("Examples:", StringComparison.Ordinal);
    parametersAt.ShouldBeGreaterThanOrEqualTo(0);
    optionsAt.ShouldBeGreaterThan(parametersAt);
    examplesAt.ShouldBeGreaterThan(optionsAt);
  }

  public static async Task Should_list_default_route_details_beside_other_commands()
  {
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .WithName("tool")
      .Map<RootHelpConvertCommand>()
      .Map("status --loud").WithHandler((bool loud) => loud ? "loud" : "quiet").WithDescription("Show status").Done()
      .Build();

    int exitCode = await app.RunAsync(["--help"]);
    string output = terminal.Output;

    exitCode.ShouldBe(0);
    output.ShouldContain("tool {input} [count] [--format,-f {format}] [--verbose,-v]");
    output.ShouldContain("tool [command] [options]");
    output.ShouldContain("--format, -f <format>");
    output.ShouldContain("Parameters:");
    int commandsAt = output.IndexOf("Commands:", StringComparison.Ordinal);
    commandsAt.ShouldBeGreaterThanOrEqualTo(0);
    string commandSection = output[commandsAt..];
    commandSection.ShouldContain("status");
    commandSection.ShouldContain("Show status");
    commandSection.ShouldNotContain("Convert a file");
    commandSection.ShouldNotContain("--loud");
    output.ShouldNotContain("--loud");
  }

  public static async Task Should_list_options_for_a_default_route_without_parameters()
  {
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .WithName("tool")
      .Map<RootHelpForceCommand>()
      .Build();

    int exitCode = await app.RunAsync(["--help"]);
    string output = terminal.Output;

    exitCode.ShouldBe(0);
    output.ShouldContain("tool [--force,-f]");
    output.ShouldContain("--force, -f");
    output.ShouldContain("Overwrite");
    output.ShouldNotContain("Parameters:");
    output.ShouldNotContain("Commands:");
  }

  public static async Task Should_list_a_catch_all_parameter_for_the_default_route()
  {
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .WithName("tool")
      .Map<RootHelpPackCommand>()
      .Build();

    int exitCode = await app.RunAsync(["--help"]);
    string output = terminal.Output;

    exitCode.ShouldBe(0);
    output.ShouldContain("tool {*files}");
    output.ShouldContain("*files");
    output.ShouldContain("Files to pack");
    output.ShouldContain("Parameters:");
    output.ShouldNotContain("Commands:");
  }

  public static async Task Should_keep_a_description_only_default_route_under_commands()
  {
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .WithName("tool")
      .Map("").WithHandler(() => "welcome").WithDescription("Welcome text").AsQuery().Done()
      .Map("status").WithHandler(() => "ok").WithDescription("Show status").Done()
      .Build();

    int exitCode = await app.RunAsync(["--help"]);
    string output = terminal.Output;

    exitCode.ShouldBe(0);
    output.ShouldContain("tool [command] [options]");
    output.ShouldContain("Welcome text");
    output.ShouldContain("Commands:");
    output.ShouldContain("status");
    output.ShouldContain("Show status");
    output.ShouldNotContain("Parameters:");
  }

  public static async Task Should_leave_named_command_parameters_off_root_help()
  {
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .WithName("tool")
      .Map("run {path} --loud").WithHandler((string path, bool loud) => loud ? path.ToUpperInvariant() : path).WithDescription("Run a path").Done()
      .Build();

    int exitCode = await app.RunAsync(["--help"]);
    string output = terminal.Output;

    exitCode.ShouldBe(0);
    output.ShouldContain("run");
    output.ShouldContain("Run a path");
    output.ShouldContain("Commands:");
    output.ShouldNotContain("Parameters:");
    output.ShouldNotContain("--loud");
  }
}

} // namespace TimeWarp.Nuru.Tests.Help

[NuruRoute("", Description = "Convert a file")]
[NuruRouteExample("in.json --format json", Description = "Write json")]
public sealed class RootHelpConvertCommand : ICommand<Unit>
{
  [Parameter(Order = 0, Description = "Source file")]
  public string Input { get; set; } = string.Empty;

  [Parameter(Order = 1, Description = "Repeat count")]
  public int? Count { get; set; }

  [Option("format", "f", Description = "Output format")]
  public string Format { get; set; } = string.Empty;

  [Option("verbose", "v", Description = "Verbose output")]
  public bool Verbose { get; set; }

  internal sealed class Handler : IRequestHandler<RootHelpConvertCommand, Unit>
  {
    public ValueTask<Unit> Handle(RootHelpConvertCommand command, CancellationToken cancellationToken)
    {
      _ = command.Input;
      _ = cancellationToken.CanBeCanceled;
      return Unit.ValueTask;
    }
  }
}

[NuruRoute("", Description = "Force the write")]
public sealed class RootHelpForceCommand : ICommand<Unit>
{
  [Option("force", "f", Description = "Overwrite")]
  public bool Force { get; set; }

  internal sealed class Handler : IRequestHandler<RootHelpForceCommand, Unit>
  {
    public ValueTask<Unit> Handle(RootHelpForceCommand command, CancellationToken cancellationToken)
    {
      _ = command.Force;
      _ = cancellationToken.CanBeCanceled;
      return Unit.ValueTask;
    }
  }
}

[NuruRoute("", Description = "Pack files")]
internal sealed class RootHelpPackCommand : ICommand<Unit>
{
  [Parameter(IsCatchAll = true, Description = "Files to pack")]
  public string[] Files { get; set; } = [];

  internal sealed class Handler : IRequestHandler<RootHelpPackCommand, Unit>
  {
    public ValueTask<Unit> Handle(RootHelpPackCommand command, CancellationToken cancellationToken)
    {
      _ = command.Files.Length;
      _ = cancellationToken.CanBeCanceled;
      return Unit.ValueTask;
    }
  }
}
