#!/usr/bin/dotnet --

// Clear cache to ensure parser changes are picked up (parsing is source-compiled)

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.Parser
{

[TestTag("Parser")]
public class MixedModifiersTests
{
  [ModuleInitializer]
  internal static void Register() => RegisterTests<MixedModifiersTests>();

  // Section 14: Mixed modifiers - combining optional (?) and repeated (*)
  // Tests validation of complex modifier combinations on options and parameters

  public static async Task Should_parse_optional_flag_with_repeated_param()
  {
    // Arrange & Act
    CompiledRoute route = PatternParser.Parse("docker --env? {var}*");

    // Assert - Optional flag with repeated parameter value
    route.ShouldNotBeNull();
    route.OptionMatchers.Count.ShouldBe(1);

    OptionMatcher envOption = route.OptionMatchers[0];
    envOption.MatchPattern.ShouldBe("--env");
    envOption.IsOptional.ShouldBeTrue();
    envOption.ExpectsValue.ShouldBeTrue();
    envOption.ParameterName.ShouldBe("var");
    envOption.IsRepeated.ShouldBeTrue();

    await Task.CompletedTask;
  }

  public static async Task Should_parse_optional_flag_with_optional_repeated_param()
  {
    // Arrange & Act
    CompiledRoute route = PatternParser.Parse("deploy --env? {var?}*");

    // Assert - Everything optional and repeated (maximum flexibility)
    route.ShouldNotBeNull();
    route.OptionMatchers.Count.ShouldBe(1);

    OptionMatcher envOption = route.OptionMatchers[0];
    envOption.MatchPattern.ShouldBe("--env");
    envOption.IsOptional.ShouldBeTrue();
    envOption.ExpectsValue.ShouldBeTrue();
    envOption.IsRepeated.ShouldBeTrue();

    await Task.CompletedTask;
  }

  public static async Task Should_parse_optional_repeated_typed_param()
  {
    // Arrange & Act
    CompiledRoute route = PatternParser.Parse("run --port? {p:int}*");

    // Assert - Optional flag with repeated typed parameter
    route.ShouldNotBeNull();
    route.OptionMatchers.Count.ShouldBe(1);

    OptionMatcher portOption = route.OptionMatchers[0];
    portOption.MatchPattern.ShouldBe("--port");
    portOption.IsOptional.ShouldBeTrue();
    portOption.IsRepeated.ShouldBeTrue();
    portOption.ParameterName.ShouldBe("p");

    await Task.CompletedTask;
  }

  public static async Task Should_parse_multiple_optional_repeated_options()
  {
    // Arrange & Act
    CompiledRoute route = PatternParser.Parse("deploy --label? {l}* --env? {e}*");

    // Assert - Multiple options with mixed modifiers
    route.ShouldNotBeNull();
    route.OptionMatchers.Count.ShouldBe(2);

    OptionMatcher labelOption = route.OptionMatchers[0];
    labelOption.MatchPattern.ShouldBe("--label");
    labelOption.IsOptional.ShouldBeTrue();
    labelOption.IsRepeated.ShouldBeTrue();

    OptionMatcher envOption = route.OptionMatchers[1];
    envOption.MatchPattern.ShouldBe("--env");
    envOption.IsOptional.ShouldBeTrue();
    envOption.IsRepeated.ShouldBeTrue();

    await Task.CompletedTask;
  }

  public static async Task Should_parse_real_world_docker_pattern()
  {
    // Arrange & Act
    CompiledRoute route = PatternParser.Parse("docker run {img} --env? {e}* --volume? {v}* --detach?");

    // Assert - Docker-like pattern with positional, optional repeated, and boolean flag
    route.ShouldNotBeNull();
    route.OptionMatchers.Count.ShouldBe(3);

    // Check positional parameter
    ParameterMatcher? imgParam = route.PositionalMatchers.OfType<ParameterMatcher>()
      .FirstOrDefault(p => p.Name == "img");
    imgParam.ShouldNotBeNull();

    // Check optional repeated options
    OptionMatcher? envOption = route.OptionMatchers.FirstOrDefault(o => o.MatchPattern == "--env");
    envOption.ShouldNotBeNull();
    envOption.IsOptional.ShouldBeTrue();
    envOption.IsRepeated.ShouldBeTrue();

    OptionMatcher? volumeOption = route.OptionMatchers.FirstOrDefault(o => o.MatchPattern == "--volume");
    volumeOption.ShouldNotBeNull();
    volumeOption.IsOptional.ShouldBeTrue();
    volumeOption.IsRepeated.ShouldBeTrue();

    // Check optional boolean flag
    OptionMatcher? detachOption = route.OptionMatchers.FirstOrDefault(o => o.MatchPattern == "--detach");
    detachOption.ShouldNotBeNull();
    detachOption.IsOptional.ShouldBeTrue();
    detachOption.ExpectsValue.ShouldBeFalse();

    await Task.CompletedTask;
  }

  public static async Task Should_parse_mixed_modifiers_with_catchall()
  {
    // Arrange & Act
    CompiledRoute route = PatternParser.Parse("exec {cmd} --env? {e}* {*args}");

    // Assert - Combines positional, optional repeated option, and catch-all
    route.ShouldNotBeNull();
    route.HasCatchAll.ShouldBeTrue();
    route.CatchAllParameterName.ShouldBe("args");

    ParameterMatcher? cmdParam = route.PositionalMatchers.OfType<ParameterMatcher>()
      .FirstOrDefault(p => p.Name == "cmd");
    cmdParam.ShouldNotBeNull();
    cmdParam.IsCatchAll.ShouldBeFalse();

    OptionMatcher envOption = route.OptionMatchers[0];
    envOption.MatchPattern.ShouldBe("--env");
    envOption.IsOptional.ShouldBeTrue();
    envOption.IsRepeated.ShouldBeTrue();

    await Task.CompletedTask;
  }

  public static async Task Should_parse_optional_repeated_with_required_and_optional_positionals()
  {
    // Arrange & Act
    CompiledRoute route = PatternParser.Parse("cmd {pos1} {pos2?} --opt? {val}*");

    // Assert - Mix of required/optional positionals with optional repeated option
    route.ShouldNotBeNull();

    ParameterMatcher? pos1 = route.PositionalMatchers.OfType<ParameterMatcher>()
      .FirstOrDefault(p => p.Name == "pos1");
    pos1.ShouldNotBeNull();
    pos1.IsOptional.ShouldBeFalse();

    ParameterMatcher? pos2 = route.PositionalMatchers.OfType<ParameterMatcher>()
      .FirstOrDefault(p => p.Name == "pos2");
    pos2.ShouldNotBeNull();
    pos2.IsOptional.ShouldBeTrue();

    OptionMatcher optOption = route.OptionMatchers[0];
    optOption.MatchPattern.ShouldBe("--opt");
    optOption.IsOptional.ShouldBeTrue();
    optOption.IsRepeated.ShouldBeTrue();

    await Task.CompletedTask;
  }

  public static async Task Should_reject_wrong_modifier_order_on_parameter()
  {
    // Arrange & Act & Assert
    PatternException exception = Should.Throw<PatternException>(() =>
      PatternParser.Parse("build --define {key}*?")
    );

    // Wrong modifier order: should be {key?}* not {key}*?
    exception.ParseErrors.ShouldNotBeNull();
    exception.ParseErrors.Count.ShouldBeGreaterThan(0);

    await Task.CompletedTask;
  }

  public static async Task Should_reject_wrong_modifier_order_on_flag()
  {
    // Arrange & Act & Assert
    PatternException exception = Should.Throw<PatternException>(() =>
      PatternParser.Parse("docker --env*?")
    );

    // Wrong modifier order on flag itself
    exception.ParseErrors.ShouldNotBeNull();
    exception.ParseErrors.Count.ShouldBeGreaterThan(0);

    await Task.CompletedTask;
  }

  public static async Task Should_reject_modifiers_on_flag_not_param()
  {
    // Arrange & Act & Assert
    PatternException exception = Should.Throw<PatternException>(() =>
      PatternParser.Parse("test --tag?* {t}")
    );

    // Modifiers should be on parameter, not on flag name
    exception.ParseErrors.ShouldNotBeNull();
    exception.ParseErrors.Count.ShouldBeGreaterThan(0);

    await Task.CompletedTask;
  }
}

} // namespace TimeWarp.Nuru.Tests.Parser
