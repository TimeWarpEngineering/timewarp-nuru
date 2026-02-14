// ═══════════════════════════════════════════════════════════════════════════════
// CURRENT WEATHER COMMAND
// ═══════════════════════════════════════════════════════════════════════════════
// Command endpoint to fetch and display current weather using Open-Meteo API.

namespace HttpClientSample.Endpoints;

using TimeWarp.Nuru;
using TimeWarp.Terminal;
using HttpClientSample.Services;

#region Design
// Demonstrates:
// - ICommand<Unit> pattern with typed handler
// - Service injection via constructor
// - TimeWarp.Terminal Table widget for formatted output
// - Async ValueTask with CancellationToken
#endregion

[NuruRoute("weather", Description = "Get current weather for a city")]
public sealed class CurrentWeatherCommand : ICommand<Unit>
{
  [Parameter(Description = "City name")]
  public string City { get; set; } = string.Empty;

  public sealed class Handler(IOpenMeteoService openMeteo) : ICommandHandler<CurrentWeatherCommand, Unit>
  {
    private readonly IOpenMeteoService OpenMeteo = openMeteo;

    public async ValueTask<Unit> Handle(CurrentWeatherCommand command, CancellationToken ct)
    {
      try
      {
        WeatherResult? weather = await OpenMeteo.GetCurrentWeatherAsync(command.City, ct);

        if (weather is null)
        {
          Terminal.WriteLine($"❌ Could not find weather data for '{command.City}'. Please check the city name and try again.".Red());
          return default;
        }

        string location = weather.Country is not null
          ? $"{weather.City}, {weather.Country}"
          : weather.City;

        Terminal.WriteRule(rule => rule
          .Title("🌤️ Weather Report".Cyan().Bold())
          .Style(LineStyle.Doubled));

        Terminal.WriteTable(table => table
          .AddColumn("Property")
          .AddColumn("Value")
          .AddRow("📍 Location".Bold(), location)
          .AddRow("🌡️ Temperature".Bold(), $"{weather.TemperatureC:F1}°C ({weather.TemperatureF:F1}°F)")
          .AddRow("☁️ Condition".Bold(), weather.Condition)
          .Border(BorderStyle.Rounded)
          .HideHeaders());

        Terminal.WriteRule(rule => rule
          .Style(LineStyle.Doubled));
      }
      catch (OperationCanceledException)
      {
        Terminal.WriteLine("⏱️ Request cancelled.".Yellow());
      }
      catch (InvalidOperationException ex)
      {
        Terminal.WriteLine($"❌ Error: {ex.Message}".Red());
      }

      return default;
    }
  }
}
