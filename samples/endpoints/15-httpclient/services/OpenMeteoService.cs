// ═══════════════════════════════════════════════════════════════════════════════
// OPENMETEO SERVICE
// ═══════════════════════════════════════════════════════════════════════════════
// Service implementation for Open-Meteo weather API using HttpClient.
// Demonstrates typed HttpClient pattern with Nuru's source-gen DI.

namespace HttpClientSample.Services;

using System.Text.Json;

#region Design
// Uses typed HttpClient pattern where HttpClient is injected via constructor.
// Configured via AddHttpClient<IOpenMeteoService, OpenMeteoService>() in main app.
// BaseAddress and timeout are pre-configured in the AddHttpClient lambda.
#endregion

/// <summary>
/// Open-Meteo weather API service implementation.
/// </summary>
public sealed class OpenMeteoService(HttpClient httpClient) : IOpenMeteoService
{
  private readonly HttpClient HttpClient = httpClient;

  public async Task<WeatherResult?> GetCurrentWeatherAsync(string city, CancellationToken ct)
  {
    // Step 1: Geocode the city name to get lat/long
    string geocodeUrl = $"https://geocoding-api.open-meteo.com/v1/search?name={Uri.EscapeDataString(city)}&count=1";

    HttpResponseMessage geocodeResponse;
    try
    {
      geocodeResponse = await HttpClient.GetAsync(geocodeUrl, ct);
    }
    catch (HttpRequestException ex)
    {
      throw new InvalidOperationException($"Failed to connect to geocoding service: {ex.Message}", ex);
    }

    if (!geocodeResponse.IsSuccessStatusCode)
    {
      throw new InvalidOperationException($"Geocoding API returned error: {geocodeResponse.StatusCode}");
    }

    string geocodeJson = await geocodeResponse.Content.ReadAsStringAsync(ct);
    GeocodingResponse? geocodingData;
    try
    {
      geocodingData = JsonSerializer.Deserialize<GeocodingResponse>(geocodeJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
    }
    catch (JsonException ex)
    {
      throw new InvalidOperationException($"Failed to parse geocoding response: {ex.Message}", ex);
    }

    if (geocodingData?.Results is null || geocodingData.Results.Count == 0)
    {
      return null;
    }

    GeocodingResult location = geocodingData.Results[0];

    // Step 2: Get weather for the coordinates
    string weatherUrl = $"https://api.open-meteo.com/v1/forecast?latitude={location.Latitude}&longitude={location.Longitude}&current=temperature_2m,weather_code";

    HttpResponseMessage weatherResponse;
    try
    {
      weatherResponse = await HttpClient.GetAsync(weatherUrl, ct);
    }
    catch (HttpRequestException ex)
    {
      throw new InvalidOperationException($"Failed to connect to weather service: {ex.Message}", ex);
    }

    if (!weatherResponse.IsSuccessStatusCode)
    {
      throw new InvalidOperationException($"Weather API returned error: {weatherResponse.StatusCode}");
    }

    string weatherJson = await weatherResponse.Content.ReadAsStringAsync(ct);
    WeatherResponse? weatherData;
    try
    {
      weatherData = JsonSerializer.Deserialize<WeatherResponse>(weatherJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
    }
    catch (JsonException ex)
    {
      throw new InvalidOperationException($"Failed to parse weather response: {ex.Message}", ex);
    }

    if (weatherData?.Current is null)
    {
      throw new InvalidOperationException("Weather API returned no current weather data.");
    }

    return new WeatherResult
    {
      City = location.Name,
      Country = location.Country,
      TemperatureC = weatherData.Current.Temperature_2m,
      WeatherCode = weatherData.Current.Weather_Code,
      Condition = GetWeatherDescription(weatherData.Current.Weather_Code)
    };
  }

  private static string GetWeatherDescription(int code)
  {
    return code switch
    {
      0 => "Clear sky",
      1 => "Mainly clear",
      2 => "Partly cloudy",
      3 => "Overcast",
      45 => "Fog",
      48 => "Depositing rime fog",
      51 => "Light drizzle",
      53 => "Moderate drizzle",
      55 => "Dense drizzle",
      61 => "Slight rain",
      63 => "Moderate rain",
      65 => "Heavy rain",
      71 => "Slight snow",
      73 => "Moderate snow",
      75 => "Heavy snow",
      95 => "Thunderstorm",
      96 => "Thunderstorm with slight hail",
      99 => "Thunderstorm with heavy hail",
      _ => "Unknown"
    };
  }
}
