#!/usr/bin/dotnet --
#:project ../../../source/timewarp-nuru/timewarp-nuru.csproj

// ═══════════════════════════════════════════════════════════════════════════════
// FLUENT DSL - RUNTIME DI
// ═══════════════════════════════════════════════════════════════════════════════
//
// This sample demonstrates UseMicrosoftDependencyInjection() with Fluent DSL.
// This is useful when you need:
//
//   - Services with constructor dependencies (MS DI resolves the chain)
//   - Factory delegate registrations
//   - Extension method registrations (AddDbContext, AddHttpClient, etc.)
//
// DSL: Fluent API with .UseMicrosoftDependencyInjection()
//
// TRADE-OFF:
//   - Slightly slower startup (~2-10ms for ServiceProvider.Build())
//   - Still AOT-compatible, but services resolved at runtime not compile-time
//
// RUN THIS SAMPLE:
//   ./fluent-runtime-di.cs greet Alice
//   ./fluent-runtime-di.cs greet Bob --uppercase
// ═══════════════════════════════════════════════════════════════════════════════

using Microsoft.Extensions.DependencyInjection;
using TimeWarp.Nuru;

NuruApp app = NuruApp.CreateBuilder()
  // Enable runtime MS DI - allows services with constructor dependencies
  .UseMicrosoftDependencyInjection()
  .ConfigureServices(services =>
  {
    // Register services - note IGreetingService depends on IMessageFormatter
    // MS DI automatically resolves this dependency chain at runtime
    services.AddSingleton<IMessageFormatter, MessageFormatter>();
    services.AddSingleton<IGreetingService, GreetingService>();
  })
  .Map("greet {name} --uppercase,-u")
    .WithDescription("Greet someone using injected services")
    .WithHandler((string name, bool uppercase, IGreetingService greeter) =>
    {
      string greeting = greeter.Greet(name);
      return uppercase ? greeting.ToUpperInvariant() : greeting;
    })
    .Done()
  .Build();

return await app.RunAsync(args);

// ═══════════════════════════════════════════════════════════════════════════════
// SERVICES WITH CONSTRUCTOR DEPENDENCIES
// ═══════════════════════════════════════════════════════════════════════════════

public interface IMessageFormatter
{
  string Format(string template, params object[] args);
}

public class MessageFormatter : IMessageFormatter
{
  public string Format(string template, params object[] args)
    => string.Format(template, args);
}

public interface IGreetingService
{
  string Greet(string name);
}

public class GreetingService : IGreetingService
{
  private readonly IMessageFormatter Formatter;

  // Constructor dependency - MS DI resolves this automatically
  public GreetingService(IMessageFormatter formatter)
  {
    Formatter = formatter;
  }

  public string Greet(string name)
    => Formatter.Format("Hello, {0}! Welcome to runtime DI.", name);
}
