#!/usr/bin/dotnet --

// ═══════════════════════════════════════════════════════════════════════════════
// REGRESSION TEST: GitHub Issue #160
// ═══════════════════════════════════════════════════════════════════════════════
//
// PURPOSE: Verify nested [NuruRouteGroup] inheritance correctly concatenates
// all prefixes from the full inheritance chain.
//
// BUG DESCRIPTION:
// When using nested [NuruRouteGroup] attributes through class inheritance,
// only the immediate parent's prefix was applied, not the full chain.
//
// Example:
//   [NuruRouteGroup("ccc1-demo")] class Ccc1DemoGroup { }
//   [NuruRouteGroup("queue")] class QueueGroup : Ccc1DemoGroup { }
//   [NuruRoute("peek")] class PeekEndpoint : QueueGroup { }
//
// Expected: "ccc1-demo queue peek"
// Actual (bug): "queue peek"
//
// ROOT CAUSE:
// ExtractGroupPrefix in endpoint-extractor.cs only checked the immediate
// BaseType for [NuruRouteGroup], not the full inheritance chain.
//
// ═══════════════════════════════════════════════════════════════════════════════

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.Routing.Issue160
{

/// <summary>
/// Tests for GitHub issue #160: Nested NuruRouteGroup inheritance does not
/// concatenate prefixes from the full inheritance chain.
/// </summary>
[TestTag("Routing")]
[TestTag("Issue160")]
public class NestedRouteGroupTests
{
  [ModuleInitializer]
  internal static void Register() => RegisterTests<NestedRouteGroupTests>();

  /// <summary>
  /// Verifies two-level nested route groups work (backward compatibility).
  /// Pattern: "level1 action" from Level1Group -> ActionCommand
  /// </summary>
  public static async Task Should_concatenate_two_level_nested_group_prefixes()
  {
    using TestTerminal terminal = new();

    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .DiscoverEndpoints()
      .Build();

    int exitCode = await app.RunAsync(["issue160-level1", "two-level-action"]);

    Console.WriteLine($"Exit code: {exitCode}");
    Console.WriteLine($"Output: {terminal.AllOutput}");

    exitCode.ShouldBe(0);
    terminal.OutputContains("Two-level action executed").ShouldBeTrue();
  }

  /// <summary>
  /// Verifies three-level nested route groups work (the bug scenario).
  /// Pattern: "ccc1-demo queue peek" from Ccc1DemoGroup -> QueueGroup -> PeekCommand
  /// </summary>
  public static async Task Should_concatenate_three_level_nested_group_prefixes()
  {
    using TestTerminal terminal = new();

    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .DiscoverEndpoints()
      .Build();

    int exitCode = await app.RunAsync(["issue160-ccc1-demo", "queue", "peek"]);

    Console.WriteLine($"Exit code: {exitCode}");
    Console.WriteLine($"Output: {terminal.AllOutput}");

    exitCode.ShouldBe(0);
    terminal.OutputContains("Peeking at queue").ShouldBeTrue();
  }

  /// <summary>
  /// Verifies four-level nested route groups work.
  /// Pattern: "cloud azure storage upload" from CloudGroup -> AzureGroup -> StorageGroup -> UploadCommand
  /// </summary>
  public static async Task Should_concatenate_four_level_nested_group_prefixes()
  {
    using TestTerminal terminal = new();

    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .DiscoverEndpoints()
      .Build();

    int exitCode = await app.RunAsync(["issue160-cloud", "azure", "storage", "upload", "myfile.txt"]);

    Console.WriteLine($"Exit code: {exitCode}");
    Console.WriteLine($"Output: {terminal.AllOutput}");

    exitCode.ShouldBe(0);
    terminal.OutputContains("Uploading: myfile.txt").ShouldBeTrue();
  }

  /// <summary>
  /// Verifies mixed inheritance works (some ancestors have groups, some don't).
  /// Pattern: "root action" from RootGroup -> MiddleNoGroup -> LeafCommand
  /// The middle class without [NuruRouteGroup] should be skipped.
  /// </summary>
  public static async Task Should_skip_ancestors_without_route_group_attribute()
  {
    using TestTerminal terminal = new();

    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .DiscoverEndpoints()
      .Build();

    int exitCode = await app.RunAsync(["issue160-root", "mixed-action"]);

    Console.WriteLine($"Exit code: {exitCode}");
    Console.WriteLine($"Output: {terminal.AllOutput}");

    exitCode.ShouldBe(0);
    terminal.OutputContains("Mixed inheritance action executed").ShouldBeTrue();
  }
}

// ═══════════════════════════════════════════════════════════════════════════════
// Two-level nesting (backward compatibility test)
// ═══════════════════════════════════════════════════════════════════════════════

/// <summary>Level 1 group.</summary>
[NuruRouteGroup("issue160-level1")]
public abstract class Issue160Level1Group;

/// <summary>Command with two-level inheritance.</summary>
[NuruRoute("two-level-action", Description = "Two-level nested command")]
public sealed class Issue160TwoLevelCommand : Issue160Level1Group, ICommand<Unit>
{
  public sealed class Handler(ITerminal terminal) : ICommandHandler<Issue160TwoLevelCommand, Unit>
  {
    public async ValueTask<Unit> Handle(Issue160TwoLevelCommand command, CancellationToken cancellationToken)
    {
      ArgumentNullException.ThrowIfNull(command);
      await terminal.WriteLineAsync("Two-level action executed").ConfigureAwait(false);
      return default;
    }
  }
}

// ═══════════════════════════════════════════════════════════════════════════════
// Three-level nesting (the bug scenario from issue #160)
// ═══════════════════════════════════════════════════════════════════════════════

/// <summary>Root group (level 1).</summary>
[NuruRouteGroup("issue160-ccc1-demo")]
public abstract class Issue160Ccc1DemoGroup;

/// <summary>Queue group (level 2) inheriting from root.</summary>
[NuruRouteGroup("queue")]
public abstract class Issue160QueueGroup : Issue160Ccc1DemoGroup;

/// <summary>Peek command (level 3) - the exact scenario from the bug report.</summary>
[NuruRoute("peek", Description = "Peek at the queue")]
public sealed class Issue160PeekCommand : Issue160QueueGroup, ICommand<Unit>
{
  public sealed class Handler(ITerminal terminal) : ICommandHandler<Issue160PeekCommand, Unit>
  {
    public async ValueTask<Unit> Handle(Issue160PeekCommand command, CancellationToken cancellationToken)
    {
      ArgumentNullException.ThrowIfNull(command);
      await terminal.WriteLineAsync("Peeking at queue").ConfigureAwait(false);
      return default;
    }
  }
}

// ═══════════════════════════════════════════════════════════════════════════════
// Four-level nesting (stress test)
// ═══════════════════════════════════════════════════════════════════════════════

/// <summary>Cloud group (level 1).</summary>
[NuruRouteGroup("issue160-cloud")]
public abstract class Issue160CloudGroup;

/// <summary>Azure group (level 2).</summary>
[NuruRouteGroup("azure")]
public abstract class Issue160AzureGroup : Issue160CloudGroup;

/// <summary>Storage group (level 3).</summary>
[NuruRouteGroup("storage")]
public abstract class Issue160StorageGroup : Issue160AzureGroup;

/// <summary>Upload command (level 4) with parameter.</summary>
[NuruRoute("upload", Description = "Upload a file")]
public sealed class Issue160UploadCommand : Issue160StorageGroup, ICommand<Unit>
{
  [Parameter(Description = "File to upload")]
  public string File { get; set; } = string.Empty;

  public sealed class Handler(ITerminal terminal) : ICommandHandler<Issue160UploadCommand, Unit>
  {
    public async ValueTask<Unit> Handle(Issue160UploadCommand command, CancellationToken cancellationToken)
    {
      ArgumentNullException.ThrowIfNull(command);
      await terminal.WriteLineAsync($"Uploading: {command.File}").ConfigureAwait(false);
      return default;
    }
  }
}

// ═══════════════════════════════════════════════════════════════════════════════
// Mixed inheritance (some ancestors without [NuruRouteGroup])
// ═══════════════════════════════════════════════════════════════════════════════

/// <summary>Root group with attribute.</summary>
[NuruRouteGroup("issue160-root")]
public abstract class Issue160RootGroup;

/// <summary>Middle class WITHOUT [NuruRouteGroup] - should be skipped.</summary>
public abstract class Issue160MiddleNoGroup : Issue160RootGroup;

/// <summary>Leaf command inheriting through a class without route group.</summary>
[NuruRoute("mixed-action", Description = "Mixed inheritance command")]
public sealed class Issue160MixedCommand : Issue160MiddleNoGroup, ICommand<Unit>
{
  public sealed class Handler(ITerminal terminal) : ICommandHandler<Issue160MixedCommand, Unit>
  {
    public async ValueTask<Unit> Handle(Issue160MixedCommand command, CancellationToken cancellationToken)
    {
      ArgumentNullException.ThrowIfNull(command);
      await terminal.WriteLineAsync("Mixed inheritance action executed").ConfigureAwait(false);
      return default;
    }
  }
}

}
