using TimeWarp.Nuru;

[NuruRoute("big-number", Description = "Work with large numbers (long)")]
public sealed class BigNumberCommand : IQuery<long>
{
  [Parameter] public long N { get; set; }

  public sealed class Handler : IQueryHandler<BigNumberCommand, long>
  {
    public ValueTask<long> Handle(BigNumberCommand c, CancellationToken ct) =>
      new ValueTask<long>(c.N * c.N);
  }
}
