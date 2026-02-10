#!/usr/bin/dotnet --
#:project ../../../source/timewarp-nuru/timewarp-nuru.csproj

// ═══════════════════════════════════════════════════════════════════════════════
// HYBRID - MIGRATION: ADD ENDPOINT TO FLUENT ⚠️ EDGE CASE
// ═══════════════════════════════════════════════════════════════════════════════
//
// Step 2 of migration: Adding Endpoint patterns to existing Fluent DSL app.
// Mixed approach combining delegates (simple operations) with endpoints (complex).
//
// DSL: Hybrid - Fluent + Endpoints mixed
//
// This sample demonstrates migrating from pure Fluent to a hybrid approach:
//   - DELEGATES: Simple operations (add, subtract, multiply, divide)
//   - ENDPOINTS: Complex operations (factorial, isprime, fibonacci)
//
// Based on: samples/02-calculator/03-calc-mixed.cs
// Next step: hybrid-migration-complete.cs for full Endpoint conversion
// ═══════════════════════════════════════════════════════════════════════════════

using TimeWarp.Nuru;
using Microsoft.Extensions.DependencyInjection;
using static System.Console;

NuruApp app = NuruApp.CreateBuilder()
  .ConfigureServices(ConfigureServices)
  // FLUENT: Simple operations (performance, inline)
  .Map("add {x:double} {y:double}")
    .WithHandler((double x, double y) => WriteLine($"{x} + {y} = {x + y}"))
    .WithDescription("Add two numbers (Fluent)")
    .Done()
  .Map("subtract {x:double} {y:double}")
    .WithHandler((double x, double y) => WriteLine($"{x} - {y} = {x - y}"))
    .WithDescription("Subtract two numbers (Fluent)")
    .Done()
  .Map("multiply {x:double} {y:double}")
    .WithHandler((double x, double y) => WriteLine($"{x} × {y} = {x * y}"))
    .WithDescription("Multiply two numbers (Fluent)")
    .Done()
  .Map("divide {x:double} {y:double}")
    .WithHandler((double x, double y) =>
    {
      if (y == 0) { WriteLine("Error: Division by zero"); return; }
      WriteLine($"{x} ÷ {y} = {x / y}");
    })
    .WithDescription("Divide two numbers (Fluent)")
    .Done()
  // ENDPOINTS: Complex operations (DI, testable)
  // FactorialCommand, PrimeCheckCommand, FibonacciCommand defined below
  .DiscoverEndpoints()
  .Build();

WriteLine("=== Hybrid Migration Demo: Step 2 - Add Endpoints ===\n");
WriteLine("This app now uses BOTH patterns:");
WriteLine("  ✓ Fluent DSL: add, subtract, multiply, divide (inline)");
WriteLine("  ✓ Endpoints: factorial, isprime, fibonacci (complex, with DI)");
WriteLine("Next step: hybrid-migration-complete.cs for full Endpoint conversion.\n");

return await app.RunAsync(args);

static void ConfigureServices(IServiceCollection services)
{
  services.AddSingleton<IScientificCalculator, ScientificCalculator>();
}

// ENDPOINT DEFINITIONS - Added for complex operations

[NuruRoute("factorial", Description = "Calculate factorial (Endpoint with DI)")]
public sealed class FactorialCommand : ICommand<Unit>
{
  [Parameter] public int N { get; set; }

  public sealed class Handler(IScientificCalculator calc) : ICommandHandler<FactorialCommand, Unit>
  {
    public ValueTask<Unit> Handle(FactorialCommand c, CancellationToken ct)
    {
      try
      {
        long result = calc.Factorial(c.N);
        WriteLine($"{c.N}! = {result}");
      }
      catch (ArgumentException ex)
      {
        WriteLine($"Error: {ex.Message}");
      }
      return default;
    }
  }
}

[NuruRoute("isprime", Description = "Check if number is prime (Endpoint with DI)")]
public sealed class PrimeCheckCommand : ICommand<Unit>
{
  [Parameter] public int N { get; set; }

  public sealed class Handler(IScientificCalculator calc) : ICommandHandler<PrimeCheckCommand, Unit>
  {
    public ValueTask<Unit> Handle(PrimeCheckCommand c, CancellationToken ct)
    {
      bool result = calc.IsPrime(c.N);
      WriteLine($"{c.N} is {(result ? "prime" : "not prime")}");
      return default;
    }
  }
}

[NuruRoute("fibonacci", Description = "Calculate Fibonacci (Endpoint with DI)")]
public sealed class FibonacciCommand : ICommand<Unit>
{
  [Parameter] public int N { get; set; }

  public sealed class Handler(IScientificCalculator calc) : ICommandHandler<FibonacciCommand, Unit>
  {
    public ValueTask<Unit> Handle(FibonacciCommand c, CancellationToken ct)
    {
      try
      {
        long result = calc.Fibonacci(c.N);
        WriteLine($"Fibonacci({c.N}) = {result}");
      }
      catch (ArgumentException ex)
      {
        WriteLine($"Error: {ex.Message}");
      }
      return default;
    }
  }
}

// SUPPORTING TYPES

public interface IScientificCalculator
{
  long Factorial(int n);
  bool IsPrime(int n);
  long Fibonacci(int n);
}

public class ScientificCalculator : IScientificCalculator
{
  public long Factorial(int n)
  {
    if (n < 0) throw new ArgumentException("Factorial not defined for negative numbers");
    if (n == 0 || n == 1) return 1;
    long result = 1;
    for (int i = 2; i <= n; i++) result *= i;
    return result;
  }

  public bool IsPrime(int n)
  {
    if (n <= 1) return false;
    if (n == 2) return true;
    if (n % 2 == 0) return false;
    for (int i = 3; i * i <= n; i += 2) if (n % i == 0) return false;
    return true;
  }

  public long Fibonacci(int n)
  {
    if (n < 0) throw new ArgumentException("Fibonacci not defined for negative numbers");
    if (n <= 1) return n;
    long a = 0, b = 1;
    for (int i = 2; i <= n; i++) { long temp = a + b; a = b; b = temp; }
    return b;
  }
}
