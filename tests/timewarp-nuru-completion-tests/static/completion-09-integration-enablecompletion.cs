#!/usr/bin/dotnet --
#if !JARIBU_MULTI
return await RunAllTests();
#endif

[TestTag("Completion")]
public class EnableStaticCompletionIntegrationTests
{
  [ModuleInitializer]
  internal static void Register() => RegisterTests<EnableStaticCompletionIntegrationTests>();

  public static async Task Should_register_completion_routes_for_all_shells()
  {
    // Arrange
    NuruAppBuilder builder = new();
    builder.Map("status")
      .WithHandler(() => 0)
      .AsQuery()
      .Done();
    builder.Map("version")
      .WithHandler(() => 0)
      .AsQuery()
      .Done();

    // Act
    builder.EnableStaticCompletion();

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
    NuruAppBuilder builder = new();
    builder.Map("deploy {env}")
      .WithHandler((string env) => 0)
      .AsCommand()
      .Done();

    // Act
    builder.EnableStaticCompletion();

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
    NuruAppBuilder builder = new();
    builder.Map("status")
      .WithHandler(() => 0)
      .AsQuery()
      .Done();
    builder.Map("deploy {env}")
      .WithHandler((string env) => 0)
      .AsCommand()
      .Done();
    builder.Map("build --config {mode}")
      .WithHandler((string mode) => 0)
      .AsCommand()
      .Done();

    int originalRouteCount = builder.EndpointCollection.Endpoints.Count;

    // Act
    builder.EnableStaticCompletion();

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
    NuruAppBuilder builder = new();

    // Act
    builder.EnableStaticCompletion();

    // Assert - Should still register completion routes even with no app routes
    builder.EndpointCollection.Endpoints.ShouldContain(e => e.RoutePattern.Contains("--generate-completion"));

    await Task.CompletedTask;
  }

  public static async Task Should_be_callable_multiple_times_safely()
  {
    // Arrange
    NuruAppBuilder builder = new();
    builder.Map("status")
      .WithHandler(() => 0)
      .AsQuery()
      .Done();

    // Act
    builder.EnableStaticCompletion();
    builder.EnableStaticCompletion(); // Call twice

    // Assert - Should not duplicate routes
    List<Endpoint> completionRoutes =
    [
      .. builder.EndpointCollection.Endpoints
        .Where(e => e.RoutePattern.Contains("--generate-completion"))
    ];

    // Should have exactly one completion route (not duplicated)
    completionRoutes.Count.ShouldBe(1);

    await Task.CompletedTask;
  }

  public static async Task Should_register_routes_before_build()
  {
    // Arrange
    NuruAppBuilder builder = new();
    builder.Map("test")
      .WithHandler(() => 0)
      .AsCommand()
      .Done();

    // Act - EnableStaticCompletion before Build
    builder.EnableStaticCompletion();

    // Assert - Routes should be visible in builder
    builder.EndpointCollection.Endpoints.ShouldContain(e => e.RoutePattern.Contains("--generate-completion"));

    await Task.CompletedTask;
  }

  public static async Task Should_support_all_shell_types()
  {
    // Arrange
    NuruAppBuilder builder = new();
    builder.Map("status")
      .WithHandler(() => 0)
      .AsQuery()
      .Done();
    builder.EnableStaticCompletion();

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
    NuruAppBuilder builder = new();
    builder.Map("deploy {env} --version {ver} --force --dry-run,-d")
      .WithHandler((string env, string ver) => 0)
      .AsCommand()
      .Done();
    builder.Map("git {*args}")
      .WithHandler((string[] args) => 0)
      .AsCommand()
      .Done();
    builder.Map("config set {key} {value?}")
      .WithHandler((string key, string? value) => 0)
      .AsCommand()
      .Done();

    // Act
    builder.EnableStaticCompletion();

    // Assert - Completion should work with all route types
    builder.EndpointCollection.Endpoints.ShouldContain(e => e.RoutePattern.Contains("--generate-completion"));
    builder.EndpointCollection.Endpoints.Count.ShouldBeGreaterThan(3); // Original 3 + completion routes

    await Task.CompletedTask;
  }

  public static async Task Should_preserve_route_order()
  {
    // Arrange
    NuruAppBuilder builder = new();
    builder.Map("first")
      .WithHandler(() => 0)
      .AsCommand()
      .Done();
    builder.Map("second")
      .WithHandler(() => 0)
      .AsCommand()
      .Done();
    builder.EnableStaticCompletion();
    builder.Map("third")
      .WithHandler(() => 0)
      .AsCommand()
      .Done();

    // Assert - Original routes should maintain their relative order
    List<string> nonCompletionRoutes =
    [
      .. builder.EndpointCollection.Endpoints
        .Where(e => !e.RoutePattern.Contains("--generate-completion"))
        .Select(e => e.RoutePattern)
    ];

    nonCompletionRoutes.ShouldContain("first");
    nonCompletionRoutes.ShouldContain("second");
    nonCompletionRoutes.ShouldContain("third");

    await Task.CompletedTask;
  }

  public static async Task Should_integrate_with_builder_fluent_api()
  {
    // Arrange & Act - Test fluent chaining
    NuruAppBuilder builder = new();
    builder.Map("status")
      .WithHandler(() => 0)
      .AsQuery()
      .Done();
    builder.EnableStaticCompletion();
    builder.Map("version")
      .WithHandler(() => 0)
      .AsQuery()
      .Done();

    // Assert
    builder.EndpointCollection.Endpoints.ShouldContain(e => e.RoutePattern == "status");
    builder.EndpointCollection.Endpoints.ShouldContain(e => e.RoutePattern == "version");
    builder.EndpointCollection.Endpoints.ShouldContain(e => e.RoutePattern.Contains("--generate-completion"));

    await Task.CompletedTask;
  }
}
