namespace TimeWarp.Nuru;

/// <summary>
/// Converts string values to GUIDs.
/// </summary>
public class GuidTypeConverter : IRouteTypeConverter
{
  public Type TargetType => typeof(Guid);
  public string ConstraintName => "guid";

  public bool TryConvert(string value, out object? result)
  {
    if (Guid.TryParse(value, out Guid guidValue))
    {
      result = guidValue;
      return true;
    }

    result = null;
    return false;
  }
}
