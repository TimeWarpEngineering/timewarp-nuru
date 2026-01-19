#!/usr/bin/dotnet --
#:project ../../source/timewarp-nuru/timewarp-nuru.csproj

// ═══════════════════════════════════════════════════════════════════════════════
// MIXED PATTERN - DELEGATES + ATTRIBUTED ROUTES EXAMPLE
// ═══════════════════════════════════════════════════════════════════════════════
//
// This sample demonstrates mixing both approaches:
// - DELEGATES: For simple operations (inline, fast, no DI needed)
// - ATTRIBUTED ROUTES: For complex operations (testable, DI, separation of concerns)
//
// WHEN TO USE EACH:
//   Delegates: Simple one-liners, no external dependencies, performance-critical
//   Attributed Routes: Complex logic, needs DI, requires unit testing, reusable handlers
//
// NO EXTERNAL PACKAGES REQUIRED:
//   TimeWarp.Nuru provides ICommand<T>, ICommandHandler<T,TResult>, and Unit
//   The source generator discovers [NuruRoute] classes and generates routing code
//
// HOW IT WORKS:
//   1. Mark command classes with [NuruRoute("pattern")]
//   2. Mark properties with [Parameter] or [Option] attributes
//   3. Create nested Handler class implementing ICommandHandler<T, TResult>
//   4. Source generator discovers these and generates invocation code
//   5. Delegate routes use .Map("pattern").WithHandler(...) fluent API
// ═══════════════════════════════════════════════════════════════════════════════

using TimeWarp.Nuru;
using Microsoft.Extensions.DependencyInjection;
using static System.Console;

NuruCoreApp app = NuruApp.CreateBuilder(args)
  .ConfigureServices(ConfigureServices)
  // Use Delegate approach for simple operations (performance)
  .Map("add {x:double} {y:double}")
    .WithHandler((double x, double y) => WriteLine($"{x} + {y} = {x + y}"))
    .WithDescription("Add two numbers together")
    .Done()
  .Map("subtract {x:double} {y:double}")
    .WithHandler((double x, double y) => WriteLine($"{x} - {y} = {x - y}"))
    .WithDescription("Subtract the second number from the first")
    .Done()
  .Map("multiply {x:double} {y:double}")
    .WithHandler((double x, double y) => WriteLine($"{x} × {y} = {x * y}"))
    .WithDescription("Multiply two numbers together")
    .Done()
  .Map("divide {x:double} {y:double}")
    .WithHandler((double x, double y) =>
    {
      if (y == 0)
      {
        WriteLine("Error: Division by zero");
        return;
      }

      WriteLine($"{x} ÷ {y} = {x / y}");
    })
    .WithDescription("Divide the first number by the second")
    .Done()
  // Attributed routes (factorial, isprime, fibonacci, stats) are auto-discovered via [NuruRoute]
  // Example: Delegate that returns an object
  .Map("compare {x:double} {y:double}")
    .WithHandler((double x, double y) => new ComparisonResult
    {
      X = x,
      Y = y,
      IsEqual = x == y,
      Difference = x - y,
      Ratio = y != 0 ? x / y : double.NaN
    })
    .WithDescription("Compare two numbers and return detailed comparison (returns JSON)")
    .Done()
  .Build();

return await app.RunAsync(args);

static void ConfigureServices(IServiceCollection services)
{
  services.AddSingleton<IScientificCalculator, ScientificCalculator>();
}

// Complex operations using attributed routes with nested handlers
[NuruRoute("factorial", Description = "Calculate factorial (n!)")]
public sealed class FactorialCommand : ICommand<Unit>
{
  [Parameter(Order = 0)]
  public int N { get; set; }

  public sealed class Handler(IScientificCalculator calc) : ICommandHandler<FactorialCommand, Unit>
  {
    public ValueTask<Unit> Handle(FactorialCommand command, CancellationToken cancellationToken)
    {
      try
      {
        long result = calc.Factorial(command.N);
        WriteLine($"{command.N}! = {result}");
      }
      catch (ArgumentException ex)
      {
        WriteLine($"Error: {ex.Message}");
      }

      return default;
    }
  }
}

[NuruRoute("isprime", Description = "Check if a number is prime")]
public sealed class PrimeCheckCommand : ICommand<Unit>
{
  [Parameter(Order = 0)]
  public int N { get; set; }

  public sealed class Handler(IScientificCalculator calc) : ICommandHandler<PrimeCheckCommand, Unit>
  {
    public ValueTask<Unit> Handle(PrimeCheckCommand command, CancellationToken cancellationToken)
    {
      bool result = calc.IsPrime(command.N);
      WriteLine($"{command.N} is {(result ? "prime" : "not prime")}");
      return default;
    }
  }
}

[NuruRoute("fibonacci", Description = "Calculate the nth Fibonacci number")]
public sealed class FibonacciCommand : ICommand<Unit>
{
  [Parameter(Order = 0)]
  public int N { get; set; }

  public sealed class Handler(IScientificCalculator calc) : ICommandHandler<FibonacciCommand, Unit>
  {
    public ValueTask<Unit> Handle(FibonacciCommand command, CancellationToken cancellationToken)
    {
      try
      {
        long result = calc.Fibonacci(command.N);
        WriteLine($"Fibonacci({command.N}) = {result}");
      }
      catch (ArgumentException ex)
      {
        WriteLine($"Error: {ex.Message}");
      }

      return default;
    }
  }
}

// Example: Attributed route command with response object
[NuruRoute("stats", Description = "Calculate statistics for a set of numbers (returns JSON)")]
public sealed class StatsCommand : ICommand<StatsResponse>
{
  [Parameter(IsCatchAll = true)]
  public string[] Values { get; set; } = [];

  public sealed class Handler : ICommandHandler<StatsCommand, StatsResponse>
  {
    public ValueTask<StatsResponse> Handle(StatsCommand command, CancellationToken cancellationToken)
    {
      if (command.Values.Length == 0)
      {
        return new ValueTask<StatsResponse>(new StatsResponse());
      }

      double[] values =
      [
        .. command.Values
          .Select(v => double.TryParse(v, out double d) ? d : 0)
          .Where(v => v != 0)
      ];

      if (values.Length == 0)
      {
        return new ValueTask<StatsResponse>(new StatsResponse());
      }

      return new ValueTask<StatsResponse>
      (
        new StatsResponse
        {
          Sum = values.Sum(),
          Average = values.Average(),
          Min = values.Min(),
          Max = values.Max(),
          Count = values.Length
        }
      );
    }
  }
}
public class StatsResponse
{
  public double Sum { get; set; }
  public double Average { get; set; }
  public double Min { get; set; }
  public double Max { get; set; }
  public int Count { get; set; }
}

// Example: Object returned by delegate
public class ComparisonResult
{
  public double X { get; set; }
  public double Y { get; set; }
  public bool IsEqual { get; set; }
  public double Difference { get; set; }
  public double Ratio { get; set; }
}

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
    for (int i = 2; i <= n; i++)
      result *= i;
    return result;
  }

  public bool IsPrime(int n)
  {
    if (n <= 1) return false;
    if (n == 2) return true;
    if (n % 2 == 0) return false;

    for (int i = 3; i * i <= n; i += 2)
      if (n % i == 0) return false;

    return true;
  }

  public long Fibonacci(int n)
  {
    if (n < 0) throw new ArgumentException("Fibonacci not defined for negative numbers");
    if (n <= 1) return n;

    long a = 0, b = 1;
    for (int i = 2; i <= n; i++)
    {
      long temp = a + b;
      a = b;
      b = temp;
    }

    return b;
  }
}
