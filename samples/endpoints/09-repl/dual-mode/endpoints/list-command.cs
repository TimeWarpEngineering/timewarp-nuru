using TimeWarp.Nuru;

[NuruRoute("list", Description = "List items")]
public sealed class ListCommand : IQuery<string[]>
{
  [Parameter] public int? Count { get; set; }

  public sealed class Handler : IQueryHandler<ListCommand, string[]>
  {
    public ValueTask<string[]> Handle(ListCommand q, CancellationToken ct)
    {
      int count = q.Count ?? 5;
      string[] items = Enumerable.Range(1, count)
        .Select(i => $"Item {i}")
        .ToArray();

      foreach (string item in items)
      {
        Console.WriteLine($"  {item}");
      }

      return new ValueTask<string[]>(items);
    }
  }
}
