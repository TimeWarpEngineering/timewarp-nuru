#!/usr/bin/env -S dotnet --
#:project $(SourceDirectory)timewarp-nuru/timewarp-nuru.csproj

// Verifies the non-intercepted RunReplAsync stub tells the caller to ENABLE the
// source generator (482-004 / review R-6). The stub is reached through a method
// group, which interceptors do not replace, so the fallback body actually runs.

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.ReplTests.RunReplAsyncStubMessage
{

[TestTag("REPL")]
public class RunReplAsyncStubMessageTests
{
  [ModuleInitializer]
  internal static void Register() => RegisterTests<RunReplAsyncStubMessageTests>();

  public static async Task Should_tell_caller_to_enable_source_generator()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Build();

    // Method group conversion is not an invocation, so it is never intercepted
    Func<CancellationToken, Task> runRepl = app.RunReplAsync;

    // Act
    InvalidOperationException exception =
      await Should.ThrowAsync<InvalidOperationException>(() => runRepl(CancellationToken.None));

    // Assert
    exception.Message.ShouldBe(
      "RunReplAsync was not intercepted. Ensure AddRepl() is called and the source generator is enabled.");
    exception.Message.ShouldNotContain("not enabled");
  }
}

}
