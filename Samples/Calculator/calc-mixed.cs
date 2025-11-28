#!/usr/bin/dotnet --
// calc-mixed - Calculator mixing Direct and Mediator approaches
#:project ../../Source/TimeWarp.Nuru/TimeWarp.Nuru.csproj
#:package Mediator.Abstractions
#:package Mediator.SourceGenerator

using TimeWarp.Nuru;
using Mediator;
using Microsoft.Extensions.DependencyInjection;
using static System.Console;

NuruCoreApp app =
  new NuruAppBuilder()
  .AddDependencyInjection()
  .AddAutoHelp()
  .ConfigureServices(services =>
  {
    // Register Mediator - source generator discovers handlers in THIS assembly
    services.AddMediator();
    services.AddSingleton<IScientificCalculator, ScientificCalculator>();
  })
  .Map // Use Delegate approach for simple operations (performance)
  (
    pattern: "add {x:double} {y:double}",
    handler: (double x, double y) => WriteLine($"{x} + {y} = {x + y}"),
    description: "Add two numbers together"
  )
  .Map
  (
    pattern: "subtract {x:double} {y:double}",
    handler: (double x, double y) => WriteLine($"{x} - {y} = {x - y}"),
    description: "Subtract the second number from the first"
  )
  .Map
  (
    pattern: "multiply {x:double} {y:double}",
    handler: (double x, double y) => WriteLine($"{x} × {y} = {x * y}"),
    description: "Multiply two numbers together"
  )
  .Map
  (
    pattern: "divide {x:double} {y:double}",
    handler: (double x, double y) =>
    {
      if (y == 0)
      {
        WriteLine("Error: Division by zero");
        return;
      }

      WriteLine($"{x} ÷ {y} = {x / y}");
    },
    description: "Divide the first number by the second"
  )
  .Map<FactorialCommand> // Use Mediator for complex operations (testability, DI)
  (
    pattern: "factorial {n:int}",
    description: "Calculate factorial (n!)"
  )
  .Map<PrimeCheckCommand>
  (
    pattern: "isprime {n:int}",
    description: "Check if a number is prime"
  )
  .Map<FibonacciCommand>
  (
    pattern: "fibonacci {n:int}",
    description: "Calculate the nth Fibonacci number"
  )
  .Map<StatsCommand, StatsResponse> // Example: Mediator command that returns a response object
  (
    pattern: "stats {*values}",
    description: "Calculate statistics for a set of numbers (returns JSON)"
  )
  .Map // Example: Delegate that returns an object
  (
    pattern: "compare {x:double} {y:double}",
    handler: (double x, double y) => new ComparisonResult
    {
      X = x,
      Y = y,
      IsEqual = x == y,
      Difference = x - y,
      Ratio = y != 0 ? x / y : double.NaN
    },
    description: "Compare two numbers and return detailed comparison (returns JSON)"
  )
  .Build();

return await app.RunAsync(args);

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
