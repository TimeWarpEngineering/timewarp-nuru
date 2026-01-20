#!/usr/bin/dotnet --
#:project ../../../source/timewarp-nuru/timewarp-nuru.csproj

// Test if RunReplAsync() is intercepted when called inline (no Setup method)
// This verifies whether bug #363 is about cross-method tracking or if
// RunReplAsync is not intercepted at all.

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.ReplTests.RunReplAsyncInline
{

[TestTag("REPL")]
public class RunReplAsyncInlineTests
{
  [ModuleInitializer]
  internal static void Register() => RegisterTests<RunReplAsyncInlineTests>();

  public static async Task Should_intercept_run_repl_async_inline()
  {
    // Arrange - all inline, no Setup method
    using TestTerminal terminal = new();
    terminal.QueueLine("exit");

    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("hello")
        .WithHandler(() => "Hello!")
        .AsQuery()
        .Done()
      .AddRepl()
      .Build();

    // Act - call RunReplAsync directly inline
    await app.RunReplAsync();

    // Assert
    terminal.OutputContains("Goodbye!")
      .ShouldBeTrue("RunReplAsync should be intercepted when called inline");
  }
}

}
