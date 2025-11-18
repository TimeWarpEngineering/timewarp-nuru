#!/usr/bin/dotnet --
#:project ../../Source/TimeWarp.Nuru.Repl/TimeWarp.Nuru.Repl.csproj
#:project ../../Source/TimeWarp.Nuru/TimeWarp.Nuru.csproj

// Sample: REPL with arrow key history navigation
// Usage: ./repl-arrow-history.cs
// Test: Enter commands like 'greet Alice', 'version', use up/down arrows to recall

using TimeWarp.Nuru;
using TimeWarp.Nuru.Repl;

var builder = new NuruAppBuilder()
  .AddRoute("greet {name}", (string name) => Console.WriteLine($"Hello, {name}!"))
  .AddRoute("version", () => Console.WriteLine("TimeWarp.Nuru REPL Sample v1.0"))
  .AddRoute("calc {a:int} add {b:int}", (int a, int b) => Console.WriteLine($"{a} + {b} = {a + b}"));

var app = builder.Build();
var options = new ReplOptions
{
  EnableArrowHistory = true,
  ShowTiming = true,
  Prompt = "arrow> ",
  WelcomeMessage = "REPL Sample with Arrow History. Use up/down arrows to navigate history."
};

var repl = new ReplMode(app, options);
await repl.RunAsync();