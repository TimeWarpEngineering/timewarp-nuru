// ═══════════════════════════════════════════════════════════════════════════════
// CURRENT WEATHER QUERY
// ═══════════════════════════════════════════════════════════════════════════════
// Query endpoint to fetch current weather for a city using Open-Meteo API.

namespace HttpClientSample.Endpoints;

using TimeWarp.Nuru;
using HttpClientSample.Services;

#region Design
// Demonstrates:
// - IQuery<T> pattern with typed handler
// - Service injection via constructor
// - Async ValueTask with CancellationToken
// - Pretty formatted output
#endregion

/// <summary>
/// Query to get current weather for a city.
/// </summary>
[NuruRoute("weather {city}", Description = "Get current weather for a city")]
public sealed class CurrentWeatherQuery : IQuery<string>
{
  /// <summary>
  /// City name to look up.
  /// </summary>
  [Parameter(Description = "City name")]
  public string City { get; set; } = string.Empty;

  /// <summary>
  /// Handler for CurrentWeatherQuery.
  /// </summary>
  public sealed class Handler(IOpenMeteoService openMeteo) : IQueryHandler<CurrentWeatherQuery, string>
  {
    private readonly IOpenMeteoService OpenMeteo = openMeteo;

    public async ValueTask<string> Handle(CurrentWeatherQuery query, CancellationToken ct)
    {
      try
      {
        WeatherResult? weather = await OpenMeteo.GetCurrentWeatherAsync(query.City, ct);

        if (weather is null)
        {
          return $"❌ Could not find weather data for '{query.City}'. Please check the city name and try again.";
        }

        string location = weather.Country is not null
          ? $"{weather.City}, {weather.Country}"
          : weather.City;

        return $"""
          ╔══════════════════════════════════════════════════════════╗
          ║                    🌤️  WEATHER REPORT                     ║
          ╠══════════════════════════════════════════════════════════╣
          ║  📍 Location:    {location,-40}║
          ║  🌡️  Temperature: {weather.TemperatureC:F1}°C ({weather.TemperatureF:F1}°F)      ║
          ║  ☁️  Condition:   {weather.Condition,-40}║
          ╚══════════════════════════════════════════════════════════╝
          """;
      }
      catch (OperationCanceledException)
      {
        return "⏱️ Request cancelled.";
      }
      catch (InvalidOperationException ex)
      {
        return $"❌ Error: {ex.Message}";
      }
    }
  }
}
