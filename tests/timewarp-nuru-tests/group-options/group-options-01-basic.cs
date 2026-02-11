#!/usr/bin/dotnet --

// ═══════════════════════════════════════════════════════════════════════════════
// GROUP OPTION TESTS - Task 419
// ═══════════════════════════════════════════════════════════════════════════════
//
// PURPOSE: Comprehensive tests for GroupOption feature allowing shared options
// across route groups via base class properties.
//
// Test scenarios covered:
// 1. Basic GroupOption inheritance (single option, long/short forms, defaults)
// 2. Multiple GroupOptions on same base class
// 3. GroupOptions coexisting with route-level [Option] attributes
// 4. Nested route groups with GroupOptions
// 5. Help text generation for GroupOptions
// 6. Typed GroupOptions (string, int, nullable)
//
// ═══════════════════════════════════════════════════════════════════════════════

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.GroupOptions
{

[TestTag("GroupOptions")]
public class GroupOptionTests
{
  [ModuleInitializer]
  internal static void Register() => RegisterTests<GroupOptionTests>();

  // ═════════════════════════════════════════════════════════════════════════════
  // TEST 1: Basic GroupOption Inheritance
  // ═════════════════════════════════════════════════════════════════════════════

  public static async Task Should_inherit_single_group_option_with_long_form()
  {
    using TestTerminal terminal = new();

    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .DiscoverEndpoints()
      .Build();

    int exitCode = await app.RunAsync(["task419", "basic", "status", "--verbose"]);

    exitCode.ShouldBe(0);
    terminal.OutputContains("Status: verbose=True").ShouldBeTrue();
  }

  public static async Task Should_inherit_single_group_option_with_short_form()
  {
    using TestTerminal terminal = new();

    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .DiscoverEndpoints()
      .Build();

    int exitCode = await app.RunAsync(["task419", "basic", "status", "-v"]);

    exitCode.ShouldBe(0);
    terminal.OutputContains("Status: verbose=True").ShouldBeTrue();
  }

  public static async Task Should_use_default_when_group_option_not_provided()
  {
    using TestTerminal terminal = new();

    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .DiscoverEndpoints()
      .Build();

    int exitCode = await app.RunAsync(["task419", "basic", "status"]);

    exitCode.ShouldBe(0);
    terminal.OutputContains("Status: verbose=False").ShouldBeTrue();
  }

  // ═════════════════════════════════════════════════════════════════════════════
  // TEST 2: Multiple GroupOptions
  // ═════════════════════════════════════════════════════════════════════════════

  public static async Task Should_support_multiple_group_options()
  {
    using TestTerminal terminal = new();

    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .DiscoverEndpoints()
      .Build();

    int exitCode = await app.RunAsync(["task419", "multi", "deploy", "--verbose", "--dry-run"]);

    exitCode.ShouldBe(0);
    terminal.OutputContains("Deploy: verbose=True, dryRun=True").ShouldBeTrue();
  }

  public static async Task Should_support_mixed_short_and_long_forms()
  {
    using TestTerminal terminal = new();

    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .DiscoverEndpoints()
      .Build();

    int exitCode = await app.RunAsync(["task419", "multi", "deploy", "-v", "-d"]);

    exitCode.ShouldBe(0);
    terminal.OutputContains("Deploy: verbose=True, dryRun=True").ShouldBeTrue();
  }

  public static async Task Should_handle_partial_group_options()
  {
    using TestTerminal terminal = new();

    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .DiscoverEndpoints()
      .Build();

    int exitCode = await app.RunAsync(["task419", "multi", "deploy", "--verbose"]);

    exitCode.ShouldBe(0);
    terminal.OutputContains("Deploy: verbose=True, dryRun=False").ShouldBeTrue();
  }

  // ═════════════════════════════════════════════════════════════════════════════
  // TEST 3: GroupOptions with Route-Level Options
  // ═════════════════════════════════════════════════════════════════════════════

  public static async Task Should_coexist_with_route_level_options()
  {
    using TestTerminal terminal = new();

    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .DiscoverEndpoints()
      .Build();

    int exitCode = await app.RunAsync(["task419", "mixed", "commit", "--verbose", "--message", "test commit"]);

    exitCode.ShouldBe(0);
    terminal.OutputContains("Commit: verbose=True, message=test commit").ShouldBeTrue();
  }

  public static async Task Should_handle_only_route_options_without_group_options()
  {
    using TestTerminal terminal = new();

    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .DiscoverEndpoints()
      .Build();

    int exitCode = await app.RunAsync(["task419", "mixed", "commit", "-m", "quick fix"]);

    exitCode.ShouldBe(0);
    terminal.OutputContains("Commit: verbose=False, message=quick fix").ShouldBeTrue();
  }

  public static async Task Should_handle_only_group_options_without_route_options()
  {
    using TestTerminal terminal = new();

    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .DiscoverEndpoints()
      .Build();

    int exitCode = await app.RunAsync(["task419", "mixed", "commit", "-v"]);

    exitCode.ShouldBe(0);
    terminal.OutputContains("Commit: verbose=True, message=").ShouldBeTrue();
  }

  // ═════════════════════════════════════════════════════════════════════════════
  // TEST 4: Nested Route Groups
  // ═════════════════════════════════════════════════════════════════════════════

  public static async Task Should_inherit_group_options_through_nested_groups()
  {
    using TestTerminal terminal = new();

    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .DiscoverEndpoints()
      .Build();

    int exitCode = await app.RunAsync(["task419", "parent", "child", "action", "--verbose"]);

    exitCode.ShouldBe(0);
    terminal.OutputContains("Grandchild: verbose=True").ShouldBeTrue();
  }

  public static async Task Should_merge_options_from_multiple_ancestors()
  {
    using TestTerminal terminal = new();

    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .DiscoverEndpoints()
      .Build();

    int exitCode = await app.RunAsync(["task419", "parent", "child", "action", "-v", "-d"]);

    exitCode.ShouldBe(0);
    terminal.OutputContains("Grandchild: verbose=True, dryRun=True").ShouldBeTrue();
  }

  // ═════════════════════════════════════════════════════════════════════════════
  // TEST 5: Help Text Generation
  // ═════════════════════════════════════════════════════════════════════════════

  public static async Task Should_show_group_options_in_route_help()
  {
    using TestTerminal terminal = new();

    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .DiscoverEndpoints()
      .Build();

    int exitCode = await app.RunAsync(["task419", "basic", "status", "--help"]);

    exitCode.ShouldBe(0);
    terminal.OutputContains("--verbose").ShouldBeTrue("Should show long form in help");
    terminal.OutputContains("-v").ShouldBeTrue("Should show short form in help");
    terminal.OutputContains("Enable verbose output").ShouldBeTrue("Should show description");
  }

  public static async Task Should_show_multiple_group_options_in_help()
  {
    using TestTerminal terminal = new();

    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .DiscoverEndpoints()
      .Build();

    int exitCode = await app.RunAsync(["task419", "multi", "deploy", "--help"]);

    exitCode.ShouldBe(0);
    terminal.OutputContains("--verbose").ShouldBeTrue();
    terminal.OutputContains("--dry-run").ShouldBeTrue();
    terminal.OutputContains("-v").ShouldBeTrue();
    terminal.OutputContains("-d").ShouldBeTrue();
  }

  public static async Task Should_show_combined_options_in_help()
  {
    using TestTerminal terminal = new();

    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .DiscoverEndpoints()
      .Build();

    int exitCode = await app.RunAsync(["task419", "mixed", "commit", "--help"]);

    exitCode.ShouldBe(0);
    terminal.OutputContains("--verbose").ShouldBeTrue("Group option should appear");
    terminal.OutputContains("--message").ShouldBeTrue("Route option should appear");
    terminal.OutputContains("-m").ShouldBeTrue("Route option short form");
  }

  // ═════════════════════════════════════════════════════════════════════════════
  // TEST 6: Typed GroupOptions
  // ═════════════════════════════════════════════════════════════════════════════

  public static async Task Should_support_typed_string_group_option()
  {
    using TestTerminal terminal = new();

    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .DiscoverEndpoints()
      .Build();

    int exitCode = await app.RunAsync(["task419", "typed", "config", "--output", "json"]);

    exitCode.ShouldBe(0);
    terminal.OutputContains("output=json").ShouldBeTrue();
  }

  public static async Task Should_support_typed_int_group_option()
  {
    using TestTerminal terminal = new();

    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .DiscoverEndpoints()
      .Build();

    int exitCode = await app.RunAsync(["task419", "typed", "config", "--verbose", "--count", "42"]);

    exitCode.ShouldBe(0);
    terminal.OutputContains("verbose=True").ShouldBeTrue();
    terminal.OutputContains("count=42").ShouldBeTrue();
  }

  public static async Task Should_support_nullable_typed_group_option()
  {
    using TestTerminal terminal = new();

    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .DiscoverEndpoints()
      .Build();

    int exitCode = await app.RunAsync(["task419", "typed", "config", "--verbose"]);

    exitCode.ShouldBe(0);
    terminal.OutputContains("verbose=True").ShouldBeTrue();
    terminal.OutputContains("count=(null)").ShouldBeTrue();
    terminal.OutputContains("output=text").ShouldBeTrue(); // default value preserved
  }

  public static async Task Should_support_nullable_string_group_option_with_value()
  {
    using TestTerminal terminal = new();

    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .DiscoverEndpoints()
      .Build();

    int exitCode = await app.RunAsync(["task419", "typed", "config", "--output", "xml", "--limit", "100"]);

    exitCode.ShouldBe(0);
    terminal.OutputContains("output=xml").ShouldBeTrue();
    terminal.OutputContains("limit=100").ShouldBeTrue();
  }
}

// ═══════════════════════════════════════════════════════════════════════════════
// ENDPOINT DEFINITIONS
// ═══════════════════════════════════════════════════════════════════════════════

// ═══════════════════════════════════════════════════════════════════════════════
// Basic GroupOption Tests
// ═══════════════════════════════════════════════════════════════════════════════

[NuruRouteGroup("task419 basic")]
public abstract class Task419BasicGroupBase
{
  [GroupOption("verbose", "v", Description = "Enable verbose output")]
  public bool Verbose { get; set; }
}

[NuruRoute("status", Description = "Show basic status")]
public sealed class Task419BasicStatusCommand : Task419BasicGroupBase, ICommand<Unit>
{
  public sealed class Handler(ITerminal terminal) : ICommandHandler<Task419BasicStatusCommand, Unit>
  {
    public async ValueTask<Unit> Handle(Task419BasicStatusCommand command, CancellationToken cancellationToken)
    {
      ArgumentNullException.ThrowIfNull(command);
      await terminal.WriteLineAsync($"Status: verbose={command.Verbose}").ConfigureAwait(false);
      return default;
    }
  }
}

// ═══════════════════════════════════════════════════════════════════════════════
// Multiple GroupOptions Tests
// ═══════════════════════════════════════════════════════════════════════════════

[NuruRouteGroup("task419 multi")]
public abstract class Task419MultiGroupBase
{
  [GroupOption("verbose", "v", Description = "Enable verbose output")]
  public bool Verbose { get; set; }

  [GroupOption("dry-run", "d", Description = "Perform a dry run")]
  public bool DryRun { get; set; }
}

[NuruRoute("deploy", Description = "Deploy application")]
public sealed class Task419MultiDeployCommand : Task419MultiGroupBase, ICommand<Unit>
{
  public sealed class Handler(ITerminal terminal) : ICommandHandler<Task419MultiDeployCommand, Unit>
  {
    public async ValueTask<Unit> Handle(Task419MultiDeployCommand command, CancellationToken cancellationToken)
    {
      ArgumentNullException.ThrowIfNull(command);
      await terminal.WriteLineAsync($"Deploy: verbose={command.Verbose}, dryRun={command.DryRun}").ConfigureAwait(false);
      return default;
    }
  }
}

// ═══════════════════════════════════════════════════════════════════════════════
// Mixed GroupOption + Route Option Tests
// ═══════════════════════════════════════════════════════════════════════════════

[NuruRouteGroup("task419 mixed")]
public abstract class Task419MixedGroupBase
{
  [GroupOption("verbose", "v", Description = "Enable verbose output")]
  public bool Verbose { get; set; }
}

[NuruRoute("commit", Description = "Commit changes")]
public sealed class Task419MixedCommitCommand : Task419MixedGroupBase, ICommand<Unit>
{
  [Option("message", "m", Description = "Commit message")]
  public string Message { get; set; } = string.Empty;

  public sealed class Handler(ITerminal terminal) : ICommandHandler<Task419MixedCommitCommand, Unit>
  {
    public async ValueTask<Unit> Handle(Task419MixedCommitCommand command, CancellationToken cancellationToken)
    {
      ArgumentNullException.ThrowIfNull(command);
      await terminal.WriteLineAsync($"Commit: verbose={command.Verbose}, message={command.Message}").ConfigureAwait(false);
      return default;
    }
  }
}

// ═══════════════════════════════════════════════════════════════════════════════
// Nested Route Groups Tests
// ═══════════════════════════════════════════════════════════════════════════════

[NuruRouteGroup("task419 parent")]
public abstract class Task419ParentGroupBase
{
  [GroupOption("verbose", "v", Description = "Enable verbose output")]
  public bool Verbose { get; set; }
}

[NuruRouteGroup("child")]
public abstract class Task419ChildGroupBase : Task419ParentGroupBase
{
  [GroupOption("dry-run", "d", Description = "Perform a dry run")]
  public bool DryRun { get; set; }
}

[NuruRoute("action", Description = "Action in grandchild")]
public sealed class Task419GrandchildCommand : Task419ChildGroupBase, ICommand<Unit>
{
  public sealed class Handler(ITerminal terminal) : ICommandHandler<Task419GrandchildCommand, Unit>
  {
    public async ValueTask<Unit> Handle(Task419GrandchildCommand command, CancellationToken cancellationToken)
    {
      ArgumentNullException.ThrowIfNull(command);
      await terminal.WriteLineAsync($"Grandchild: verbose={command.Verbose}, dryRun={command.DryRun}").ConfigureAwait(false);
      return default;
    }
  }
}

// ═══════════════════════════════════════════════════════════════════════════════
// Typed GroupOptions Tests
// ═══════════════════════════════════════════════════════════════════════════════

[NuruRouteGroup("task419 typed")]
public abstract class Task419TypedGroupBase
{
  [GroupOption("verbose", "v", Description = "Enable verbose output")]
  public bool Verbose { get; set; }

  [GroupOption("output", "o", Description = "Output format")]
  public string Output { get; set; } = "text";

  [GroupOption("count", "c", Description = "Item count")]
  public int? Count { get; set; }

  [GroupOption("limit", "l", Description = "Result limit")]
  public int? Limit { get; set; }
}

[NuruRoute("config", Description = "Show configuration")]
public sealed class Task419TypedConfigCommand : Task419TypedGroupBase, ICommand<Unit>
{
  public sealed class Handler(ITerminal terminal) : ICommandHandler<Task419TypedConfigCommand, Unit>
  {
    public async ValueTask<Unit> Handle(Task419TypedConfigCommand command, CancellationToken cancellationToken)
    {
      ArgumentNullException.ThrowIfNull(command);
      string countStr = command.Count?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "(null)";
      string limitStr = command.Limit?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "(null)";
      await terminal.WriteLineAsync($"Config: verbose={command.Verbose}, output={command.Output}, count={countStr}, limit={limitStr}").ConfigureAwait(false);
      return default;
    }
  }
}

} // namespace TimeWarp.Nuru.Tests.GroupOptions
