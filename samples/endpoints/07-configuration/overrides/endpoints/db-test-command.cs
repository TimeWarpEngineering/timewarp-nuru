using Microsoft.Extensions.Configuration;
using TimeWarp.Nuru;
using static System.Console;

[NuruRoute("db-test", Description = "Test database connection with config")]
public sealed class DbTestCommand : ICommand<Unit>
{
  public sealed class Handler(IConfiguration config) : ICommandHandler<DbTestCommand, Unit>
  {
    public ValueTask<Unit> Handle(DbTestCommand command, CancellationToken ct)
    {
      string host = config["Database:Host"] ?? "localhost";
      string port = config["Database:Port"] ?? "5432";

      WriteLine($"Testing connection to {host}:{port}...");
      WriteLine("✓ Connection successful (simulated)");

      return default;
    }
  }
}
