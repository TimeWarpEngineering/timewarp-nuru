namespace TimeWarp.Nuru;

/// <summary>
/// Converts string values to 32-bit integers.
/// </summary>
/// <remarks>
/// The primary constraint name is "int" (C# keyword).
/// Also supports "Int32" (CLR type name) via case-insensitive matching.
/// </remarks>
public class IntTypeConverter : IRouteTypeConverter
{
  public Type TargetType => typeof(int);
  public string? ConstraintAlias => null;

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
