# Add HttpClient sample calling public API

## Description

Create a runnable sample demonstrating HttpClient usage in a Nuru CLI application. The sample should call a free public API and display the results. This fills a gap in our samples collection - we currently have no examples showing HTTP requests in actual runnable code.

This sample will demonstrate the idiomatic .NET 10 pattern of using `AddHttpClient()` with Nuru's source-gen DI - no `UseMicrosoftDependencyInjection()` required.

## Checklist

- [x] Create the sample in `/samples/endpoints/15-httpclient/` directory (Endpoint DSL)
- [x] Demonstrate AddHttpClient() registration with typed client
- [x] Show async handler patterns with injected HTTP service
- [x] Add proper error handling for HTTP failures
- [x] Add README.md explaining how to run the sample
- [x] Test the sample locally to ensure it works
- [x] Update `/samples/endpoints/readme.md` to include the new sample

## Planned Implementation

**API:** Open-Meteo Weather API (https://open-meteo.com/)
- Free, no API key
- Familiar domain (weather)
- Demonstrates geocoding (city → lat/long) then weather fetch

**Structure:**
```
/samples/endpoints/15-httpclient/
  ├── endpoint-httpclient-openmeteo.cs
  ├── endpoints/
  │   ├── CurrentWeatherEndpoint.cs    # [NuruRoute("weather {city}")]
  │   └── ForecastEndpoint.cs          # [NuruRoute("forecast {city} --days {days:int}")]
  └── services/
      ├── IOpenMeteoService.cs
      ├── OpenMeteoService.cs
      └── GeoCodingResponse.cs
```

**Key Features to Demonstrate:**
- `AddHttpClient<TService, TImplementation>()` registration
- Constructor injection of typed HTTP client
- Proper HttpClient lifetime management (no `new HttpClient()` code smell)
- JSON deserialization with System.Text.Json
- Error handling for invalid cities, network failures, timeouts

## Research Notes

**Open-Meteo API Structure:**
1. Geocoding: `https://geocoding-api.open-meteo.com/v1/search?name={city}`
   - Returns lat/long for city name
2. Weather: `https://api.open-meteo.com/v1/forecast?latitude={lat}&longitude={lon}&current=temperature_2m,weather_code`
   - Returns current weather for coordinates

**Example Response:**
```json
{
  "latitude": 52.52,
  "longitude": 13.419,
  "current": {
    "temperature_2m": 15.3,
    "weather_code": 1
  }
}
```

## Related Tasks

- **423** - Implement AddHttpClient support in source generator - Unblocks this task

## Results

Successfully created HttpClient sample demonstrating AddHttpClient support with Nuru's source-gen DI.

### Files Created

1. **endpoint-httpclient-openmeteo.cs** - Main runfile showing AddHttpClient registration
2. **endpoints/CurrentWeatherEndpoint.cs** - `[NuruRoute("weather")]` endpoint with `[Parameter]`
3. **services/IOpenMeteoService.cs** - Service interface
4. **services/OpenMeteoService.cs** - Implementation using HttpClient
5. **services/WeatherModels.cs** - DTOs with AOT-compatible `JsonSerializerContext`
6. **readme.md** - Documentation

### Fixes Applied

- Route pattern fixed: `[NuruRoute("weather {city}")]` → `[NuruRoute("weather")]` + `[Parameter]` (correct endpoint DSL)
- JSON deserialization fixed for AOT: Added `OpenMeteoJsonContext` with `[JsonSerializable]` and `[JsonPropertyName]` attributes (reflection-based serialization is disabled in AOT mode)
- Property names normalized: `Temperature_2m` → `Temperature2m`, `Weather_Code` → `WeatherCode`

### Key Demonstration

The sample shows the idiomatic .NET 10 pattern WITHOUT UseMicrosoftDependencyInjection():

```csharp
.ConfigureServices(services =>
{
  services.AddHttpClient<IOpenMeteoService, OpenMeteoService>(client =>
  {
    client.Timeout = TimeSpan.FromSeconds(30);
  });
})
```

### Features

- ✅ Typed client registration with AddHttpClient
- ✅ Constructor injection of service
- ✅ Real Open-Meteo Weather API integration
- ✅ Proper error handling (invalid cities, network failures)
- ✅ Pretty formatted weather output
- ✅ Works without Microsoft DI (pure source-gen)
- ✅ AOT-compatible JSON serialization via source-generated context

### Usage

```bash
dotnet run samples/endpoints/15-httpclient/endpoint-httpclient-openmeteo.cs -- weather "London"
```
