#!/usr/bin/dotnet --

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.Routing
{

  [TestTag("Routing")]
  public class ParameterBindingTests
  {
    [ModuleInitializer]
    internal static void Register() => RegisterTests<ParameterBindingTests>();

    public static async Task Should_bind_string_parameter_greet_Alice()
    {
      // Arrange
      using TestTerminal terminal = new();
      NuruCoreApp app = NuruApp.CreateBuilder([])
        .UseTerminal(terminal)
        .Map("greet {name}").WithHandler((string name) => $"name:{name}").AsQuery().Done()
        .Build();

      // Act
      int exitCode = await app.RunAsync(["greet", "Alice"]);

      // Assert
      exitCode.ShouldBe(0);
      terminal.OutputContains("name:Alice").ShouldBeTrue();

      await Task.CompletedTask;
    }

    public static async Task Should_bind_integer_parameter_delay_500()
    {
      // Arrange
      using TestTerminal terminal = new();
      NuruCoreApp app = NuruApp.CreateBuilder([])
        .UseTerminal(terminal)
        .Map("delay {ms:int}").WithHandler((int ms) => $"ms:{ms}").AsQuery().Done()
        .Build();

      // Act
      int exitCode = await app.RunAsync(["delay", "500"]);

      // Assert
      exitCode.ShouldBe(0);
      terminal.OutputContains("ms:500").ShouldBeTrue();

      await Task.CompletedTask;
    }

    public static async Task Should_not_bind_integer_parameter_delay_abc()
    {
      // Arrange
      using TestTerminal terminal = new();
#pragma warning disable RCS1163 // Unused parameter
      NuruCoreApp app = NuruApp.CreateBuilder([])
        .UseTerminal(terminal)
        .Map("delay {ms:int}").WithHandler((int ms) => $"ms:{ms}").AsQuery().Done()
        .Build();
#pragma warning restore RCS1163 // Unused parameter

      // Act - type conversion failure returns exit code 1 with error message
      int exitCode = await app.RunAsync(["delay", "abc"]);

      // Assert
      exitCode.ShouldBe(1);
      terminal.OutputContains("Error:").ShouldBeTrue();
      terminal.OutputContains("ms:").ShouldBeFalse(); // Handler should not have been invoked

      await Task.CompletedTask;
    }

    public static async Task Should_bind_double_parameter_calculate_3_14()
    {
      // Arrange
      using TestTerminal terminal = new();
      NuruCoreApp app = NuruApp.CreateBuilder([])
        .UseTerminal(terminal)
        .Map("calculate {value:double}").WithHandler((double value) => $"value:{value}").AsQuery().Done()
        .Build();

      // Act
      int exitCode = await app.RunAsync(["calculate", "3.14"]);

      // Assert
      exitCode.ShouldBe(0);
      terminal.OutputContains("value:3.14").ShouldBeTrue();

      await Task.CompletedTask;
    }

    public static async Task Should_bind_bool_parameter_set_true()
    {
      // Arrange
      using TestTerminal terminal = new();
      NuruCoreApp app = NuruApp.CreateBuilder([])
        .UseTerminal(terminal)
        .Map("set {flag:bool}").WithHandler((bool flag) => $"flag:{flag}").AsQuery().Done()
        .Build();

      // Act
      int exitCode = await app.RunAsync(["set", "true"]);

      // Assert
      exitCode.ShouldBe(0);
      terminal.OutputContains("flag:True").ShouldBeTrue();

      await Task.CompletedTask;
    }

    public static async Task Should_bind_bool_parameter_set_false()
    {
      // Arrange
      using TestTerminal terminal = new();
      NuruCoreApp app = NuruApp.CreateBuilder([])
        .UseTerminal(terminal)
        .Map("set {flag:bool}").WithHandler((bool flag) => $"flag:{flag}").AsQuery().Done()
        .Build();

      // Act
      int exitCode = await app.RunAsync(["set", "false"]);

      // Assert
      exitCode.ShouldBe(0);
      terminal.OutputContains("flag:False").ShouldBeTrue();

      await Task.CompletedTask;
    }

    public static async Task Should_bind_multiple_parameters_connect_localhost_8080()
    {
      // Arrange
      using TestTerminal terminal = new();
      NuruCoreApp app = NuruApp.CreateBuilder([])
        .UseTerminal(terminal)
        .Map("connect {host} {port:int}").WithHandler((string host, int port) => $"host:{host},port:{port}").AsQuery().Done()
        .Build();

      // Act
      int exitCode = await app.RunAsync(["connect", "localhost", "8080"]);

      // Assert
      exitCode.ShouldBe(0);
      terminal.OutputContains("host:localhost,port:8080").ShouldBeTrue();

      await Task.CompletedTask;
    }

    public static async Task Should_not_bind_type_mismatch_age_twenty()
    {
      // Arrange
      using TestTerminal terminal = new();
      NuruCoreApp app = NuruApp.CreateBuilder([])
        .UseTerminal(terminal)
        .Map("age {years:int}").WithHandler((int years) => $"years:{years}").AsQuery().Done()
        .Build();

      // Act - type conversion failure returns exit code 1 with error message
      int exitCode = await app.RunAsync(["age", "twenty"]);

      // Assert
      exitCode.ShouldBe(1);
      terminal.OutputContains("Error:").ShouldBeTrue();
      terminal.OutputContains("years:").ShouldBeFalse(); // Handler should not have been invoked

      await Task.CompletedTask;
    }
  }

}
