using static System.Console;

public interface ILogger
{
  void Log(string message);
}

public class ConsoleLogger : ILogger
{
  public void Log(string message) => WriteLine($"[LOG] {message}");
}
