using Microsoft.Extensions.Options;
using TimeWarp.Nuru;
using static System.Console;

[NuruRoute("test-connection", Description = "Test connection using validated settings")]
public sealed class TestConnectionCommand : ICommand<Unit>
{
  public sealed class Handler(IOptions<ValidatedSettings> settings) : ICommandHandler<TestConnectionCommand, Unit>
  {
    public async ValueTask<Unit> Handle(TestConnectionCommand command, CancellationToken ct)
    {
      ValidatedSettings s = settings.Value;

      WriteLine($"Connecting to {s.EndpointUrl}...");
      WriteLine($"  Timeout: {s.TimeoutMs}ms");
      WriteLine($"  Retries: {s.MaxRetries}");

      await Task.Delay(100, ct);

      WriteLine("✓ Connection successful!");
      return default;
    }
  }
}
