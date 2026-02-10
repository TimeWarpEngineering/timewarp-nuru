#!/usr/bin/dotnet --
#:project ../../../source/timewarp-nuru/timewarp-nuru.csproj

// ═══════════════════════════════════════════════════════════════════════════════
// HYBRID - MIGRATION: COMPLETE CONVERSION ⚠️ EDGE CASE
// ═══════════════════════════════════════════════════════════════════════════════
//
// Step 3 of migration: Full conversion to Endpoint DSL.
// All operations now use Endpoint pattern for consistency.
//
// DSL: Endpoint (all routes are now class-based)
//
// This is the final step of migration - everything uses Endpoint DSL:
//   - All operations are [NuruRoute] classes
//   - Dependency injection available everywhere
//   - Consistent testability across all commands
//
// See hybrid-migration-start-fluent.cs for the starting point.
// ═══════════════════════════════════════════════════════════════════════════════

using TimeWarp.Nuru;
using Microsoft.Extensions.DependencyInjection;
using static System.Console;

NuruApp app = NuruApp.CreateBuilder()
  .ConfigureServices(ConfigureServices)
  .DiscoverEndpoints() // All routes discovered from [NuruRoute] classes
  .Build();

WriteLine("=== Hybrid Migration Demo: Step 3 - Complete Endpoint Conversion ===\n");
WriteLine("This app now uses ONLY Endpoint DSL:");
WriteLine("  ✓ All operations are [NuruRoute] classes");
WriteLine("  ✓ Full dependency injection support");
WriteLine("  ✓ Complete testability");
WriteLine("  ✓ Consistent architecture");
WriteLine();
WriteLine("Migration complete! All code now follows Endpoint DSL patterns.\n");

return await app.RunAsync(args);

static void ConfigureServices(IServiceCollection services)
{
  services.AddSingleton<IScientificCalculator, ScientificCalculator>();
}

// =============================================================================
// ALL ENDPOINT DEFINITIONS - Converted from Fluent to Endpoint
// =============================================================================

[NuruRoute("add", Description = "Add two numbers (Endpoint)")]
public sealed class AddCommand : IQuery<double>
{
  [Parameter] public double X { get; set; }
  [Parameter] public double Y { get; set; }

  public sealed class Handler : IQueryHandler<AddCommand, double>
  {
    public ValueTask<double> Handle(AddCommand c, CancellationToken ct)
    {
      WriteLine($"{c.X} + {c.Y} = {c.X + c.Y}");
      return new ValueTask<double>(c.X + c.Y);
    }
  }
}

[NuruRoute("subtract", Description = "Subtract two numbers (Endpoint)")]
public sealed class SubtractCommand : IQuery<double>
{
  [Parameter] public double X { get; set; }
  [Parameter] public double Y { get; set; }

  public sealed class Handler : IQueryHandler<SubtractCommand, double>
  {
    public ValueTask<double> Handle(SubtractCommand c, CancellationToken ct)
    {
      WriteLine($"{c.X} - {c.Y} = {c.X - c.Y}");
      return new ValueTask<double>(c.X - c.Y);
    }
  }
}

[NuruRoute("multiply", Description = "Multiply two numbers (Endpoint)")]
public sealed class MultiplyCommand : IQuery<double>
{
  [Parameter] public double X { get; set; }
  [Parameter] public double Y { get; set; }

  public sealed class Handler : IQueryHandler<MultiplyCommand, double>
  {
    public ValueTask<double> Handle(MultiplyCommand c, CancellationToken ct)
    {
      WriteLine($"{c.X} × {c.Y} = {c.X * c.Y}");
      return new ValueTask<double>(c.X * c.Y);
    }
  }
}

[NuruRoute("divide", Description = "Divide two numbers (Endpoint)")]
public sealed class DivideCommand : IQuery<double>
{
  [Parameter] public double X { get; set; }
  [Parameter] public double Y { get; set; }

  public sealed class Handler : IQueryHandler<DivideCommand, double>
  {
    public ValueTask<double> Handle(DivideCommand c, CancellationToken ct)
    {
      if (c.Y == 0) throw new DivideByZeroException("Cannot divide by zero");
      WriteLine($"{c.X} ÷ {c.Y} = {c.X / c.Y}");
      return new ValueTask<double>(c.X / c.Y);
    }
  }
}

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
