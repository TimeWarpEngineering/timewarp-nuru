namespace TimeWarp.Nuru;

/// <summary>
/// Service for providing application description.
/// </summary>
internal class AppDescriptionService(string description)
{
  public string Description { get; } = description;
}