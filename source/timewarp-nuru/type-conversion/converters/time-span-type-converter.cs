namespace TimeWarp.Nuru;

/// <summary>
/// Converts string values to TimeSpan.
/// Supports formats like "1:30:00", "00:00:30", "1.05:00:00".
/// </summary>
/// <remarks>
/// The primary constraint name is "TimeSpan" (type name).
/// </remarks>
public class TimeSpanTypeConverter : IRouteTypeConverter
{
  public Type TargetType => typeof(TimeSpan);
  public string? ConstraintAlias => null;

  public bool TryConvert(string value, out object? result)
  {
    if (TimeSpan.TryParse(value, out TimeSpan timeSpanValue))
    {
      result = timeSpanValue;
      return true;
    }

    result = null;
    return false;
  }
}
