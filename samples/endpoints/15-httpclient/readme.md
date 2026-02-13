# HttpClient Sample - Open-Meteo Weather API

This sample demonstrates using `AddHttpClient()` with Nuru's source-generated dependency injection - **no `UseMicrosoftDependencyInjection()` required**.

## What This Sample Demonstrates

- **Typed HttpClient Pattern**: Uses `AddHttpClient<TService, TImplementation>()` to register services
- **Source-Gen DI**: Works with Nuru's compile-time DI without runtime Microsoft DI container
- **Real API Integration**: Calls the free Open-Meteo Weather API
- **Async Query Handlers**: Shows proper async patterns with `IQuery<T>`
- **Error Handling**: Handles network failures, invalid cities, and API errors gracefully

## Running the Sample

### Using dotnet run

```bash
dotnet run samples/endpoints/15-httpclient/endpoint-httpclient-openmeteo.cs -- weather "London"
dotnet run samples/endpoints/15-httpclient/endpoint-httpclient-openmeteo.cs -- weather "Tokyo"
dotnet run samples/endpoints/15-httpclient/endpoint-httpclient-openmeteo.cs -- weather "New York"
```

### Using shebang (Linux/macOS)

```bash
chmod +x samples/endpoints/15-httpclient/endpoint-httpclient-openmeteo.cs
./samples/endpoints/15-httpclient/endpoint-httpclient-openmeteo.cs weather "Paris"
```

## Key Code Pattern

### Main Program

```csharp
NuruApp app = NuruApp.CreateBuilder()
  .WithName("Weather CLI")
  .ConfigureServices(services =>
  {
    // This works with source-gen DI! No UseMicrosoftDependencyInjection() needed.
    services.AddHttpClient<IOpenMeteoService, OpenMeteoService>(client =>
    {
      client.Timeout = TimeSpan.FromSeconds(30);
    });
  })
  .DiscoverEndpoints()
  .Build();
```

### Service Interface and Implementation

```csharp
public interface IOpenMeteoService
{
  Task<WeatherResult?> GetCurrentWeatherAsync(string city, CancellationToken ct);
}

public sealed class OpenMeteoService(HttpClient httpClient) : IOpenMeteoService
{
  private readonly HttpClient HttpClient = httpClient;
  
  public async Task<WeatherResult?> GetCurrentWeatherAsync(string city, CancellationToken ct)
  {
    // HttpClient is injected and pre-configured
    var response = await HttpClient.GetAsync(...);
    // ...
  }
}
```

### Query Handler with Injection

```csharp
[NuruRoute("weather {city}", Description = "Get current weather for a city")]
public sealed class CurrentWeatherQuery : IQuery<string>
{
  [Parameter(Description = "City name")]
  public string City { get; set; } = string.Empty;

  public sealed class Handler(IOpenMeteoService openMeteo) : IQueryHandler<CurrentWeatherQuery, string>
  {
    private readonly IOpenMeteoService OpenMeteo = openMeteo;

    public async ValueTask<string> Handle(CurrentWeatherQuery query, CancellationToken ct)
    {
      var weather = await OpenMeteo.GetCurrentWeatherAsync(query.City, ct);
      // Format and return result
    }
  }
}
```

## Why This Works Without UseMicrosoftDependencyInjection()

Nuru's source generator detects `AddHttpClient<TService, TImplementation>()` calls at compile time and:

1. **Extracts the registration**: Captures service type, implementation type, and configuration
2. **Generates DI code**: Creates optimized registration code in the generated `Program.g.cs`
3. **Handles HttpClient lifetime**: Manages HttpClient instances properly without runtime DI container

This gives you:
- ✅ AOT-compatible code (no reflection)
- ✅ Fast startup (no runtime service scanning)
- ✅ Familiar patterns (standard .NET `AddHttpClient()` API)

## API Details

The sample uses the [Open-Meteo API](https://open-meteo.com/):

1. **Geocoding**: `https://geocoding-api.open-meteo.com/v1/search?name={city}`
   - Converts city name to latitude/longitude
   
2. **Weather**: `https://api.open-meteo.com/v1/forecast?latitude={lat}&longitude={lon}&current=temperature_2m,weather_code`
   - Returns current temperature and weather condition code

No API key required - completely free to use.

## Error Handling

The sample handles:
- **Invalid city names**: Returns friendly "Could not find weather data" message
- **Network failures**: Catches `HttpRequestException` with clear error messages
- **API errors**: Checks HTTP status codes and JSON parsing
- **Cancellation**: Respects `CancellationToken` for graceful shutdown

## File Structure

```
/samples/endpoints/15-httpclient/
├── endpoint-httpclient-openmeteo.cs     # Main runfile
├── readme.md                            # This file
├── endpoints/
│   └── CurrentWeatherEndpoint.cs        # [NuruRoute("weather {city}")]
└── services/
    ├── IOpenMeteoService.cs             # Service interface
    ├── OpenMeteoService.cs              # Service implementation
    └── WeatherModels.cs                 # DTOs for API responses
```
