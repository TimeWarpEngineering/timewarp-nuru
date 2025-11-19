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
  .WithMetadata
  (
    name: "repl-demo",
    description: "Interactive REPL demo application for TimeWarp.Nuru framework."
  )
  .AddRoute
  (
    pattern:"greet {name}", 
    handler: (string name) => WriteLine($"Hello, {name}!"),
    description: "Greets the person with the specified name."
  )
  .AddRoute
  (
    pattern:"status",
    handler: () => WriteLine("System is running OK"),
    description: "Displays the current system status."
  )
  .AddRoute
  (
    pattern:"echo {*message}", 
    handler: (string[] message) => WriteLine(string.Join(" ", message)),
    description: "Echoes the provided message back to the user."
  )
  .AddRoute
  (
    pattern:"add {a:int} {b:int}", 
    handler: (int a, int b) => WriteLine($"{a} + {b} = {a + b}"),
    description: "Adds two integers and displays the result."
  )
  .AddRoute(
    pattern:"time", 
    handler: () => WriteLine($"Current time: {DateTime.Now:HH:mm:ss}"),
    description: "Displays the current time."
  )
  .AddReplSupport
  (
    options =>
    {
      options.Prompt = "demo> ";
      options.WelcomeMessage = "Welcome to REPL demo! Try: greet World, status, add 5 3, time, or 'exit' to quit.";
      options.GoodbyeMessage = "Thanks for trying the REPL demo!";
      options.PersistHistory = false; // Don't persist history for demo
    }
  )
  .Build();

// Start REPL mode directly
return await app.RunReplAsync();
