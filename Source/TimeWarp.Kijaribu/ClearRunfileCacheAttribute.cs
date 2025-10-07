namespace TimeWarp.Kijaribu;

/// <summary>
/// Controls whether the .NET runfile cache should be cleared before running tests.
/// Runfile cache clearing ensures tests pick up latest source changes but adds overhead.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public sealed class ClearRunfileCacheAttribute(bool enabled = true) : Attribute
{
  public bool Enabled { get; } = enabled;
}
