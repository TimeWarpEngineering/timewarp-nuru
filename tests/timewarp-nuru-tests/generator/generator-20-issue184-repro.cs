#!/usr/bin/dotnet --
// ═══════════════════════════════════════════════════════════════════════════════
// GENERATOR TEST: Issue #184 Reproduction
// ═══════════════════════════════════════════════════════════════════════════════
//
// PURPOSE: Reproduce the exact scenario from GitHub issue #184
//
// ═══════════════════════════════════════════════════════════════════════════════

#pragma warning disable CA1819 // Properties should not return arrays
#pragma warning disable CA1725 // Parameter names should match base declaration
#pragma warning disable CA1849 // Call async methods when async method
#pragma warning disable CA1062 // Validate arguments of public methods
#pragma warning disable RCS1248 // Use pattern matching to check for null

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.Generator.Issue184
{
  [TestTag("Generator")]
  [TestTag("Issue184")]
  public class Issue184ReproTests
  {
    [ModuleInitializer]
    internal static void Register() => RegisterTests<Issue184ReproTests>();

    /// <summary>
    /// Reproduce issue #184: filtering by RepoGroupBase should strip "repo" prefix
    /// </summary>
    public static async Task FilterByRepoGroupBase_StripsRepoPrefix()
    {
      // Arrange
      using TestTerminal terminal = new();

      NuruApp app = NuruApp.CreateBuilder()
        .UseTerminal(terminal)
        .DiscoverEndpoints(typeof(RepoGroupBase))
        .Build();

      // Act - "base sync" should work (repo prefix stripped)
      int exitCode = await app.RunAsync(["base", "sync"]);

      // Assert
      exitCode.ShouldBe(0);
      terminal.OutputContains("Syncing base branch...").ShouldBeTrue();

      // Act - "repo base sync" should NOT work (full prefix)
      terminal.ClearOutput();
      exitCode = await app.RunAsync(["repo", "base", "sync"]);

      // Assert
      exitCode.ShouldBe(1); // Unknown command
    }
  }
}

[NuruRouteGroup("repo")]
public abstract class RepoGroupBase;

[NuruRouteGroup("base")]
public abstract class RepoBaseGroupBase : RepoGroupBase;

[NuruRoute("sync", Description = "Sync/update the base branch")]
public sealed class RepoBaseSyncCommand : RepoBaseGroupBase, ICommand<Unit>
{
  public sealed class Handler(ITerminal terminal) : ICommandHandler<RepoBaseSyncCommand, Unit>
  {
    public ValueTask<Unit> Handle(RepoBaseSyncCommand command, CancellationToken ct)
    {
      terminal.WriteLine("Syncing base branch...");
      return default;
    }
  }
}
