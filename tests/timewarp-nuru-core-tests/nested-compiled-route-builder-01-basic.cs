#!/usr/bin/dotnet --
#:project ../../source/timewarp-nuru/timewarp-nuru.csproj

// Tests for NestedCompiledRouteBuilder<TParent>
// Verifies the nested builder pattern with Done() returning to parent

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.Core.NestedCompiledRouteBuilderTests
{

/// <summary>
/// Tests for NestedCompiledRouteBuilder - verifies fluent chain with Done()
/// </summary>
[TestTag("NestedCompiledRouteBuilder")]
public sealed class NestedCompiledRouteBuilderTests
{
  [ModuleInitializer]
  internal static void Register() => RegisterTests<NestedCompiledRouteBuilderTests>();

  /// <summary>
  /// Test: Map with nested builder creates route correctly
  /// </summary>
  public static async Task Should_create_route_via_nested_builder()
  {
    // Arrange - use SlimBuilder to avoid auto-registered routes (help, version, etc.)
    NuruCoreAppBuilder builder = NuruApp.CreateBuilder([]);

    // Act - use nested builder pattern
    EndpointBuilder<NuruCoreAppBuilder> endpointBuilder = builder.Map(
      r => r
        .WithLiteral("deploy")
        .WithParameter("env")
        .Done());

    // Assert
    builder.EndpointCollection.Count.ShouldBe(1);
    Endpoint endpoint = builder.EndpointCollection.First();
    endpoint.RoutePattern.ShouldContain("deploy");
    endpoint.RoutePattern.ShouldContain("{env}");
    endpoint.CompiledRoute.Segments.Count.ShouldBe(2);

    WriteLine("PASS: Nested builder creates route correctly");
    await Task.CompletedTask;
  }

  /// <summary>
  /// Test: Done() returns EndpointBuilder for further configuration
  /// </summary>
  public static async Task Should_return_endpoint_builder_from_done()
  {
    // Arrange
    NuruAppBuilder builder = NuruApp.CreateBuilder([]);

    // Act
    EndpointBuilder<NuruAppBuilder> endpointBuilder = builder.Map(
      r => r
        .WithLiteral("test")
        .Done())
      .WithHandler(() => "test executed");

    // Assert - should be able to configure handler
    Endpoint endpoint = builder.EndpointCollection.First();
    endpoint.Handler.ShouldNotBeNull();

    WriteLine("PASS: Done() returns EndpointBuilder for configuration");
    await Task.CompletedTask;
  }

  /// <summary>
  /// Test: Full fluent chain with Done() returning to app builder
  /// </summary>
  public static async Task Should_enable_full_fluent_chain()
  {
    // Arrange & Act
    NuruAppBuilder builder = NuruApp.CreateBuilder([])
      .Map(r => r
        .WithLiteral("deploy")
        .WithParameter("env")
        .WithOption("force", "f")
        .Done())
      .WithHandler((string env, bool force) => $"Deploying to {env}, force={force}")
      .AsCommand()
      .Done()
      .Map(r => r
        .WithLiteral("status")
        .Done())
      .WithHandler(() => "OK")
      .AsQuery()
      .Done();

    // Assert - check that routes were registered (accounting for auto-registered routes)
    // NuruApp.CreateBuilder adds help and version routes automatically
    int routeCount = builder.EndpointCollection.Count;
    routeCount.ShouldBeGreaterThanOrEqualTo(2);  // At least our 2 routes

    // Find our specific routes
    Endpoint? deployEndpoint = builder.EndpointCollection.FirstOrDefault(e => e.RoutePattern.Contains("deploy"));
    Endpoint? statusEndpoint = builder.EndpointCollection.FirstOrDefault(e => e.RoutePattern.Contains("status"));

    deployEndpoint.ShouldNotBeNull();
    deployEndpoint!.MessageType.ShouldBe(MessageType.Command);

    statusEndpoint.ShouldNotBeNull();
    statusEndpoint!.MessageType.ShouldBe(MessageType.Query);

    WriteLine("PASS: Full fluent chain works with Done()");
    await Task.CompletedTask;
  }

  /// <summary>
  /// Test: WithMessageType works on nested builder
  /// </summary>
  public static async Task Should_support_message_type_on_nested_builder()
  {
    // Arrange
    NuruAppBuilder builder = NuruApp.CreateBuilder([]);

    // Act
    builder.Map(r => r
      .WithLiteral("query")
      .WithMessageType(MessageType.Query)
      .Done());

    // Assert
    Endpoint? endpoint = builder.EndpointCollection.FirstOrDefault(e => e.RoutePattern.Contains("query"));
    endpoint.ShouldNotBeNull();
    endpoint!.CompiledRoute.MessageType.ShouldBe(MessageType.Query);

    WriteLine("PASS: WithMessageType works on nested builder");
    await Task.CompletedTask;
  }

  /// <summary>
  /// Test: Nested builder wraps standalone builder (composition)
  /// </summary>
  public static async Task Should_produce_same_route_as_standalone_builder()
  {
    // Build with standalone builder
    CompiledRoute standaloneRoute = new CompiledRouteBuilder()
      .WithLiteral("deploy")
      .WithParameter("env")
      .WithOption("force", "f")
      .Build();

    // Build with nested builder
    NuruAppBuilder builder = NuruApp.CreateBuilder([]);
    builder.Map(r => r
      .WithLiteral("deploy")
      .WithParameter("env")
      .WithOption("force", "f")
      .Done());

    Endpoint? endpoint = builder.EndpointCollection.FirstOrDefault(e => e.RoutePattern.Contains("deploy"));
    endpoint.ShouldNotBeNull();
    CompiledRoute nestedRoute = endpoint!.CompiledRoute;

    // Assert - routes should have same structure
    nestedRoute.Segments.Count.ShouldBe(standaloneRoute.Segments.Count);
    nestedRoute.Specificity.ShouldBe(standaloneRoute.Specificity);
    nestedRoute.MessageType.ShouldBe(standaloneRoute.MessageType);

    WriteLine("PASS: Nested builder produces same route as standalone");
    await Task.CompletedTask;
  }
}

} // namespace
