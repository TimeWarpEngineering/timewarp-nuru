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
    using TestTerminal terminal = new();
    NuruCoreApp app = NuruApp.CreateBuilder([])
      .UseTerminal(terminal)
      .Map("connect {dataSource}").WithHandler((string dataSource) => $"ds:{dataSource}")
      .AsCommand().Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["connect", "//0.0.0.0:1521/test_db"]);

    // Assert
    exitCode.ShouldBe(0);
    terminal.OutputContains("ds://0.0.0.0:1521/test_db").ShouldBeTrue();
  }

  public static async Task Should_option_argument_with_colon_connection_string()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruCoreApp app = NuruApp.CreateBuilder([])
      .UseTerminal(terminal)
      .Map("connect --data-source {dataSource}").WithHandler((string dataSource) => $"ds:{dataSource}")
      .AsCommand().Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["connect", "--data-source", "//0.0.0.0:1521/test_db"]);

    // Assert
    exitCode.ShouldBe(0);
    terminal.OutputContains("ds://0.0.0.0:1521/test_db").ShouldBeTrue();
  }

  public static async Task Should_multiple_parameters_with_colon_in_connection_string()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruCoreApp app = NuruApp.CreateBuilder([])
      .UseTerminal(terminal)
      .Map("deploy {env} {connectionString}").WithHandler((string env, string connectionString) => $"env:{env}|conn:{connectionString}")
      .AsCommand().Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["deploy", "production", "Server=localhost:5432;Database=mydb"]);

    // Assert
    exitCode.ShouldBe(0);
    terminal.OutputContains("env:production|conn:Server=localhost:5432;Database=mydb").ShouldBeTrue();
  }

  public static async Task Should_url_with_port_as_parameter()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruCoreApp app = NuruApp.CreateBuilder([])
      .UseTerminal(terminal)
      .Map("fetch {url}").WithHandler((string url) => $"url:{url}")
      .AsQuery().Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["fetch", "https://api.example.com:8080/data"]);

    // Assert
    exitCode.ShouldBe(0);
    terminal.OutputContains("url:https://api.example.com:8080/data").ShouldBeTrue();
  }

  public static async Task Should_filter_configuration_override_with_colon()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruCoreApp app = NuruApp.CreateBuilder([])
      .UseTerminal(terminal)
      .Map("connect {dataSource}").WithHandler((string dataSource) => $"ds:{dataSource}")
      .AsCommand().Done()
      .Build();

    // Act - Config override --Logging:LogLevel:Default=Debug should be filtered out
    int exitCode = await app.RunAsync(["connect", "//0.0.0.0:1521/test_db", "--Logging:LogLevel:Default=Debug"]);

    // Assert - Connection should succeed, config override should be ignored
    exitCode.ShouldBe(0);
    terminal.OutputContains("ds://0.0.0.0:1521/test_db").ShouldBeTrue();
  }

  public static async Task Should_allow_colon_in_catch_all_args()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruCoreApp app = NuruApp.CreateBuilder([])
      .UseTerminal(terminal)
      .Map("docker run {*args}").WithHandler((string[] args) => $"args:{string.Join(",", args)}")
      .AsCommand().Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["docker", "run", "nginx", "--port", "8080:80"]);

    // Assert
    exitCode.ShouldBe(0);
    terminal.OutputContains("args:nginx,--port,8080:80").ShouldBeTrue();
  }

  // SKIPPED: Requires alternative option-value separator support (task 023)
  [Skip("Requires alternative option-value separator support - see task 023")]
  public static async Task Should_not_filter_single_dash_with_colon()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruCoreApp app = NuruApp.CreateBuilder([])
      .UseTerminal(terminal)
      .Map("test -x {value}").WithHandler((string value) => $"value:{value}")
      .AsCommand().Done()
      .Build();

    // Act - Single dash option with colon in value should NOT be filtered
    int exitCode = await app.RunAsync(["test", "-x", "value"]);

    // Assert
    exitCode.ShouldBe(0);
    terminal.OutputContains("value:value").ShouldBeTrue();
  }

  public static async Task Should_filter_only_double_dash_config_overrides()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruCoreApp app = NuruApp.CreateBuilder([])
      .UseTerminal(terminal)
      .Map("run {param}").WithHandler((string param) => $"param:{param}")
      .AsCommand().Done()
      .Build();

    // Act - Multiple args with different colon patterns
    int exitCode = await app.RunAsync([
      "run",
      "host:port",           // Should NOT be filtered (no --)
      "--Config:Key=value"   // Should be filtered (config override)
    ]);

    // Assert
    exitCode.ShouldBe(0);
    terminal.OutputContains("param:host:port").ShouldBeTrue();
  }

  public static async Task Should_not_filter_option_with_url_containing_port()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruCoreApp app = NuruApp.CreateBuilder([])
      .UseTerminal(terminal)
      .Map("fetch --url {url}").WithHandler((string url) => $"url:{url}")
      .AsQuery().Done()
      .Build();

    // Act - Colon is in the option VALUE, not the config path structure
    int exitCode = await app.RunAsync(["fetch", "--url", "https://api.example.com:8080/data"]);

    // Assert
    exitCode.ShouldBe(0);
    terminal.OutputContains("url:https://api.example.com:8080/data").ShouldBeTrue();
  }

  public static async Task Should_not_filter_option_with_connection_string()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruCoreApp app = NuruApp.CreateBuilder([])
      .UseTerminal(terminal)
      .Map("connect --connection {conn}").WithHandler((string conn) => $"conn:{conn}")
      .AsCommand().Done()
      .Build();

    // Act - Colon is in the option VALUE (connection string), not config path
    int exitCode = await app.RunAsync(["connect", "--connection", "Server=localhost:5432;Database=mydb"]);

    // Assert
    exitCode.ShouldBe(0);
    terminal.OutputContains("conn:Server=localhost:5432;Database=mydb").ShouldBeTrue();
  }

  public static async Task Should_filter_nested_config_override()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruCoreApp app = NuruApp.CreateBuilder([])
      .UseTerminal(terminal)
      .Map("run {param}").WithHandler((string param) => $"param:{param}")
      .AsCommand().Done()
      .Build();

    // Act - Nested config override --Logging:LogLevel:Default=Debug should be filtered
    int exitCode = await app.RunAsync([
      "run",
      "myvalue",
      "--Logging:LogLevel:Default=Debug"  // Should be filtered (nested config path)
    ]);

    // Assert
    exitCode.ShouldBe(0);
    terminal.OutputContains("param:myvalue").ShouldBeTrue();
  }
}

} // namespace TimeWarp.Nuru.Tests.Routing
