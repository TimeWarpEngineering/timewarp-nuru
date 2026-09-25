using TimeWarp.Mediator;
using TimeWarp.Nuru;

[NuruRoute("big-number", Description = "Work with large numbers (long)")]
public sealed class BigNumberCommand : IQuery<long>
{
  [Parameter] public long N { get; set; }

  public sealed class Handler : IQueryHandler<BigNumberCommand, long>
  {
    public Task<long> Handle(BigNumberCommand c, CancellationToken ct) =>
      Task.FromResult<long>(c.N * c.N);
  }
}
