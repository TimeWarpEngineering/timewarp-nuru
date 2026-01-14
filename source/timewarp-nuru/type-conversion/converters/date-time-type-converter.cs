namespace TimeWarp.Nuru;

/// <summary>
/// Converts string values to DateTime.
/// Supports ISO 8601 and other common formats.
/// </summary>
/// <remarks>
/// The primary constraint name is "DateTime" (type name).
/// </remarks>
public class DateTimeTypeConverter : IRouteTypeConverter
{
  public Type TargetType => typeof(DateTime);
  public string? ConstraintAlias => null;

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
