#!/usr/bin/env -S dotnet --
#:project ../../../source/timewarp-nuru/timewarp-nuru.csproj
#region Purpose
// Runs one Nuru command twice in a single process so the second call sees the loaded solution.
#endregion

#region Design
// Both RunAsync calls are top-level statements. The source generator does not walk if
// statements, so a flag on the pitch CLI cannot hide the second call and still intercept it.
// One static service provider is shared by both intercepted calls.
// Repeating diagnostics hits Roslynk's memo. That cache key is includeAnalyzers alone,
// so this entry measures a cache hit when those arguments do not change. The refs route
// is the warm cost that still does work.
#endregion

using System.Diagnostics;
using System.Globalization;
using TimeWarp.Nuru;

NuruApp app = NuruApp.CreateBuilder()
  .UseMicrosoftDependencyInjection()
  .ConfigureServices(services =>
  {
    global::Morris.Roslynk.ServicesRegistration.AddRoslynk(services);
    global::Microsoft.Extensions.DependencyInjection.ServiceCollectionServiceExtensions
      .AddSingleton<global::Morris.Roslynk.Features.Diagnostics.GetDiagnostics.GetDiagnosticsTool>(services);
    global::Microsoft.Extensions.DependencyInjection.ServiceCollectionServiceExtensions
      .AddSingleton<global::Morris.Roslynk.Features.References.FindReferences.FindReferencesTool>(services);
  })
  .DiscoverEndpoints()
  .Build();

Stopwatch clock = Stopwatch.StartNew();
int first = await app.RunAsync(args);
double firstMs = clock.Elapsed.TotalMilliseconds;
clock.Restart();
int second = await app.RunAsync(args);
double secondMs = clock.Elapsed.TotalMilliseconds;
Console.Error.WriteLine($"warmPair.firstMs={firstMs.ToString("F3", CultureInfo.InvariantCulture)}");
Console.Error.WriteLine($"warmPair.secondMs={secondMs.ToString("F3", CultureInfo.InvariantCulture)}");
Console.Error.WriteLine($"warmPair.firstExit={first}");
Console.Error.WriteLine($"warmPair.secondExit={second}");
return first != 0 ? first : second;
