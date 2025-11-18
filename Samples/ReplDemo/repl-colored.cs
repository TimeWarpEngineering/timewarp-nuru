#!/usr/bin/dotnet --
#:project ../TimeWarp.Nuru.Repl/TimeWarp.Nuru.Repl.csproj
#:project ../../TimeWarp.Nuru/TimeWarp.Nuru.csproj

// Sample: REPL with colored prompts and execution timing
// Usage: ./repl-colored.cs
// Observe: Green prompt, timing after commands, red for errors

using TimeWarp.Nuru;
using TimeWarp.Nuru.Repl;

var builder = new NuruAppBuilder()
  .AddRoute("ping", () =>
  {
    Thread.Sleep(50); // Simulate work
    Console.WriteLine("Pong!");
  })
  .AddRoute("fail", () => throw new ArgumentException("Intentional failure"));

var app = builder.Build();
var options = new ReplOptions
{
  EnableColors = true,
  ShowTiming = true,
  Prompt = "color> ",
  WelcomeMessage = "REPL Sample with Colors and Timing. Errors in red, timing in gray."
};

await app.RunReplAsync(options);