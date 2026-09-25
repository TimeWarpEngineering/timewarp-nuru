using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using TimeWarp.Mediator;
using TimeWarp.Nuru;
using static System.Console;

[NuruRoute("settings", Description = "Show all current settings")]
public sealed class ShowSettingsQuery : IQuery<Unit>
{
  public sealed class Handler(IOptions<ValidatedSettings> settings) : IQueryHandler<ShowSettingsQuery, Unit>
  {
    public Task<Unit> Handle(ShowSettingsQuery query, CancellationToken ct)
    {
      ValidatedSettings s = settings.Value;

      WriteLine("Current Settings:");
      WriteLine($"  ApiKey: {s.ApiKey}");
      WriteLine($"  TimeoutMs: {s.TimeoutMs}");
      WriteLine($"  MaxRetries: {s.MaxRetries}");
      WriteLine($"  EndpointUrl: {s.EndpointUrl}");
      WriteLine($"  Environment: {s.Environment}");
      WriteLine($"  Tags: [{string.Join(", ", s.Tags)}]");

      return Unit.Task;
    }
  }
}
