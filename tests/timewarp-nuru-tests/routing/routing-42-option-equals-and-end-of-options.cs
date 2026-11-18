#!/usr/bin/env -S dotnet --

#region Purpose
// Pins how the configuration-override filter treats option tokens (task 591-001): a declared long
// option's --name=value and --name:value bind, a declared flag's --flag=value is an error, and every
// token after "--" reaches the route as typed and never becomes configuration.
#endregion

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.Routing
{

[TestTag("Routing")]
public class OptionEqualsAndEndOfOptionsTests
{
  [ModuleInitializer]
  internal static void Register() => RegisterTests<OptionEqualsAndEndOfOptionsTests>();

  public static async Task Should_bind_declared_option_with_equals_value()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("report --format {format}").WithHandler((string format) => $"format:{format}")
      .AsQuery().Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["report", "--format=json"]);

    // Assert
    exitCode.ShouldBe(0);
    terminal.OutputContains("format:json").ShouldBeTrue();
  }

  public static async Task Should_bind_declared_option_with_colon_value()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("report --format {format}").WithHandler((string format) => $"format:{format}")
      .AsQuery().Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["report", "--format:json"]);

    // Assert
    exitCode.ShouldBe(0);
    terminal.OutputContains("format:json").ShouldBeTrue();
  }

  public static async Task Should_keep_value_text_after_the_first_separator()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("connect --connection {conn}").WithHandler((string conn) => $"conn:{conn}")
      .AsCommand().Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["connect", "--connection=Server=localhost:5432;Database=mydb"]);

    // Assert
    exitCode.ShouldBe(0);
    terminal.OutputContains("conn:Server=localhost:5432;Database=mydb").ShouldBeTrue();
  }

  public static async Task Should_reject_declared_flag_with_equals_value()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("build --verbose").WithHandler((bool verbose) => $"build:{verbose}")
      .AsCommand().Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["build", "--verbose=true"]);

    // Assert
    exitCode.ShouldBe(1);
    terminal.ErrorContains("Unknown option '--verbose=true'").ShouldBeTrue();
    terminal.OutputContains("build:").ShouldBeFalse();
  }

  public static async Task Should_still_filter_undeclared_configuration_override()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("report --format {format}").WithHandler((string format) => $"format:{format}")
      .AsQuery().Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["report", "--format=json", "--Logging:LogLevel:Default=Debug", "/Mode=fast"]);

    // Assert
    exitCode.ShouldBe(0);
    terminal.OutputContains("format:json").ShouldBeTrue();
  }

  public static async Task Should_forward_every_token_after_end_of_options()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("run {target} {*args}")
        .WithHandler((string target, string[] args) => $"run:{target}|{string.Join(",", args)}")
        .AsCommand().Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["run", "build", "--", "--foo=bar", "/p:X=Y", "--Section:Key=v", "/usr/bin"]);

    // Assert
    exitCode.ShouldBe(0);
    terminal.OutputContains("run:build|--foo=bar,/p:X=Y,--Section:Key=v,/usr/bin").ShouldBeTrue();
  }

  public static async Task Should_not_bind_declared_option_after_end_of_options()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("exec --format? {format?} {*args}")
        .WithHandler((string? format, string[] args) => $"exec:{format ?? "none"}|{string.Join(",", args)}")
        .AsCommand().Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["exec", "--", "--format:json", "--format=xml"]);

    // Assert
    exitCode.ShouldBe(0);
    terminal.OutputContains("exec:none|--format:json,--format=xml").ShouldBeTrue();
  }

  public static async Task Should_not_read_tokens_after_end_of_options_as_configuration()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .AddConfiguration()
      .Map("pass {*args}")
        .WithHandler((string[] args, IConfiguration config) =>
          $"pass:{string.Join(",", args)}|before:{config["Before"] ?? "unset"}|after:{config["After"] ?? "unset"}")
        .AsQuery().Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["pass", "--Before=yes", "--", "--After=yes"]);

    // Assert
    exitCode.ShouldBe(0);
    terminal.OutputContains("pass:--After=yes|before:yes|after:unset").ShouldBeTrue();
  }
}

} // namespace TimeWarp.Nuru.Tests.Routing
