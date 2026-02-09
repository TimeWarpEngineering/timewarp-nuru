#!/usr/bin/dotnet --
// ═══════════════════════════════════════════════════════════════════════════════
// CALCULATOR - ENDPOINT DSL ⭐ RECOMMENDED
// ═══════════════════════════════════════════════════════════════════════════════
// Full-featured calculator using Endpoint DSL pattern.
// Demonstrates: Commands with parameters, dependency injection, testable handlers
// DSL: Endpoint (class-based with [NuruRoute], nested Handler classes)
// ═══════════════════════════════════════════════════════════════════════════════
#:project ../../../source/timewarp-nuru/timewarp-nuru.csproj

using TimeWarp.Nuru;
using Microsoft.Extensions.DependencyInjection;

NuruApp app = NuruApp.CreateBuilder()
  .ConfigureServices(ConfigureServices)
  .DiscoverEndpoints()
  .Build();

return await app.RunAsync(args);

static void ConfigureServices(IServiceCollection services)
{
  services.AddSingleton<IScientificCalculator, ScientificCalculator>();
}

// =============================================================================
// BASIC OPERATIONS
// =============================================================================

[NuruRoute("add", Description = "Add two numbers together")]
public sealed class AddCommand : IQuery<double>
{
  [Parameter(Description = "First number")]
  public double X { get; set; }

  [Parameter(Description = "Second number")]
  public double Y { get; set; }

  public sealed class Handler : IQueryHandler<AddCommand, double>
  {
    public ValueTask<double> Handle(AddCommand command, CancellationToken ct)
    {
      return new ValueTask<double>(command.X + command.Y);
    }
  }
}

[NuruRoute("subtract", Description = "Subtract the second number from the first")]
public sealed class SubtractCommand : IQuery<double>
{
  [Parameter(Description = "First number")]
  public double X { get; set; }

  [Parameter(Description = "Second number")]
  public double Y { get; set; }

  public sealed class Handler : IQueryHandler<SubtractCommand, double>
  {
    public ValueTask<double> Handle(SubtractCommand command, CancellationToken ct)
    {
      return new ValueTask<double>(command.X - command.Y);
    }
  }
}

[NuruRoute("multiply", Description = "Multiply two numbers together")]
public sealed class MultiplyCommand : IQuery<double>
{
  [Parameter(Description = "First number")]
  public double X { get; set; }

  [Parameter(Description = "Second number")]
  public double Y { get; set; }

  public sealed class Handler : IQueryHandler<MultiplyCommand, double>
  {
    public ValueTask<double> Handle(MultiplyCommand command, CancellationToken ct)
    {
      return new ValueTask<double>(command.X * command.Y);
    }
  }
}

[NuruRoute("divide", Description = "Divide the first number by the second")]
public sealed class DivideCommand : IQuery<double>
{
  [Parameter(Description = "Dividend")]
  public double X { get; set; }

  [Parameter(Description = "Divisor")]
  public double Y { get; set; }

  public sealed class Handler : IQueryHandler<DivideCommand, double>
  {
    public ValueTask<double> Handle(DivideCommand command, CancellationToken ct)
    {
      if (command.Y == 0)
      {
        throw new DivideByZeroException("Cannot divide by zero");
      }

      return new ValueTask<double>(command.X / command.Y);
    }
  }
}

// =============================================================================
// ADVANCED OPERATIONS (with DI)
// =============================================================================

[NuruRoute("factorial", Description = "Calculate factorial (n!)")]
public sealed class FactorialCommand : ICommand<Unit>
{
  [Parameter(Description = "Number to calculate factorial for")]
  public int N { get; set; }

  public sealed class Handler(IScientificCalculator calc) : ICommandHandler<FactorialCommand, Unit>
  {
    public ValueTask<Unit> Handle(FactorialCommand command, CancellationToken cancellationToken)
    {
      try
      {
        long result = calc.Factorial(command.N);
        Console.WriteLine($"{command.N}! = {result}");
      }
      catch (ArgumentException ex)
      {
        Console.WriteLine($"Error: {ex.Message}");
      }

      return default;
    }
  }
}

[NuruRoute("isprime", Description = "Check if a number is prime")]
public sealed class PrimeCheckCommand : ICommand<Unit>
{
  [Parameter(Description = "Number to check")]
  public int N { get; set; }

  public sealed class Handler(IScientificCalculator calc) : ICommandHandler<PrimeCheckCommand, Unit>
  {
    public ValueTask<Unit> Handle(PrimeCheckCommand command, CancellationToken cancellationToken)
    {
      bool result = calc.IsPrime(command.N);
      Console.WriteLine($"{command.N} is {(result ? "prime" : "not prime")}");
      return default;
    }
  }
}

[NuruRoute("fibonacci", Description = "Calculate the nth Fibonacci number")]
public sealed class FibonacciCommand : ICommand<Unit>
{
  [Parameter(Description = "Index in Fibonacci sequence")]
  public int N { get; set; }

  public sealed class Handler(IScientificCalculator calc) : ICommandHandler<FibonacciCommand, Unit>
  {
    public ValueTask<Unit> Handle(FibonacciCommand command, CancellationToken cancellationToken)
    {
      try
      {
        long result = calc.Fibonacci(command.N);
        Console.WriteLine($"Fibonacci({command.N}) = {result}");
      }
      catch (ArgumentException ex)
      {
        Console.WriteLine($"Error: {ex.Message}");
      }

      return default;
    }
  }
}

[NuruRoute("round", Description = "Round a number to the nearest integer")]
public sealed class RoundCommand : IQuery<double>
{
  [Parameter(Description = "Number to round")]
  public double Value { get; set; }

  [Option("mode", "m", Description = "Rounding mode: up, down, nearest, banker")]
  public string Mode { get; set; } = "nearest";

  public sealed class Handler : IQueryHandler<RoundCommand, double>
  {
    public ValueTask<double> Handle(RoundCommand command, CancellationToken ct)
    {
      double result = command.Mode.ToLower() switch
      {
        "up" => Math.Ceiling(command.Value),
        "down" => Math.Floor(command.Value),
        "nearest" => Math.Round(command.Value),
        "banker" or "accountancy" => Math.Round(command.Value, MidpointRounding.ToEven),
        _ => throw new ArgumentException($"Unknown rounding mode: {command.Mode}")
      };

      return new ValueTask<double>(result);
    }
  }
}

[NuruRoute("stats", Description = "Calculate statistics for a set of numbers")]
public sealed class StatsCommand : IQuery<StatsResponse>
{
  [Parameter(IsCatchAll = true, Description = "Numbers to analyze")]
  public string[] Values { get; set; } = [];

  public sealed class Handler : IQueryHandler<StatsCommand, StatsResponse>
  {
    public ValueTask<StatsResponse> Handle(StatsCommand command, CancellationToken ct)
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

// =============================================================================
// SUPPORTING TYPES
// =============================================================================

public class StatsResponse
{
  public double Sum { get; set; }
  public double Average { get; set; }
  public double Min { get; set; }
  public double Max { get; set; }
  public int Count { get; set; }
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
