#!/usr/bin/env -S dotnet --

#region Purpose
// Pins binding strictness: extra positionals, missing required option values, bool defaults,
// repeated values, parameter Order and Name.
#endregion

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.Routing
{

[TestTag("Routing")]
public class BindingStrictnessTests
{
  [ModuleInitializer]
  internal static void Register() => RegisterTests<BindingStrictnessTests>();

  public static async Task Should_reject_an_extra_token_after_an_optional_parameter()
  {
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("deploy {env?}").WithHandler((string? env) => $"env:{env ?? "NULL"}").AsCommand().Done()
      .Build();

    int exitCode = await app.RunAsync(["deploy", "prod", "extra"]);

    exitCode.ShouldBe(1);
    terminal.ErrorContains("Unknown command").ShouldBeTrue();
    terminal.OutputContains("env:").ShouldBeFalse();
  }

  public static async Task Should_bind_one_optional_parameter()
  {
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("deploy {env?}").WithHandler((string? env) => $"env:{env ?? "NULL"}").AsCommand().Done()
      .Build();

    int exitCode = await app.RunAsync(["deploy", "prod"]);

    exitCode.ShouldBe(0);
    terminal.OutputContains("env:prod").ShouldBeTrue();
  }

  public static async Task Should_let_a_catch_all_take_the_remaining_tokens()
  {
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("exec {cmd} {*args}").WithHandler((string cmd, string[] args) => $"cmd:{cmd}|args:{string.Join(",", args)}")
      .AsCommand().Done()
      .Build();

    int exitCode = await app.RunAsync(["exec", "tool", "a", "b"]);

    exitCode.ShouldBe(0);
    terminal.OutputContains("cmd:tool|args:a,b").ShouldBeTrue();
  }

  public static async Task Should_keep_a_string_default_when_the_option_is_absent()
  {
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map<BindingStrictnessWriteCommand>()
      .Build();

    int exitCode = await app.RunAsync(["write"]);

    exitCode.ShouldBe(0);
    terminal.OutputContains("output:text").ShouldBeTrue();
  }

  public static async Task Should_reject_a_present_option_whose_required_value_is_missing()
  {
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map<BindingStrictnessWriteCommand>()
      .Build();

    int exitCode = await app.RunAsync(["write", "-o"]);

    exitCode.ShouldBe(1);
    terminal.ErrorContains("Unknown command").ShouldBeTrue();
    terminal.OutputContains("output:").ShouldBeFalse();
  }

  public static async Task Should_treat_an_empty_equals_value_as_present()
  {
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map<BindingStrictnessWriteCommand>()
      .Build();

    int exitCode = await app.RunAsync(["write", "-o="]);

    exitCode.ShouldBe(0);
    terminal.OutputContains("output:").ShouldBeTrue();
    terminal.OutputContains("output:text").ShouldBeFalse();
  }

  public static async Task Should_keep_a_true_bool_initializer_when_the_flag_is_absent()
  {
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map<BindingStrictnessCreateCommand>()
      .Build();

    int exitCode = await app.RunAsync(["make"]);

    exitCode.ShouldBe(0);
    terminal.OutputContains("create:True|verbose:False").ShouldBeTrue();
  }

  public static async Task Should_bind_false_and_true_on_a_default_true_bool()
  {
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map<BindingStrictnessCreateCommand>()
      .Build();

    int exitCode = await app.RunAsync(["make", "--create", "false"]);

    exitCode.ShouldBe(0);
    terminal.OutputContains("create:False|verbose:False").ShouldBeTrue();

    using TestTerminal trueTerminal = new();
    NuruApp trueApp = NuruApp.CreateBuilder()
      .UseTerminal(trueTerminal)
      .Map<BindingStrictnessCreateCommand>()
      .Build();

    int trueExitCode = await trueApp.RunAsync(["make", "--create", "true"]);

    trueExitCode.ShouldBe(0);
    trueTerminal.OutputContains("create:True|verbose:False").ShouldBeTrue();
  }

  public static async Task Should_reject_a_bare_default_true_bool()
  {
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map<BindingStrictnessCreateCommand>()
      .Build();

    int exitCode = await app.RunAsync(["make", "--create"]);

    exitCode.ShouldBe(1);
    terminal.ErrorContains("Unknown command").ShouldBeTrue();
    terminal.OutputContains("create:").ShouldBeFalse();
  }

  public static async Task Should_keep_a_boolean_spelling_as_an_optional_positional()
  {
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("seek {pos:int?} --verbose")
      .WithHandler((int? pos, bool verbose) => $"pos:{pos}|verbose:{verbose}")
      .AsCommand().Done()
      .Build();

    int exitCode = await app.RunAsync(["seek", "--verbose", "0"]);

    exitCode.ShouldBe(0);
    terminal.OutputContains("pos:0|verbose:True").ShouldBeTrue();
  }

  public static async Task Should_keep_a_boolean_spelling_as_a_required_positional()
  {
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("repeat {count:int} --quiet")
      .WithHandler((int count, bool quiet) => $"count:{count}|quiet:{quiet}")
      .AsCommand().Done()
      .Build();

    int exitCode = await app.RunAsync(["repeat", "--quiet", "1"]);

    exitCode.ShouldBe(0);
    terminal.OutputContains("count:1|quiet:True").ShouldBeTrue();
  }

  public static async Task Should_bind_true_for_a_flag_when_the_next_token_is_not_a_boolean()
  {
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("note {text?} --verbose").WithHandler((string? text, bool verbose) => $"text:{text ?? "NULL"}|verbose:{verbose}")
      .AsCommand().Done()
      .Build();

    int exitCode = await app.RunAsync(["note", "--verbose", "hello"]);

    exitCode.ShouldBe(0);
    terminal.OutputContains("text:hello|verbose:True").ShouldBeTrue();
  }

  public static async Task Should_take_every_following_value_of_a_repeated_option()
  {
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("run --env {e}*").WithHandler((string[] e) => $"e:[{string.Join(",", e)}]")
      .AsCommand().Done()
      .Build();

    int exitCode = await app.RunAsync(["run", "--env", "A", "B"]);

    exitCode.ShouldBe(0);
    terminal.OutputContains("e:[A,B]").ShouldBeTrue();
  }

  public static async Task Should_keep_repeated_values_from_separate_flags()
  {
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("run --env {e}*").WithHandler((string[] e) => $"e:[{string.Join(",", e)}]")
      .AsCommand().Done()
      .Build();

    int exitCode = await app.RunAsync(["run", "--env", "A", "--env", "B"]);

    exitCode.ShouldBe(0);
    terminal.OutputContains("e:[A,B]").ShouldBeTrue();
  }

  public static async Task Should_leave_the_token_after_an_equals_repeated_value()
  {
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("run --env {e}* {name?}").WithHandler((string[] e, string? name) => $"e:[{string.Join(",", e)}]|name:{name ?? "NULL"}")
      .AsCommand().Done()
      .Build();

    int exitCode = await app.RunAsync(["run", "--env=A", "B"]);

    exitCode.ShouldBe(0);
    terminal.OutputContains("e:[A]|name:B").ShouldBeTrue();
  }

  public static async Task Should_stop_a_repeated_option_at_the_next_option()
  {
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("run --env {e}* --verbose").WithHandler((string[] e, bool verbose) => $"e:[{string.Join(",", e)}]|verbose:{verbose}")
      .AsCommand().Done()
      .Build();

    int exitCode = await app.RunAsync(["run", "--env", "A", "B", "--verbose"]);

    exitCode.ShouldBe(0);
    terminal.OutputContains("e:[A,B]|verbose:True").ShouldBeTrue();
  }

  public static async Task Should_reject_a_bare_repeated_option()
  {
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("run --env {e}*").WithHandler((string[] e) => $"e:[{string.Join(",", e)}]")
      .AsCommand().Done()
      .Build();

    int exitCode = await app.RunAsync(["run", "--env"]);

    exitCode.ShouldBe(1);
    terminal.OutputContains("e:").ShouldBeFalse();
  }

  public static async Task Should_bind_parameters_by_order_not_declaration_order()
  {
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map<BindingStrictnessCopyCommand>()
      .Build();

    int exitCode = await app.RunAsync(["copy", "from", "to"]);

    exitCode.ShouldBe(0);
    terminal.OutputContains("source:from|destination:to").ShouldBeTrue();
  }

  public static async Task Should_use_the_parameter_name_override()
  {
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map<BindingStrictnessSaveCommand>()
      .Build();

    int exitCode = await app.RunAsync(["save", "--help"]);

    exitCode.ShouldBe(0);
    terminal.OutputContains("src").ShouldBeTrue();
    terminal.OutputContains("sourcepath").ShouldBeFalse();
  }

  public static async Task Should_repeat_a_collection_option_marked_is_repeated()
  {
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map<BindingStrictnessEnvCommand>()
      .Build();

    int exitCode = await app.RunAsync(["env", "--env", "A", "B"]);

    exitCode.ShouldBe(0);
    terminal.OutputContains("env:[A,B]").ShouldBeTrue();
  }
}

[NuruRoute("write")]
public sealed class BindingStrictnessWriteCommand : ICommand<Unit>
{
  [Option("output", "o")]
  public string Output { get; set; } = "text";

  internal sealed class Handler(ITerminal terminal) : IRequestHandler<BindingStrictnessWriteCommand, Unit>
  {
    public async ValueTask<Unit> Handle(BindingStrictnessWriteCommand command, CancellationToken cancellationToken)
    {
      _ = cancellationToken.CanBeCanceled;
      await terminal.WriteLineAsync($"output:{command.Output}").ConfigureAwait(false);
      return default;
    }
  }
}

[NuruRoute("make")]
public sealed class BindingStrictnessCreateCommand : ICommand<Unit>
{
  [Option("create")]
  public bool Create { get; set; } = true;

  [Option("verbose")]
  public bool Verbose { get; set; }

  internal sealed class Handler(ITerminal terminal) : IRequestHandler<BindingStrictnessCreateCommand, Unit>
  {
    public async ValueTask<Unit> Handle(BindingStrictnessCreateCommand command, CancellationToken cancellationToken)
    {
      _ = cancellationToken.CanBeCanceled;
      await terminal.WriteLineAsync($"create:{command.Create}|verbose:{command.Verbose}").ConfigureAwait(false);
      return default;
    }
  }
}

[NuruRoute("copy")]
public sealed class BindingStrictnessCopyCommand : ICommand<Unit>
{
  [Parameter(Order = 1)]
  public string Destination { get; set; } = "";

  [Parameter(Order = 0)]
  public string Source { get; set; } = "";

  internal sealed class Handler(ITerminal terminal) : IRequestHandler<BindingStrictnessCopyCommand, Unit>
  {
    public async ValueTask<Unit> Handle(BindingStrictnessCopyCommand command, CancellationToken cancellationToken)
    {
      _ = cancellationToken.CanBeCanceled;
      await terminal.WriteLineAsync($"source:{command.Source}|destination:{command.Destination}").ConfigureAwait(false);
      return default;
    }
  }
}

[NuruRoute("save", Description = "Save a file")]
public sealed class BindingStrictnessSaveCommand : ICommand<Unit>
{
  [Parameter(Name = "src", Description = "Source file")]
  public string SourcePath { get; set; } = "";

  internal sealed class Handler : IRequestHandler<BindingStrictnessSaveCommand, Unit>
  {
    public ValueTask<Unit> Handle(BindingStrictnessSaveCommand command, CancellationToken cancellationToken)
    {
      _ = command.SourcePath;
      _ = cancellationToken.CanBeCanceled;
      return Unit.ValueTask;
    }
  }
}

[NuruRoute("env")]
public sealed class BindingStrictnessEnvCommand : ICommand<Unit>
{
  [Option("env", IsRepeated = true)]
  public IReadOnlyList<string> Env { get; set; } = [];

  internal sealed class Handler(ITerminal terminal) : IRequestHandler<BindingStrictnessEnvCommand, Unit>
  {
    public async ValueTask<Unit> Handle(BindingStrictnessEnvCommand command, CancellationToken cancellationToken)
    {
      _ = cancellationToken.CanBeCanceled;
      await terminal.WriteLineAsync($"env:[{string.Join(",", command.Env)}]").ConfigureAwait(false);
      return default;
    }
  }
}

} // namespace TimeWarp.Nuru.Tests.Routing
