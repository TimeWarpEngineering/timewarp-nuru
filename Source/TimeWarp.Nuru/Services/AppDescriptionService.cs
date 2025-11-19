namespace TimeWarp.Nuru;

/// <summary>
/// Service for providing application description.
/// </summary>
public class AppDescriptionService(string description)
{
  public string Description { get; } = description;
}