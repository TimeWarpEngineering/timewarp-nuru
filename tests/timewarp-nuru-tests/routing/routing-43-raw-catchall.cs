#!/usr/bin/env -S dotnet --
#:project $(SourceDirectory)timewarp-nuru/timewarp-nuru.csproj

#region Purpose
// Pins {**args}: every remaining token reaches the handler unchanged, including option-shaped
// tokens, "--", configuration overrides, and --help/--version. {*args} still fails closed.
#endregion

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.Routing
{

[TestTag("Routing")]
public class RawCatchAllRoutingTests
{
  [ModuleInitializer]
  internal static void Register() => RegisterTests<RawCatchAllRoutingTests>();

  private static readonly string[] Tokens = ["-n", "-40", "+5", "--", "--lines=40", "-", ""];

  public static async Task Should_pass_every_token_through_a_default_raw_route()
  {
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("{**args}").WithHandler((string[] args) => string.Join("|", args)).AsCommand().Done()
      .Build();

    int exitCode = await app.RunAsync(Tokens);

    exitCode.ShouldBe(0);
    terminal.Output.Trim().ShouldBe(string.Join("|", Tokens));
  }

  public static async Task Should_pass_every_token_through_a_literal_prefixed_raw_route()
  {
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("tail {**args}").WithHandler((string[] args) => string.Join("|", args)).AsCommand().Done()
      .Build();

    int exitCode = await app.RunAsync(["tail", .. Tokens]);

    exitCode.ShouldBe(0);
    terminal.Output.Trim().ShouldBe(string.Join("|", Tokens));
  }

  public static async Task Should_keep_slash_and_colon_configuration_forms()
  {
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("{**args}").WithHandler((string[] args) => string.Join("|", args)).AsCommand().Done()
      .Build();

    int exitCode = await app.RunAsync(["/lines=40", "--Foo:Bar=1"]);

    exitCode.ShouldBe(0);
    terminal.Output.Trim().ShouldBe("/lines=40|--Foo:Bar=1");
  }

  public static async Task Should_match_an_empty_raw_route()
  {
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("{**args}").WithHandler((string[] args) => string.Join("|", args)).AsCommand().Done()
      .Build();

    int exitCode = await app.RunAsync([]);

    exitCode.ShouldBe(0);
    terminal.Output.Trim().ShouldBe("");
  }

  public static async Task Should_pass_help_version_and_capabilities_through_a_default_raw_route()
  {
    using TestTerminal helpTerminal = new();
    NuruApp helpApp = NuruApp.CreateBuilder()
      .UseTerminal(helpTerminal)
      .Map("{**args}").WithHandler((string[] args) => string.Join("|", args)).AsCommand().Done()
      .Build();
    (await helpApp.RunAsync(["--help"])).ShouldBe(0);
    helpTerminal.Output.Trim().ShouldBe("--help");

    using TestTerminal versionTerminal = new();
    NuruApp versionApp = NuruApp.CreateBuilder()
      .UseTerminal(versionTerminal)
      .Map("{**args}").WithHandler((string[] args) => string.Join("|", args)).AsCommand().Done()
      .Build();
    (await versionApp.RunAsync(["--version"])).ShouldBe(0);
    versionTerminal.Output.Trim().ShouldBe("--version");

    using TestTerminal capabilitiesTerminal = new();
    NuruApp capabilitiesApp = NuruApp.CreateBuilder()
      .UseTerminal(capabilitiesTerminal)
      .Map("{**args}").WithHandler((string[] args) => string.Join("|", args)).AsCommand().Done()
      .Build();
    (await capabilitiesApp.RunAsync(["--capabilities"])).ShouldBe(0);
    capabilitiesTerminal.Output.Trim().ShouldBe("--capabilities");
  }

  public static async Task Should_leave_app_help_when_the_raw_route_has_a_literal()
  {
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("tail {**args}").WithHandler((string[] args) => string.Join("|", args)).AsCommand().Done()
      .Build();

    int exitCode = await app.RunAsync(["--help"]);

    exitCode.ShouldBe(0);
    terminal.OutputContains("Commands:").ShouldBeTrue();
    terminal.OutputContains("-n").ShouldBeFalse();
  }

  public static async Task Should_pass_help_through_on_the_raw_command()
  {
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("tail {**args}").WithHandler((string[] args) => string.Join("|", args)).AsCommand().Done()
      .Build();

    int exitCode = await app.RunAsync(["tail", "--help"]);

    exitCode.ShouldBe(0);
    terminal.Output.Trim().ShouldBe("--help");
    terminal.OutputContains("Parameters:").ShouldBeFalse();
  }

  public static async Task Should_pass_tail_dash_i_to_the_handler()
  {
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("tail {**args}").WithHandler((string[] args) => string.Join("|", args)).AsCommand().Done()
      .AddRepl()
      .Build();

    int exitCode = await app.RunAsync(["tail", "-i"]);

    exitCode.ShouldBe(0);
    terminal.Output.Trim().ShouldBe("-i");
  }

  public static async Task Should_reject_option_shaped_token_on_star_catchall()
  {
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("{*args}").WithHandler((string[] args) => string.Join("|", args)).AsCommand().Done()
      .Build();

    int exitCode = await app.RunAsync(["-n", "40"]);

    exitCode.ShouldBe(1);
    terminal.ErrorContains("Unknown option '-n'").ShouldBeTrue();
    terminal.OutputContains("-n").ShouldBeFalse();
  }

  public static async Task Should_let_a_more_specific_route_keep_configuration_stripping()
  {
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("status").WithHandler(() => "status-hit").AsCommand().Done()
      .Map("{**args}").WithHandler((string[] args) => "raw:" + string.Join("|", args)).AsCommand().Done()
      .Build();

    int exitCode = await app.RunAsync(["status", "--Logging:LogLevel:Default=Debug"]);

    exitCode.ShouldBe(0);
    terminal.Output.Trim().ShouldBe("status-hit");
  }

  public static async Task Should_prefer_a_parameter_route_and_fall_through_to_raw()
  {
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("tail {file}").WithHandler((string file) => "file:" + file).AsCommand().Done()
      .Map("tail {**args}").WithHandler((string[] args) => "raw:" + string.Join("|", args)).AsCommand().Done()
      .Build();

    (await app.RunAsync(["tail", "readme"])).ShouldBe(0);
    terminal.Output.Trim().ShouldBe("file:readme");
  }

  public static async Task Should_fall_through_to_raw_when_the_parameter_route_sees_an_option()
  {
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("tail {file}").WithHandler((string file) => "file:" + file).AsCommand().Done()
      .Map("tail {**args}").WithHandler((string[] args) => "raw:" + string.Join("|", args)).AsCommand().Done()
      .Build();

    (await app.RunAsync(["tail", "-n", "40"])).ShouldBe(0);
    terminal.Output.Trim().ShouldBe("raw:-n|40");
  }

  public static async Task Should_pass_tokens_through_in_the_repl()
  {
    using TestTerminal terminal = new();
    terminal.QueueLine("tail -n 40");
    terminal.QueueLine("exit");

    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("tail {**args}").WithHandler((string[] args) => string.Join("|", args)).AsCommand().Done()
      .AddRepl(options => options.EnableColors = false)
      .Build();

    await app.RunAsync(["--interactive"]);

    terminal.OutputContains("-n|40").ShouldBeTrue();
  }
}
}
