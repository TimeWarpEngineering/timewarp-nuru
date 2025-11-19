namespace TimeWarp.Nuru;

/// <summary>
/// Service for providing application name.
/// </summary>
internal class AppNameService(string name)
{
  public string Name { get; } = name;
}