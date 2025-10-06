#!/usr/bin/dotnet --

return await RunTests<RouteSelectionTests>(clearCache: true);

[TestTag("Routing")]
public class RouteSelectionTests
{
  public static async Task Should_select_literal_over_parameter_git_status()
  {
    // Arrange
    NuruAppBuilder builder = new();
    bool literalSelected = false;
    bool parameterSelected = false;
    builder.AddRoute("git status", () => { literalSelected = true; return 0; });
    builder.AddRoute("git {command}", (string _) => { parameterSelected = true; return 0; });

    NuruApp app = builder.Build();

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
    NuruAppBuilder builder = new();
    bool typedSelected = false;
    bool untypedSelected = false;
    builder.AddRoute("delay {ms:int}", (int _) => { typedSelected = true; return 0; });
    builder.AddRoute("delay {duration}", (string _) => { untypedSelected = true; return 0; });

    NuruApp app = builder.Build();

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
    NuruAppBuilder builder = new();
    bool requiredSelected = false;
    bool optionalSelected = false;
    builder.AddRoute("deploy {env}", (string _) => { requiredSelected = true; return 0; });
    builder.AddRoute("deploy {env?}", (string? _) => { optionalSelected = true; return 0; });

    NuruApp app = builder.Build();

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
    NuruAppBuilder builder = new();
    bool moreOptionsSelected = false;
    bool fewerOptionsSelected = false;
    builder.AddRoute("build --verbose --watch", (bool _, bool _) => { moreOptionsSelected = true; return 0; });
    builder.AddRoute("build --verbose", (bool _) => { fewerOptionsSelected = true; return 0; });

    NuruApp app = builder.Build();

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
    NuruAppBuilder builder = new();
    bool noOptionSelected = false;
    bool requiredOptionSelected = false;
    builder.AddRoute("build", () => { noOptionSelected = true; return 0; });
    builder.AddRoute("build --config {m}", (string _) => { requiredOptionSelected = true; return 0; });

    NuruApp app = builder.Build();

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
    NuruAppBuilder builder = new();
    bool statusSelected = false;
    bool commitSelected = false;
    bool catchAllSelected = false;
    builder.AddRoute("git status", () => { statusSelected = true; return 0; });
    builder.AddRoute("git commit", () => { commitSelected = true; return 0; });
    builder.AddRoute("git {*args}", (string[] _) => { catchAllSelected = true; return 0; });

    NuruApp app = builder.Build();

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
    NuruAppBuilder builder = new();
    bool firstSelected = false;
    bool secondSelected = false;
    builder.AddRoute("greet {name}", (string _) => { firstSelected = true; return 0; });
    builder.AddRoute("hello {person}", (string _) => { secondSelected = true; return 0; });

    NuruApp app = builder.Build();

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
    NuruAppBuilder builder = new();
    bool mostSpecificSelected = false;
    bool mediumSelected = false;
    bool lessSpecificSelected = false;
    bool leastSpecificSelected = false;
    builder.AddRoute("deploy {env} --tag {t} --verbose", (string _, string _, bool _) => { mostSpecificSelected = true; return 0; });
    builder.AddRoute("deploy {env} --tag {t}", (string _, string _) => { mediumSelected = true; return 0; });
    builder.AddRoute("deploy {env}", (string _) => { lessSpecificSelected = true; return 0; });
    builder.AddRoute("deploy", () => { leastSpecificSelected = true; return 0; });

    NuruApp app = builder.Build();

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