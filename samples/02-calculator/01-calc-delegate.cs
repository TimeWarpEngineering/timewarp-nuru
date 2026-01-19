#!/usr/bin/dotnet --
// calc-delegate - Calculator using Delegate approach for maximum performance
// Uses CreateBuilder for lightweight delegate-only patterns
#:project ../../source/timewarp-nuru/timewarp-nuru.csproj

using TimeWarp.Nuru;
using static System.Console;

NuruCoreApp app =
  NuruApp.CreateBuilder(args)
  // Basic operations
  .Map("add {x:double} {y:double}")
    .WithHandler((double x, double y) => WriteLine($"{x} + {y} = {x + y}"))
    .WithDescription("Add two numbers together")
    .AsQuery()
    .Done()
  .Map("subtract {x:double} {y:double}")
    .WithHandler((double x, double y) => WriteLine($"{x} - {y} = {x - y}"))
    .WithDescription("Subtract the second number from the first")
    .AsQuery()
    .Done()
  .Map("multiply {x:double} {y:double}")
    .WithHandler((double x, double y) => WriteLine($"{x} × {y} = {x * y}"))
    .WithDescription("Multiply two numbers together")
    .AsQuery()
    .Done()
  .Map("divide {x:double} {y:double}")
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
    .Done()
  // Rounding with options
  .Map("round {value:double} --mode {mode}")
    .WithHandler((double value, string mode) =>
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
    })
    .WithDescription("Round a number using specified mode (up, down, nearest, banker/accountancy)")
    .AsQuery()
    .Done()
  // Default rounding (nearest)
  .Map("round {value:double}")
    .WithHandler((double value) => WriteLine($"Round({value}) = {Math.Round(value)}"))
    .WithDescription("Round a number to the nearest integer")
    .AsQuery()
    .Done()
  .Build();

return await app.RunAsync(args);