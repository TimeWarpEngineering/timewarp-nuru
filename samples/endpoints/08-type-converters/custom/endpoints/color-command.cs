using TimeWarp.Nuru;
using TimeWarp.Terminal;
using static System.Console;

[NuruRoute("color", Description = "Set theme color")]
public sealed class ColorCommand : ICommand<Unit>
{
  [Parameter] public string Primary { get; set; } = "#FF5733";
  [Parameter] public string? Secondary { get; set; }

  public sealed class Handler : ICommandHandler<ColorCommand, Unit>
  {
    public ValueTask<Unit> Handle(ColorCommand c, CancellationToken ct)
    {
      HexColor primary = new HexColor(c.Primary);
      WriteLine("Theme colors:");
      WriteLine($"  Primary: {primary}");
      WriteLine($"    RGB: ({primary.R}, {primary.G}, {primary.B})");

      if (!string.IsNullOrEmpty(c.Secondary))
      {
        HexColor secondary = new HexColor(c.Secondary);
        WriteLine($"  Secondary: {secondary}");
        WriteLine($"    RGB: ({secondary.R}, {secondary.G}, {secondary.B})");
      }

      return default;
    }
  }
}
