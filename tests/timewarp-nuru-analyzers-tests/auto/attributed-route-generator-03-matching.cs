#!/usr/bin/dotnet run

// ═══════════════════════════════════════════════════════════════════════════════
// ATTRIBUTED ROUTE GENERATOR TESTS - Route Matching
// ═══════════════════════════════════════════════════════════════════════════════
// Tests that routes generated from [NuruRoute] attributes correctly match input
// arguments using RouteMatchEngine.
//
// These tests verify the integration between:
// 1. Source generator creating routes from attributes
// 2. NuruRouteRegistry storing the routes
// 3. RouteMatchEngine matching input against routes
// ═══════════════════════════════════════════════════════════════════════════════

int passed = 0;
int failed = 0;

// ─────────────────────────────────────────────────────────────────────────────
// Helper Methods
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// Creates an EndpointCollection containing all routes for a request type (including aliases).
/// </summary>
EndpointCollection CreateEndpointsForType<T>() where T : IMessage
{
  EndpointCollection endpoints = new();

  foreach (RegisteredRoute registeredRoute in NuruRouteRegistry.RegisteredRoutes
    .Where(r => r.RequestType == typeof(T)))
  {
    endpoints.Add(new Endpoint
    {
      RoutePattern = registeredRoute.Pattern,
      CompiledRoute = registeredRoute.CompiledRoute,
      CommandType = registeredRoute.RequestType,
      Description = registeredRoute.Description
    });
  }

  if (endpoints.Count == 0)
    throw new InvalidOperationException($"No routes registered for {typeof(T).Name}");

  return endpoints;
}

/// <summary>
/// Tests matching args against endpoints, returns match state for first endpoint.
/// </summary>
RouteMatchState? MatchArgs(EndpointCollection endpoints, params string[] args)
{
  ParsedInput input = new(args, null, true);
  IReadOnlyList<RouteMatchState> states = RouteMatchEngine.Instance.Match(input, endpoints);
  return states.FirstOrDefault();
}

/// <summary>
/// Finds a specific match state by pattern prefix.
/// </summary>
RouteMatchState? FindMatchByPattern(IReadOnlyList<RouteMatchState> states, string patternPrefix)
{
  return states.FirstOrDefault(s =>
    s.Endpoint.RoutePattern.StartsWith(patternPrefix, StringComparison.OrdinalIgnoreCase));
}

// ═══════════════════════════════════════════════════════════════════════════════
// LITERAL MATCHING TESTS
// ═══════════════════════════════════════════════════════════════════════════════

// ─────────────────────────────────────────────────────────────────────────────
// Test 1: Simple literal matches
// ─────────────────────────────────────────────────────────────────────────────
{
  EndpointCollection endpoints = CreateEndpointsForType<StatusMatchTestRequest>();
  RouteMatchState? state = MatchArgs(endpoints, "status");

  if (state is not null && state.IsViable && state.IsExactMatch)
  {
    Console.WriteLine("✓ Test 1: Simple literal 'status' matches");
    passed++;
  }
  else
  {
    Console.WriteLine($"✗ Test 1: Expected IsViable=true, IsExactMatch=true, got IsViable={state?.IsViable}, IsExactMatch={state?.IsExactMatch}");
    failed++;
  }
}

// ─────────────────────────────────────────────────────────────────────────────
// Test 2: Wrong literal does not match
// ─────────────────────────────────────────────────────────────────────────────
{
  EndpointCollection endpoints = CreateEndpointsForType<StatusMatchTestRequest>();
  RouteMatchState? state = MatchArgs(endpoints, "help");

  if (state is not null && !state.IsViable)
  {
    Console.WriteLine("✓ Test 2: Wrong literal 'help' does not match 'status'");
    passed++;
  }
  else
  {
    Console.WriteLine($"✗ Test 2: Expected IsViable=false, got IsViable={state?.IsViable}");
    failed++;
  }
}

// ─────────────────────────────────────────────────────────────────────────────
// Test 3: Empty default route matches empty args
// ─────────────────────────────────────────────────────────────────────────────
{
  EndpointCollection endpoints = CreateEndpointsForType<DefaultMatchTestRequest>();
  RouteMatchState? state = MatchArgs(endpoints);

  if (state is not null && state.IsViable && state.IsExactMatch)
  {
    Console.WriteLine("✓ Test 3: Empty default route matches empty args");
    passed++;
  }
  else
  {
    Console.WriteLine($"✗ Test 3: Expected IsViable=true, IsExactMatch=true, got IsViable={state?.IsViable}, IsExactMatch={state?.IsExactMatch}");
    failed++;
  }
}

// ─────────────────────────────────────────────────────────────────────────────
// Test 4: Multi-word literal matches
// ─────────────────────────────────────────────────────────────────────────────
{
  EndpointCollection endpoints = CreateEndpointsForType<DockerComposeUpMatchTestRequest>();
  RouteMatchState? state = MatchArgs(endpoints, "docker", "compose", "up");

  if (state is not null && state.IsViable && state.IsExactMatch)
  {
    Console.WriteLine("✓ Test 4: Multi-word literal 'docker compose up' matches");
    passed++;
  }
  else
  {
    Console.WriteLine($"✗ Test 4: Expected IsViable=true, IsExactMatch=true, got IsViable={state?.IsViable}, IsExactMatch={state?.IsExactMatch}");
    failed++;
  }
}

// ═══════════════════════════════════════════════════════════════════════════════
// PARAMETER MATCHING TESTS
// ═══════════════════════════════════════════════════════════════════════════════

// ─────────────────────────────────────────────────────────────────────────────
// Test 5: Required parameter present
// ─────────────────────────────────────────────────────────────────────────────
{
  EndpointCollection endpoints = CreateEndpointsForType<GreetMatchTestRequest>();
  RouteMatchState? state = MatchArgs(endpoints, "greet", "Alice");

  if (state is not null && state.IsViable && state.IsExactMatch)
  {
    Console.WriteLine("✓ Test 5: Required parameter present - 'greet Alice' matches");
    passed++;
  }
  else
  {
    Console.WriteLine($"✗ Test 5: Expected IsViable=true, IsExactMatch=true, got IsViable={state?.IsViable}, IsExactMatch={state?.IsExactMatch}");
    failed++;
  }
}

// ─────────────────────────────────────────────────────────────────────────────
// Test 6: Required parameter missing - viable but not exact
// ─────────────────────────────────────────────────────────────────────────────
{
  EndpointCollection endpoints = CreateEndpointsForType<GreetMatchTestRequest>();
  RouteMatchState? state = MatchArgs(endpoints, "greet");

  if (state is not null && state.IsViable && !state.IsExactMatch)
  {
    Console.WriteLine("✓ Test 6: Required parameter missing - 'greet' is viable but not exact");
    passed++;
  }
  else
  {
    Console.WriteLine($"✗ Test 6: Expected IsViable=true, IsExactMatch=false, got IsViable={state?.IsViable}, IsExactMatch={state?.IsExactMatch}");
    failed++;
  }
}

// ─────────────────────────────────────────────────────────────────────────────
// Test 7: Optional parameter present
// ─────────────────────────────────────────────────────────────────────────────
{
  EndpointCollection endpoints = CreateEndpointsForType<GreetStyleMatchTestRequest>();
  RouteMatchState? state = MatchArgs(endpoints, "hello", "Alice", "formal");

  if (state is not null && state.IsViable && state.IsExactMatch)
  {
    Console.WriteLine("✓ Test 7: Optional parameter present - 'hello Alice formal' matches");
    passed++;
  }
  else
  {
    Console.WriteLine($"✗ Test 7: Expected IsViable=true, IsExactMatch=true, got IsViable={state?.IsViable}, IsExactMatch={state?.IsExactMatch}");
    failed++;
  }
}

// ─────────────────────────────────────────────────────────────────────────────
// Test 8: Optional parameter absent - still exact match
// ─────────────────────────────────────────────────────────────────────────────
{
  EndpointCollection endpoints = CreateEndpointsForType<GreetStyleMatchTestRequest>();
  RouteMatchState? state = MatchArgs(endpoints, "hello", "Alice");

  if (state is not null && state.IsViable && state.IsExactMatch)
  {
    Console.WriteLine("✓ Test 8: Optional parameter absent - 'hello Alice' still exact match");
    passed++;
  }
  else
  {
    Console.WriteLine($"✗ Test 8: Expected IsViable=true, IsExactMatch=true, got IsViable={state?.IsViable}, IsExactMatch={state?.IsExactMatch}");
    failed++;
  }
}

// ═══════════════════════════════════════════════════════════════════════════════
// OPTION MATCHING TESTS
// ═══════════════════════════════════════════════════════════════════════════════

// ─────────────────────────────────────────────────────────────────────────────
// Test 9: Bool flag with long form
// ─────────────────────────────────────────────────────────────────────────────
{
  EndpointCollection endpoints = CreateEndpointsForType<DeployForceMatchTestRequest>();
  RouteMatchState? state = MatchArgs(endpoints, "deploy", "--force");

  bool hasForceOption = state?.OptionsUsed.Contains("--force") == true;

  if (state is not null && state.IsViable && hasForceOption)
  {
    Console.WriteLine("✓ Test 9: Bool flag long form - 'deploy --force' matches with --force in OptionsUsed");
    passed++;
  }
  else
  {
    Console.WriteLine($"✗ Test 9: Expected IsViable=true with --force in OptionsUsed, got IsViable={state?.IsViable}, hasForce={hasForceOption}");
    failed++;
  }
}

// ─────────────────────────────────────────────────────────────────────────────
// Test 10: Bool flag with short form
// ─────────────────────────────────────────────────────────────────────────────
{
  EndpointCollection endpoints = CreateEndpointsForType<DeployForceMatchTestRequest>();
  RouteMatchState? state = MatchArgs(endpoints, "deploy", "-f");

  bool hasShortOption = state?.OptionsUsed.Contains("-f") == true;

  if (state is not null && state.IsViable && hasShortOption)
  {
    Console.WriteLine("✓ Test 10: Bool flag short form - 'deploy -f' matches with -f in OptionsUsed");
    passed++;
  }
  else
  {
    Console.WriteLine($"✗ Test 10: Expected IsViable=true with -f in OptionsUsed, got IsViable={state?.IsViable}, hasShort={hasShortOption}");
    failed++;
  }
}

// ─────────────────────────────────────────────────────────────────────────────
// Test 11: Valued option with separate arg
// ─────────────────────────────────────────────────────────────────────────────
{
  EndpointCollection endpoints = CreateEndpointsForType<DeployConfigMatchTestRequest>();
  RouteMatchState? state = MatchArgs(endpoints, "publish", "--config", "app.json");

  bool hasConfigOption = state?.OptionsUsed.Contains("--config") == true;

  if (state is not null && state.IsViable && hasConfigOption)
  {
    Console.WriteLine("✓ Test 11: Valued option - 'publish --config app.json' matches with --config in OptionsUsed");
    passed++;
  }
  else
  {
    Console.WriteLine($"✗ Test 11: Expected IsViable=true with --config in OptionsUsed, got IsViable={state?.IsViable}, hasConfig={hasConfigOption}");
    failed++;
  }
}

// ═══════════════════════════════════════════════════════════════════════════════
// SPECIAL MATCHING TESTS
// ═══════════════════════════════════════════════════════════════════════════════

// ─────────────────────────────────────────────────────────────────────────────
// Test 12: Catch-all captures remaining args
// ─────────────────────────────────────────────────────────────────────────────
{
  EndpointCollection endpoints = CreateEndpointsForType<ExecMatchTestRequest>();
  RouteMatchState? state = MatchArgs(endpoints, "exec", "echo", "hello", "world");

  if (state is not null && state.IsViable)
  {
    Console.WriteLine("✓ Test 12: Catch-all - 'exec echo hello world' matches");
    passed++;
  }
  else
  {
    Console.WriteLine($"✗ Test 12: Expected IsViable=true, got IsViable={state?.IsViable}");
    failed++;
  }
}

// ─────────────────────────────────────────────────────────────────────────────
// Test 13: Alias matches
// ─────────────────────────────────────────────────────────────────────────────
{
  EndpointCollection endpoints = CreateEndpointsForType<GoodbyeMatchTestRequest>();
  ParsedInput input = new(["bye"], null, true);
  IReadOnlyList<RouteMatchState> states = RouteMatchEngine.Instance.Match(input, endpoints);

  // Find the alias route
  RouteMatchState? aliasState = FindMatchByPattern(states, "bye");

  if (aliasState is not null && aliasState.IsViable && aliasState.IsExactMatch)
  {
    Console.WriteLine("✓ Test 13: Alias - 'bye' matches via [NuruRouteAlias]");
    passed++;
  }
  else
  {
    Console.WriteLine($"✗ Test 13: Expected alias 'bye' to match, got IsViable={aliasState?.IsViable}, IsExactMatch={aliasState?.IsExactMatch}");
    failed++;
  }
}

// ─────────────────────────────────────────────────────────────────────────────
// Test 14: Grouped route matches with prefix
// ─────────────────────────────────────────────────────────────────────────────
{
  EndpointCollection endpoints = CreateEndpointsForType<ContainerRunMatchTestRequest>();
  RouteMatchState? state = MatchArgs(endpoints, "container", "run", "nginx");

  if (state is not null && state.IsViable && state.IsExactMatch)
  {
    Console.WriteLine("✓ Test 14: Grouped route - 'container run nginx' matches");
    passed++;
  }
  else
  {
    Console.WriteLine($"✗ Test 14: Expected IsViable=true, IsExactMatch=true, got IsViable={state?.IsViable}, IsExactMatch={state?.IsExactMatch}");
    failed++;
  }
}

// ─────────────────────────────────────────────────────────────────────────────
// Test 15: Group option on grouped route
// ─────────────────────────────────────────────────────────────────────────────
{
  EndpointCollection endpoints = CreateEndpointsForType<ContainerRunMatchTestRequest>();
  RouteMatchState? state = MatchArgs(endpoints, "container", "run", "nginx", "--debug");

  bool hasDebugOption = state?.OptionsUsed.Contains("--debug") == true;

  if (state is not null && state.IsViable && hasDebugOption)
  {
    Console.WriteLine("✓ Test 15: Group option - 'container run nginx --debug' matches with --debug in OptionsUsed");
    passed++;
  }
  else
  {
    Console.WriteLine($"✗ Test 15: Expected IsViable=true with --debug in OptionsUsed, got IsViable={state?.IsViable}, hasDebug={hasDebugOption}");
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
// Note: Using "MatchTest" suffix to avoid conflicts with classes in other test files

/// <summary>Simple literal route for matching tests.</summary>
[NuruRoute("status", Description = "Check status")]
public sealed class StatusMatchTestRequest : IRequest
{
  public sealed class Handler : IRequestHandler<StatusMatchTestRequest>
  {
    public ValueTask<Unit> Handle(StatusMatchTestRequest request, CancellationToken ct) => default;
  }
}

/// <summary>Empty/default route for matching tests.</summary>
[NuruRoute("", Description = "Default command")]
public sealed class DefaultMatchTestRequest : IRequest
{
  public sealed class Handler : IRequestHandler<DefaultMatchTestRequest>
  {
    public ValueTask<Unit> Handle(DefaultMatchTestRequest request, CancellationToken ct) => default;
  }
}

/// <summary>Group base for multi-word literal route test.</summary>
[NuruRouteGroup("docker compose")]
public abstract class DockerComposeGroupMatchTestBase { }

/// <summary>Multi-word literal route for matching tests.</summary>
[NuruRoute("up", Description = "Start containers")]
public sealed class DockerComposeUpMatchTestRequest : DockerComposeGroupMatchTestBase, IRequest
{
  public sealed class Handler : IRequestHandler<DockerComposeUpMatchTestRequest>
  {
    public ValueTask<Unit> Handle(DockerComposeUpMatchTestRequest request, CancellationToken ct) => default;
  }
}

/// <summary>Route with required parameter for matching tests.</summary>
[NuruRoute("greet", Description = "Greet someone")]
public sealed class GreetMatchTestRequest : IRequest
{
  [Parameter(Description = "Name to greet")]
  public string Name { get; set; } = string.Empty;

  public sealed class Handler : IRequestHandler<GreetMatchTestRequest>
  {
    public ValueTask<Unit> Handle(GreetMatchTestRequest request, CancellationToken ct) => default;
  }
}

/// <summary>Route with optional parameter for matching tests.</summary>
[NuruRoute("hello", Description = "Greet with optional style")]
public sealed class GreetStyleMatchTestRequest : IRequest
{
  [Parameter(Order = 0, Description = "Name to greet")]
  public string Name { get; set; } = string.Empty;

  [Parameter(Order = 1, Description = "Greeting style")]
  public string? Style { get; set; }

  public sealed class Handler : IRequestHandler<GreetStyleMatchTestRequest>
  {
    public ValueTask<Unit> Handle(GreetStyleMatchTestRequest request, CancellationToken ct) => default;
  }
}

/// <summary>Route with bool flag option for matching tests.</summary>
[NuruRoute("deploy", Description = "Deploy with force option")]
public sealed class DeployForceMatchTestRequest : IRequest
{
  [Option("force", "f", Description = "Force deployment")]
  public bool Force { get; set; }

  public sealed class Handler : IRequestHandler<DeployForceMatchTestRequest>
  {
    public ValueTask<Unit> Handle(DeployForceMatchTestRequest request, CancellationToken ct) => default;
  }
}

/// <summary>Route with valued option for matching tests.</summary>
[NuruRoute("publish", Description = "Publish with config")]
public sealed class DeployConfigMatchTestRequest : IRequest
{
  [Option("config", "c", Description = "Config file")]
  public string? ConfigFile { get; set; }

  public sealed class Handler : IRequestHandler<DeployConfigMatchTestRequest>
  {
    public ValueTask<Unit> Handle(DeployConfigMatchTestRequest request, CancellationToken ct) => default;
  }
}

/// <summary>Route with catch-all parameter for matching tests.</summary>
[NuruRoute("exec", Description = "Execute command")]
public sealed class ExecMatchTestRequest : IRequest
{
  [Parameter(IsCatchAll = true, Description = "Command arguments")]
  public string[] Args { get; set; } = [];

  public sealed class Handler : IRequestHandler<ExecMatchTestRequest>
  {
    public ValueTask<Unit> Handle(ExecMatchTestRequest request, CancellationToken ct) => default;
  }
}

/// <summary>Route with alias for matching tests.</summary>
[NuruRoute("goodbye", Description = "Say goodbye")]
[NuruRouteAlias("bye")]
public sealed class GoodbyeMatchTestRequest : IRequest
{
  public sealed class Handler : IRequestHandler<GoodbyeMatchTestRequest>
  {
    public ValueTask<Unit> Handle(GoodbyeMatchTestRequest request, CancellationToken ct) => default;
  }
}

/// <summary>Group base class for matching tests.</summary>
[NuruRouteGroup("container")]
public abstract class ContainerGroupMatchTestBase
{
  [GroupOption("debug", "D", Description = "Debug mode")]
  public bool Debug { get; set; }
}

/// <summary>Grouped route for matching tests.</summary>
[NuruRoute("run", Description = "Run container")]
public sealed class ContainerRunMatchTestRequest : ContainerGroupMatchTestBase, IRequest
{
  [Parameter(Description = "Image name")]
  public string Image { get; set; } = string.Empty;

  public sealed class Handler : IRequestHandler<ContainerRunMatchTestRequest>
  {
    public ValueTask<Unit> Handle(ContainerRunMatchTestRequest request, CancellationToken ct) => default;
  }
}
