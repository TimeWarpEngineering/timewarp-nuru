#!/usr/bin/dotnet --
#pragma warning disable RCS1163 // Unused parameter - parameters must match route pattern names for binding

// Issue #98: Auto-generated --help routes should not match before user-defined routes with optional flags
// When invoking 'recent', the user's handler should execute, not the auto-generated help route

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.Routing
{

[TestTag("Routing")]
public class HelpRoutePriorityTests
{
  [ModuleInitializer]
  internal static void Register() => RegisterTests<HelpRoutePriorityTests>();

  public static async Task Should_execute_user_route_not_help_when_optional_flag_omitted()
  {
    // Arrange - Issue #98 reproduction: user route with optional flag
    // Must use AddAutoHelp() to enable auto-generated help routes
    bool userRouteExecuted = false;
    NuruCoreApp app = NuruApp.CreateBuilder([])
      .AddAutoHelp()
      .Map("recent --verbose?").WithHandler((bool verbose) =>
      {
        userRouteExecuted = true;
      }).WithDescription("Show recent items").AsQuery().Done()
      .Build();

    // Act - invoke without the optional flag
    int exitCode = await app.RunAsync(["recent"]);

    // Assert - user route should win over auto-generated help route
    exitCode.ShouldBe(0);
    userRouteExecuted.ShouldBeTrue();
  }

  public static async Task Should_show_help_when_help_flag_explicitly_provided()
  {
    // Arrange - Must use AddAutoHelp() to enable auto-generated help routes
    bool userRouteExecuted = false;
    NuruCoreApp app = NuruApp.CreateBuilder([])
      .AddAutoHelp()
      .Map("recent --verbose?").WithHandler((bool verbose) =>
      {
        userRouteExecuted = true;
      }).WithDescription("Show recent items").AsQuery().Done()
      .Build();

    // Act - invoke with --help flag
    int exitCode = await app.RunAsync(["recent", "--help"]);

    // Assert - help route should match when explicitly requested
    exitCode.ShouldBe(0);
    userRouteExecuted.ShouldBeFalse(); // Help route handled it, not user route
  }

  public static async Task Should_execute_user_route_when_optional_flag_provided()
  {
    // Arrange - Must use AddAutoHelp() to enable auto-generated help routes
    bool userRouteExecuted = false;
    bool verboseValue = false;
    NuruCoreApp app = NuruApp.CreateBuilder([])
      .AddAutoHelp()
      .Map("recent --verbose?").WithHandler((bool verbose) =>
      {
        userRouteExecuted = true;
        verboseValue = verbose;
      }).WithDescription("Show recent items").AsQuery().Done()
      .Build();

    // Act - invoke with the optional flag
    int exitCode = await app.RunAsync(["recent", "--verbose"]);

    // Assert
    exitCode.ShouldBe(0);
    userRouteExecuted.ShouldBeTrue();
    verboseValue.ShouldBeTrue();
  }

  public static async Task Should_prefer_user_route_over_help_with_multiple_optional_flags()
  {
    // Arrange - route with multiple optional flags
    // Must use AddAutoHelp() to enable auto-generated help routes
    bool userRouteExecuted = false;
    NuruCoreApp app = NuruApp.CreateBuilder([])
      .AddAutoHelp()
      .Map("list --all? --verbose?").WithHandler((bool all, bool verbose) =>
      {
        userRouteExecuted = true;
      }).WithDescription("List items").AsQuery().Done()
      .Build();

    // Act - invoke without any optional flags
    int exitCode = await app.RunAsync(["list"]);

    // Assert - user route should win
    exitCode.ShouldBe(0);
    userRouteExecuted.ShouldBeTrue();
  }

  public static async Task Help_routes_with_optional_marker_match_user_optional_specificity()
  {
    // Arrange - verify auto-generated help routes (using --help?) have same specificity as user optional flags
    // This is key to making insertion order the tie-breaker
    CompiledRoute helpRoute = PatternParser.Parse("recent --help?");
    CompiledRoute verboseRoute = PatternParser.Parse("recent --verbose?");

    // Assert - both should have equal specificity (125 = 100 literal + 25 optional)
    helpRoute.Specificity.ShouldBe(verboseRoute.Specificity);

    await Task.CompletedTask;
  }
}

} // namespace TimeWarp.Nuru.Tests.Routing
