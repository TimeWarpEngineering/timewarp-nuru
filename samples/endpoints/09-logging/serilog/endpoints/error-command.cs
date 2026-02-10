using Microsoft.Extensions.Logging;
using TimeWarp.Nuru;

[NuruRoute("error", Description = "Simulate and log an error")]
public sealed class ErrorCommand : ICommand<Unit>
{
  [Parameter] public string Scenario { get; set; } = "";

  public sealed class Handler(ILogger<ErrorCommand> Logger) : ICommandHandler<ErrorCommand, Unit>
  {
    public ValueTask<Unit> Handle(ErrorCommand c, CancellationToken ct)
    {
      try
      {
        throw c.Scenario.ToLower() switch
        {
          "timeout" => new TimeoutException("Operation timed out"),
          "auth" => new UnauthorizedAccessException("Access denied"),
          _ => new InvalidOperationException($"Unknown error: {c.Scenario}")
        };
      }
      catch (Exception ex)
      {
        Logger.LogError(ex, "Error processing scenario: {Scenario}", c.Scenario);
        throw;
      }
    }
  }
}
