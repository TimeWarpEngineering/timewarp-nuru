#!/usr/bin/dotnet run

// ═══════════════════════════════════════════════════════════════════════════════
// ENDPOINT GENERATOR TESTS - Basic Generation
// ═══════════════════════════════════════════════════════════════════════════════
// Tests that the NuruEndpointGenerator source generator correctly
// generates route registration code for classes with [NuruRoute] attributes.
//
// NOTE: These tests verify the integration works end-to-end. The source generator
// runs at compile time, so if the routes are registered correctly, the generator
// worked.
// ═══════════════════════════════════════════════════════════════════════════════

int passed = 0;
int failed = 0;

// ─────────────────────────────────────────────────────────────────────────────
// Test 1: Basic route is registered via attribute
// ─────────────────────────────────────────────────────────────────────────────
{
  // The source generator should have registered SimpleTestRequest
  bool isRegistered = NuruRouteRegistry.IsRegistered<SimpleTestRequest>();

  if (isRegistered)
  {
    Console.WriteLine("✓ Test 1: Basic route registered via [NuruRoute] attribute");
    passed++;
  }
  else
  {
    Console.WriteLine("✗ Test 1: SimpleTestRequest not found in registry");
    failed++;
  }
}

// ─────────────────────────────────────────────────────────────────────────────
// Test 2: Route with parameter is registered
// ─────────────────────────────────────────────────────────────────────────────
{
  bool isRegistered = NuruRouteRegistry.IsRegistered<ParameterTestRequest>();

  if (isRegistered)
  {
    Console.WriteLine("✓ Test 2: Route with parameter registered");
    passed++;
  }
  else
  {
    Console.WriteLine("✗ Test 2: ParameterTestRequest not found in registry");
    failed++;
  }
}

// ─────────────────────────────────────────────────────────────────────────────
// Test 3: Route with options is registered
// ─────────────────────────────────────────────────────────────────────────────
{
  bool isRegistered = NuruRouteRegistry.IsRegistered<OptionTestRequest>();

  if (isRegistered)
  {
    Console.WriteLine("✓ Test 3: Route with options registered");
    passed++;
  }
  else
  {
    Console.WriteLine("✗ Test 3: OptionTestRequest not found in registry");
    failed++;
  }
}

// ─────────────────────────────────────────────────────────────────────────────
// Test 4: Aliases create multiple routes for same type
// ─────────────────────────────────────────────────────────────────────────────
{
  // Count routes for AliasTestRequest
  int aliasRouteCount = NuruRouteRegistry.RegisteredRoutes
    .Count(r => r.RequestType == typeof(AliasTestRequest));

  // Should have 3: main pattern + 2 aliases
  if (aliasRouteCount == 3)
  {
    Console.WriteLine("✓ Test 4: Aliases create 3 routes for same type");
    passed++;
  }
  else
  {
    Console.WriteLine($"✗ Test 4: Expected 3 routes for AliasTestRequest, got {aliasRouteCount}");
    failed++;
  }
}

// ─────────────────────────────────────────────────────────────────────────────
// Test 5: Grouped route inherits prefix
// ─────────────────────────────────────────────────────────────────────────────
{
  RegisteredRoute? groupedRoute = NuruRouteRegistry.RegisteredRoutes
    .FirstOrDefault(r => r.RequestType == typeof(GroupedTestRequest));

  // Pattern should start with group prefix "mygroup"
  bool hasPrefix = groupedRoute?.Pattern.StartsWith("mygroup", StringComparison.OrdinalIgnoreCase) == true;

  if (hasPrefix)
  {
    Console.WriteLine("✓ Test 5: Grouped route inherits prefix from base class");
    passed++;
  }
  else
  {
    Console.WriteLine($"✗ Test 5: Expected pattern starting with 'mygroup', got '{groupedRoute?.Pattern}'");
    failed++;
  }
}

// ─────────────────────────────────────────────────────────────────────────────
// Test 6: Route patterns are correct
// ─────────────────────────────────────────────────────────────────────────────
{
  RegisteredRoute? simpleRoute = NuruRouteRegistry.RegisteredRoutes
    .FirstOrDefault(r => r.RequestType == typeof(SimpleTestRequest));

  RegisteredRoute? paramRoute = NuruRouteRegistry.RegisteredRoutes
    .FirstOrDefault(r => r.RequestType == typeof(ParameterTestRequest));

  bool simpleCorrect = simpleRoute?.Pattern == "simple";
  // Pattern uses display format <param> not AST format {param}
  bool paramCorrect = paramRoute?.Pattern.Contains("<value>") == true;

  if (simpleCorrect && paramCorrect)
  {
    Console.WriteLine("✓ Test 6: Route patterns are correctly generated");
    passed++;
  }
  else
  {
    Console.WriteLine($"✗ Test 6: simple='{simpleRoute?.Pattern}', param='{paramRoute?.Pattern}'");
    failed++;
  }
}

// ─────────────────────────────────────────────────────────────────────────────
// Test 7: Description is captured
// ─────────────────────────────────────────────────────────────────────────────
{
  RegisteredRoute? route = NuruRouteRegistry.RegisteredRoutes
    .FirstOrDefault(r => r.RequestType == typeof(SimpleTestRequest));

  bool hasDescription = route?.Description == "Simple test command";

  if (hasDescription)
  {
    Console.WriteLine("✓ Test 7: Description captured from attribute");
    passed++;
  }
  else
  {
    Console.WriteLine($"✗ Test 7: Expected 'Simple test command', got '{route?.Description}'");
    failed++;
  }
}

// ─────────────────────────────────────────────────────────────────────────────
// Test 8: Compiled route has correct segments
// ─────────────────────────────────────────────────────────────────────────────
{
  RegisteredRoute? route = NuruRouteRegistry.RegisteredRoutes
    .FirstOrDefault(r => r.RequestType == typeof(SimpleTestRequest));

  // CompiledRoute should have segments
  bool hasSegments = route?.CompiledRoute.Segments.Count > 0;

  if (hasSegments)
  {
    Console.WriteLine("✓ Test 8: Compiled route has segments");
    passed++;
  }
  else
  {
    Console.WriteLine($"✗ Test 8: No segments in compiled route");
    failed++;
  }
}

// ─────────────────────────────────────────────────────────────────────────────
// Test 9: Default route with empty pattern is registered
// ─────────────────────────────────────────────────────────────────────────────
{
  bool isRegistered = NuruRouteRegistry.IsRegistered<DefaultTestRequest>();

  RegisteredRoute? route = NuruRouteRegistry.RegisteredRoutes
    .FirstOrDefault(r => r.RequestType == typeof(DefaultTestRequest));

  bool hasEmptyPattern = route?.Pattern == "";

  if (isRegistered && hasEmptyPattern)
  {
    Console.WriteLine("✓ Test 9: Default route with empty pattern registered");
    passed++;
  }
  else
  {
    Console.WriteLine($"✗ Test 9: Default route failed - registered={isRegistered}, pattern='{route?.Pattern}'");
    failed++;
  }
}

// ─────────────────────────────────────────────────────────────────────────────
// Test 10: Catch-all parameter is registered correctly
// ─────────────────────────────────────────────────────────────────────────────
{
  bool isRegistered = NuruRouteRegistry.IsRegistered<CatchAllTestRequest>();

  RegisteredRoute? route = NuruRouteRegistry.RegisteredRoutes
    .FirstOrDefault(r => r.RequestType == typeof(CatchAllTestRequest));

  // The compiled route should have a catch-all parameter
  bool hasCatchAll = route?.CompiledRoute.HasCatchAll == true;

  if (isRegistered && hasCatchAll)
  {
    Console.WriteLine("✓ Test 10: Catch-all parameter registered correctly");
    passed++;
  }
  else
  {
    Console.WriteLine($"✗ Test 10: Catch-all failed - registered={isRegistered}, hasCatchAll={hasCatchAll}");
    failed++;
  }
}

// ─────────────────────────────────────────────────────────────────────────────
// Test 11: Typed int option is registered correctly
// ─────────────────────────────────────────────────────────────────────────────
{
  bool isRegistered = NuruRouteRegistry.IsRegistered<TypedOptionTestRequest>();

  RegisteredRoute? route = NuruRouteRegistry.RegisteredRoutes
    .FirstOrDefault(r => r.RequestType == typeof(TypedOptionTestRequest));

  // The compiled route should have options
  bool hasOptions = route?.CompiledRoute.OptionMatchers.Count > 0;

  // Check if we have an option named "count" that accepts values (not a flag)
  OptionMatcher? countOption = route?.CompiledRoute.OptionMatchers
    .FirstOrDefault(o => o.MatchPattern == "--count");

  // ExpectsValue=true means it's a valued option (not a flag)
  bool hasIntOption = countOption != null && countOption.ExpectsValue;

  if (isRegistered && hasOptions && hasIntOption)
  {
    Console.WriteLine("✓ Test 11: Typed int option registered correctly");
    passed++;
  }
  else
  {
    Console.WriteLine($"✗ Test 11: Typed option failed - registered={isRegistered}, hasOptions={hasOptions}, hasIntOption={hasIntOption}");
    failed++;
  }
}

// ─────────────────────────────────────────────────────────────────────────────
// Test 12: Two-level nested route groups (backward compatibility)
// ─────────────────────────────────────────────────────────────────────────────
{
  RegisteredRoute? route = NuruRouteRegistry.RegisteredRoutes
    .FirstOrDefault(r => r.RequestType == typeof(TwoLevelNestedRequest));

  // Pattern should be "level1 nested-two"
  bool correctPattern = route?.Pattern == "level1 nested-two";

  if (correctPattern)
  {
    Console.WriteLine("✓ Test 12: Two-level nested route group prefix works");
    passed++;
  }
  else
  {
    Console.WriteLine($"✗ Test 12: Expected 'level1 nested-two', got '{route?.Pattern}'");
    failed++;
  }
}

// ─────────────────────────────────────────────────────────────────────────────
// Test 13: Three-level nested route groups (GitHub #160 fix)
// ─────────────────────────────────────────────────────────────────────────────
{
  RegisteredRoute? route = NuruRouteRegistry.RegisteredRoutes
    .FirstOrDefault(r => r.RequestType == typeof(ThreeLevelNestedRequest));

  // Pattern should be "ccc1-demo queue peek" (grandparent + parent + route)
  bool correctPattern = route?.Pattern == "ccc1-demo queue peek";

  if (correctPattern)
  {
    Console.WriteLine("✓ Test 13: Three-level nested route group prefix works (GitHub #160)");
    passed++;
  }
  else
  {
    Console.WriteLine($"✗ Test 13: Expected 'ccc1-demo queue peek', got '{route?.Pattern}'");
    failed++;
  }
}

// ─────────────────────────────────────────────────────────────────────────────
// Test 14: Mixed inheritance (some parents have groups, some don't)
// ─────────────────────────────────────────────────────────────────────────────
{
  RegisteredRoute? route = NuruRouteRegistry.RegisteredRoutes
    .FirstOrDefault(r => r.RequestType == typeof(MixedInheritanceRequest));

  // Pattern should be "root-group mixed" (skips middle class without group)
  bool correctPattern = route?.Pattern == "root-group mixed";

  if (correctPattern)
  {
    Console.WriteLine("✓ Test 14: Mixed inheritance skips classes without [NuruRouteGroup]");
    passed++;
  }
  else
  {
    Console.WriteLine($"✗ Test 14: Expected 'root-group mixed', got '{route?.Pattern}'");
    failed++;
  }
}

// ─────────────────────────────────────────────────────────────────────────────
// Summary
// ─────────────────────────────────────────────────────────────────────────────
Console.WriteLine();
Console.WriteLine($"Results: {passed} passed, {failed} failed");

return failed > 0 ? 1 : 0;

// ═══════════════════════════════════════════════════════════════════════════════
// TEST REQUEST CLASSES - These trigger the source generator
// ═══════════════════════════════════════════════════════════════════════════════

/// <summary>Simple route with no parameters or options.</summary>
[NuruRoute("simple", Description = "Simple test command")]
public sealed class SimpleTestRequest : IQuery<Unit>
{
  public sealed class Handler : IQueryHandler<SimpleTestRequest, Unit>
  {
    public ValueTask<Unit> Handle(SimpleTestRequest request, CancellationToken ct) => default;
  }
}

/// <summary>Route with a required parameter.</summary>
[NuruRoute("param", Description = "Parameter test command")]
public sealed class ParameterTestRequest : IQuery<Unit>
{
  [Parameter(Description = "Test value")]
  public string Value { get; set; } = string.Empty;

  public sealed class Handler : IQueryHandler<ParameterTestRequest, Unit>
  {
    public ValueTask<Unit> Handle(ParameterTestRequest request, CancellationToken ct) => default;
  }
}

/// <summary>Route with options (flag and valued).</summary>
[NuruRoute("option", Description = "Option test command")]
public sealed class OptionTestRequest : IQuery<Unit>
{
  [Option("verbose", "v", Description = "Enable verbose mode")]
  public bool Verbose { get; set; }

  [Option("config", "c", Description = "Config file path")]
  public string? Config { get; set; }

  public sealed class Handler : IQueryHandler<OptionTestRequest, Unit>
  {
    public ValueTask<Unit> Handle(OptionTestRequest request, CancellationToken ct) => default;
  }
}

/// <summary>Route with aliases.</summary>
[NuruRoute("aliased", Description = "Alias test command")]
[NuruRouteAlias("alias1", "alias2")]
public sealed class AliasTestRequest : IQuery<Unit>
{
  public sealed class Handler : IQueryHandler<AliasTestRequest, Unit>
  {
    public ValueTask<Unit> Handle(AliasTestRequest request, CancellationToken ct) => default;
  }
}

/// <summary>Base class for grouped routes.</summary>
[NuruRouteGroup("mygroup")]
public abstract class TestGroupBase
{
  [GroupOption("debug", "d", Description = "Debug mode")]
  public bool Debug { get; set; }
}

/// <summary>Route that inherits group prefix.</summary>
[NuruRoute("subcommand", Description = "Grouped test command")]
public sealed class GroupedTestRequest : TestGroupBase, IQuery<Unit>
{
  public sealed class Handler : IQueryHandler<GroupedTestRequest, Unit>
  {
    public ValueTask<Unit> Handle(GroupedTestRequest request, CancellationToken ct) => default;
  }
}

/// <summary>Default route with empty pattern.</summary>
[NuruRoute("", Description = "Default command when no arguments provided")]
public sealed class DefaultTestRequest : IQuery<Unit>
{
  public sealed class Handler : IQueryHandler<DefaultTestRequest, Unit>
  {
    public ValueTask<Unit> Handle(DefaultTestRequest request, CancellationToken ct) => default;
  }
}

/// <summary>Route with catch-all parameter to capture multiple arguments.</summary>
[NuruRoute("catchall", Description = "Catch-all test command")]
public sealed class CatchAllTestRequest : IQuery<Unit>
{
  [Parameter(IsCatchAll = true, Description = "All remaining arguments")]
  public string[] Args { get; set; } = [];

  public sealed class Handler : IQueryHandler<CatchAllTestRequest, Unit>
  {
    public ValueTask<Unit> Handle(CatchAllTestRequest request, CancellationToken ct) => default;
  }
}

/// <summary>Route with typed int option.</summary>
[NuruRoute("typed", Description = "Typed option test command")]
public sealed class TypedOptionTestRequest : IQuery<Unit>
{
  [Option("count", "c", Description = "Number of items")]
  public int Count { get; set; } = 1;

  [Option("verbose", "v", Description = "Enable verbose output")]
  public bool Verbose { get; set; }

  public sealed class Handler : IQueryHandler<TypedOptionTestRequest, Unit>
  {
    public ValueTask<Unit> Handle(TypedOptionTestRequest request, CancellationToken ct) => default;
  }
}

// ═══════════════════════════════════════════════════════════════════════════════
// NESTED ROUTE GROUP TEST CLASSES - Test inheritance chain prefix concatenation
// ═══════════════════════════════════════════════════════════════════════════════

/// <summary>Base class for two-level nesting test.</summary>
[NuruRouteGroup("level1")]
public abstract class Level1GroupBase { }

/// <summary>Route with two-level inheritance (backward compatibility test).</summary>
[NuruRoute("nested-two", Description = "Two-level nested command")]
public sealed class TwoLevelNestedRequest : Level1GroupBase, IQuery<Unit>
{
  public sealed class Handler : IQueryHandler<TwoLevelNestedRequest, Unit>
  {
    public ValueTask<Unit> Handle(TwoLevelNestedRequest request, CancellationToken ct) => default;
  }
}

/// <summary>Root group for three-level nesting (GitHub #160).</summary>
[NuruRouteGroup("ccc1-demo")]
public abstract class Ccc1DemoGroup { }

/// <summary>Middle group inheriting from root.</summary>
[NuruRouteGroup("queue")]
public abstract class QueueGroup : Ccc1DemoGroup { }

/// <summary>Route with three-level inheritance (the bug scenario from GitHub #160).</summary>
[NuruRoute("peek", Description = "Peek at the queue")]
public sealed class ThreeLevelNestedRequest : QueueGroup, IQuery<Unit>
{
  public sealed class Handler : IQueryHandler<ThreeLevelNestedRequest, Unit>
  {
    public ValueTask<Unit> Handle(ThreeLevelNestedRequest request, CancellationToken ct) => default;
  }
}

/// <summary>Root group for mixed inheritance test.</summary>
[NuruRouteGroup("root-group")]
public abstract class RootGroupBase { }

/// <summary>Middle class WITHOUT [NuruRouteGroup] attribute.</summary>
public abstract class MiddleNoGroupBase : RootGroupBase { }

/// <summary>Route testing mixed inheritance (some parents have groups, some don't).</summary>
[NuruRoute("mixed", Description = "Mixed inheritance command")]
public sealed class MixedInheritanceRequest : MiddleNoGroupBase, IQuery<Unit>
{
  public sealed class Handler : IQueryHandler<MixedInheritanceRequest, Unit>
  {
    public ValueTask<Unit> Handle(MixedInheritanceRequest request, CancellationToken ct) => default;
  }
}
