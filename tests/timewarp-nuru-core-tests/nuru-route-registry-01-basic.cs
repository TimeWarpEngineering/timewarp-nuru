#!/usr/bin/dotnet --
#:project ../../source/timewarp-nuru/timewarp-nuru.csproj

// ═══════════════════════════════════════════════════════════════════════════════
// NURU ROUTE REGISTRY TESTS - Basic Registration
// ═══════════════════════════════════════════════════════════════════════════════
// Tests for NuruRouteRegistry which stores auto-registered routes from
// source-generated [ModuleInitializer] code.
// ═══════════════════════════════════════════════════════════════════════════════

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.Core.RouteRegistry
{

[TestTag("RouteRegistry")]
public sealed class NuruRouteRegistryTests
{
  [ModuleInitializer]
  internal static void Register() => RegisterTests<NuruRouteRegistryTests>();

  public static async Task Should_register_single_route()
  {
    // Arrange
    NuruRouteRegistry.Clear();

    CompiledRoute route = new TimeWarp.Nuru.CompiledRouteBuilder()
      .WithLiteral("greet")
      .WithParameter("name")
      .Build();

    // Act
    NuruRouteRegistry.Register<TestRequest1>(route, "greet {name}", "Greet someone");

    // Assert
    NuruRouteRegistry.Count.ShouldBe(1);
    NuruRouteRegistry.IsRegistered<TestRequest1>().ShouldBeTrue();

    // Cleanup
    NuruRouteRegistry.Clear();

    await Task.CompletedTask;
  }

  public static async Task Should_register_multiple_routes_for_same_type_as_aliases()
  {
    // Arrange
    NuruRouteRegistry.Clear();

    CompiledRoute route1 = new TimeWarp.Nuru.CompiledRouteBuilder().WithLiteral("exit").Build();
    CompiledRoute route2 = new TimeWarp.Nuru.CompiledRouteBuilder().WithLiteral("quit").Build();
    CompiledRoute route3 = new TimeWarp.Nuru.CompiledRouteBuilder().WithLiteral("q").Build();

    // Act
    NuruRouteRegistry.Register<TestRequest2>(route1, "exit", "Exit app");
    NuruRouteRegistry.Register<TestRequest2>(route2, "quit", "Exit app");
    NuruRouteRegistry.Register<TestRequest2>(route3, "q", "Exit app");

    // Assert
    NuruRouteRegistry.Count.ShouldBe(3);

    // Cleanup
    NuruRouteRegistry.Clear();

    await Task.CompletedTask;
  }

  public static async Task Should_ignore_duplicate_pattern_registration()
  {
    // Arrange
    NuruRouteRegistry.Clear();

    CompiledRoute route = new TimeWarp.Nuru.CompiledRouteBuilder().WithLiteral("test").Build();

    // Act
    NuruRouteRegistry.Register<TestRequest1>(route, "test", "First");
    NuruRouteRegistry.Register<TestRequest1>(route, "test", "Second"); // Duplicate

    // Assert
    NuruRouteRegistry.Count.ShouldBe(1);

    // Cleanup
    NuruRouteRegistry.Clear();

    await Task.CompletedTask;
  }

  public static async Task Should_return_correct_is_registered_status()
  {
    // Arrange
    NuruRouteRegistry.Clear();

    CompiledRoute route = new TimeWarp.Nuru.CompiledRouteBuilder().WithLiteral("foo").Build();
    NuruRouteRegistry.Register<TestRequest1>(route, "foo", "Foo command");

    // Act & Assert
    NuruRouteRegistry.IsRegistered<TestRequest1>().ShouldBeTrue();
    NuruRouteRegistry.IsRegistered<TestRequest2>().ShouldBeFalse();

    // Cleanup
    NuruRouteRegistry.Clear();

    await Task.CompletedTask;
  }

  public static async Task Should_enumerate_all_registered_routes()
  {
    // Arrange
    NuruRouteRegistry.Clear();

    CompiledRoute route1 = new TimeWarp.Nuru.CompiledRouteBuilder().WithLiteral("cmd1").Build();
    CompiledRoute route2 = new TimeWarp.Nuru.CompiledRouteBuilder().WithLiteral("cmd2").Build();
    CompiledRoute route3 = new TimeWarp.Nuru.CompiledRouteBuilder().WithLiteral("cmd3").Build();

    NuruRouteRegistry.Register<TestRequest1>(route1, "cmd1", "Command 1");
    NuruRouteRegistry.Register<TestRequest2>(route2, "cmd2", "Command 2");
    NuruRouteRegistry.Register<TestRequest1>(route3, "cmd3", "Command 3"); // Same type, different pattern

    // Act
    List<RegisteredRoute> routes = [.. NuruRouteRegistry.RegisteredRoutes];

    // Assert
    routes.Count.ShouldBe(3);
    routes.ShouldContain(r => r.Pattern == "cmd1");
    routes.ShouldContain(r => r.Pattern == "cmd2");
    routes.ShouldContain(r => r.Pattern == "cmd3");

    // Cleanup
    NuruRouteRegistry.Clear();

    await Task.CompletedTask;
  }

  public static async Task Should_clear_all_routes()
  {
    // Arrange
    NuruRouteRegistry.Clear();

    CompiledRoute route = new TimeWarp.Nuru.CompiledRouteBuilder().WithLiteral("test").Build();
    NuruRouteRegistry.Register<TestRequest1>(route, "test", "Test");

    int countBefore = NuruRouteRegistry.Count;

    // Act
    NuruRouteRegistry.Clear();
    int countAfter = NuruRouteRegistry.Count;

    // Assert
    countBefore.ShouldBe(1);
    countAfter.ShouldBe(0);

    await Task.CompletedTask;
  }
}

// ─────────────────────────────────────────────────────────────────────────────
// Test request types
// ─────────────────────────────────────────────────────────────────────────────
public sealed class TestRequest1 : IRequest { }
public sealed class TestRequest2 : IRequest { }

} // namespace TimeWarp.Nuru.Tests.Core.RouteRegistry
