#!/usr/bin/dotnet --
// calc-mixed - Calculator mixing Direct and Mediator approaches
#:project ../../Source/TimeWarp.Nuru/TimeWarp.Nuru.csproj
#:property PublishAot=false
#:property TrimMode=partial

using TimeWarp.Nuru;
using TimeWarp.Mediator;
using Microsoft.Extensions.DependencyInjection;
using static System.Console;

NuruAppBuilder builder =
  new NuruAppBuilder()
  .AddDependencyInjection(config => config.RegisterServicesFromAssembly(typeof(FactorialCommand).Assembly))
  .AddAutoHelp();

// Register services for complex operations
builder.Services.AddSingleton<IScientificCalculator, ScientificCalculator>();

NuruApp app =
  builder
  .AddRoute // Use Delegate approach for simple operations (performance)
  (
    pattern: "add {x:double} {y:double}",
    handler: (double x, double y) => WriteLine($"{x} + {y} = {x + y}"),
    description: "Add two numbers together"
  )
  .AddRoute
  (
    pattern: "subtract {x:double} {y:double}",
    handler: (double x, double y) => WriteLine($"{x} - {y} = {x - y}"),
    description: "Subtract the second number from the first"
  )
  .AddRoute
  (
    pattern: "multiply {x:double} {y:double}",
    handler: (double x, double y) => WriteLine($"{x} × {y} = {x * y}"),
    description: "Multiply two numbers together"
  )
  .AddRoute
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
  .AddRoute<FactorialCommand> // Use Mediator for complex operations (testability, DI)
  (
    pattern: "factorial {n:int}",
    description: "Calculate factorial (n!)"
  )
  .AddRoute<PrimeCheckCommand>
  (
    pattern: "isprime {n:int}",
    description: "Check if a number is prime"
  )
  .AddRoute<FibonacciCommand>
  (
    pattern: "fibonacci {n:int}",
    description: "Calculate the nth Fibonacci number"
  )
  .AddRoute<StatsCommand, StatsResponse> // Example: Mediator command that returns a response object
  (
    pattern: "stats {*values}",
    description: "Calculate statistics for a set of numbers (returns JSON)"
  )
  .AddRoute // Example: Delegate that returns an object
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
    public async Task Handle(FactorialCommand request, CancellationToken cancellationToken)
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

      await Task.CompletedTask;
    }
  }
}

public class PrimeCheckCommand : IRequest
{
  public int N { get; set; }

  public sealed class Handler(IScientificCalculator calc) : IRequestHandler<PrimeCheckCommand>
  {
    public async Task Handle(PrimeCheckCommand request, CancellationToken cancellationToken)
    {
      bool result = calc.IsPrime(request.N);
      WriteLine($"{request.N} is {(result ? "prime" : "not prime")}");
      await Task.CompletedTask;
    }
  }
}

public class FibonacciCommand : IRequest
{
  public int N { get; set; }

  public sealed class Handler(IScientificCalculator calc) : IRequestHandler<FibonacciCommand>
  {
    public async Task Handle(FibonacciCommand request, CancellationToken cancellationToken)
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

      await Task.CompletedTask;
    }
  }
}

// Example: Mediator command with response
public class StatsCommand : IRequest<StatsResponse>
{
  public string Values { get; set; } = "";

  internal sealed class Handler : IRequestHandler<StatsCommand, StatsResponse>
  {
    public Task<StatsResponse> Handle(StatsCommand request, CancellationToken cancellationToken)
    {
      if (string.IsNullOrWhiteSpace(request.Values))
      {
        return Task.FromResult(new StatsResponse());
      }

      double[] values =
      [
        .. request.Values.Split(' ', StringSplitOptions.RemoveEmptyEntries)
          .Select(v => double.TryParse(v, out double d) ? d : 0)
          .Where(v => v != 0)
      ];

      if (values.Length == 0)
      {
        return Task.FromResult(new StatsResponse());
      }

      return Task.FromResult
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
