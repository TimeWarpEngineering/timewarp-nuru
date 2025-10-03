namespace TimeWarp.Nuru;

/// <summary>
/// Converts string values to 32-bit integers.
/// </summary>
public class IntTypeConverter : IRouteTypeConverter
{
  public Type TargetType => typeof(int);
  public string ConstraintName => "int";

  public bool TryConvert(string value, out object? result)
  {
    if (int.TryParse(value, out int intValue))
    {
      result = intValue;
      return true;
    }

    result = null;
    return false;
  }
}
