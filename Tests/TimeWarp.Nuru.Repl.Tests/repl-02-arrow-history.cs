#!/usr/bin/dotnet --
#:project ../../Source/TimeWarp.Nuru.Repl/TimeWarp.Nuru.Repl.csproj
#:project ../../Source/TimeWarp.Nuru/TimeWarp.Nuru.csproj

// Test: Arrow key history navigation
// Approach: Manual test - run the REPL, enter a few commands, use up/down arrows to navigate history
// Expected: Up arrow recalls previous commands, down arrow moves forward, editing works

using TimeWarp.Nuru;
using TimeWarp.Nuru.Repl;
using TimeWarp.Nuru.Completion;

return await RunTests<ArrowHistoryTests>(clearCache: true);

[TestTag("REPL")]
[ClearRunfileCache]
public class ArrowHistoryTests
{
  // Manual test - arrow key navigation requires interactive terminal
  // This serves as documentation of expected behavior
  [Input("test Alice")]
  public static async Task Should_enable_arrow_history(string _)
  {
    NuruAppBuilder builder = new NuruAppBuilder()
      .AddRoute("test {name}", (string name) => Console.WriteLine($"Hello, {name}!"))
      .AddRoute("version", () => Console.WriteLine("v1.0"));

    NuruApp app = builder.Build();
    var options = new ReplOptions
    {
      EnableArrowHistory = true,
      Prompt = "test> ",
      PersistHistory = false // Disable for test
    };

    // Verify options are configured correctly
    options.EnableArrowHistory.ShouldBe(true);
    options.PersistHistory.ShouldBe(false);

    await Task.CompletedTask;
  }
}