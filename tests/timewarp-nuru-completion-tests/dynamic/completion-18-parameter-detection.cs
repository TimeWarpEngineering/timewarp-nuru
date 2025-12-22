#!/usr/bin/dotnet --

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.Completion.ParameterDetection
{

[TestTag("Completion")]
public class ParameterDetectionTests
{
  [ModuleInitializer]
  internal static void Register() => RegisterTests<ParameterDetectionTests>();

  public static async Task Should_detect_first_positional_parameter()
  {
    // Arrange
    NuruAppBuilder builder = new();
    builder.Map("greet {name}").WithHandler((string name) => 0).AsQuery().Done();

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
    NuruAppBuilder builder = new();
    builder.Map("deploy {env} {tag}").WithHandler((string env, string tag) => 0).AsCommand().Done();

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
    NuruAppBuilder builder = new();
    builder.Map("deploy {env} {tag?}").WithHandler((string env, string? tag) => 0).AsCommand().Done();

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
    NuruAppBuilder builder = new();
    builder.Map("build --config {mode}").WithHandler((string mode) => 0).AsCommand().Done();

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
    NuruAppBuilder builder = new();
    builder.Map("connect {port:int}").WithHandler((int port) => 0).AsCommand().Done();

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
    NuruAppBuilder builder = new();
    builder.Map("deploy {env} --mode {mode}").WithHandler((string env, TestMode mode) => 0).AsCommand().Done();

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
    NuruAppBuilder builder = new();
    builder.Map("greet {name}").WithHandler((string name) => 0).AsQuery().Done();

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
    NuruAppBuilder builder = new();
    builder.Map("git status").WithHandler(() => 0).AsQuery().Done();

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
    NuruAppBuilder builder = new();
    builder.Map("build --config,-c {mode}").WithHandler((string mode) => 0).AsCommand().Done();

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
    NuruAppBuilder builder = new();
    builder.Map("greet {name}").WithHandler((string name) => 0).AsQuery().Done();

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

} // namespace TimeWarp.Nuru.Tests.Completion.ParameterDetection
