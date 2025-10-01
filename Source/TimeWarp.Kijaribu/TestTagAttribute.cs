namespace TimeWarp.Kijaribu;

/// <summary>
/// Tags a test method for filtering and categorization.
/// Can be applied multiple times to add multiple tags.
/// </summary>
/// <param name="tag">The tag name (e.g., "Fast", "Parser", "Integration").</param>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true)]
public sealed class TestTagAttribute(string tag) : Attribute
{
  /// <summary>
  /// Gets the tag name.
  /// </summary>
  public string Tag { get; } = tag;
}
