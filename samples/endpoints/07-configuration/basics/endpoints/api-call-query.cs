using Microsoft.Extensions.Options;
using TimeWarp.Mediator;
using TimeWarp.Nuru;
using static System.Console;

[NuruRoute("api-call", Description = "Call API endpoint using config")]
public sealed class ApiCallQuery : IQuery<Unit>
{
  [Parameter(Description = "API endpoint to call")]
  public string Endpoint { get; set; } = "";

  public sealed class Handler(IOptions<ApiSettings> apiOptions) : IQueryHandler<ApiCallQuery, Unit>
  {
    public Task<Unit> Handle(ApiCallQuery query, CancellationToken ct)
    {
      ApiSettings api = apiOptions.Value;
      string fullUrl = $"{api.BaseUrl}/{query.Endpoint}";

      WriteLine("Calling API endpoint...");
      WriteLine($"  URL: {fullUrl}");
      WriteLine($"  Timeout: {api.TimeoutSeconds}s");
      WriteLine($"  Max Retries: {api.RetryCount}");
      WriteLine("✓ API call successful (simulated)");
      return Unit.Task;
    }
  }
}
