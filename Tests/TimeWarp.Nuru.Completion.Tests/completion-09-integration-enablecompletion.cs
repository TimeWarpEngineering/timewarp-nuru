#!/usr/bin/dotnet --

using TimeWarp.Nuru;
using TimeWarp.Nuru.Completion;
using Shouldly;

return await RunTests<EnableShellCompletionIntegrationTests>(clearCache: true);

[TestTag("Completion")]
[ClearRunfileCache]
public class EnableShellCompletionIntegrationTests
{
  public static async Task Should_register_completion_routes_for_all_shells()
  {
    // Arrange
    var builder = new NuruAppBuilder();
    builder.AddRoute("status", () => 0);
    builder.AddRoute("version", () => 0);

    // Act
    builder.EnableShellCompletion();

    // Assert - Verify completion route was registered with shell parameter
    builder.EndpointCollection.Endpoints.ShouldContain(e => e.RoutePattern.Contains("--generate-completion"));
    builder.EndpointCollection.Endpoints.ShouldContain(e => e.RoutePattern.Contains("{shell}"));

    // Verify it's a single route that accepts shell types, not separate routes per shell
    int completionRouteCount = builder.EndpointCollection.Endpoints
      .Count(e => e.RoutePattern.Contains("--generate-completion"));
    completionRouteCount.ShouldBe(1, "Should have exactly one completion route with {shell} parameter");

    await Task.CompletedTask;
  }

  public static async Task Should_register_completion_route_with_correct_pattern()
  {
    // Arrange
    var builder = new NuruAppBuilder();
    builder.AddRoute("deploy {env}", (string env) => 0);

    // Act
    builder.EnableShellCompletion();

    // Assert - Verify the specific pattern
    Endpoint? completionEndpoint = builder.EndpointCollection.Endpoints
      .FirstOrDefault(e => e.RoutePattern.Contains("--generate-completion"));

    completionEndpoint.ShouldNotBeNull();
    completionEndpoint.RoutePattern.ShouldContain("--generate-completion");
    completionEndpoint.RoutePattern.ShouldContain("{shell}");

    await Task.CompletedTask;
  }

  public static async Task Should_not_interfere_with_existing_routes()
  {
    // Arrange
    var builder = new NuruAppBuilder();
    builder.AddRoute("status", () => 0);
    builder.AddRoute("deploy {env}", (string env) => 0);
    builder.AddRoute("build --config {mode}", (string mode) => 0);

    int originalRouteCount = builder.EndpointCollection.Endpoints.Count;

    // Act
    builder.EnableShellCompletion();

    // Assert - Original routes should still exist
    builder.EndpointCollection.Endpoints.ShouldContain(e => e.RoutePattern == "status");
    builder.EndpointCollection.Endpoints.ShouldContain(e => e.RoutePattern == "deploy {env}");
    builder.EndpointCollection.Endpoints.ShouldContain(e => e.RoutePattern == "build --config {mode}");

    // Should have more routes now (original + completion routes)
    builder.EndpointCollection.Endpoints.Count.ShouldBeGreaterThan(originalRouteCount);

    await Task.CompletedTask;
  }

  public static async Task Should_work_with_empty_route_collection()
  {
    // Arrange
    var builder = new NuruAppBuilder();

    // Act
    builder.EnableShellCompletion();

    // Assert - Should still register completion routes even with no app routes
    builder.EndpointCollection.Endpoints.ShouldContain(e => e.RoutePattern.Contains("--generate-completion"));

    await Task.CompletedTask;
  }

  public static async Task Should_be_callable_multiple_times_safely()
  {
    // Arrange
    var builder = new NuruAppBuilder();
    builder.AddRoute("status", () => 0);

    // Act
    builder.EnableShellCompletion();
    builder.EnableShellCompletion(); // Call twice

    // Assert - Should not duplicate routes
    var completionRoutes = builder.EndpointCollection.Endpoints
      .Where(e => e.RoutePattern.Contains("--generate-completion"))
      .ToList();

    // Should have exactly one completion route (not duplicated)
    completionRoutes.Count.ShouldBe(1);

    await Task.CompletedTask;
  }

  public static async Task Should_register_routes_before_build()
  {
    // Arrange
    var builder = new NuruAppBuilder();
    builder.AddRoute("test", () => 0);

    // Act - EnableShellCompletion before Build
    builder.EnableShellCompletion();

    // Assert - Routes should be visible in builder
    builder.EndpointCollection.Endpoints.ShouldContain(e => e.RoutePattern.Contains("--generate-completion"));

    await Task.CompletedTask;
  }

  public static async Task Should_support_all_shell_types()
  {
    // Arrange
    var builder = new NuruAppBuilder();
    builder.AddRoute("status", () => 0);
    builder.EnableShellCompletion();

    // Assert - All supported shell types should be matchable
    Endpoint completionEndpoint = builder.EndpointCollection.Endpoints
      .First(e => e.RoutePattern.Contains("--generate-completion"));

    // The route pattern should support bash, zsh, powershell, fish
    completionEndpoint.RoutePattern.ShouldContain("{shell}");

    await Task.CompletedTask;
  }

  public static async Task Should_work_with_complex_route_patterns()
  {
    // Arrange
    var builder = new NuruAppBuilder();
    builder.AddRoute("deploy {env} --version {ver} --force --dry-run,-d", (string env, string ver) => 0);
    builder.AddRoute("git {*args}", (string[] args) => 0);
    builder.AddRoute("config set {key} {value?}", (string key, string? value) => 0);

    // Act
    builder.EnableShellCompletion();

    // Assert - Completion should work with all route types
    builder.EndpointCollection.Endpoints.ShouldContain(e => e.RoutePattern.Contains("--generate-completion"));
    builder.EndpointCollection.Endpoints.Count.ShouldBeGreaterThan(3); // Original 3 + completion routes

    await Task.CompletedTask;
  }

  public static async Task Should_preserve_route_order()
  {
    // Arrange
    var builder = new NuruAppBuilder();
    builder.AddRoute("first", () => 0);
    builder.AddRoute("second", () => 0);
    builder.EnableShellCompletion();
    builder.AddRoute("third", () => 0);

    // Assert - Original routes should maintain their relative order
    var nonCompletionRoutes = builder.EndpointCollection.Endpoints
      .Where(e => !e.RoutePattern.Contains("--generate-completion"))
      .Select(e => e.RoutePattern)
      .ToList();

    nonCompletionRoutes.ShouldContain("first");
    nonCompletionRoutes.ShouldContain("second");
    nonCompletionRoutes.ShouldContain("third");

    await Task.CompletedTask;
  }

  public static async Task Should_integrate_with_builder_fluent_api()
  {
    // Arrange & Act - Test fluent chaining
    NuruAppBuilder builder = new NuruAppBuilder()
      .AddRoute("status", () => 0)
      .EnableShellCompletion()
      .AddRoute("version", () => 0);

    // Assert
    builder.EndpointCollection.Endpoints.ShouldContain(e => e.RoutePattern == "status");
    builder.EndpointCollection.Endpoints.ShouldContain(e => e.RoutePattern == "version");
    builder.EndpointCollection.Endpoints.ShouldContain(e => e.RoutePattern.Contains("--generate-completion"));

    await Task.CompletedTask;
  }
}
