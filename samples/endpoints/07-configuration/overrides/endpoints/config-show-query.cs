using Microsoft.Extensions.Configuration;
using TimeWarp.Nuru;
using static System.Console;

[NuruRoute("config-show", Description = "Show effective configuration after overrides")]
public sealed class ConfigShowQuery : IQuery<Unit>
{
  public sealed class Handler(IConfiguration config) : IQueryHandler<ConfigShowQuery, Unit>
  {
    public ValueTask<Unit> Handle(ConfigShowQuery query, CancellationToken ct)
    {
      WriteLine("\n=== Effective Configuration ===");
      WriteLine("(Values may be overridden via command line)\n");

      WriteLine("Database Settings:");
      WriteLine($"  Host: {config["Database:Host"] ?? "(not set)"}");
      WriteLine($"  Port: {config["Database:Port"] ?? "(not set)"}");
      WriteLine($"  Name: {config["Database:DatabaseName"] ?? "(not set)"}");

      WriteLine("\nAPI Settings:");
      WriteLine($"  BaseUrl: {config["Api:BaseUrl"] ?? "(not set)"}");
      WriteLine($"  TimeoutSeconds: {config["Api:TimeoutSeconds"] ?? "(not set)"}");
      WriteLine($"  RetryCount: {config["Api:RetryCount"] ?? "(not set)"}");

      WriteLine("\nLogging Settings:");
      WriteLine($"  Level: {config["Logging:LogLevel:Default"] ?? "(not set)"}");

      return default;
    }
  }
}
