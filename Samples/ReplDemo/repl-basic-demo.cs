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
  .WithMetadata(
    name: "repl-demo",
    description: "Interactive REPL demo application for TimeWarp.Nuru framework."
  )
  .AddRoute(
    "greet {name}", 
    (string name) => WriteLine($"Hello, {name}!")
  )
  .AddRoute(
    "status", 
    () => WriteLine("System is running OK")
  )
  .AddRoute(
    "echo {*message}", 
    (string[] message) => WriteLine(string.Join(" ", message))
  )
  .AddRoute(
    "add {a:int} {b:int}", 
    (int a, int b) => WriteLine($"{a} + {b} = {a + b}")
  )
  .AddRoute(
    "time", 
    () => WriteLine($"Current time: {DateTime.Now:HH:mm:ss}")
  )
  .AddReplSupport(options =>
  {
    options.Prompt = "demo> ";
    options.WelcomeMessage = "Welcome to REPL demo! Try: greet World, status, add 5 3, time, or 'exit' to quit.";
    options.GoodbyeMessage = "Thanks for trying the REPL demo!";
    options.PersistHistory = false; // Don't persist history for demo
  })
  .Build();

// Start REPL mode directly
return await app.RunReplAsync();
