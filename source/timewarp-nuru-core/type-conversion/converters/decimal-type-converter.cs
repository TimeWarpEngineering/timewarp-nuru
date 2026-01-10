namespace TimeWarp.Nuru;

/// <summary>
/// Converts string values to decimal numbers.
/// </summary>
public class DecimalTypeConverter : IRouteTypeConverter
{
  public Type TargetType => typeof(decimal);
  public string? ConstraintAlias => null;

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
