#!/usr/bin/dotnet --
#:project ../TimeWarp.Nuru.Repl/TimeWarp.Nuru.Repl.csproj
#:project ../../TimeWarp.Nuru/TimeWarp.Nuru.csproj
#:project ../../TimeWarp.Nuru.Completion/TimeWarp.Nuru.Completion.csproj

// Sample: REPL with enhanced help using CompletionProvider
// Usage: ./repl-with-hints.cs
// Test: Type 'help' to see integrated command list with descriptions

using TimeWarp.Nuru;
using TimeWarp.Nuru.Repl;

var builder = new NuruAppBuilder()
  .AddRoute("user list", () => Console.WriteLine("Users: Alice, Bob"), description: "List all users")
  .AddRoute("user add {name}", name => Console.WriteLine($"Added user {name}"), description: "Add a new user")
  .AddRoute("config set {key} {value}", (key, value) => Console.WriteLine($"Set {key}={value}"), description: "Set configuration")
  .AddRoute("help --detailed", () => Console.WriteLine("Detailed help"), description: "Show detailed help");

var app = builder.Build();
var options = new ReplOptions
{
  EnableColors = true,
  Prompt = "hints> ",
  WelcomeMessage = "REPL Sample with Enhanced Help. Type 'help' for command list."
};

await app.RunReplAsync(options);