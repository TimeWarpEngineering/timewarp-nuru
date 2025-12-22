#!/usr/bin/dotnet --
// calc-createbuilder - Calculator using ASP.NET Core-style CreateBuilder API
#:project ../../source/timewarp-nuru/timewarp-nuru.csproj
#:package Mediator.Abstractions
#:package Mediator.SourceGenerator

using TimeWarp.Nuru;
using Mediator;
using Microsoft.Extensions.DependencyInjection;
using static System.Console;

// ASP.NET Core-style API - familiar to web developers
NuruAppBuilder builder = NuruApp.CreateBuilder(args);

// ConfigureServices works just like ASP.NET Core
builder.ConfigureServices(services =>
{
  // Register Mediator - source generator discovers handlers in THIS assembly

  builder.Services.AddMediator();
  services.AddSingleton<IScientificCalculator, ScientificCalculator>();
});

// Map() fluent API - inspired by ASP.NET Core's app.Map()
builder.Map("add {x:double} {y:double}")
  .WithHandler((double x, double y) => WriteLine($"{x} + {y} = {x + y}"))
  .WithDescription("Add two numbers together")
  .AsQuery()
  .Done();

builder.Map("subtract {x:double} {y:double}")
  .WithHandler((double x, double y) => WriteLine($"{x} - {y} = {x - y}"))
  .WithDescription("Subtract the second number from the first")
  .AsQuery()
  .Done();

builder.Map("multiply {x:double} {y:double}")
  .WithHandler((double x, double y) => WriteLine($"{x} × {y} = {x * y}"))
  .WithDescription("Multiply two numbers together")
  .AsQuery()
  .Done();

builder.Map("divide {x:double} {y:double}")
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
  .AsQuery()
  .Done();

// Map<TCommand> works with Mediator pattern
builder.Map<FactorialCommand>("factorial {n:int}")
  .WithDescription("Calculate factorial (n!)")
  .AsQuery()
  .Done();
builder.Map<PrimeCheckCommand>("isprime {n:int}")
  .WithDescription("Check if a number is prime")
  .AsQuery()
  .Done();
builder.Map<FibonacciCommand>("fibonacci {n:int}")
  .WithDescription("Calculate the nth Fibonacci number")
  .AsQuery()
  .Done();

// Default route for when no arguments provided
builder.Map("")
  .WithHandler(() => WriteLine("Calculator - use --help for available commands"))
  .AsQuery()
  .Done();

// Build and run - same pattern as ASP.NET Core
NuruCoreApp app = builder.Build();
return await app.RunAsync(args);

// Command definitions (unchanged from calc-mixed.cs)
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
