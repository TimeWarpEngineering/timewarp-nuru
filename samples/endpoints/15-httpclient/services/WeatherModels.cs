// ═══════════════════════════════════════════════════════════════════════════════
// WEATHER MODELS
// ═══════════════════════════════════════════════════════════════════════════════
// DTOs for Open-Meteo API responses with AOT-compatible JSON serialization.

namespace HttpClientSample.Services;

using System.Text.Json.Serialization;

#region Design
// These models map directly to the Open-Meteo API JSON responses.
// Uses [JsonPropertyName] and [JsonSerializable] for AOT compatibility.
// The OpenMeteoJsonContext provides source-generated serialization.
#endregion

/// <summary>
/// Source-generated JSON serializer context for AOT-compatible deserialization.
/// </summary>
[JsonSerializable(typeof(GeocodingResponse))]
[JsonSerializable(typeof(WeatherResponse))]
public sealed partial class OpenMeteoJsonContext : JsonSerializerContext;

/// <summary>
/// Response from the Open-Meteo geocoding API.
/// </summary>
public sealed class GeocodingResponse
{
  [JsonPropertyName("results")]
  public List<GeocodingResult> Results { get; set; } = [];
}

/// <summary>
/// A single geocoding result containing location data.
/// </summary>
public sealed class GeocodingResult
{
  [JsonPropertyName("name")]
  public string Name { get; set; } = string.Empty;

  [JsonPropertyName("latitude")]
  public double Latitude { get; set; }

  [JsonPropertyName("longitude")]
  public double Longitude { get; set; }

  [JsonPropertyName("country")]
  public string? Country { get; set; }
}

/// <summary>
/// Response from the Open-Meteo weather forecast API.
/// </summary>
public sealed class WeatherResponse
{
  [JsonPropertyName("current")]
  public CurrentWeather Current { get; set; } = new();
}

/// <summary>
/// Current weather conditions.
/// </summary>
public sealed class CurrentWeather
{
  [JsonPropertyName("temperature_2m")]
  public double Temperature2m { get; set; }

  [JsonPropertyName("weather_code")]
  public int WeatherCode { get; set; }
}

/// <summary>
/// Aggregated weather result for display.
/// </summary>
public sealed class WeatherResult
{
  public string City { get; set; } = string.Empty;
  public string? Country { get; set; }
  public double TemperatureC { get; set; }
  public double TemperatureF => Math.Round(TemperatureC * 9 / 5 + 32, 1);
  public string Condition { get; set; } = string.Empty;
  public int WeatherCode { get; set; }
}
