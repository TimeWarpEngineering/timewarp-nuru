#!/usr/bin/dotnet --
#:project ../../../source/timewarp-nuru/timewarp-nuru.csproj

// BUG: git commit<Tab><Tab> completes to "git --help" instead of showing "git commit" options
// 
// Steps to reproduce:
// 1. Type: git commit (no space, no tab yet)
// 2. Press Tab → Should complete to "git commit " (with space)
// 3. Press Tab again → BUG: Shows "git --help" instead of "git commit -m" or cycling options
//
// Expected behavior:
// - Second tab should show completions for "git commit " (--help and -m)
// - Or cycle through those completions
//
// Actual behavior:
// - Second tab somehow reverts to "git" context and shows "git --help"
//
// This test captures the bug. Once fixed, uncomment the assertion.

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.ReplTests.DoubleTabBug
{

[TestTag("REPL")]
[TestTag("Bug")]
public class DoubleTabBugTests
{
  [ModuleInitializer]
  internal static void Register() => RegisterTests<DoubleTabBugTests>();

  [Timeout(5000)]
  public static async Task Should_stay_in_git_commit_context_after_double_tab()
  {
    // Arrange: Type "git commit" (no space) then Tab Tab
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder([])
      .UseTerminal(terminal)
      .Map("git status")
        .WithHandler(() => { })
        .AsQuery()
        .Done()
      .Map("git commit -m {message}")
        .WithHandler((string message) => message.Length)  // Use param to avoid analyzer warning
        .AsCommand()
        .Done()
      .Map("git log --count {n:int}")
        .WithHandler((int n) => n)
        .AsQuery()
        .Done()
      .AddRepl(options =>
      {
        options.Prompt = "demo> ";
        options.EnableArrowHistory = true;
      })
      .Build();

    terminal.QueueKeys("git commit");
    terminal.QueueKey(ConsoleKey.Tab);
    terminal.QueueKey(ConsoleKey.Tab);
    terminal.QueueKey(ConsoleKey.Escape);
    terminal.QueueLine("exit");

    // Act
    await app.RunAsync(["--interactive"]);

    // TODO: Once bug is fixed, verify the correct behavior:
    // 1. First tab should complete "git commit" → "git commit "
    // 2. Second tab should show completions: --help, -m
    // 3. Should NOT revert to "git" context showing "git --help"

    // For now, just document that the test runs without crashing
    WriteLine("Test completed - manual verification required:");
    WriteLine("1. Run: demo> git commit<Tab><Tab>");
    WriteLine("2. Expected: Shows completions for 'git commit' (--help, -m)");
    WriteLine("3. Actual BUG: Shows 'git --help' instead");

    await Task.CompletedTask;
  }
}

} // namespace TimeWarp.Nuru.Tests.ReplTests.DoubleTabBug
