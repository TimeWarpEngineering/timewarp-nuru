using Microsoft.Extensions.Logging;
using Serilog;
using TimeWarp.Nuru;

[NuruRoute("order", Description = "Process an order with structured logging")]
public sealed class OrderCommand : ICommand<Unit>
{
  [Parameter] public string OrderId { get; set; } = "";
  [Parameter] public decimal Amount { get; set; }

  public sealed class Handler(ILogger<OrderCommand> Logger) : ICommandHandler<OrderCommand, Unit>
  {
    public ValueTask<Unit> Handle(OrderCommand c, CancellationToken ct)
    {
      using IDisposable? scope = Logger.BeginScope(new Dictionary<string, object>
      {
        ["OrderId"] = c.OrderId,
        ["Amount"] = c.Amount
      });

      Logger.LogInformation("Processing order {OrderId} for ${Amount}", c.OrderId, c.Amount);

      Logger.LogDebug("Validating order {OrderId}", c.OrderId);
      Thread.Sleep(50);

      Logger.LogDebug("Charging payment for order {OrderId}", c.OrderId);
      Thread.Sleep(50);

      Logger.LogInformation("Order {OrderId} completed successfully", c.OrderId);

      return default;
    }
  }
}
