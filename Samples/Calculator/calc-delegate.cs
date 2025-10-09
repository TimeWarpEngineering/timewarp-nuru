#!/usr/bin/dotnet --
// calc-delegate - Calculator using Delegate approach for maximum performance
#:project ../../Source/TimeWarp.Nuru/TimeWarp.Nuru.csproj

using TimeWarp.Nuru;
using static System.Console;

NuruApp app =
  new NuruAppBuilder()
  .AddAutoHelp()
  .AddRoute // Basic operations
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
  .AddRoute // Rounding with options
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
  .AddRoute // Default rounding (nearest)
  (
    pattern: "round {value:double}",
    handler: (double value) => WriteLine($"Round({value}) = {Math.Round(value)}"),
    description: "Round a number to the nearest integer"
  )
  .Build();

return await app.RunAsync(args);