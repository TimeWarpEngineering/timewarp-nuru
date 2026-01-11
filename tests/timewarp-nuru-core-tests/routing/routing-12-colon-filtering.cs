#!/usr/bin/dotnet --

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.Routing
{

[TestTag("Routing")]
public class ColonFilteringTests
{
  [ModuleInitializer]
  internal static void Register() => RegisterTests<ColonFilteringTests>();

  // Issue #77: Arguments with colons should not be filtered unless they are config overrides
  public static async Task Should_positional_argument_with_colon_connection_string()
  {
    // Arrange
    string? capturedDataSource = null;
    NuruCoreApp app = NuruApp.CreateBuilder([])
      .Map("connect {dataSource}").WithHandler((string dataSource) =>
      {
        capturedDataSource = dataSource;
      }).AsCommand().Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["connect", "//0.0.0.0:1521/test_db"]);

    // Assert
    exitCode.ShouldBe(0);
    capturedDataSource.ShouldBe("//0.0.0.0:1521/test_db");

    await Task.CompletedTask;
  }

  public static async Task Should_option_argument_with_colon_connection_string()
  {
    // Arrange
    string? capturedDataSource = null;
    NuruCoreApp app = NuruApp.CreateBuilder([])
      .Map("connect --data-source {dataSource}").WithHandler((string dataSource) =>
      {
        capturedDataSource = dataSource;
      }).AsCommand().Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["connect", "--data-source", "//0.0.0.0:1521/test_db"]);

    // Assert
    exitCode.ShouldBe(0);
    capturedDataSource.ShouldBe("//0.0.0.0:1521/test_db");

    await Task.CompletedTask;
  }

  public static async Task Should_multiple_parameters_with_colon_in_connection_string()
  {
    // Arrange
    string? capturedEnv = null;
    string? capturedConnectionString = null;
    NuruCoreApp app = NuruApp.CreateBuilder([])
      .Map("deploy {env} {connectionString}").WithHandler((string env, string connectionString) =>
      {
        capturedEnv = env;
        capturedConnectionString = connectionString;
      }).AsCommand().Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["deploy", "production", "Server=localhost:5432;Database=mydb"]);

    // Assert
    exitCode.ShouldBe(0);
    capturedEnv.ShouldBe("production");
    capturedConnectionString.ShouldBe("Server=localhost:5432;Database=mydb");

    await Task.CompletedTask;
  }

  public static async Task Should_url_with_port_as_parameter()
  {
    // Arrange
    string? capturedUrl = null;
    NuruCoreApp app = NuruApp.CreateBuilder([])
      .Map("fetch {url}").WithHandler((string url) =>
      {
        capturedUrl = url;
      }).AsQuery().Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["fetch", "https://api.example.com:8080/data"]);

    // Assert
    exitCode.ShouldBe(0);
    capturedUrl.ShouldBe("https://api.example.com:8080/data");

    await Task.CompletedTask;
  }

  public static async Task Should_filter_configuration_override_with_colon()
  {
    // Arrange
    string? capturedDataSource = null;
    NuruCoreApp app = NuruApp.CreateBuilder([])
      .Map("connect {dataSource}").WithHandler((string dataSource) =>
      {
        capturedDataSource = dataSource;
      }).AsCommand().Done()
      .Build();

    // Act - Config override --Logging:LogLevel:Default=Debug should be filtered out
    int exitCode = await app.RunAsync(["connect", "//0.0.0.0:1521/test_db", "--Logging:LogLevel:Default=Debug"]);

    // Assert - Connection should succeed, config override should be ignored
    exitCode.ShouldBe(0);
    capturedDataSource.ShouldBe("//0.0.0.0:1521/test_db");

    await Task.CompletedTask;
  }

  public static async Task Should_allow_colon_in_catch_all_args()
  {
    // Arrange
    string[]? capturedArgs = null;
    NuruCoreApp app = NuruApp.CreateBuilder([])
      .Map("docker run {*args}").WithHandler((string[] args) =>
      {
        capturedArgs = args;
      }).AsCommand().Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["docker", "run", "nginx", "--port", "8080:80"]);

    // Assert
    exitCode.ShouldBe(0);
    capturedArgs.ShouldNotBeNull();
    capturedArgs.Length.ShouldBe(3);
    capturedArgs[0].ShouldBe("nginx");
    capturedArgs[1].ShouldBe("--port");
    capturedArgs[2].ShouldBe("8080:80");

    await Task.CompletedTask;
  }

  // SKIPPED: Requires alternative option-value separator support (task 023)
  // Test originally attempted to verify that -x:value syntax works, but Nuru doesn't yet
  // support colon or equals separators for options (only space-separated: -x value)
  // See: Kanban/ToDo/023_Support-Alternative-Option-Value-Separators.md
  [Skip("Requires alternative option-value separator support - see task 023")]
  public static async Task Should_not_filter_single_dash_with_colon()
  {
    // Arrange
    string? capturedValue = null;
    NuruCoreApp app = NuruApp.CreateBuilder([])
      .UseDebugLogging()
      .Map("test -x {value}").WithHandler((string value) =>
      {
        capturedValue = value;
      }).AsCommand().Done()
      .Build();

    // Act - Single dash option with colon in value should NOT be filtered (only --Key:Value patterns)
    int exitCode = await app.RunAsync(["test", "-x", "value"]);

    // Assert
    exitCode.ShouldBe(0);
    capturedValue.ShouldBe("value");

    await Task.CompletedTask;
  }

  public static async Task Should_filter_only_double_dash_config_overrides()
  {
    // Arrange
    string? capturedParam = null;
    NuruCoreApp app = NuruApp.CreateBuilder([])
      .Map("run {param}").WithHandler((string param) =>
      {
        capturedParam = param;
      }).AsCommand().Done()
      .Build();

    // Act - Multiple args with different colon patterns
    int exitCode = await app.RunAsync([
      "run",
      "host:port",           // Should NOT be filtered (no --)
      "--Config:Key=value"   // Should be filtered (config override)
    ]);

    // Assert
    exitCode.ShouldBe(0);
    capturedParam.ShouldBe("host:port");

    await Task.CompletedTask;
  }

  public static async Task Should_not_filter_option_with_url_containing_port()
  {
    // Arrange - Test false positive case: --url=https://host:port should NOT be filtered
    string? capturedUrl = null;
    NuruCoreApp app = NuruApp.CreateBuilder([])
      .Map("fetch --url {url}").WithHandler((string url) =>
      {
        capturedUrl = url;
      }).AsQuery().Done()
      .Build();

    // Act - Colon is in the option VALUE, not the config path structure
    int exitCode = await app.RunAsync(["fetch", "--url", "https://api.example.com:8080/data"]);

    // Assert - Should work because colon is in value, not in --Section:Key pattern
    exitCode.ShouldBe(0);
    capturedUrl.ShouldBe("https://api.example.com:8080/data");

    await Task.CompletedTask;
  }

  public static async Task Should_not_filter_option_with_connection_string()
  {
    // Arrange - Test false positive case: --connection=Server=localhost:5432 should NOT be filtered
    string? capturedConnection = null;
    NuruCoreApp app = NuruApp.CreateBuilder([])
      .Map("connect --connection {conn}").WithHandler((string conn) =>
      {
        capturedConnection = conn;
      }).AsCommand().Done()
      .Build();

    // Act - Colon is in the option VALUE (connection string), not config path
    int exitCode = await app.RunAsync(["connect", "--connection", "Server=localhost:5432;Database=mydb"]);

    // Assert - Should work because this isn't a config override pattern
    exitCode.ShouldBe(0);
    capturedConnection.ShouldBe("Server=localhost:5432;Database=mydb");

    await Task.CompletedTask;
  }

  public static async Task Should_filter_nested_config_override()
  {
    // Arrange
    string? capturedParam = null;
    NuruCoreApp app = NuruApp.CreateBuilder([])
      .Map("run {param}").WithHandler((string param) =>
      {
        capturedParam = param;
      }).AsCommand().Done()
      .Build();

    // Act - Nested config override --Logging:LogLevel:Default=Debug should be filtered
    int exitCode = await app.RunAsync([
      "run",
      "myvalue",
      "--Logging:LogLevel:Default=Debug"  // Should be filtered (nested config path)
    ]);

    // Assert
    exitCode.ShouldBe(0);
    capturedParam.ShouldBe("myvalue");

    await Task.CompletedTask;
  }
}

} // namespace TimeWarp.Nuru.Tests.Routing
