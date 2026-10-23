// Probe: AddTypeConverter lets a route parameter bind to a custom type.
using TimeWarp.Nuru;

NuruApp app = NuruApp.CreateBuilder()
  .AddTypeConverter(new HexColorConverter())
  .Map("color {value:HexColor}")
    .WithHandler((HexColor value) => $"rgb({value.Red},{value.Green},{value.Blue})")
    .AsQuery()
    .Done()
  .Build();

return await app.RunAsync(args);

public sealed record HexColor(byte Red, byte Green, byte Blue);

public sealed class HexColorConverter : IRouteTypeConverter
{
  public Type TargetType => typeof(HexColor);
  public string? ConstraintAlias => "hexcolor";

  public bool TryConvert(string value, out object? result)
  {
    result = null;
    string hex = value.TrimStart('#');
    if (hex.Length != 6) return false;
    result = new HexColor(
      Convert.ToByte(hex[0..2], 16),
      Convert.ToByte(hex[2..4], 16),
      Convert.ToByte(hex[4..6], 16));
    return true;
  }
}
