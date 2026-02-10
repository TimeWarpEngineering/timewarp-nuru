// ═══════════════════════════════════════════════════════════════════════════════
// DB SAVE COMMAND
// ═══════════════════════════════════════════════════════════════════════════════
// Save to database with retry on failure.

namespace PipelineRetry.Endpoints;

using PipelineRetry.Behaviors;
using TimeWarp.Nuru;

[NuruRoute("db-save", Description = "Save to database with retry on failure")]
public sealed class DbSaveCommand : ICommand<Unit>, IRetryable
{
  [Parameter(Description = "Data to save")]
  public string Data { get; set; } = string.Empty;

  public int MaxRetries => 5;

  public sealed class Handler : ICommandHandler<DbSaveCommand, Unit>
  {
    private static int FailureCount = 0;

    public ValueTask<Unit> Handle(DbSaveCommand command, CancellationToken ct)
    {
      FailureCount++;

      if (FailureCount < 4)
      {
        throw new IOException($"Database connection failed (attempt {FailureCount})");
      }

      Console.WriteLine($"✓ Saved '{command.Data}' to database after {FailureCount} attempts");
      FailureCount = 0;
      return default;
    }
  }
}
