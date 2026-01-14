namespace TimeWarp.Nuru;

/// <summary>
/// Converts string values to GUIDs.
/// </summary>
/// <remarks>
/// The primary constraint name is "Guid" (type name).
/// </remarks>
public class GuidTypeConverter : IRouteTypeConverter
{
  public Type TargetType => typeof(Guid);
  public string? ConstraintAlias => null;

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
