using TimeWarp.Mediator;
using TimeWarp.Nuru;
using static System.Console;

[NuruRoute("session", Description = "Get current session info")]
public sealed class SessionQuery : IQuery<Unit>
{
  public sealed class Handler(ISession Session) : IRequestHandler<SessionQuery, Unit>
  {
    public ValueTask<Unit> Handle(SessionQuery q, CancellationToken ct)
    {
      WriteLine($"Session ID: {Session.SessionId}");
      WriteLine($"Created: {Session.CreatedAt:HH:mm:ss} UTC");
      return Unit.ValueTask;
    }
  }
}
