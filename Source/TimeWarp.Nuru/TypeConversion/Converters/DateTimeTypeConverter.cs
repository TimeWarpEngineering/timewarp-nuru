namespace TimeWarp.Nuru.TypeConversion.Converters;

/// <summary>
/// Converts string values to DateTime.
/// Supports ISO 8601 and other common formats.
/// </summary>
public class DateTimeTypeConverter : IRouteTypeConverter
{
  public Type TargetType => typeof(DateTime);
  public string ConstraintName => "datetime";

  public bool TryConvert(string value, out object? result)
  {
    if (DateTime.TryParse(value, out DateTime dateTimeValue))
    {
      result = dateTimeValue;
      return true;
    }

    result = null;
    return false;
  }
}
