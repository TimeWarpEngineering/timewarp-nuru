// ═══════════════════════════════════════════════════════════════════════════════
// IOPENMETEO SERVICE INTERFACE
// ═══════════════════════════════════════════════════════════════════════════════
// Service interface for Open-Meteo weather API operations.

namespace HttpClientSample.Services;

/// <summary>
/// Interface for Open-Meteo weather API service.
/// </summary>
public interface IOpenMeteoService
{
  /// <summary>
  /// Gets current weather for a city.
  /// </summary>
  /// <param name="city">City name to look up.</param>
  /// <param name="ct">Cancellation token.</param>
  /// <returns>Weather result or null if not found.</returns>
  Task<WeatherResult?> GetCurrentWeatherAsync(string city, CancellationToken ct);
}
