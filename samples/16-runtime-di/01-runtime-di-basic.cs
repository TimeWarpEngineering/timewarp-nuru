#!/usr/bin/dotnet --
#:project ../../source/timewarp-nuru/timewarp-nuru.csproj

// ═══════════════════════════════════════════════════════════════════════════════
// RUNTIME DI - BASIC EXAMPLE
// ═══════════════════════════════════════════════════════════════════════════════
//
// This sample demonstrates UseMicrosoftDependencyInjection() which enables full
// MS DI container support at runtime. This is useful when you need:
//
//   - Services with constructor dependencies (MS DI resolves the chain)
//   - Factory delegate registrations
//   - Extension method registrations (AddDbContext, AddHttpClient, etc.)
//
// TRADE-OFF:
//   - Slightly slower startup (~2-10ms for ServiceProvider.Build())
//   - Still AOT-compatible, but services resolved at runtime not compile-time
//
// WITHOUT UseMicrosoftDependencyInjection():
//   Source generator emits `new MyService()` - fails if constructor has dependencies
//
// WITH UseMicrosoftDependencyInjection():
//   Source generator emits GetRequiredService<T>() - MS DI resolves dependencies
//
// RUN THIS SAMPLE:
//   ./01-runtime-di-basic.cs greet Alice
//   ./01-runtime-di-basic.cs greet Bob --uppercase
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

/// <summary>
/// Formats messages (no dependencies).
/// </summary>
public interface IMessageFormatter
{
  string Format(string template, params object[] args);
}

public class MessageFormatter : IMessageFormatter
{
  public string Format(string template, params object[] args)
    => string.Format(template, args);
}

/// <summary>
/// Greeting service that DEPENDS on IMessageFormatter.
/// Without runtime DI, the source generator can't instantiate this
/// because it doesn't know how to resolve the constructor dependency.
/// </summary>
public interface IGreetingService
{
  string Greet(string name);
}

public class GreetingService : IGreetingService
{
  private readonly IMessageFormatter _formatter;

  // Constructor dependency - MS DI resolves this automatically
  public GreetingService(IMessageFormatter formatter)
  {
    _formatter = formatter;
  }

  public string Greet(string name)
    => _formatter.Format("Hello, {0}! Welcome to runtime DI.", name);
}
