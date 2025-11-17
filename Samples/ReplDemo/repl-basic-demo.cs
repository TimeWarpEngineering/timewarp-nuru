#!/usr/bin/dotnet --
#:project ../../Source/TimeWarp.Nuru/TimeWarp.Nuru.csproj
#:project ../../Source/TimeWarp.Nuru.Repl/TimeWarp.Nuru.Repl.csproj

// Basic REPL Demo for TimeWarp.Nuru
// Run this file to see REPL mode in action

using TimeWarp.Nuru;
using TimeWarp.Nuru.Repl;
using static System.Console;

WriteLine("TimeWarp.Nuru REPL Demo");
WriteLine("========================");
WriteLine();

// Build a simple CLI app
var app = new NuruAppBuilder()
  .AddRoute("greet {name}", (string name) => WriteLine($"Hello, {name}!"))
  .AddRoute("status", () => WriteLine("System is running OK"))
  .AddRoute("echo {*message}", (string[] message) => WriteLine(string.Join(" ", message)))
  .AddRoute("add {a:int} {b:int}", (int a, int b) => WriteLine($"{a} + {b} = {a + b}"))
  .AddRoute("time", () => WriteLine($"Current time: {DateTime.Now:HH:mm:ss}"))
  .Build();

// Start REPL mode directly
return await app.RunReplAsync(new ReplOptions
{
  Prompt = "demo> ",
  WelcomeMessage = "Welcome to the REPL demo! Try: greet World, status, add 5 3, time, or 'exit' to quit.",
  GoodbyeMessage = "Thanks for trying the REPL demo!",
  PersistHistory = false // Don't persist history for demo
});
