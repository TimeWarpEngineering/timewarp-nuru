#!/usr/bin/dotnet --
// calc-createbuilder - Calculator using ASP.NET Core-style CreateBuilder API
#:project ../../Source/TimeWarp.Nuru/TimeWarp.Nuru.csproj

using TimeWarp.Nuru;
using TimeWarp.Mediator;
using Microsoft.Extensions.DependencyInjection;
using static System.Console;

// ASP.NET Core-style API - familiar to web developers
var builder = NuruApp.CreateBuilder(args);

// ConfigureServices works just like ASP.NET Core
builder.ConfigureServices(services =>
{
  services.AddSingleton<IScientificCalculator, ScientificCalculator>();

  // Register mediator handlers
  services.AddTransient<IRequestHandler<FactorialCommand>, FactorialCommand.Handler>();
  services.AddTransient<IRequestHandler<PrimeCheckCommand>, PrimeCheckCommand.Handler>();
  services.AddTransient<IRequestHandler<FibonacciCommand>, FibonacciCommand.Handler>();
});

// Map() is an alias for AddRoute() - just like ASP.NET Core's app.Map()
builder.Map("add {x:double} {y:double}",
  (double x, double y) => WriteLine($"{x} + {y} = {x + y}"),
  "Add two numbers together");

builder.Map("subtract {x:double} {y:double}",
  (double x, double y) => WriteLine($"{x} - {y} = {x - y}"),
  "Subtract the second number from the first");

builder.Map("multiply {x:double} {y:double}",
  (double x, double y) => WriteLine($"{x} × {y} = {x * y}"),
  "Multiply two numbers together");

builder.Map("divide {x:double} {y:double}",
  (double x, double y) =>
  {
    if (y == 0)
    {
      WriteLine("Error: Division by zero");
      return;
    }
    WriteLine($"{x} ÷ {y} = {x / y}");
  },
  "Divide the first number by the second");

// Map<TCommand> works with Mediator pattern
builder.Map<FactorialCommand>("factorial {n:int}", "Calculate factorial (n!)");
builder.Map<PrimeCheckCommand>("isprime {n:int}", "Check if a number is prime");
builder.Map<FibonacciCommand>("fibonacci {n:int}", "Calculate the nth Fibonacci number");

// MapDefault for when no arguments provided
builder.MapDefault(() =>
{
  WriteLine("Calculator - use --help for available commands");
  return 0;
});

// Build and run - same pattern as ASP.NET Core
var app = builder.Build();
return await app.RunAsync(args);

// Command definitions (unchanged from calc-mixed.cs)
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
