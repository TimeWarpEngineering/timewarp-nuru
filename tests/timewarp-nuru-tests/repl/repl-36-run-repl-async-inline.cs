#!/usr/bin/env -S dotnet --
#:project $(SourceDirectory)timewarp-nuru/timewarp-nuru.csproj

#region Purpose
// Pins whether RunReplAsync() is intercepted when called inline, isolating bug #363 from cross-method
// tracking.
#endregion

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
