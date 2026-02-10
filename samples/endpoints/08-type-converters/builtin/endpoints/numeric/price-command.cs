using TimeWarp.Nuru;

[NuruRoute("price", Description = "Calculate with decimal precision")]
public sealed class PriceCommand : IQuery<decimal>
{
  [Parameter] public decimal Amount { get; set; }
  [Parameter] public decimal TaxRate { get; set; } = 0.08m;

  public sealed class Handler : IQueryHandler<PriceCommand, decimal>
  {
    public ValueTask<decimal> Handle(PriceCommand c, CancellationToken ct) =>
      new ValueTask<decimal>(c.Amount * (1 + c.TaxRate));
  }
}
