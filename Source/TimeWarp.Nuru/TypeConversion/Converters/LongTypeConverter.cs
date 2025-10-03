namespace TimeWarp.Nuru;

/// <summary>
/// Converts string values to 64-bit integers.
/// </summary>
public class LongTypeConverter : IRouteTypeConverter
{
  public Type TargetType => typeof(long);
  public string ConstraintName => "long";

  public bool TryConvert(string value, out object? result)
  {
    if (long.TryParse(value, out long longValue))
    {
      result = longValue;
      return true;
    }

    result = null;
    return false;
  }
}
