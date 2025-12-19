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
    string? boundName = null;
    NuruCoreApp app = new NuruAppBuilder()
      .Map("greet {name}").WithHandler((string name) => { boundName = name; return 0; }).AsQuery().Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["greet", "Alice"]);

    // Assert
    exitCode.ShouldBe(0);
    boundName.ShouldBe("Alice");

    await Task.CompletedTask;
  }

  public static async Task Should_bind_integer_parameter_delay_500()
  {
    // Arrange
    int boundMs = 0;
    NuruCoreApp app = new NuruAppBuilder()
      .Map("delay {ms:int}").WithHandler((int ms) => { boundMs = ms; return 0; }).AsCommand().Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["delay", "500"]);

    // Assert
    exitCode.ShouldBe(0);
    boundMs.ShouldBe(500);

    await Task.CompletedTask;
  }

  public static async Task Should_not_bind_integer_parameter_delay_abc()
  {
    // Arrange
#pragma warning disable RCS1163 // Unused parameter
    NuruCoreApp app = new NuruAppBuilder()
      .Map("delay {ms:int}").WithHandler((int ms) => 0).AsCommand().Done()
      .Build();
#pragma warning restore RCS1163 // Unused parameter

    // Act
    int exitCode = await app.RunAsync(["delay", "abc"]);

    // Assert
    exitCode.ShouldBe(1); // Type conversion failure

    await Task.CompletedTask;
  }

  public static async Task Should_bind_double_parameter_calculate_3_14()
  {
    // Arrange
    double boundValue = 0;
    NuruCoreApp app = new NuruAppBuilder()
      .Map("calculate {value:double}").WithHandler((double value) => { boundValue = value; return 0; }).AsCommand().Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["calculate", "3.14"]);

    // Assert
    exitCode.ShouldBe(0);
    boundValue.ShouldBe(3.14);

    await Task.CompletedTask;
  }

  public static async Task Should_bind_bool_parameter_set_true()
  {
    // Arrange
    bool boundFlag = false;
    NuruCoreApp app = new NuruAppBuilder()
      .Map("set {flag:bool}").WithHandler((bool flag) => { boundFlag = flag; return 0; }).AsCommand().Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["set", "true"]);

    // Assert
    exitCode.ShouldBe(0);
    boundFlag.ShouldBeTrue();

    await Task.CompletedTask;
  }

  public static async Task Should_bind_bool_parameter_set_false()
  {
    // Arrange
    bool boundFlag = true;
    NuruCoreApp app = new NuruAppBuilder()
      .Map("set {flag:bool}").WithHandler((bool flag) => { boundFlag = flag; return 0; }).AsCommand().Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["set", "false"]);

    // Assert
    exitCode.ShouldBe(0);
    boundFlag.ShouldBeFalse();

    await Task.CompletedTask;
  }

  public static async Task Should_bind_multiple_parameters_connect_localhost_8080()
  {
    // Arrange
    string? boundHost = null;
    int boundPort = 0;
    NuruCoreApp app = new NuruAppBuilder()
      .Map("connect {host} {port:int}").WithHandler((string host, int port) => { boundHost = host; boundPort = port; return 0; }).AsCommand().Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["connect", "localhost", "8080"]);

    // Assert
    exitCode.ShouldBe(0);
    boundHost.ShouldBe("localhost");
    boundPort.ShouldBe(8080);

    await Task.CompletedTask;
  }

  public static async Task Should_not_bind_type_mismatch_age_twenty()
  {
    // Arrange
#pragma warning disable RCS1163 // Unused parameter
    NuruCoreApp app = new NuruAppBuilder()
      .Map("age {years:int}").WithHandler((int years) => 0).AsCommand().Done()
      .Build();
#pragma warning restore RCS1163 // Unused parameter

    // Act
    int exitCode = await app.RunAsync(["age", "twenty"]);

    // Assert
    exitCode.ShouldBe(1); // Type conversion failure

    await Task.CompletedTask;
  }
}

} // namespace TimeWarp.Nuru.Tests.Routing
