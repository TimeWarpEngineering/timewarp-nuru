#!/usr/bin/dotnet --

return await RunTests<EndpointMatchingTests>(clearCache: true);

[TestTag("Completion")]
[ClearRunfileCache]
public class EndpointMatchingTests
{
  public static async Task Should_match_route_with_multiple_literals()
  {
    // Arrange
    var builder = new NuruAppBuilder();
    builder.AddRoute("git remote add {name} {url}", (string name, string url) => 0);

    string[] typedWords = ["git", "remote", "add"];
    Endpoint endpoint = builder.EndpointCollection.First();

    // Act
    bool result = DynamicCompletionHandler.TryMatchEndpoint(endpoint, typedWords, out string? paramName, out Type? paramType);

    // Assert
    result.ShouldBeTrue();
    paramName.ShouldBe("name");
    paramType.ShouldBe(typeof(string));

    await Task.CompletedTask;
  }

  public static async Task Should_match_third_parameter_in_sequence()
  {
    // Arrange
    var builder = new NuruAppBuilder();
    builder.AddRoute("deploy {env} {version} {tag}", (string env, string version, string tag) => 0);

    string[] typedWords = ["deploy", "production", "v1.0"];
    Endpoint endpoint = builder.EndpointCollection.First();

    // Act
    bool result = DynamicCompletionHandler.TryMatchEndpoint(endpoint, typedWords, out string? paramName, out Type? paramType);

    // Assert
    result.ShouldBeTrue();
    paramName.ShouldBe("tag");
    paramType.ShouldBe(typeof(string));

    await Task.CompletedTask;
  }

  public static async Task Should_return_false_when_all_parameters_filled()
  {
    // Arrange
    var builder = new NuruAppBuilder();
    builder.AddRoute("greet {name}", (string name) => 0);

    string[] typedWords = ["greet", "alice"];
    Endpoint endpoint = builder.EndpointCollection.First();

    // Act
    bool result = DynamicCompletionHandler.TryMatchEndpoint(endpoint, typedWords, out string? paramName, out Type? paramType);

    // Assert
    result.ShouldBeFalse();
    paramName.ShouldBeNull();
    paramType.ShouldBeNull();

    await Task.CompletedTask;
  }

  public static async Task Should_match_catchall_parameter()
  {
    // Arrange
    var builder = new NuruAppBuilder();
    builder.AddRoute("docker {*args}", (string[] args) => 0);

    string[] typedWords = ["docker"];
    Endpoint endpoint = builder.EndpointCollection.First();

    // Act
    bool result = DynamicCompletionHandler.TryMatchEndpoint(endpoint, typedWords, out string? paramName, out Type? paramType);

    // Assert
    result.ShouldBeTrue();
    paramName.ShouldBe("args");
    paramType.ShouldBe(typeof(string[]));

    await Task.CompletedTask;
  }

  public static async Task Should_match_typed_double_parameter()
  {
    // Arrange
    var builder = new NuruAppBuilder();
    builder.AddRoute("scale {factor:double}", (double factor) => 0);

    string[] typedWords = ["scale"];
    Endpoint endpoint = builder.EndpointCollection.First();

    // Act
    bool result = DynamicCompletionHandler.TryMatchEndpoint(endpoint, typedWords, out string? paramName, out Type? paramType);

    // Assert
    result.ShouldBeTrue();
    paramName.ShouldBe("factor");
    paramType.ShouldBe(typeof(double));

    await Task.CompletedTask;
  }

  public static async Task Should_match_typed_bool_parameter()
  {
    // Arrange
    var builder = new NuruAppBuilder();
    builder.AddRoute("enable {flag:bool}", (bool flag) => 0);

    string[] typedWords = ["enable"];
    Endpoint endpoint = builder.EndpointCollection.First();

    // Act
    bool result = DynamicCompletionHandler.TryMatchEndpoint(endpoint, typedWords, out string? paramName, out Type? paramType);

    // Assert
    result.ShouldBeTrue();
    paramName.ShouldBe("flag");
    paramType.ShouldBe(typeof(bool));

    await Task.CompletedTask;
  }

  public static async Task Should_match_mixed_literal_and_option_route()
  {
    // Arrange
    var builder = new NuruAppBuilder();
    builder.AddRoute("git commit --message,-m {message} --amend", (string message, bool amend) => 0);

    string[] typedWords = ["git", "commit", "-m"];
    Endpoint endpoint = builder.EndpointCollection.First();

    // Act
    bool result = DynamicCompletionHandler.TryMatchEndpoint(endpoint, typedWords, out string? paramName, out Type? paramType);

    // Assert
    result.ShouldBeTrue();
    paramName.ShouldBe("message");
    paramType.ShouldBe(typeof(string));

    await Task.CompletedTask;
  }

  public static async Task Should_return_false_for_partial_literal_match()
  {
    // Arrange
    var builder = new NuruAppBuilder();
    builder.AddRoute("git remote add {name}", (string name) => 0);

    string[] typedWords = ["git", "remote"]; // Missing "add"
    Endpoint endpoint = builder.EndpointCollection.First();

    // Act
    bool result = DynamicCompletionHandler.TryMatchEndpoint(endpoint, typedWords, out string? paramName, out Type? paramType);

    // Assert
    result.ShouldBeFalse(); // Next segment is literal "add", not a parameter

    await Task.CompletedTask;
  }

  public static async Task Should_match_optional_parameter_when_previous_filled()
  {
    // Arrange
    var builder = new NuruAppBuilder();
    builder.AddRoute("deploy {env} {tag?} {note?}", (string env, string? tag, string? note) => 0);

    string[] typedWords = ["deploy", "production", "v1.0"];
    Endpoint endpoint = builder.EndpointCollection.First();

    // Act
    bool result = DynamicCompletionHandler.TryMatchEndpoint(endpoint, typedWords, out string? paramName, out Type? paramType);

    // Assert
    result.ShouldBeTrue();
    paramName.ShouldBe("note");
    paramType.ShouldBe(typeof(string));

    await Task.CompletedTask;
  }

  public static async Task Should_handle_empty_typed_words()
  {
    // Arrange
    var builder = new NuruAppBuilder();
    builder.AddRoute("greet {name}", (string name) => 0);

    string[] typedWords = [];
    Endpoint endpoint = builder.EndpointCollection.First();

    // Act
    bool result = DynamicCompletionHandler.TryMatchEndpoint(endpoint, typedWords, out string? paramName, out Type? paramType);

    // Assert
    result.ShouldBeFalse(); // No words typed, expecting literal "greet" first

    await Task.CompletedTask;
  }

  public static async Task Should_match_option_with_long_form()
  {
    // Arrange
    var builder = new NuruAppBuilder();
    builder.AddRoute("build --configuration,-c {mode}", (string mode) => 0);

    string[] typedWords = ["build", "--configuration"];
    Endpoint endpoint = builder.EndpointCollection.First();

    // Act
    bool result = DynamicCompletionHandler.TryMatchEndpoint(endpoint, typedWords, out string? paramName, out Type? paramType);

    // Assert
    result.ShouldBeTrue();
    paramName.ShouldBe("mode");
    paramType.ShouldBe(typeof(string));

    await Task.CompletedTask;
  }

  public static async Task Should_return_false_when_option_value_already_provided()
  {
    // Arrange
    var builder = new NuruAppBuilder();
    builder.AddRoute("build --config {mode}", (string mode) => 0);

    string[] typedWords = ["build", "--config", "Release"];
    Endpoint endpoint = builder.EndpointCollection.First();

    // Act
    bool result = DynamicCompletionHandler.TryMatchEndpoint(endpoint, typedWords, out string? paramName, out Type? paramType);

    // Assert
    result.ShouldBeFalse(); // Option value already filled

    await Task.CompletedTask;
  }
}
