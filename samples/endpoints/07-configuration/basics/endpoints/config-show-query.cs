using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using TimeWarp.Nuru;
using static System.Console;

[NuruRoute("config-show", Description = "Show current configuration values")]
public sealed class ConfigShowQuery : IQuery<Unit>
{
  public sealed class Handler(
    IOptions<DatabaseOptions> dbOptions,
    IOptions<ApiSettings> apiOptions,
    IConfiguration config) : IQueryHandler<ConfigShowQuery, Unit>
  {
    public ValueTask<Unit> Handle(ConfigShowQuery query, CancellationToken ct)
    {
      WriteLine("\n=== Configuration Values ===");
      WriteLine($"App Name: {config["AppName"]}");
      WriteLine($"Environment: {config["Environment"]}");

      WriteLine("\nDatabase Configuration:");
      WriteLine($"  Host: {dbOptions.Value.Host}");
      WriteLine($"  Port: {dbOptions.Value.Port}");
      WriteLine($"  Database: {dbOptions.Value.DatabaseName}");
      WriteLine($"  Timeout: {dbOptions.Value.Timeout}s");

      WriteLine("\nAPI Configuration:");
      WriteLine($"  Base URL: {apiOptions.Value.BaseUrl}");
      WriteLine($"  Timeout: {apiOptions.Value.TimeoutSeconds}s");
      WriteLine($"  Retry Count: {apiOptions.Value.RetryCount}");

      return default;
    }
  }
}
