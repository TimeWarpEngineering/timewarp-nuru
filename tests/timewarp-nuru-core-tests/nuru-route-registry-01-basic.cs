#!/usr/bin/dotnet run

// ═══════════════════════════════════════════════════════════════════════════════
// NURU ROUTE REGISTRY TESTS - Basic Registration
// ═══════════════════════════════════════════════════════════════════════════════
// Tests for NuruRouteRegistry which stores auto-registered routes from
// source-generated [ModuleInitializer] code.
// ═══════════════════════════════════════════════════════════════════════════════

int passed = 0;
int failed = 0;

// Clear registry before tests
NuruRouteRegistry.Clear();

// ─────────────────────────────────────────────────────────────────────────────
// Test 1: Register single route
// ─────────────────────────────────────────────────────────────────────────────
{
  NuruRouteRegistry.Clear();

  CompiledRoute route = new CompiledRouteBuilder()
    .WithLiteral("greet")
    .WithParameter("name")
    .Build();

  NuruRouteRegistry.Register<TestRequest1>(route, "greet {name}", "Greet someone");

  if (NuruRouteRegistry.Count == 1 &&
      NuruRouteRegistry.IsRegistered<TestRequest1>())
  {
    Console.WriteLine("✓ Test 1: Register single route");
    passed++;
  }
  else
  {
    Console.WriteLine($"✗ Test 1: Register single route - Count={NuruRouteRegistry.Count}");
    failed++;
  }
}

// ─────────────────────────────────────────────────────────────────────────────
// Test 2: Register multiple routes for same type (aliases)
// ─────────────────────────────────────────────────────────────────────────────
{
  NuruRouteRegistry.Clear();

  CompiledRoute route1 = new CompiledRouteBuilder().WithLiteral("exit").Build();
  CompiledRoute route2 = new CompiledRouteBuilder().WithLiteral("quit").Build();
  CompiledRoute route3 = new CompiledRouteBuilder().WithLiteral("q").Build();

  NuruRouteRegistry.Register<TestRequest2>(route1, "exit", "Exit app");
  NuruRouteRegistry.Register<TestRequest2>(route2, "quit", "Exit app");
  NuruRouteRegistry.Register<TestRequest2>(route3, "q", "Exit app");

  if (NuruRouteRegistry.Count == 3)
  {
    Console.WriteLine("✓ Test 2: Register multiple routes for same type (aliases)");
    passed++;
  }
  else
  {
    Console.WriteLine($"✗ Test 2: Expected 3 routes, got {NuruRouteRegistry.Count}");
    failed++;
  }
}

// ─────────────────────────────────────────────────────────────────────────────
// Test 3: Duplicate pattern registration is ignored
// ─────────────────────────────────────────────────────────────────────────────
{
  NuruRouteRegistry.Clear();

  CompiledRoute route = new CompiledRouteBuilder().WithLiteral("test").Build();

  NuruRouteRegistry.Register<TestRequest1>(route, "test", "First");
  NuruRouteRegistry.Register<TestRequest1>(route, "test", "Second"); // Duplicate

  if (NuruRouteRegistry.Count == 1)
  {
    Console.WriteLine("✓ Test 3: Duplicate pattern registration is ignored");
    passed++;
  }
  else
  {
    Console.WriteLine($"✗ Test 3: Expected 1 route, got {NuruRouteRegistry.Count}");
    failed++;
  }
}

// ─────────────────────────────────────────────────────────────────────────────
// Test 4: IsRegistered returns true for registered type
// ─────────────────────────────────────────────────────────────────────────────
{
  NuruRouteRegistry.Clear();

  CompiledRoute route = new CompiledRouteBuilder().WithLiteral("foo").Build();
  NuruRouteRegistry.Register<TestRequest1>(route, "foo", "Foo command");

  bool isRegistered = NuruRouteRegistry.IsRegistered<TestRequest1>();
  bool isNotRegistered = !NuruRouteRegistry.IsRegistered<TestRequest2>();

  if (isRegistered && isNotRegistered)
  {
    Console.WriteLine("✓ Test 4: IsRegistered returns correct values");
    passed++;
  }
  else
  {
    Console.WriteLine($"✗ Test 4: IsRegistered={isRegistered}, IsNotRegistered={isNotRegistered}");
    failed++;
  }
}

// ─────────────────────────────────────────────────────────────────────────────
// Test 5: RegisteredRoutes enumerates all routes
// ─────────────────────────────────────────────────────────────────────────────
{
  NuruRouteRegistry.Clear();

  CompiledRoute route1 = new CompiledRouteBuilder().WithLiteral("cmd1").Build();
  CompiledRoute route2 = new CompiledRouteBuilder().WithLiteral("cmd2").Build();
  CompiledRoute route3 = new CompiledRouteBuilder().WithLiteral("cmd3").Build();

  NuruRouteRegistry.Register<TestRequest1>(route1, "cmd1", "Command 1");
  NuruRouteRegistry.Register<TestRequest2>(route2, "cmd2", "Command 2");
  NuruRouteRegistry.Register<TestRequest1>(route3, "cmd3", "Command 3"); // Same type, different pattern

  List<RegisteredRoute> routes = [.. NuruRouteRegistry.RegisteredRoutes];

  if (routes.Count == 3 &&
      routes.Exists(r => r.Pattern == "cmd1") &&
      routes.Exists(r => r.Pattern == "cmd2") &&
      routes.Exists(r => r.Pattern == "cmd3"))
  {
    Console.WriteLine("✓ Test 5: RegisteredRoutes enumerates all routes");
    passed++;
  }
  else
  {
    Console.WriteLine($"✗ Test 5: Expected 3 routes with patterns cmd1/cmd2/cmd3, got {routes.Count}");
    failed++;
  }
}

// ─────────────────────────────────────────────────────────────────────────────
// Test 6: Clear removes all routes
// ─────────────────────────────────────────────────────────────────────────────
{
  NuruRouteRegistry.Clear();

  CompiledRoute route = new CompiledRouteBuilder().WithLiteral("test").Build();
  NuruRouteRegistry.Register<TestRequest1>(route, "test", "Test");

  int countBefore = NuruRouteRegistry.Count;
  NuruRouteRegistry.Clear();
  int countAfter = NuruRouteRegistry.Count;

  if (countBefore == 1 && countAfter == 0)
  {
    Console.WriteLine("✓ Test 6: Clear removes all routes");
    passed++;
  }
  else
  {
    Console.WriteLine($"✗ Test 6: Before={countBefore}, After={countAfter}");
    failed++;
  }
}

// ─────────────────────────────────────────────────────────────────────────────
// Summary
// ─────────────────────────────────────────────────────────────────────────────
Console.WriteLine();
Console.WriteLine($"Results: {passed} passed, {failed} failed");

return failed > 0 ? 1 : 0;

// ─────────────────────────────────────────────────────────────────────────────
// Test request types
// ─────────────────────────────────────────────────────────────────────────────
public sealed class TestRequest1 : IRequest { }
public sealed class TestRequest2 : IRequest { }
