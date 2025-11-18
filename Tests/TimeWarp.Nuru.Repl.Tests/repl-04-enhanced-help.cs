#!/usr/bin/dotnet --
#:project ../../Source/TimeWarp.Nuru.Repl/TimeWarp.Nuru.Repl.csproj
#:project ../../Source/TimeWarp.Nuru/TimeWarp.Nuru.csproj
#:project ../../Source/TimeWarp.Nuru.Completion/TimeWarp.Nuru.Completion.csproj

// Test: Enhanced help with CompletionProvider integration
#pragma warning disable IDE0007, IDE0008 // Suppress conflicting var/type analyzer rules
// Approach: Manual test - run REPL, type 'help', verify it lists application commands with descriptions
// Expected: Shows REPL commands + application routes from CompletionProvider

using TimeWarp.Nuru;
using TimeWarp.Nuru.Repl;
using TimeWarp.Nuru.Completion;

NuruAppBuilder builder = new NuruAppBuilder()
  .AddRoute("git status", () => Console.WriteLine("Git status output"), description: "Show git repository status")
      .AddRoute("deploy {env}", (string env) => { Console.WriteLine($"Deploying to {env}"); return 0; }, description: "Deploy to environment")
  .AddRoute("build --release", () => Console.WriteLine("Building in release mode"), description: "Build project");

var app = builder.Build();
ReplOptions options = new ReplOptions
{
  EnableColors = true
};

await app.RunReplAsync(options);