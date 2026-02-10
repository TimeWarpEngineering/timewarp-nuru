using Microsoft.Extensions.Options;
using TimeWarp.Nuru;
using static System.Console;

[NuruRoute("db-connect", Description = "Connect to database using config")]
public sealed class DbConnectCommand : ICommand<Unit>
{
  public sealed class Handler(IOptions<DatabaseOptions> dbOptions) : ICommandHandler<DbConnectCommand, Unit>
  {
    public ValueTask<Unit> Handle(DbConnectCommand command, CancellationToken ct)
    {
      DatabaseOptions db = dbOptions.Value;
      WriteLine("Connecting to database...");
      WriteLine($"  Server: {db.Host}:{db.Port}");
      WriteLine($"  Database: {db.DatabaseName}");
      WriteLine($"  Timeout: {db.Timeout}s");
      WriteLine("✓ Connected successfully (simulated)");
      return default;
    }
  }
}
