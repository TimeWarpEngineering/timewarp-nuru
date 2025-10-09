#!/usr/bin/dotnet --
// calc-delegate - Calculator using Delegate approach for maximum performance
#:project ../../Source/TimeWarp.Nuru/TimeWarp.Nuru.csproj

using TimeWarp.Nuru;
using static System.Console;

// Calculator using Delegate approach for maximum performance
var app = new NuruAppBuilder()
    .AddAutoHelp() // Automatically add a help command

    // Basic operations
    .AddRoute
    (
      "add {x:double} {y:double}",
      (double x, double y) => WriteLine($"{x} + {y} = {x + y}"),
      "Add two numbers together"
    )

    .AddRoute
    (
      "subtract {x:double} {y:double}",
      (double x, double y) => WriteLine($"{x} - {y} = {x - y}"),
      "Subtract the second number from the first"
    )

    .AddRoute
    (
      "multiply {x:double} {y:double}",
      (double x, double y) => WriteLine($"{x} × {y} = {x * y}"),
      "Multiply two numbers together"
    )

    .AddRoute
    (
      "divide {x:double} {y:double}",
        (double x, double y) =>
        {
          if (y == 0)
          {
            WriteLine("Error: Division by zero");
            return;
          }
          WriteLine($"{x} ÷ {y} = {x / y}");
        },
        "Divide the first number by the second"
      )

    // Rounding with options
    .AddRoute
    (
      "round {value:double} --mode {mode}",
      (double value, string mode) =>
      {
        var result = mode.ToLower() switch
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
      "Round a number using specified mode (up, down, nearest, banker/accountancy)"
    )

    // Default rounding (nearest)
    .AddRoute
    (
      "round {value:double}",
      (double value) => WriteLine($"Round({value}) = {Math.Round(value)}"),
      "Round a number to the nearest integer"
    )

    .Build();

return await app.RunAsync(args);