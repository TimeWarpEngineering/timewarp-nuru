#!/usr/bin/dotnet --
#:project ../../source/timewarp-nuru/timewarp-nuru.csproj
#:package Mediator.Abstractions
#:package Mediator.SourceGenerator

// ═══════════════════════════════════════════════════════════════════════════════
// MEDIATOR PATTERN - CALCULATOR EXAMPLE
// ═══════════════════════════════════════════════════════════════════════════════
//
// This sample demonstrates the Mediator pattern for CLI commands with:
// - Testable command handlers via dependency injection
// - Clean separation of concerns (commands, handlers, services)
// - Type-safe parameter binding
//
// REQUIRED PACKAGES:
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
  .Map<AddCommand>("add {x:double} {y:double}")
    .WithDescription("Add two numbers together")
  .Map<SubtractCommand>("subtract {x:double} {y:double}")
    .WithDescription("Subtract the second number from the first")
  .Map<MultiplyCommand>("multiply {x:double} {y:double}")
    .WithDescription("Multiply two numbers together")
  .Map<DivideCommand>("divide {x:double} {y:double}")
    .WithDescription("Divide the first number by the second")
  .Map<RoundCommand>("round {value:double} --mode {mode}")
    .WithDescription("Round a number using specified mode (up, down, nearest, banker/accountancy)")
  .Map<RoundCommand>("round {value:double}")
    .WithDescription("Round a number to the nearest integer")
  .Build();

return await app.RunAsync(args);

static void ConfigureServices(IServiceCollection services)
{
  // Register Mediator - source generator discovers handlers in THIS assembly
  services.AddMediator();
  services.AddSingleton<ICalculatorService, CalculatorService>();
}

// Command definitions with nested handlers
public sealed class AddCommand : IRequest
{
  public double X { get; set; }
  public double Y { get; set; }

  public sealed class Handler(ICalculatorService calc) : IRequestHandler<AddCommand>
  {
    public ValueTask<Unit> Handle(AddCommand request, CancellationToken cancellationToken)
    {
      double result = calc.Add(request.X, request.Y);
      WriteLine($"{request.X} + {request.Y} = {result}");
      return default;
    }
  }
}

public sealed class SubtractCommand : IRequest
{
  public double X { get; set; }
  public double Y { get; set; }

  public sealed class Handler(ICalculatorService calc) : IRequestHandler<SubtractCommand>
  {
    public ValueTask<Unit> Handle(SubtractCommand request, CancellationToken cancellationToken)
    {
      double result = calc.Subtract(request.X, request.Y);
      WriteLine($"{request.X} - {request.Y} = {result}");
      return default;
    }
  }
}

public sealed class MultiplyCommand : IRequest
{
  public double X { get; set; }
  public double Y { get; set; }

  public sealed class Handler(ICalculatorService calc) : IRequestHandler<MultiplyCommand>
  {
    public ValueTask<Unit> Handle(MultiplyCommand request, CancellationToken cancellationToken)
    {
      double result = calc.Multiply(request.X, request.Y);
      WriteLine($"{request.X} × {request.Y} = {result}");
      return default;
    }
  }
}

public sealed class DivideCommand : IRequest
{
  public double X { get; set; }
  public double Y { get; set; }

  public sealed class Handler(ICalculatorService calc) : IRequestHandler<DivideCommand>
  {
    public ValueTask<Unit> Handle(DivideCommand request, CancellationToken cancellationToken)
    {
      (double result, string? error) = calc.Divide(request.X, request.Y);
      if (error != null)
        WriteLine($"Error: {error}");
      else
        WriteLine($"{request.X} ÷ {request.Y} = {result}");
      return default;
    }
  }
}

public sealed class RoundCommand : IRequest
{
  public double Value { get; set; }
  public string? Mode { get; set; } = "nearest";

  public sealed class Handler(ICalculatorService calc) : IRequestHandler<RoundCommand>
  {
    public ValueTask<Unit> Handle(RoundCommand request, CancellationToken cancellationToken)
    {
      (double result, string? error) = calc.Round(request.Value, request.Mode ?? "nearest");
      if (error != null)
      {
        WriteLine($"Error: {error}");
        WriteLine("Valid modes: up, down, nearest, banker/accountancy");
      }
      else
        WriteLine($"Round({request.Value}, {request.Mode ?? "nearest"}) = {result}");

      return default;
    }
  }
}

// Service interface
public interface ICalculatorService
{
  double Add(double x, double y);
  double Subtract(double x, double y);
  double Multiply(double x, double y);
  (double result, string? error) Divide(double x, double y);
  (double result, string? error) Round(double value, string mode);
}

// Service implementation
public class CalculatorService : ICalculatorService
{
  public double Add(double x, double y) => x + y;

  public double Subtract(double x, double y) => x - y;

  public double Multiply(double x, double y) => x * y;

  public (double result, string? error) Divide(double x, double y) =>
    y == 0
    ? (0, "Division by zero")
    : (x / y, null);

  public (double result, string? error) Round(double value, string mode) =>
    mode.ToLower() switch
    {
      "up" => (Math.Ceiling(value), null),
      "down" => (Math.Floor(value), null),
      "nearest" => (Math.Round(value), null),
      "banker" or "accountancy" => (Math.Round(value, MidpointRounding.ToEven), null),
      _ => (0, $"Unknown rounding mode '{mode}'")
    };
}
