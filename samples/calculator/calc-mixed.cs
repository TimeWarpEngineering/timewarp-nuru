#!/usr/bin/dotnet --
#:project ../../source/timewarp-nuru/timewarp-nuru.csproj
#:package Mediator.Abstractions
#:package Mediator.SourceGenerator

// ═══════════════════════════════════════════════════════════════════════════════
// MIXED PATTERN - DELEGATES + MEDIATOR EXAMPLE
// ═══════════════════════════════════════════════════════════════════════════════
//
// This sample demonstrates mixing both approaches:
// - DELEGATES: For simple operations (inline, fast, no DI needed)
// - MEDIATOR: For complex operations (testable, DI, separation of concerns)
//
// WHEN TO USE EACH:
//   Delegates: Simple one-liners, no external dependencies, performance-critical
//   Mediator:  Complex logic, needs DI, requires unit testing, reusable handlers
//
// REQUIRED PACKAGES (for Mediator commands):
//   #:package Mediator.Abstractions    - Interfaces (IRequest, IRequestHandler)
//   #:package Mediator.SourceGenerator - Generates AddMediator() in YOUR assembly
//
// HOW IT WORKS:
//   Mediator.SourceGenerator scans YOUR assembly at compile time for:
//   - IRequest implementations (commands)
//   - IRequestHandler<> implementations (handlers)
//   Then generates a type-safe AddMediator() extension method specific to YOUR project.
//
// COMMON ERROR:
//   "No service for type 'Mediator.IMediator' has been registered"
//   SOLUTION: Install BOTH packages AND call services.AddMediator()
// ═══════════════════════════════════════════════════════════════════════════════

using TimeWarp.Nuru;
using Mediator;
using Microsoft.Extensions.DependencyInjection;
using static System.Console;

NuruCoreApp app = NuruApp.CreateBuilder(args)
  .ConfigureServices(ConfigureServices)
  // Use Delegate approach for simple operations (performance)
  .Map("add {x:double} {y:double}")
    .WithHandler((double x, double y) => WriteLine($"{x} + {y} = {x + y}"))
    .WithDescription("Add two numbers together")
  .Map("subtract {x:double} {y:double}")
    .WithHandler((double x, double y) => WriteLine($"{x} - {y} = {x - y}"))
    .WithDescription("Subtract the second number from the first")
  .Map("multiply {x:double} {y:double}")
    .WithHandler((double x, double y) => WriteLine($"{x} × {y} = {x * y}"))
    .WithDescription("Multiply two numbers together")
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
  .Map<FactorialCommand>("factorial {n:int}") // Use Mediator for complex operations (testability, DI)
    .WithDescription("Calculate factorial (n!)")
  .Map<PrimeCheckCommand>("isprime {n:int}")
    .WithDescription("Check if a number is prime")
  .Map<FibonacciCommand>("fibonacci {n:int}")
    .WithDescription("Calculate the nth Fibonacci number")
  .Map<StatsCommand, StatsResponse>("stats {*values}") // Example: Mediator command that returns a response object
    .WithDescription("Calculate statistics for a set of numbers (returns JSON)")
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
  .Build();

return await app.RunAsync(args);

static void ConfigureServices(IServiceCollection services)
{
  // Register Mediator - source generator discovers handlers in THIS assembly
  services.AddMediator();
  services.AddSingleton<IScientificCalculator, ScientificCalculator>();
}

// Complex operations using Mediator pattern with nested handlers
public sealed class FactorialCommand : IRequest
{
  public int N { get; set; }

  public sealed class Handler(IScientificCalculator calc) : IRequestHandler<FactorialCommand>
  {
    public ValueTask<Unit> Handle(FactorialCommand request, CancellationToken cancellationToken)
    {
      try
      {
        long result = calc.Factorial(request.N);
        WriteLine($"{request.N}! = {result}");
      }
      catch (ArgumentException ex)
      {
        WriteLine($"Error: {ex.Message}");
      }

      return default;
    }
  }
}

public class PrimeCheckCommand : IRequest
{
  public int N { get; set; }

  public sealed class Handler(IScientificCalculator calc) : IRequestHandler<PrimeCheckCommand>
  {
    public ValueTask<Unit> Handle(PrimeCheckCommand request, CancellationToken cancellationToken)
    {
      bool result = calc.IsPrime(request.N);
      WriteLine($"{request.N} is {(result ? "prime" : "not prime")}");
      return default;
    }
  }
}

public class FibonacciCommand : IRequest
{
  public int N { get; set; }

  public sealed class Handler(IScientificCalculator calc) : IRequestHandler<FibonacciCommand>
  {
    public ValueTask<Unit> Handle(FibonacciCommand request, CancellationToken cancellationToken)
    {
      try
      {
        long result = calc.Fibonacci(request.N);
        WriteLine($"Fibonacci({request.N}) = {result}");
      }
      catch (ArgumentException ex)
      {
        WriteLine($"Error: {ex.Message}");
      }

      return default;
    }
  }
}

// Example: Mediator command with response
public class StatsCommand : IRequest<StatsResponse>
{
  public string Values { get; set; } = "";

  internal sealed class Handler : IRequestHandler<StatsCommand, StatsResponse>
  {
    public ValueTask<StatsResponse> Handle(StatsCommand request, CancellationToken cancellationToken)
    {
      if (string.IsNullOrWhiteSpace(request.Values))
      {
        return new ValueTask<StatsResponse>(new StatsResponse());
      }

      double[] values =
      [
        .. request.Values.Split(' ', StringSplitOptions.RemoveEmptyEntries)
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
