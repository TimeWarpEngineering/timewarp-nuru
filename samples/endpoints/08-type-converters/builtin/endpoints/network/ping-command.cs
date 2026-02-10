using TimeWarp.Nuru;
using static System.Console;

[NuruRoute("ping", Description = "Ping an IP address")]
public sealed class PingCommand : ICommand<Unit>
{
  [Parameter] public string Addr { get; set; } = "127.0.0.1";

  public sealed class Handler : ICommandHandler<PingCommand, Unit>
  {
    public ValueTask<Unit> Handle(PingCommand c, CancellationToken ct)
    {
      System.Net.IPAddress addr = System.Net.IPAddress.Parse(c.Addr);
      WriteLine($"Pinging: {addr}");
      WriteLine($"  AddressFamily: {addr.AddressFamily}");
      return default;
    }
  }
}
