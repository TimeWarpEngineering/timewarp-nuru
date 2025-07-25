namespace TimeWarp.Nuru.TypeConversion.Converters;

/// <summary>
/// Converts string values to booleans.
/// Supports: true/false, yes/no, 1/0, on/off, enabled/disabled
/// </summary>
public class BoolTypeConverter : IRouteTypeConverter
{
  private static readonly HashSet<string> TrueValues = new(StringComparer.OrdinalIgnoreCase)
    {
        "true", "yes", "1", "on", "enabled"
    };

  private static readonly HashSet<string> FalseValues = new(StringComparer.OrdinalIgnoreCase)
    {
        "false", "no", "0", "off", "disabled"
    };

  public Type TargetType => typeof(bool);
  public string ConstraintName => "bool";

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
