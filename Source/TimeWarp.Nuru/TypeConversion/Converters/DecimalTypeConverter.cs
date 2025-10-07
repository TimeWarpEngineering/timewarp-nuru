namespace TimeWarp.Nuru;

/// <summary>
/// Converts string values to decimal numbers.
/// </summary>
public class DecimalTypeConverter : IRouteTypeConverter
{
  public Type TargetType => typeof(decimal);
  public string ConstraintName => "decimal";

  public bool TryConvert(string value, out object? result)
  {
    if (decimal.TryParse(value, out decimal decimalValue))
    {
      result = decimalValue;
      return true;
    }

    result = null;
    return false;
  }
}
