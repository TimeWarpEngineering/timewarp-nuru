namespace TimeWarp.Nuru;

/// <summary>
/// Service for providing application name.
/// </summary>
public class AppNameService(string name)
{
  public string Name { get; } = name;
}