#!/usr/bin/dotnet --
#:project ../../source/timewarp-nuru/timewarp-nuru.csproj

// Tests for CompiledRouteBuilder
// Verifies that builder-constructed routes match PatternParser.Parse() output

return await RunTests<CompiledRouteBuilderTests>();

/// <summary>
/// Tests for CompiledRouteBuilder - verifies parity with PatternParser.Parse()
/// </summary>
[TestTag("CompiledRouteBuilder")]
public sealed class CompiledRouteBuilderTests
{
  /// <summary>
  /// Test: Simple literal route ("greet")
  /// </summary>
  public static async Task Should_build_simple_literal_route()
  {
    // Arrange
    const string pattern = "greet";
    CompiledRoute builderRoute = new CompiledRouteBuilder()
      .WithLiteral("greet")
      .Build();

    // Act & Assert
    AssertRoutesMatch(pattern, builderRoute);
    WriteLine($"PASS: {pattern}");

    await Task.CompletedTask;
  }

  /// <summary>
  /// Test: Literal + parameter ("greet {name}")
  /// </summary>
  public static async Task Should_build_literal_with_parameter()
  {
    // Arrange
    const string pattern = "greet {name}";
    CompiledRoute builderRoute = new CompiledRouteBuilder()
      .WithLiteral("greet")
      .WithParameter("name")
      .Build();

    // Act & Assert
    AssertRoutesMatch(pattern, builderRoute);
    WriteLine($"PASS: {pattern}");

    await Task.CompletedTask;
  }

  /// <summary>
  /// Test: Multiple literals ("git commit")
  /// </summary>
  public static async Task Should_build_multiple_literals()
  {
    // Arrange
    const string pattern = "git commit";
    CompiledRoute builderRoute = new CompiledRouteBuilder()
      .WithLiteral("git")
      .WithLiteral("commit")
      .Build();

    // Act & Assert
    AssertRoutesMatch(pattern, builderRoute);
    WriteLine($"PASS: {pattern}");

    await Task.CompletedTask;
  }

  /// <summary>
  /// Test: Optional parameter ("greet {name?}")
  /// </summary>
  public static async Task Should_build_optional_parameter()
  {
    // Arrange
    const string pattern = "greet {name?}";
    CompiledRoute builderRoute = new CompiledRouteBuilder()
      .WithLiteral("greet")
      .WithParameter("name", isOptional: true)
      .Build();

    // Act & Assert
    AssertRoutesMatch(pattern, builderRoute);
    WriteLine($"PASS: {pattern}");

    await Task.CompletedTask;
  }

  /// <summary>
  /// Test: Typed parameter ("add {x:int} {y:int}")
  /// </summary>
  public static async Task Should_build_typed_parameters()
  {
    // Arrange
    const string pattern = "add {x:int} {y:int}";
    CompiledRoute builderRoute = new CompiledRouteBuilder()
      .WithLiteral("add")
      .WithParameter("x", type: "int")
      .WithParameter("y", type: "int")
      .Build();

    // Act & Assert
    AssertRoutesMatch(pattern, builderRoute);
    WriteLine($"PASS: {pattern}");

    await Task.CompletedTask;
  }

  /// <summary>
  /// Test: Boolean flag option ("deploy --force")
  /// </summary>
  public static async Task Should_build_boolean_flag_option()
  {
    // Arrange
    const string pattern = "deploy --force";
    CompiledRoute builderRoute = new CompiledRouteBuilder()
      .WithLiteral("deploy")
      .WithOption("force")
      .Build();

    // Act & Assert
    AssertRoutesMatch(pattern, builderRoute);
    WriteLine($"PASS: {pattern}");

    await Task.CompletedTask;
  }

  /// <summary>
  /// Test: Option with short form ("deploy --force,-f")
  /// </summary>
  public static async Task Should_build_option_with_short_form()
  {
    // Arrange
    const string pattern = "deploy --force,-f";
    CompiledRoute builderRoute = new CompiledRouteBuilder()
      .WithLiteral("deploy")
      .WithOption("force", shortForm: "f")
      .Build();

    // Act & Assert
    AssertRoutesMatch(pattern, builderRoute);
    WriteLine($"PASS: {pattern}");

    await Task.CompletedTask;
  }

  /// <summary>
  /// Test: Option with value ("deploy --config {file}")
  /// </summary>
  public static async Task Should_build_option_with_value()
  {
    // Arrange
    const string pattern = "deploy --config {file}";
    CompiledRoute builderRoute = new CompiledRouteBuilder()
      .WithLiteral("deploy")
      .WithOption("config", parameterName: "file", expectsValue: true)
      .Build();

    // Act & Assert
    AssertRoutesMatch(pattern, builderRoute);
    WriteLine($"PASS: {pattern}");

    await Task.CompletedTask;
  }

  /// <summary>
  /// Test: Catch-all parameter ("exec {*args}")
  /// </summary>
  public static async Task Should_build_catch_all_parameter()
  {
    // Arrange
    const string pattern = "exec {*args}";
    CompiledRoute builderRoute = new CompiledRouteBuilder()
      .WithLiteral("exec")
      .WithCatchAll("args")
      .Build();

    // Act & Assert
    AssertRoutesMatch(pattern, builderRoute);
    WriteLine($"PASS: {pattern}");

    await Task.CompletedTask;
  }

  /// <summary>
  /// Test: Complex route ("deploy {env} --force,-f --config,-c {file?}")
  /// </summary>
  public static async Task Should_build_complex_route()
  {
    // Arrange
    const string pattern = "deploy {env} --force,-f --config,-c {file?}";
    CompiledRoute builderRoute = new CompiledRouteBuilder()
      .WithLiteral("deploy")
      .WithParameter("env")
      .WithOption("force", shortForm: "f")
      .WithOption("config", shortForm: "c", parameterName: "file", expectsValue: true, parameterIsOptional: true)
      .Build();

    // Act & Assert
    AssertRoutesMatch(pattern, builderRoute);
    WriteLine($"PASS: {pattern}");

    await Task.CompletedTask;
  }

  /// <summary>
  /// Test: Only catch-all disallows multiple
  /// </summary>
  public static async Task Should_throw_on_multiple_catch_all()
  {
    // Arrange & Act & Assert
    Should.Throw<InvalidOperationException>(() =>
    {
      new CompiledRouteBuilder()
        .WithCatchAll("args1")
        .WithCatchAll("args2")
        .Build();
    });

    WriteLine("PASS: Multiple catch-all throws InvalidOperationException");

    await Task.CompletedTask;
  }

  /// <summary>
  /// Test: Typed option value ("server --port {port:int}")
  /// </summary>
  public static async Task Should_build_typed_option_value()
  {
    // Arrange
    const string pattern = "server --port {port:int}";
    CompiledRoute builderRoute = new CompiledRouteBuilder()
      .WithLiteral("server")
      .WithOption("port", parameterName: "port", expectsValue: true, parameterType: "int")
      .Build();

    // Act & Assert
    AssertRoutesMatch(pattern, builderRoute);
    WriteLine($"PASS: {pattern}");

    await Task.CompletedTask;
  }

  /// <summary>
  /// Test: Optional flag modifier ("deploy --verbose?")
  /// </summary>
  public static async Task Should_build_optional_flag()
  {
    // Arrange
    const string pattern = "deploy --verbose?";
    CompiledRoute builderRoute = new CompiledRouteBuilder()
      .WithLiteral("deploy")
      .WithOption("verbose", isOptionalFlag: true)
      .Build();

    // Act & Assert
    AssertRoutesMatch(pattern, builderRoute);
    WriteLine($"PASS: {pattern}");

    await Task.CompletedTask;
  }
}
