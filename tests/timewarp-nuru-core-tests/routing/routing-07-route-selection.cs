#!/usr/bin/dotnet --
#pragma warning disable RCS1163 // Unused parameter - parameters must match route pattern names for binding

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.Routing
{

[TestTag("Routing")]
public class RouteSelectionTests
{
  [ModuleInitializer]
  internal static void Register() => RegisterTests<RouteSelectionTests>();

  public static async Task Should_select_literal_over_parameter_git_status()
  {
    // Arrange
    bool literalSelected = false;
    bool parameterSelected = false;
    NuruCoreApp app = new NuruAppBuilder()
      .Map("git status", () => { literalSelected = true; return 0; })
      .Map("git {command}", (string command) => { parameterSelected = true; return 0; })
      .Build();

    // Act
    int exitCode = await app.RunAsync(["git", "status"]);

    // Assert
    exitCode.ShouldBe(0);
    literalSelected.ShouldBeTrue();
    parameterSelected.ShouldBeFalse();

    await Task.CompletedTask;
  }

  public static async Task Should_select_typed_over_untyped_delay_500()
  {
    // Arrange
    bool typedSelected = false;
    bool untypedSelected = false;
    NuruCoreApp app = new NuruAppBuilder()
      .Map("delay {ms:int}", (int ms) => { typedSelected = true; return 0; })
      .Map("delay {duration}", (string duration) => { untypedSelected = true; return 0; })
      .Build();

    // Act
    int exitCode = await app.RunAsync(["delay", "500"]);

    // Assert
    exitCode.ShouldBe(0);
    typedSelected.ShouldBeTrue();
    untypedSelected.ShouldBeFalse();

    await Task.CompletedTask;
  }

  public static async Task Should_select_required_over_optional_deploy_prod()
  {
    // Arrange
    bool requiredSelected = false;
    bool optionalSelected = false;
    NuruCoreApp app = new NuruAppBuilder()
      .Map("deploy {env}", (string env) => { requiredSelected = true; return 0; })
      .Map("deploy {env?}", (string? env) => { optionalSelected = true; return 0; })
      .Build();

    // Act
    int exitCode = await app.RunAsync(["deploy", "prod"]);

    // Assert
    exitCode.ShouldBe(0);
    requiredSelected.ShouldBeTrue();
    optionalSelected.ShouldBeFalse();

    await Task.CompletedTask;
  }

  public static async Task Should_select_more_options_over_fewer_build_verbose_watch()
  {
    // Arrange
    bool moreOptionsSelected = false;
    bool fewerOptionsSelected = false;
    NuruCoreApp app = new NuruAppBuilder()
      .Map("build --verbose --watch", (bool verbose, bool watch) => { moreOptionsSelected = true; return 0; })
      .Map("build --verbose", (bool verbose) => { fewerOptionsSelected = true; return 0; })
      .Build();

    // Act
    int exitCode = await app.RunAsync(["build", "--verbose", "--watch"]);

    // Assert
    exitCode.ShouldBe(0);
    moreOptionsSelected.ShouldBeTrue();
    fewerOptionsSelected.ShouldBeFalse();

    await Task.CompletedTask;
  }

  public static async Task Should_select_no_option_over_required_option_build()
  {
    // Arrange
    bool noOptionSelected = false;
    bool requiredOptionSelected = false;
    NuruCoreApp app = new NuruAppBuilder()
      .Map("build", () => { noOptionSelected = true; return 0; })
      .Map("build --config {m}", (string m) => { requiredOptionSelected = true; return 0; })
      .Build();

    // Act
    int exitCode = await app.RunAsync(["build"]);

    // Assert
    exitCode.ShouldBe(0);
    noOptionSelected.ShouldBeTrue();
    requiredOptionSelected.ShouldBeFalse();

    await Task.CompletedTask;
  }

  public static async Task Should_select_catch_all_fallback_git_push()
  {
    // Arrange
    bool statusSelected = false;
    bool commitSelected = false;
    bool catchAllSelected = false;
    NuruCoreApp app = new NuruAppBuilder()
      .Map("git status", () => { statusSelected = true; return 0; })
      .Map("git commit", () => { commitSelected = true; return 0; })
      .Map("git {*args}", (string[] args) => { catchAllSelected = true; return 0; })
      .Build();

    // Act
    int exitCode = await app.RunAsync(["git", "push"]);

    // Assert
    exitCode.ShouldBe(0);
    statusSelected.ShouldBeFalse();
    commitSelected.ShouldBeFalse();
    catchAllSelected.ShouldBeTrue();

    await Task.CompletedTask;
  }

  public static async Task Should_select_first_registered_on_equal_specificity_greet_Alice()
  {
    // Arrange - Equal specificity but different literals, so only one matches
    bool firstSelected = false;
    bool secondSelected = false;
    NuruCoreApp app = new NuruAppBuilder()
      .Map("greet {name}", (string name) => { firstSelected = true; return 0; })
      .Map("hello {person}", (string person) => { secondSelected = true; return 0; })
      .Build();

    // Act
    int exitCode = await app.RunAsync(["greet", "Alice"]);

    // Assert
    exitCode.ShouldBe(0);
    firstSelected.ShouldBeTrue();
    secondSelected.ShouldBeFalse();

    await Task.CompletedTask;
  }

  public static async Task Should_select_progressive_specificity_deploy_prod_tag_v1_0()
  {
    // Arrange
    bool mostSpecificSelected = false;
    bool mediumSelected = false;
    bool lessSpecificSelected = false;
    bool leastSpecificSelected = false;
    NuruCoreApp app = new NuruAppBuilder()
      .Map("deploy {env} --tag {t} --verbose", (string env, string t, bool verbose) => { mostSpecificSelected = true; return 0; })
      .Map("deploy {env} --tag {t}", (string env, string t) => { mediumSelected = true; return 0; })
      .Map("deploy {env}", (string env) => { lessSpecificSelected = true; return 0; })
      .Map("deploy", () => { leastSpecificSelected = true; return 0; })
      .Build();

    // Act
    int exitCode = await app.RunAsync(["deploy", "prod", "--tag", "v1.0"]);

    // Assert
    exitCode.ShouldBe(0);
    mediumSelected.ShouldBeTrue();
    mostSpecificSelected.ShouldBeFalse(); // Missing --verbose
    lessSpecificSelected.ShouldBeFalse();
    leastSpecificSelected.ShouldBeFalse();

    await Task.CompletedTask;
  }
}

} // namespace TimeWarp.Nuru.Tests.Routing
