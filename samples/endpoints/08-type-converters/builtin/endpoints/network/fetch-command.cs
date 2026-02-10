using TimeWarp.Nuru;
using static System.Console;

[NuruRoute("fetch", Description = "Fetch from a URI")]
public sealed class FetchCommand : ICommand<Unit>
{
  [Parameter] public Uri Url { get; set; } = new Uri("http://localhost");

  public sealed class Handler : ICommandHandler<FetchCommand, Unit>
  {
    public ValueTask<Unit> Handle(FetchCommand c, CancellationToken ct)
    {
      WriteLine($"Fetching: {c.Url}");
      WriteLine($"  Scheme: {c.Url.Scheme}");
      WriteLine($"  Host: {c.Url.Host}");
      WriteLine($"  Port: {c.Url.Port}");
      WriteLine($"  Path: {c.Url.AbsolutePath}");
      return default;
    }
  }
}
