// calc.cs - Shared calculator source for dual-build testing (AppA/AppB)
// This file is compiled by both appA and appB projects
// to enable parity testing between runtime and source-generated paths.

using TimeWarp.Nuru;
using static System.Console;

NuruCoreApp app =
  NuruCoreApp.CreateSlimBuilder(args)
  .Map("add {x:double} {y:double}")
    .WithHandler((double x, double y) => WriteLine($"{x} + {y} = {x + y}"))
    .WithDescription("Add two numbers")
    .AsQuery()
    .Done()
  .Map("subtract {x:double} {y:double}")
    .WithHandler((double x, double y) => WriteLine($"{x} - {y} = {x - y}"))
    .WithDescription("Subtract two numbers")
    .AsQuery()
    .Done()
  .Map("multiply {x:double} {y:double}")
    .WithHandler((double x, double y) => WriteLine($"{x} * {y} = {x * y}"))
    .WithDescription("Multiply two numbers")
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
      WriteLine($"{x} / {y} = {x / y}");
    })
    .WithDescription("Divide two numbers")
    .AsQuery()
    .Done()
  .Build();

return await app.RunAsync(args);
