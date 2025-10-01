namespace TimeWarp.Kijaribu;

/// <summary>
/// Marks a test method to be skipped during test execution.
/// </summary>
/// <param name="reason">The reason this test is being skipped.</param>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public sealed class SkipAttribute(string reason) : Attribute
{
  /// <summary>
  /// Gets the reason this test is being skipped.
  /// </summary>
  public string Reason { get; } = reason;
}
