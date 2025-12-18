namespace AttributedRoutes.Messages;

using TimeWarp.Nuru;
using Mediator;
using static System.Console;

/// <summary>
/// Simple health check ping.
/// This is Unspecified ( ) - developer hasn't yet decided if it's a Query or Command.
/// Using IRequest indicates "TODO: classify this properly".
/// </summary>
[NuruRoute("ping", Description = "Simple health check")]
public sealed class PingRequest : IRequest
{
  public sealed class Handler : IRequestHandler<PingRequest>
  {
    public ValueTask<Unit> Handle(PingRequest request, CancellationToken ct)
    {
      WriteLine("pong");
      return default;
    }
  }
}
