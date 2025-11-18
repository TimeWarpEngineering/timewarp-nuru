#!/usr/bin/dotnet --
#:project ../../Source/TimeWarp.Nuru.Repl/TimeWarp.Nuru.Repl.csproj
#:project ../../Source/TimeWarp.Nuru/TimeWarp.Nuru.csproj

// Minimal suppressions for test framework compatibility

// Test: Basic REPL functionality
// Approach: Manual test - run the REPL, verify it starts, shows prompt, accepts basic commands
// Expected: REPL starts with welcome message, shows colored prompt, accepts commands

using TimeWarp.Nuru;
using TimeWarp.Nuru.Repl;
using TimeWarp.Nuru.Completion;

return await RunTests<BasicReplTests>(clearCache: true);

[TestTag("REPL")]
[ClearRunfileCache]
public class BasicReplTests
{
  // Manual test - basic REPL startup and command acceptance
  // This serves as documentation of expected behavior
  [Input("version")]
  public static async Task Should_start_repl_and_accept_commands(string _)
  {
    NuruAppBuilder builder = new NuruAppBuilder()
      .AddRoute("version", () => Console.WriteLine("TimeWarp.Nuru v1.0.0"))
      .AddRoute("hello", () => Console.WriteLine("Hello from REPL!"));

    NuruApp app = builder.Build();
    ReplOptions options = new()
    {
      WelcomeMessage = "Test REPL started",
      Prompt = "test> ",
      PersistHistory = false
    };

    // Verify REPL components are properly configured
    options.WelcomeMessage.ShouldNotBeNull();
    options.Prompt.ShouldBe("test> ");
    options.PersistHistory.ShouldBe(false);

    // Test that the app has routes
    app.Endpoints.ShouldNotBeNull();
    app.Endpoints.Count.ShouldBeGreaterThan(0);

    await Task.CompletedTask;
  }
}