#!/usr/bin/dotnet --

// Clear cache to ensure parser changes are picked up (parsing is source-compiled)

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.Parser
{

[TestTag("Parser")]
public class ComplexIntegrationTests
{
  [ModuleInitializer]
  internal static void Register() => RegisterTests<ComplexIntegrationTests>();

  // Section 14: Complex pattern integration
  // Tests real-world CLI patterns combining multiple features

  public static async Task Should_parse_docker_style_command()
  {
    // Arrange & Act - Short options, repeated option, end-of-options, catch-all
    CompiledRoute route = PatternParser.Parse("docker run -i -t --env {e}* -- {*cmd}");

    // Assert
    route.ShouldNotBeNull();
    route.PositionalMatchers.Count.ShouldBeGreaterThan(0);
    route.OptionMatchers.Count.ShouldBe(3); // -i, -t, and --env

    // Find the -i and -t options (boolean flags)
    OptionMatcher? iOption = route.OptionMatchers.FirstOrDefault(o => o.MatchPattern == "-i");
    iOption.ShouldNotBeNull();
    iOption.ExpectsValue.ShouldBeFalse();

    OptionMatcher? tOption = route.OptionMatchers.FirstOrDefault(o => o.MatchPattern == "-t");
    tOption.ShouldNotBeNull();
    tOption.ExpectsValue.ShouldBeFalse();

    // Find the --env option (repeated)
    OptionMatcher? envOption = route.OptionMatchers.FirstOrDefault(o => o.MatchPattern == "--env");
    envOption.ShouldNotBeNull();
    envOption.ExpectsValue.ShouldBeTrue();
    envOption.IsRepeated.ShouldBeTrue();

    route.HasCatchAll.ShouldBeTrue();
    route.CatchAllParameterName.ShouldBe("cmd");

    await Task.CompletedTask;
  }

  public static async Task Should_parse_git_style_with_aliases()
  {
    // Arrange & Act - Option aliases, multiple flags
    CompiledRoute route = PatternParser.Parse("git commit --message,-m {msg} --amend --no-verify");

    // Assert
    route.ShouldNotBeNull();
    route.OptionMatchers.Count.ShouldBe(3);

    // Message option with alias
    OptionMatcher? msgOption = route.OptionMatchers.FirstOrDefault(o =>
      o.MatchPattern == "--message" || o.AlternateForm == "-m"
    );
    msgOption.ShouldNotBeNull();
    msgOption.MatchPattern.ShouldBe("--message");
    msgOption.AlternateForm.ShouldBe("-m");
    msgOption.ExpectsValue.ShouldBeTrue();

    // Boolean flags
    OptionMatcher? amendOption = route.OptionMatchers.FirstOrDefault(o => o.MatchPattern == "--amend");
    amendOption.ShouldNotBeNull();
    amendOption.ExpectsValue.ShouldBeFalse();

    OptionMatcher? noVerifyOption = route.OptionMatchers.FirstOrDefault(o => o.MatchPattern == "--no-verify");
    noVerifyOption.ShouldNotBeNull();
    noVerifyOption.ExpectsValue.ShouldBeFalse();

    await Task.CompletedTask;
  }

  public static async Task Should_parse_kubectl_style_imperative()
  {
    // Arrange & Act - Typed parameter, aliases, optional option value
    CompiledRoute route = PatternParser.Parse("kubectl create {resource:string} {name} --namespace,-n {ns?}");

    // Assert
    route.ShouldNotBeNull();

    // Verify positional parameters
    ParameterMatcher? resourceParam = route.PositionalMatchers.OfType<ParameterMatcher>()
      .FirstOrDefault(p => p.Name == "resource");
    resourceParam.ShouldNotBeNull();
    resourceParam.Constraint.ShouldBe("string");

    ParameterMatcher? nameParam = route.PositionalMatchers.OfType<ParameterMatcher>()
      .FirstOrDefault(p => p.Name == "name");
    nameParam.ShouldNotBeNull();

    // Verify option with alias
    OptionMatcher? nsOption = route.OptionMatchers.FirstOrDefault(o =>
      o.MatchPattern == "--namespace" || o.AlternateForm == "-n"
    );
    nsOption.ShouldNotBeNull();
    nsOption.MatchPattern.ShouldBe("--namespace");
    nsOption.AlternateForm.ShouldBe("-n");
    nsOption.ExpectsValue.ShouldBeTrue();

    await Task.CompletedTask;
  }

  public static async Task Should_parse_progressive_enhancement_pattern()
  {
    // Arrange & Act - Optional param, optional option value, boolean flags
    CompiledRoute route = PatternParser.Parse("build {project?} --config {cfg?} --verbose --watch");

    // Assert
    route.ShouldNotBeNull();

    // Optional parameter
    ParameterMatcher? projectParam = route.PositionalMatchers.OfType<ParameterMatcher>()
      .FirstOrDefault(p => p.Name == "project");
    projectParam.ShouldNotBeNull();
    projectParam.IsOptional.ShouldBeTrue();

    // Options
    route.OptionMatchers.Count.ShouldBe(3);

    OptionMatcher? configOption = route.OptionMatchers.FirstOrDefault(o => o.MatchPattern == "--config");
    configOption.ShouldNotBeNull();
    configOption.ExpectsValue.ShouldBeTrue();

    OptionMatcher? verboseOption = route.OptionMatchers.FirstOrDefault(o => o.MatchPattern == "--verbose");
    verboseOption.ShouldNotBeNull();
    verboseOption.ExpectsValue.ShouldBeFalse();

    OptionMatcher? watchOption = route.OptionMatchers.FirstOrDefault(o => o.MatchPattern == "--watch");
    watchOption.ShouldNotBeNull();
    watchOption.ExpectsValue.ShouldBeFalse();

    await Task.CompletedTask;
  }

  public static async Task Should_parse_passthrough_pattern()
  {
    // Arrange & Act - Universal catch-all for gradual refinement
    CompiledRoute catchAllRoute = PatternParser.Parse("npm {*args}");
    CompiledRoute specificRoute = PatternParser.Parse("npm install {pkg}");

    // Assert - Specificity ordering
    catchAllRoute.ShouldNotBeNull();
    catchAllRoute.HasCatchAll.ShouldBeTrue();

    specificRoute.ShouldNotBeNull();
    specificRoute.PositionalMatchers.OfType<ParameterMatcher>()
      .FirstOrDefault(p => p.Name == "pkg")
      .ShouldNotBeNull();

    // Specific route should have higher specificity
    specificRoute.Specificity.ShouldBeGreaterThan(catchAllRoute.Specificity);

    await Task.CompletedTask;
  }

  public static async Task Should_parse_multi_valued_options_with_types()
  {
    // Arrange & Act - Multiple array options, trailing parameter
    CompiledRoute route = PatternParser.Parse("process --id {id:int}* --tag {t}* {script}");

    // Assert
    route.ShouldNotBeNull();
    route.OptionMatchers.Count.ShouldBe(2);

    // Typed repeated option
    OptionMatcher? idOption = route.OptionMatchers.FirstOrDefault(o => o.MatchPattern == "--id");
    idOption.ShouldNotBeNull();
    idOption.ExpectsValue.ShouldBeTrue();
    idOption.IsRepeated.ShouldBeTrue();
    idOption.ParameterName.ShouldBe("id");

    // String repeated option
    OptionMatcher? tagOption = route.OptionMatchers.FirstOrDefault(o => o.MatchPattern == "--tag");
    tagOption.ShouldNotBeNull();
    tagOption.ExpectsValue.ShouldBeTrue();
    tagOption.IsRepeated.ShouldBeTrue();

    // Trailing positional parameter
    ParameterMatcher? scriptParam = route.PositionalMatchers.OfType<ParameterMatcher>()
      .FirstOrDefault(p => p.Name == "script");
    scriptParam.ShouldNotBeNull();

    await Task.CompletedTask;
  }

  public static async Task Should_parse_description_rich_pattern()
  {
    // Arrange & Act - Descriptions throughout (don't affect matching)
    CompiledRoute route = PatternParser.Parse("deploy {env|Environment} --dry-run,-d|Preview mode --tag {t|Version}*");

    // Assert
    route.ShouldNotBeNull();

    // Parameter with description
    ParameterMatcher? envParam = route.PositionalMatchers.OfType<ParameterMatcher>()
      .FirstOrDefault(p => p.Name == "env");
    envParam.ShouldNotBeNull();
    envParam.Description.ShouldBe("Environment");

    // Option with description and alias
    OptionMatcher? dryRunOption = route.OptionMatchers.FirstOrDefault(o =>
      o.MatchPattern == "--dry-run" || o.AlternateForm == "-d"
    );
    dryRunOption.ShouldNotBeNull();
    dryRunOption.MatchPattern.ShouldBe("--dry-run");
    dryRunOption.AlternateForm.ShouldBe("-d");
    dryRunOption.Description.ShouldBe("Preview mode");

    // Repeated option
    OptionMatcher? tagOption = route.OptionMatchers.FirstOrDefault(o => o.MatchPattern == "--tag");
    tagOption.ShouldNotBeNull();
    tagOption.IsRepeated.ShouldBeTrue();
    // Note: Description for repeated option is on the parameter, not parsed in pattern

    await Task.CompletedTask;
  }
}

} // namespace TimeWarp.Nuru.Tests.Parser
