#!/usr/bin/dotnet --

return await RunTests<ParameterBindingTests>(clearCache: true);

[TestTag("Routing")]
public class ParameterBindingTests
{
  public static async Task Should_bind_string_parameter_greet_Alice()
  {
    // Arrange
    NuruAppBuilder builder = new();
    string? boundName = null;
    builder.AddRoute("greet {name}", (string name) => { boundName = name; return 0; });

    NuruApp app = builder.Build();

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
    NuruAppBuilder builder = new();
    int boundMs = 0;
    builder.AddRoute("delay {ms:int}", (int ms) => { boundMs = ms; return 0; });

    NuruApp app = builder.Build();

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
    NuruAppBuilder builder = new();
#pragma warning disable RCS1163 // Unused parameter
    builder.AddRoute("delay {ms:int}", (int ms) => 0);
#pragma warning restore RCS1163 // Unused parameter

    NuruApp app = builder.Build();

    // Act
    int exitCode = await app.RunAsync(["delay", "abc"]);

    // Assert
    exitCode.ShouldBe(1); // Type conversion failure

    await Task.CompletedTask;
  }

  public static async Task Should_bind_double_parameter_calculate_3_14()
  {
    // Arrange
    NuruAppBuilder builder = new();
    double boundValue = 0;
    builder.AddRoute("calculate {value:double}", (double value) => { boundValue = value; return 0; });

    NuruApp app = builder.Build();

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
    NuruAppBuilder builder = new();
    bool boundFlag = false;
    builder.AddRoute("set {flag:bool}", (bool flag) => { boundFlag = flag; return 0; });

    NuruApp app = builder.Build();

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
    NuruAppBuilder builder = new();
    bool boundFlag = true;
    builder.AddRoute("set {flag:bool}", (bool flag) => { boundFlag = flag; return 0; });

    NuruApp app = builder.Build();

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
    NuruAppBuilder builder = new();
    string? boundHost = null;
    int boundPort = 0;
    builder.AddRoute("connect {host} {port:int}", (string host, int port) => { boundHost = host; boundPort = port; return 0; });

    NuruApp app = builder.Build();

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
    NuruAppBuilder builder = new();
#pragma warning disable RCS1163 // Unused parameter
    builder.AddRoute("age {years:int}", (int years) => 0);
#pragma warning restore RCS1163 // Unused parameter

    NuruApp app = builder.Build();

    // Act
    int exitCode = await app.RunAsync(["age", "twenty"]);

    // Assert
    exitCode.ShouldBe(1); // Type conversion failure

    await Task.CompletedTask;
  }
}