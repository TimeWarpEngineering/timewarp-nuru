#!/usr/bin/env -S dotnet --

// ═══════════════════════════════════════════════════════════════════════════════
// REGRESSION TEST: kanban 454-028 item #5 — multi-word group alias index math
// ═══════════════════════════════════════════════════════════════════════════════
//
// BUG: ExtractAndCombineAliases split the joined FullPrefix into WORDS and indexed it
// with GroupPrefixIndex, which is a GROUP index. For a multi-word group prefix like
// [NuruRouteGroup("t028git t028remote")] with alias "t028gr", the alias replaced only the
// first WORD -> "t028gr t028remote show" instead of replacing the whole group prefix ->
// "t028gr show".
//
// FIX: replace at the GROUP level via GroupInfo.GroupPrefixes, so the alias swaps the
// group's entire (possibly multi-word) prefix.
// ═══════════════════════════════════════════════════════════════════════════════

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.Routing.MultiWordGroupAlias
{

[TestTag("Routing")]
public class MultiWordGroupAliasTests
{
  [ModuleInitializer]
  internal static void Register() => RegisterTests<MultiWordGroupAliasTests>();

  /// <summary>
  /// The primary multi-word group prefix still resolves the command.
  /// </summary>
  public static async Task Should_match_via_full_multiword_prefix()
  {
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map<T028ShowCommand>()
      .Build();

    int exitCode = await app.RunAsync(["t028git", "t028remote", "show"]);

    exitCode.ShouldBe(0);
    terminal.OutputContains("remote shown").ShouldBeTrue();
  }

  /// <summary>
  /// The group alias replaces the ENTIRE two-word group prefix -> "t028gr show".
  /// </summary>
  public static async Task Should_match_via_alias_replacing_whole_group_prefix()
  {
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map<T028ShowCommand>()
      .Build();

    int exitCode = await app.RunAsync(["t028gr", "show"]);

    exitCode.ShouldBe(0);
    terminal.OutputContains("remote shown").ShouldBeTrue();
  }

  /// <summary>
  /// Regression guard for the OLD word-index bug: the buggy alias would have been
  /// "t028gr t028remote show", so that partially-substituted form must NOT resolve.
  /// </summary>
  public static async Task Should_not_match_buggy_partial_word_alias()
  {
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map<T028ShowCommand>()
      .Build();

    int exitCode = await app.RunAsync(["t028gr", "t028remote", "show"]);

    exitCode.ShouldNotBe(0);
  }
}

/// <summary>
/// Group base class with a MULTI-WORD prefix and a single-word alias.
/// </summary>
[NuruRouteGroup("t028git t028remote")]
[NuruRouteAlias("t028gr")]
public abstract class T028GitRemoteGroup;

/// <summary>
/// Command under the multi-word group.
/// </summary>
[NuruRoute("show", Description = "Show remote")]
public sealed class T028ShowCommand : T028GitRemoteGroup, ICommand<Unit>
{
  public sealed class Handler(ITerminal terminal) : ICommandHandler<T028ShowCommand, Unit>
  {
    public async ValueTask<Unit> Handle(T028ShowCommand command, CancellationToken cancellationToken)
    {
      await terminal.WriteLineAsync("remote shown").ConfigureAwait(false);
      return default;
    }
  }
}

}
