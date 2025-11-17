#!/usr/bin/dotnet --

return await RunTests<ParameterDetectionTests>(clearCache: true);

[TestTag("Completion")]
[ClearRunfileCache]
public class ParameterDetectionTests
{
  public static async Task Should_detect_first_positional_parameter()
  {
    // Arrange
    var builder = new NuruAppBuilder();
    builder.AddRoute("greet {name}", (string name) => 0);

    string[] typedWords = ["greet"];
    Endpoint endpoint = builder.EndpointCollection.First();

    // Act
    bool result = DynamicCompletionHandler.TryMatchEndpoint(endpoint, typedWords, out string? paramName, out Type? paramType);

    // Assert
    result.ShouldBeTrue();
    paramName.ShouldBe("name");
    paramType.ShouldBe(typeof(string));

    await Task.CompletedTask;
  }

  public static async Task Should_detect_second_positional_parameter()
  {
    // Arrange
    var builder = new NuruAppBuilder();
    builder.AddRoute("deploy {env} {tag}", (string env, string tag) => 0);

    string[] typedWords = ["deploy", "production"];
    Endpoint endpoint = builder.EndpointCollection.First();

    // Act
    bool result = DynamicCompletionHandler.TryMatchEndpoint(endpoint, typedWords, out string? paramName, out Type? paramType);

    // Assert
    result.ShouldBeTrue();
    paramName.ShouldBe("tag");
    paramType.ShouldBe(typeof(string));

    await Task.CompletedTask;
  }

  public static async Task Should_detect_optional_parameter()
  {
    // Arrange
    var builder = new NuruAppBuilder();
    builder.AddRoute("deploy {env} {tag?}", (string env, string? tag) => 0);

    string[] typedWords = ["deploy", "production"];
    Endpoint endpoint = builder.EndpointCollection.First();

    // Act
    bool result = DynamicCompletionHandler.TryMatchEndpoint(endpoint, typedWords, out string? paramName, out Type? paramType);

    // Assert
    result.ShouldBeTrue();
    paramName.ShouldBe("tag");
    paramType.ShouldBe(typeof(string));

    await Task.CompletedTask;
  }

  public static async Task Should_detect_option_value_parameter()
  {
    // Arrange
    var builder = new NuruAppBuilder();
    builder.AddRoute("build --config {mode}", (string mode) => 0);

    string[] typedWords = ["build", "--config"];
    Endpoint endpoint = builder.EndpointCollection.First();

    // Act
    bool result = DynamicCompletionHandler.TryMatchEndpoint(endpoint, typedWords, out string? paramName, out Type? paramType);

    // Assert
    result.ShouldBeTrue();
    paramName.ShouldBe("mode");
    paramType.ShouldBe(typeof(string));

    await Task.CompletedTask;
  }

  public static async Task Should_detect_typed_parameter()
  {
    // Arrange
    var builder = new NuruAppBuilder();
    builder.AddRoute("connect {port:int}", (int port) => 0);

    string[] typedWords = ["connect"];
    Endpoint endpoint = builder.EndpointCollection.First();

    // Act
    bool result = DynamicCompletionHandler.TryMatchEndpoint(endpoint, typedWords, out string? paramName, out Type? paramType);

    // Assert
    result.ShouldBeTrue();
    paramName.ShouldBe("port");
    paramType.ShouldBe(typeof(int));

    await Task.CompletedTask;
  }

  public static async Task Should_detect_enum_parameter()
  {
    // Arrange
    var builder = new NuruAppBuilder();
    builder.AddRoute("deploy {env} --mode {mode}", (string env, TestMode mode) => 0);

    string[] typedWords = ["deploy", "production", "--mode"];
    Endpoint endpoint = builder.EndpointCollection.First();

    // Act
    bool result = DynamicCompletionHandler.TryMatchEndpoint(endpoint, typedWords, out string? paramName, out Type? paramType);

    // Assert
    result.ShouldBeTrue();
    paramName.ShouldBe("mode");
    paramType.ShouldBe(typeof(TestMode));

    await Task.CompletedTask;
  }

  public static async Task Should_return_false_when_literal_mismatch()
  {
    // Arrange
    var builder = new NuruAppBuilder();
    builder.AddRoute("greet {name}", (string name) => 0);

    string[] typedWords = ["hello"]; // Wrong literal
    Endpoint endpoint = builder.EndpointCollection.First();

    // Act
    bool result = DynamicCompletionHandler.TryMatchEndpoint(endpoint, typedWords, out string? paramName, out Type? paramType);

    // Assert
    result.ShouldBeFalse();
    paramName.ShouldBeNull();
    paramType.ShouldBeNull();

    await Task.CompletedTask;
  }

  public static async Task Should_return_false_when_completing_literal()
  {
    // Arrange
    var builder = new NuruAppBuilder();
    builder.AddRoute("git status", () => 0);

    string[] typedWords = ["git"];
    Endpoint endpoint = builder.EndpointCollection.First();

    // Act
    bool result = DynamicCompletionHandler.TryMatchEndpoint(endpoint, typedWords, out string? paramName, out Type? paramType);

    // Assert
    result.ShouldBeFalse(); // Next segment is literal "status", not a parameter

    await Task.CompletedTask;
  }

  public static async Task Should_detect_short_option_value_parameter()
  {
    // Arrange
    var builder = new NuruAppBuilder();
    builder.AddRoute("build --config,-c {mode}", (string mode) => 0);

    string[] typedWords = ["build", "-c"];
    Endpoint endpoint = builder.EndpointCollection.First();

    // Act
    bool result = DynamicCompletionHandler.TryMatchEndpoint(endpoint, typedWords, out string? paramName, out Type? paramType);

    // Assert
    result.ShouldBeTrue();
    paramName.ShouldBe("mode");
    paramType.ShouldBe(typeof(string));

    await Task.CompletedTask;
  }

  public static async Task Should_use_try_get_parameter_info_with_full_context()
  {
    // Arrange
    var builder = new NuruAppBuilder();
    builder.AddRoute("greet {name}", (string name) => 0);

    CompletionContext context = new(
      Args: ["app", "greet"],
      CursorPosition: 2,
      Endpoints: builder.EndpointCollection
    );

    // Act
    bool result = DynamicCompletionHandler.TryGetParameterInfo(context, out string? paramName, out Type? paramType);

    // Assert
    result.ShouldBeTrue();
    paramName.ShouldBe("name");
    paramType.ShouldBe(typeof(string));

    await Task.CompletedTask;
  }
}

enum TestMode
{
  Fast,
  Standard,
  Slow
}
