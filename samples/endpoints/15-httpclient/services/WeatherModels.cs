// ═══════════════════════════════════════════════════════════════════════════════
// WEATHER MODELS
// ═══════════════════════════════════════════════════════════════════════════════
// DTOs for Open-Meteo API responses.

namespace HttpClientSample.Services;

#region Design
// These models map directly to the Open-Meteo API JSON responses.
// Using nullable reference types to handle missing data gracefully.
// Properties use nullable types where the API may not return values.
#endregion

/// <summary>
/// Response from the Open-Meteo geocoding API.
/// </summary>
public sealed class GeocodingResponse
{
  /// <summary>
  /// The list of matching locations.
  /// </summary>
  public List<GeocodingResult> Results { get; set; } = [];
}

/// <summary>
/// A single geocoding result containing location data.
/// </summary>
public sealed class GeocodingResult
{
  /// <summary>
  /// The location name.
  /// </summary>
  public string Name { get; set; } = string.Empty;

  /// <summary>
  /// Latitude of the location.
  /// </summary>
  public double Latitude { get; set; }

  /// <summary>
  /// Longitude of the location.
  /// </summary>
  public double Longitude { get; set; }

  /// <summary>
  /// Country where the location is.
  /// </summary>
  public string? Country { get; set; }
}

/// <summary>
/// Response from the Open-Meteo weather forecast API.
/// </summary>
public sealed class WeatherResponse
{
  /// <summary>
  /// Current weather data.
  /// </summary>
  public CurrentWeather Current { get; set; } = new();
}

/// <summary>
/// Current weather conditions.
/// </summary>
public sealed class CurrentWeather
{
  /// <summary>
  /// Temperature in Celsius.
  /// </summary>
  public double Temperature_2m { get; set; }

  /// <summary>
  /// WMO weather interpretation code.
  /// </summary>
  public int Weather_Code { get; set; }
}

/// <summary>
/// Aggregated weather result for display.
/// </summary>
public sealed class WeatherResult
{
  /// <summary>
  /// City name.
  /// </summary>
  public string City { get; set; } = string.Empty;

  /// <summary>
  /// Country name (if available).
  /// </summary>
  public string? Country { get; set; }

  /// <summary>
  /// Temperature in Celsius.
  /// </summary>
  public double TemperatureC { get; set; }

  /// <summary>
  /// Temperature in Fahrenheit.
  /// </summary>
  public double TemperatureF => Math.Round(TemperatureC * 9 / 5 + 32, 1);

  /// <summary>
  /// Weather condition description.
  /// </summary>
  public string Condition { get; set; } = string.Empty;

  /// <summary>
  /// Weather interpretation code from API.
  /// </summary>
  public int WeatherCode { get; set; }
}
