using TimeWarp.Mediator;
using TimeWarp.Nuru;
using static System.Console;

[NuruRoute("query", Description = "Query data from service")]
public sealed class QueryCommand : IQuery<string[]>
{
  [Parameter] public string Search { get; set; } = "";

  public sealed class Handler(IDataService Data) : IRequestHandler<QueryCommand, string[]>
  {
    public async ValueTask<string[]> Handle(QueryCommand q, CancellationToken ct)
    {
      WriteLine("=== Query Command ===");
      return await Data.GetDataAsync(q.Search);
    }
  }
}
