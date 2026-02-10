using System.Text.RegularExpressions;
using TimeWarp.Nuru;

public readonly record struct HexColor
{
  public string Value { get; }
  public byte R => Convert.ToByte(Value.Substring(1, 2), 16);
  public byte G => Convert.ToByte(Value.Substring(3, 2), 16);
  public byte B => Convert.ToByte(Value.Substring(5, 2), 16);

  public HexColor(string value)
  {
    if (!IsValid(value))
      throw new ArgumentException($"Invalid hex color: {value}");
    Value = value.ToLowerInvariant();
  }

  public static bool IsValid(string color)
  {
    if (string.IsNullOrWhiteSpace(color)) return false;
    return Regex.IsMatch(color, @"^#[0-9A-Fa-f]{6}$");
  }

  public override string ToString() => Value;
}

public class HexColorConverter : IRouteTypeConverter
{
  public Type TargetType => typeof(HexColor);
  public string? ConstraintAlias => "color";

  public bool TryConvert(string value, out object? result)
  {
    if (HexColor.IsValid(value))
    {
      result = new HexColor(value);
      return true;
    }

    result = null;
    return false;
  }
}
