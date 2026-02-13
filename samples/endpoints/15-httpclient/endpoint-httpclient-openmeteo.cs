#!/usr/bin/dotnet --
// ═══════════════════════════════════════════════════════════════════════════════
// HTTPCLIENT SAMPLE - OPEN-METEO WEATHER API
// ═══════════════════════════════════════════════════════════════════════════════
// Demonstrates AddHttpClient support with Nuru's source-gen DI.
//
// KEY FEATURE:
//   Uses AddHttpClient<TService, TImplementation>() WITHOUT
//   UseMicrosoftDependencyInjection() - pure source-gen DI!
//
// PATTERNS:
//   - Typed HttpClient registration
//   - IOpenMeteoService injection in query handler
//   - Proper HttpClient lifetime management
//   - Real-world API integration (Open-Meteo Weather API)
//
// USAGE:
//   dotnet run samples/endpoints/15-httpclient/endpoint-httpclient-openmeteo.cs -- weather "London"
//   dotnet run samples/endpoints/15-httpclient/endpoint-httpclient-openmeteo.cs -- weather "Tokyo"
//
//   Or make it executable and run directly:
//   chmod +x samples/endpoints/15-httpclient/endpoint-httpclient-openmeteo.cs
//   ./samples/endpoints/15-httpclient/endpoint-httpclient-openmeteo.cs weather "Paris"
// ═══════════════════════════════════════════════════════════════════════════════
#:project ../../../source/timewarp-nuru/timewarp-nuru.csproj

using TimeWarp.Nuru;

NuruApp app = NuruApp.CreateBuilder()
  .WithName("Weather CLI")
  .WithDescription("Get weather information using Open-Meteo API")
  .ConfigureServices(services =>
  {
    // This is the key pattern - AddHttpClient works with source-gen DI!
    // No UseMicrosoftDependencyInjection() required.
    services.AddHttpClient<IOpenMeteoService, OpenMeteoService>(client =>
    {
      client.Timeout = TimeSpan.FromSeconds(30);
    });
  })
  .DiscoverEndpoints()
  .AddHelp()
  .Build();

return await app.RunAsync(args);
