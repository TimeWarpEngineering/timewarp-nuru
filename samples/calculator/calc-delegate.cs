#!/usr/bin/dotnet --
// calc-delegate - Calculator using Delegate approach for maximum performance
// Uses CreateSlimBuilder for lightweight delegate-only patterns (no DI, no Mediator)
// Note: CreateSlimBuilder includes auto-help, Configuration, and logging by default
#:project ../../source/timewarp-nuru/timewarp-nuru.csproj

using TimeWarp.Nuru;
using static System.Console;

NuruCoreApp app =
  NuruCoreApp.CreateSlimBuilder(args)
  .Map // Basic operations
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
  .Map // Rounding with options
  (
    pattern: "round {value:double} --mode {mode}",
    handler: (double value, string mode) =>
    {
      double result = mode.ToLower() switch
      {
        "up" => Math.Ceiling(value),
        "down" => Math.Floor(value),
        "nearest" => Math.Round(value),
        "banker" or "accountancy" => Math.Round(value, MidpointRounding.ToEven),
        _ => double.NaN
      };

      if (double.IsNaN(result))
      {
        WriteLine($"Error: Unknown rounding mode '{mode}'");
        WriteLine("Valid modes: up, down, nearest, banker/accountancy");
        return;
      }

      WriteLine($"Round({value}, {mode}) = {result}");
    },
    description: "Round a number using specified mode (up, down, nearest, banker/accountancy)"
  )
  .Map // Default rounding (nearest)
  (
    pattern: "round {value:double}",
    handler: (double value) => WriteLine($"Round({value}) = {Math.Round(value)}"),
    description: "Round a number to the nearest integer"
  )
  .Build();

return await app.RunAsync(args);