#!/usr/bin/dotnet --
#:project ../../source/timewarp-nuru/timewarp-nuru.csproj

// ═══════════════════════════════════════════════════════════════════════════════
// COMMAND PATTERN - CALCULATOR EXAMPLE
// ═══════════════════════════════════════════════════════════════════════════════
//
// This sample demonstrates the command/handler pattern for CLI commands with:
// - Testable command handlers via dependency injection
// - Clean separation of concerns (commands, handlers, services)
// - Type-safe parameter binding via attributes
// - Auto-discovery of [NuruRoute] attributed classes by source generator
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
//
// ═══════════════════════════════════════════════════════════════════════════════

using TimeWarp.Nuru;
using Microsoft.Extensions.DependencyInjection;
using static System.Console;

NuruApp app = NuruApp.CreateBuilder(args)
  .ConfigureServices(services => services.AddSingleton<ICalculatorService, CalculatorService>())
  .Build();

return await app.RunAsync(args);

// Command definitions with nested handlers - discovered automatically via [NuruRoute]

[NuruRoute("add", Description = "Add two numbers together")]
public sealed class AddCommand : ICommand<Unit>
{
  [Parameter(Order = 0)]
  public double X { get; set; }

  [Parameter(Order = 1)]
  public double Y { get; set; }

  public sealed class Handler(ICalculatorService calc) : ICommandHandler<AddCommand, Unit>
  {
    public ValueTask<Unit> Handle(AddCommand command, CancellationToken cancellationToken)
    {
      double result = calc.Add(command.X, command.Y);
      WriteLine($"{command.X} + {command.Y} = {result}");
      return default;
    }
  }
}

[NuruRoute("subtract", Description = "Subtract the second number from the first")]
public sealed class SubtractCommand : ICommand<Unit>
{
  [Parameter(Order = 0)]
  public double X { get; set; }

  [Parameter(Order = 1)]
  public double Y { get; set; }

  public sealed class Handler(ICalculatorService calc) : ICommandHandler<SubtractCommand, Unit>
  {
    public ValueTask<Unit> Handle(SubtractCommand command, CancellationToken cancellationToken)
    {
      double result = calc.Subtract(command.X, command.Y);
      WriteLine($"{command.X} - {command.Y} = {result}");
      return default;
    }
  }
}

[NuruRoute("multiply", Description = "Multiply two numbers together")]
public sealed class MultiplyCommand : ICommand<Unit>
{
  [Parameter(Order = 0)]
  public double X { get; set; }

  [Parameter(Order = 1)]
  public double Y { get; set; }

  public sealed class Handler(ICalculatorService calc) : ICommandHandler<MultiplyCommand, Unit>
  {
    public ValueTask<Unit> Handle(MultiplyCommand command, CancellationToken cancellationToken)
    {
      double result = calc.Multiply(command.X, command.Y);
      WriteLine($"{command.X} × {command.Y} = {result}");
      return default;
    }
  }
}

[NuruRoute("divide", Description = "Divide the first number by the second")]
public sealed class DivideCommand : ICommand<Unit>
{
  [Parameter(Order = 0)]
  public double X { get; set; }

  [Parameter(Order = 1)]
  public double Y { get; set; }

  public sealed class Handler(ICalculatorService calc) : ICommandHandler<DivideCommand, Unit>
  {
    public ValueTask<Unit> Handle(DivideCommand command, CancellationToken cancellationToken)
    {
      (double result, string? error) = calc.Divide(command.X, command.Y);
      if (error != null)
        WriteLine($"Error: {error}");
      else
        WriteLine($"{command.X} ÷ {command.Y} = {result}");
      return default;
    }
  }
}

[NuruRoute("round", Description = "Round a number using specified mode (up, down, nearest, banker/accountancy)")]
public sealed class RoundCommand : ICommand<Unit>
{
  [Parameter(Order = 0)]
  public double Value { get; set; }

  [Option("mode", "m", Description = "Rounding mode: up, down, nearest, banker")]
  public string? Mode { get; set; }

  public sealed class Handler(ICalculatorService calc) : ICommandHandler<RoundCommand, Unit>
  {
    public ValueTask<Unit> Handle(RoundCommand command, CancellationToken cancellationToken)
    {
      (double result, string? error) = calc.Round(command.Value, command.Mode ?? "nearest");
      if (error != null)
      {
        WriteLine($"Error: {error}");
        WriteLine("Valid modes: up, down, nearest, banker/accountancy");
      }
      else
        WriteLine($"Round({command.Value}, {command.Mode ?? "nearest"}) = {result}");

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
