using TimeWarp.Mediator;
using TimeWarp.Nuru;

[NuruRoute("big-number", Description = "Work with large numbers (long)")]
public sealed class BigNumberCommand : IQuery<long>
{
  [Parameter] public long N { get; set; }

  public sealed class Handler : IRequestHandler<BigNumberCommand, long>
  {
    public ValueTask<long> Handle(BigNumberCommand c, CancellationToken ct) =>
      ValueTask.FromResult<long>(c.N * c.N);
  }
}
