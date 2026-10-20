namespace ReferencedContracts;

using TimeWarp.Contracts;
using TimeWarp.Mediator;

/// <summary>
/// Sums numbers and returns an object, so the CLI writes it as JSON.
/// The JSON context MSBuild task registers <see cref="SumResult"/> for this referenced endpoint.
/// </summary>
[Operation("sum", Description = "Sum numbers")]
public sealed class SumQuery : IQuery<SumResult>
{
  public int[] Values { get; set; } = [];

  public sealed class Handler : IQueryHandler<SumQuery, SumResult>
  {
    public Task<SumResult> Handle(SumQuery query, CancellationToken cancellationToken)
    {
      _ = cancellationToken;
      return Task.FromResult(new SumResult { Count = query.Values.Length, Total = query.Values.Sum() });
    }
  }
}

public sealed class SumResult
{
  public int Count { get; set; }

  public int Total { get; set; }
}
