#!/usr/bin/dotnet --

#:project ../../Source/TimeWarp.Nuru.Repl/TimeWarp.Nuru.Repl.csproj
#:project ../../Source/TimeWarp.Nuru/TimeWarp.Nuru.csproj

// Test: Colored output and execution timing
// Approach: Manual test - run REPL, execute commands, verify colored prompts/errors and timing display
// Expected: Green prompt, red errors, gray timing info after commands

using TimeWarp.Nuru;
using TimeWarp.Nuru.Repl;

    NuruAppBuilder builder = new NuruAppBuilder()
      .AddRoute("slow", async () =>
      {
        await Task.Delay(100);
        Console.WriteLine("Slow command executed");
      })
      .AddRoute("error", (Func<int>)(() => throw new InvalidOperationException("Test error")));

    NuruApp app = builder.Build();
    var options = new ReplOptions
{
  EnableColors = true,
  ShowTiming = true,
  EnableArrowHistory = false // Focus on colors/timing
};

await app.RunReplAsync(options);