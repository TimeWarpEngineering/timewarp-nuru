namespace TimeWarp.Nuru;

/// <summary>
/// Converts string values to booleans.
/// Supports: true/false, yes/no, 1/0, on/off, enabled/disabled
/// </summary>
public class BoolTypeConverter : IRouteTypeConverter
{
  private static readonly HashSet<string> TrueValues = new(StringComparer.OrdinalIgnoreCase)
    {
        CommonStrings.True, CommonStrings.Yes, CommonStrings.One, CommonStrings.On, CommonStrings.Enabled
    };

  private static readonly HashSet<string> FalseValues = new(StringComparer.OrdinalIgnoreCase)
    {
        CommonStrings.False, CommonStrings.No, CommonStrings.Zero, CommonStrings.Off, CommonStrings.Disabled
    };

  public Type TargetType => typeof(bool);
  public string? ConstraintAlias => null;

  public bool TryConvert(string value, out object? result)
  {
    if (TrueValues.Contains(value))
    {
      result = true;
      return true;
    }

    if (FalseValues.Contains(value))
    {
      result = false;
      return true;
    }

    result = null;
    return false;
  }
}
