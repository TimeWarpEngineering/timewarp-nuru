namespace TimeWarp.Nuru;

/// <summary>
/// Converts string values to double-precision floating point numbers.
/// </summary>
public class DoubleTypeConverter : IRouteTypeConverter
{
  public Type TargetType => typeof(double);
  public string ConstraintName => "double";

  public bool TryConvert(string value, out object? result)
  {
    if (double.TryParse(value, out double doubleValue))
    {
      result = doubleValue;
      return true;
    }

    result = null;
    return false;
  }
}
