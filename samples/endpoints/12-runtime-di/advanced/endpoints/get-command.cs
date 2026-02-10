using TimeWarp.Nuru;
using static System.Console;

[NuruRoute("get", Description = "Get value (uses decorated repository with caching)")]
public sealed class GetCommand : ICommand<string>
{
  [Parameter] public string Key { get; set; } = "";

  public sealed class Handler(IRepository Repo) : ICommandHandler<GetCommand, string>
  {
    public async ValueTask<string> Handle(GetCommand c, CancellationToken ct)
    {
      WriteLine("=== First call (cache miss expected) ===");
      string result1 = await Repo.GetAsync(c.Key);

      WriteLine("\n=== Second call (cache hit expected) ===");
      string result2 = await Repo.GetAsync(c.Key);

      return result2;
    }
  }
}
