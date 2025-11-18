#!/usr/bin/dotnet --
#!/usr/bin/dotnet --
#:project ../../Source/TimeWarp.Nuru.Repl/TimeWarp.Nuru.Repl.csproj
#:project ../../Source/TimeWarp.Nuru/TimeWarp.Nuru.csproj
#:project ../../Source/TimeWarp.Nuru.Completion/TimeWarp.Nuru.Completion.csproj

// Test: Enhanced help with CompletionProvider integration
// Approach: Manual test - run REPL, type 'help', verify it lists application commands with descriptions
// Expected: Shows REPL commands + application routes from CompletionProvider

using TimeWarp.Nuru;
using TimeWarp.Nuru.Repl;

var builder = new NuruAppBuilder()
  .AddRoute("git status", () => Console.WriteLine("Git status output"), description: "Show git repository status")
      .AddRoute("deploy {env}", (string env) => { Console.WriteLine($"Deploying to {env}"); return 0; }, description: "Deploy to environment")
  .AddRoute("build --release", () => Console.WriteLine("Building in release mode"), description: "Build project");

var app = builder.Build();
var options = new ReplOptions
{
  EnableColors = true
};

await app.RunReplAsync(options);