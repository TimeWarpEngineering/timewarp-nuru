#!/usr/bin/dotnet --

return await RunTests<ColonFilteringTests>(clearCache: true);

[TestTag("Routing")]
[ClearRunfileCache]
public class ColonFilteringTests
{
  // Issue #77: Arguments with colons should not be filtered unless they are config overrides
  public static async Task Should_positional_argument_with_colon_connection_string()
  {
    // Arrange
    string? capturedDataSource = null;
    NuruApp app = new NuruAppBuilder()
      .Map("connect {dataSource}", (string dataSource) =>
      {
        capturedDataSource = dataSource;
        return 0;
      })
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
    NuruApp app = new NuruAppBuilder()
      .Map("connect --data-source {dataSource}", (string dataSource) =>
      {
        capturedDataSource = dataSource;
        return 0;
      })
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
    NuruApp app = new NuruAppBuilder()
      .Map("deploy {env} {connectionString}", (string env, string connectionString) =>
      {
        capturedEnv = env;
        capturedConnectionString = connectionString;
        return 0;
      })
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
    NuruApp app = new NuruAppBuilder()
      .Map("fetch {url}", (string url) =>
      {
        capturedUrl = url;
        return 0;
      })
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
    NuruApp app = new NuruAppBuilder()
      .Map("connect {dataSource}", (string dataSource) =>
      {
        capturedDataSource = dataSource;
        return 0;
      })
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
    NuruApp app = new NuruAppBuilder()
      .Map("docker run {*args}", (string[] args) =>
      {
        capturedArgs = args;
        return 0;
      })
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
    NuruApp app = new NuruAppBuilder()
      .UseDebugLogging()
      .Map("test -x {value}", (string value) =>
      {
        capturedValue = value;
        return 0;
      })
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
    NuruApp app = new NuruAppBuilder()
      .Map("run {param}", (string param) =>
      {
        capturedParam = param;
        return 0;
      })
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
    NuruApp app = new NuruAppBuilder()
      .Map("fetch --url {url}", (string url) =>
      {
        capturedUrl = url;
        return 0;
      })
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
    NuruApp app = new NuruAppBuilder()
      .Map("connect --connection {conn}", (string conn) =>
      {
        capturedConnection = conn;
        return 0;
      })
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
    NuruApp app = new NuruAppBuilder()
      .Map("run {param}", (string param) =>
      {
        capturedParam = param;
        return 0;
      })
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
