#!/usr/bin/env -S dotnet --

#region Purpose
// Pins a repeated option beside a later positional: greedy runs, give-back to a required
// positional, one value before a catch-all, and an endpoint collection that leaves the path.
#endregion

#region Design
// These cases sit in their own runfile so routing-45 stays under the 500-line cap.
// The builder chains wrap across lines. The cap is met by the split.
#endregion

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.Routing
{

[TestTag("Routing")]
public class RepeatedOptionBindingTests
{
  [ModuleInitializer]
  internal static void Register() => RegisterTests<RepeatedOptionBindingTests>();

  public static async Task Should_keep_a_required_positional_after_a_greedy_repeated_option()
  {
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("process --id {id:int}* --tag {t}* {script}")
      .WithHandler((int[] id, string[] t, string script) =>
        $"id:[{string.Join(",", id)}]|t:[{string.Join(",", t)}]|script:{script}")
      .AsCommand()
      .Done()
      .Build();

    int exitCode = await app.RunAsync(["process", "--id", "1", "--id", "2", "--tag", "A", "run.sh"]);

    exitCode.ShouldBe(0);
    terminal.OutputContains("id:[1,2]|t:[A]|script:run.sh").ShouldBeTrue();
  }

  public static async Task Should_swallow_an_optional_positional_after_a_repeated_option()
  {
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("run --env {e}* {name?}")
      .WithHandler((string[] e, string? name) => $"e:[{string.Join(",", e)}]|name:{name ?? "NULL"}")
      .AsCommand()
      .Done()
      .Build();

    int exitCode = await app.RunAsync(["run", "--env", "A", "B"]);

    exitCode.ShouldBe(0);
    terminal.OutputContains("e:[A,B]|name:NULL").ShouldBeTrue();
  }

  public static async Task Should_give_trailing_greedy_values_to_a_required_positional()
  {
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("run --env {e}* {name}")
      .WithHandler((string[] e, string name) => $"e:[{string.Join(",", e)}]|name:{name}")
      .AsCommand()
      .Done()
      .Build();

    int exitCode = await app.RunAsync(["run", "--env", "A", "B", "C"]);

    exitCode.ShouldBe(0);
    terminal.OutputContains("e:[A,B]|name:C").ShouldBeTrue();
  }

  public static async Task Should_bind_a_required_path_after_repeated_build_args()
  {
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("docker build --build-arg {arg}* {path}")
      .WithHandler((string[] arg, string path) => $"args:[{string.Join(",", arg)}]|path:{path}")
      .AsCommand()
      .Done()
      .Build();

    int exitCode = await app.RunAsync(["docker", "build", "--build-arg", "K1=v1", "--build-arg", "K2=v2", "."]);

    exitCode.ShouldBe(0);
    terminal.OutputContains("args:[K1=v1,K2=v2]|path:.").ShouldBeTrue();
  }

  public static async Task Should_leave_non_integers_for_the_image_and_command()
  {
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("fluent-docker-run --env {e}* -p {port:int}* {image} {*cmd}")
      .WithHandler((string[] e, int[] port, string image, string[] cmd) =>
        $"e:{e.Length}|p:[{string.Join(",", port)}]|image:{image}|cmd:[{string.Join(",", cmd)}]")
      .AsCommand()
      .Done()
      .Build();

    int exitCode = await app.RunAsync(["fluent-docker-run", "-p", "80", "nginx", "echo", "hi"]);

    exitCode.ShouldBe(0);
    terminal.OutputContains("e:0|p:[80]|image:nginx|cmd:[echo,hi]").ShouldBeTrue();
  }

  public static async Task Should_bind_one_env_value_and_leave_the_image_and_command()
  {
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("fluent-docker-run --env {e}* -p {port:int}* {image} {*cmd}")
      .WithHandler((string[] e, int[] port, string image, string[] cmd) =>
        $"e:[{string.Join(",", e)}]|p:[{string.Join(",", port)}]|image:{image}|cmd:[{string.Join(",", cmd)}]")
      .AsCommand()
      .Done()
      .Build();

    int exitCode = await app.RunAsync(["fluent-docker-run", "--env", "A=1", "nginx", "echo", "hi"]);

    exitCode.ShouldBe(0);
    terminal.OutputContains("e:[A=1]|p:[]|image:nginx|cmd:[echo,hi]").ShouldBeTrue();
  }

  public static async Task Should_bind_each_env_flag_before_the_image_and_command()
  {
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("fluent-docker-run --env {e}* -p {port:int}* {image} {*cmd}")
      .WithHandler((string[] e, int[] port, string image, string[] cmd) =>
        $"e:[{string.Join(",", e)}]|p:[{string.Join(",", port)}]|image:{image}|cmd:[{string.Join(",", cmd)}]")
      .AsCommand()
      .Done()
      .Build();

    int exitCode = await app.RunAsync(["fluent-docker-run", "--env", "A=1", "--env", "B=2", "nginx", "echo", "hi"]);

    exitCode.ShouldBe(0);
    terminal.OutputContains("e:[A=1,B=2]|p:[]|image:nginx|cmd:[echo,hi]").ShouldBeTrue();
  }

  public static async Task Should_bind_every_converting_port_before_the_image_and_command()
  {
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("fluent-docker-run --env {e}* -p {port:int}* {image} {*cmd}")
      .WithHandler((string[] e, int[] port, string image, string[] cmd) =>
        $"e:[{string.Join(",", e)}]|p:[{string.Join(",", port)}]|image:{image}|cmd:[{string.Join(",", cmd)}]")
      .AsCommand()
      .Done()
      .Build();

    int exitCode = await app.RunAsync(["fluent-docker-run", "-p", "80", "443", "nginx", "echo", "hi"]);

    exitCode.ShouldBe(0);
    terminal.OutputContains("e:[]|p:[80,443]|image:nginx|cmd:[echo,hi]").ShouldBeTrue();
  }

  public static async Task Should_bind_one_optional_env_value_and_leave_the_command()
  {
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("docker --env? {e}* {*cmd}")
      .WithHandler((string[] e, string[] cmd) => $"e:[{string.Join(",", e)}]|cmd:[{string.Join(",", cmd)}]")
      .AsCommand()
      .Done()
      .Build();

    int exitCode = await app.RunAsync(["docker", "--env", "A", "echo", "hi"]);

    exitCode.ShouldBe(0);
    terminal.OutputContains("e:[A]|cmd:[echo,hi]").ShouldBeTrue();
  }

  public static async Task Should_bind_every_env_value_before_the_end_of_options_separator()
  {
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("docker --env {e}* -- {*cmd}")
      .WithHandler((string[] e, string[] cmd) => $"e:[{string.Join(",", e)}]|cmd:[{string.Join(",", cmd)}]")
      .AsCommand()
      .Done()
      .Build();

    int exitCode = await app.RunAsync(["docker", "--env", "A", "B", "--", "echo", "hi"]);

    exitCode.ShouldBe(0);
    terminal.OutputContains("e:[A,B]|cmd:[echo,hi]").ShouldBeTrue();
  }

  public static async Task Should_keep_a_required_parameter_after_an_endpoint_collection()
  {
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map<BindingStrictnessPackCommand>()
      .Build();

    int exitCode = await app.RunAsync(["pack", "--build-arg", "K1=v1", "."]);

    exitCode.ShouldBe(0);
    terminal.OutputContains("args:[K1=v1]|path:.").ShouldBeTrue();
  }
}

[NuruRoute("pack")]
public sealed class BindingStrictnessPackCommand : ICommand<Unit>
{
  [Option("build-arg")]
  public IReadOnlyList<string> BuildArg { get; set; } = [];

  [Parameter]
  public string Path { get; set; } = "";

  internal sealed class Handler(ITerminal terminal) : IRequestHandler<BindingStrictnessPackCommand, Unit>
  {
    public async ValueTask<Unit> Handle(BindingStrictnessPackCommand command, CancellationToken cancellationToken)
    {
      _ = cancellationToken.CanBeCanceled;
      await terminal.WriteLineAsync($"args:[{string.Join(",", command.BuildArg)}]|path:{command.Path}")
        .ConfigureAwait(false);
      return default;
    }
  }
}

} // namespace TimeWarp.Nuru.Tests.Routing
