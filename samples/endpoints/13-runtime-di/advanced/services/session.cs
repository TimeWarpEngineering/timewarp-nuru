using static System.Console;

public interface ISession : IDisposable
{
  string SessionId { get; }
  DateTime CreatedAt { get; }
}

public class UserSession : ISession
{
  public string SessionId { get; } = Guid.NewGuid().ToString()[..8];
  public DateTime CreatedAt { get; } = DateTime.UtcNow;

  public void Dispose()
  {
    WriteLine($"[SESSION] Disposing session {SessionId}");
  }
}
