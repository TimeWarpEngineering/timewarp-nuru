#!/usr/bin/dotnet --

// Clear cache to ensure parser changes are picked up (parsing is source-compiled)

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.Parser
{

[TestTag("Parser")]
public class OptionModifiersTests
{
  [ModuleInitializer]
  internal static void Register() => RegisterTests<OptionModifiersTests>();

  // Section 8: Option modifier combinations and parsing
  // Options are flags (--verbose, -v) that can optionally take parameters

  public static async Task Should_parse_boolean_flag()
  {
    // Arrange & Act
    CompiledRoute route = PatternParser.Parse("build --verbose");

    // Assert - Boolean flag (no parameter)
    route.ShouldNotBeNull();
    route.OptionMatchers.Count.ShouldBe(1);

    OptionMatcher verboseOption = route.OptionMatchers[0];
    verboseOption.MatchPattern.ShouldBe("--verbose");
    verboseOption.ExpectsValue.ShouldBeFalse(); // Boolean flag

    await Task.CompletedTask;
  }

  public static async Task Should_parse_option_with_required_value()
  {
    // Arrange & Act
    CompiledRoute route = PatternParser.Parse("build --config {mode}");

    // Assert - Option with required (non-nullable) value
    route.ShouldNotBeNull();
    route.OptionMatchers.Count.ShouldBe(1);

    OptionMatcher configOption = route.OptionMatchers[0];
    configOption.MatchPattern.ShouldBe("--config");
    configOption.ExpectsValue.ShouldBeTrue();
    configOption.ParameterName.ShouldBe("mode");
    configOption.IsOptional.ShouldBeFalse();

    await Task.CompletedTask;
  }

  public static async Task Should_parse_option_with_optional_value()
  {
    // Arrange & Act
    CompiledRoute route = PatternParser.Parse("build --config {mode?}");

    // Assert - Option required, but VALUE is optional (nullable)
    // This is the correct way to make an option's value optional
    route.ShouldNotBeNull();
    route.OptionMatchers.Count.ShouldBe(1);

    OptionMatcher configOption = route.OptionMatchers[0];
    configOption.MatchPattern.ShouldBe("--config");
    configOption.ExpectsValue.ShouldBeTrue();
    configOption.ParameterName.ShouldBe("mode");
    // Note: IsOptional is False - it refers to whether the option itself can be omitted

    await Task.CompletedTask;
  }

  public static async Task Should_parse_optional_flag_with_required_value()
  {
    // Arrange & Act
    CompiledRoute route = PatternParser.Parse("--tag? {tag}");

    // Assert - Flag is optional (? modifier), but value is required if flag present
    // Pattern: --flag? {value} (per syntax-rules.md and parameter-optionality.md)
    // Use case: ./script (tag=null) OR ./script --tag Lexer (tag="Lexer")
    route.ShouldNotBeNull();
    route.OptionMatchers.Count.ShouldBe(1);

    OptionMatcher tagOption = route.OptionMatchers[0];
    tagOption.MatchPattern.ShouldBe("--tag");
    tagOption.ExpectsValue.ShouldBeTrue();
    tagOption.ParameterName.ShouldBe("tag");
    tagOption.IsOptional.ShouldBeTrue(); // ← Flag itself is optional (can be omitted entirely)

    await Task.CompletedTask;
  }

  public static async Task Should_parse_optional_flag_with_optional_value()
  {
    // Arrange & Act
    CompiledRoute route = PatternParser.Parse("--config? {mode?}");

    // Assert - Both flag and value are optional (? on both)
    // Pattern: --flag? {value?} (per syntax-rules.md and parameter-optionality.md)
    // Use case: command (mode=null), command --config (mode=null), command --config debug (mode="debug")
    route.ShouldNotBeNull();
    route.OptionMatchers.Count.ShouldBe(1);

    OptionMatcher configOption = route.OptionMatchers[0];
    configOption.MatchPattern.ShouldBe("--config");
    configOption.ExpectsValue.ShouldBeTrue();
    configOption.ParameterName.ShouldBe("mode");
    configOption.IsOptional.ShouldBeTrue(); // ← Flag is optional

    // Note: Parameter optionality (the ? in {mode?}) is handled during parameter binding,
    // not stored on OptionMatcher. The router will check handler parameter nullability.

    await Task.CompletedTask;
  }

  public static async Task Should_parse_short_option_flag()
  {
    // Arrange & Act
    CompiledRoute route = PatternParser.Parse("build -v");

    // Assert - Short form boolean flag
    route.ShouldNotBeNull();
    route.OptionMatchers.Count.ShouldBe(1);

    OptionMatcher vOption = route.OptionMatchers[0];
    vOption.MatchPattern.ShouldBe("-v");
    vOption.ExpectsValue.ShouldBeFalse();

    await Task.CompletedTask;
  }

  public static async Task Should_parse_short_option_with_value()
  {
    // Arrange & Act
    CompiledRoute route = PatternParser.Parse("build -c {mode}");

    // Assert - Short form with value parameter
    route.ShouldNotBeNull();
    route.OptionMatchers.Count.ShouldBe(1);

    OptionMatcher cOption = route.OptionMatchers[0];
    cOption.MatchPattern.ShouldBe("-c");
    cOption.ExpectsValue.ShouldBeTrue();
    cOption.ParameterName.ShouldBe("mode");

    await Task.CompletedTask;
  }

  public static async Task Should_parse_option_with_alias()
  {
    // Arrange & Act
    CompiledRoute route = PatternParser.Parse("build --verbose,-v");

    // Assert - Aliases stored as MatchPattern (primary/long) + AlternateForm (short)
    route.ShouldNotBeNull();
    route.OptionMatchers.Count.ShouldBe(1);

    OptionMatcher verboseOption = route.OptionMatchers[0];
    verboseOption.MatchPattern.ShouldBe("--verbose");
    verboseOption.AlternateForm.ShouldBe("-v");
    verboseOption.ExpectsValue.ShouldBeFalse();

    await Task.CompletedTask;
  }

  public static async Task Should_parse_option_alias_with_value()
  {
    // Arrange & Act
    CompiledRoute route = PatternParser.Parse("build --config,-c {mode}");

    // Assert - Aliased option with parameter
    route.ShouldNotBeNull();
    route.OptionMatchers.Count.ShouldBe(1);

    OptionMatcher configOption = route.OptionMatchers[0];
    configOption.MatchPattern.ShouldBe("--config");
    configOption.AlternateForm.ShouldBe("-c");
    configOption.ExpectsValue.ShouldBeTrue();
    configOption.ParameterName.ShouldBe("mode");

    await Task.CompletedTask;
  }

  public static async Task Should_parse_multiple_options()
  {
    // Arrange & Act
    CompiledRoute route = PatternParser.Parse("deploy --verbose --force --config {env}");

    // Assert - Multiple options: 2 boolean, 1 parameterized
    route.ShouldNotBeNull();
    route.OptionMatchers.Count.ShouldBe(3);

    OptionMatcher verboseOption = route.OptionMatchers[0];
    verboseOption.MatchPattern.ShouldBe("--verbose");
    verboseOption.ExpectsValue.ShouldBeFalse();

    OptionMatcher forceOption = route.OptionMatchers[1];
    forceOption.MatchPattern.ShouldBe("--force");
    forceOption.ExpectsValue.ShouldBeFalse();

    OptionMatcher configOption = route.OptionMatchers[2];
    configOption.MatchPattern.ShouldBe("--config");
    configOption.ExpectsValue.ShouldBeTrue();

    await Task.CompletedTask;
  }

  public static async Task Should_parse_typed_option_value()
  {
    // Arrange & Act
    CompiledRoute route = PatternParser.Parse("server --port {num:int}");

    // Assert - Option with typed parameter
    // Type information is resolved during parameter binding, not stored on OptionMatcher
    route.ShouldNotBeNull();
    route.OptionMatchers.Count.ShouldBe(1);

    OptionMatcher portOption = route.OptionMatchers[0];
    portOption.MatchPattern.ShouldBe("--port");
    portOption.ExpectsValue.ShouldBeTrue();
    portOption.ParameterName.ShouldBe("num");

    await Task.CompletedTask;
  }

  public static async Task Should_parse_repeated_option()
  {
    // Arrange & Act
    CompiledRoute route = PatternParser.Parse("docker --env {var}*");

    // Assert - Repeated option collects multiple values into array
    route.ShouldNotBeNull();
    route.OptionMatchers.Count.ShouldBe(1);

    OptionMatcher envOption = route.OptionMatchers[0];
    envOption.MatchPattern.ShouldBe("--env");
    envOption.ExpectsValue.ShouldBeTrue();
    envOption.ParameterName.ShouldBe("var");
    envOption.IsRepeated.ShouldBeTrue();

    await Task.CompletedTask;
  }

  public static async Task Should_parse_repeated_typed_option()
  {
    // Arrange & Act
    CompiledRoute route = PatternParser.Parse("server --port {num:int}*");

    // Assert - Repeated option with type constraint
    route.ShouldNotBeNull();
    route.OptionMatchers.Count.ShouldBe(1);

    OptionMatcher portOption = route.OptionMatchers[0];
    portOption.MatchPattern.ShouldBe("--port");
    portOption.ExpectsValue.ShouldBeTrue();
    portOption.ParameterName.ShouldBe("num");
    portOption.IsRepeated.ShouldBeTrue();

    await Task.CompletedTask;
  }

  public static async Task Should_parse_multiple_repeated_options()
  {
    // Arrange & Act
    CompiledRoute route = PatternParser.Parse("build --define {key}* --exclude {pattern}*");

    // Assert - Multiple repeated options in same route
    route.ShouldNotBeNull();
    route.OptionMatchers.Count.ShouldBe(2);

    OptionMatcher defineOption = route.OptionMatchers[0];
    defineOption.MatchPattern.ShouldBe("--define");
    defineOption.IsRepeated.ShouldBeTrue();

    OptionMatcher excludeOption = route.OptionMatchers[1];
    excludeOption.MatchPattern.ShouldBe("--exclude");
    excludeOption.IsRepeated.ShouldBeTrue();

    await Task.CompletedTask;
  }

  public static async Task Should_parse_optional_flag_with_alias_boolean()
  {
    // Arrange & Act - Optional boolean flag with alias
    // Pattern: --verbose,-v? (per optional-flag-alias-syntax.md)
    // The ? after alias applies to BOTH forms (--verbose and -v are both optional)
    CompiledRoute route = PatternParser.Parse("build --verbose,-v?");

    // Assert
    route.ShouldNotBeNull();
    route.OptionMatchers.Count.ShouldBe(1);

    OptionMatcher verboseOption = route.OptionMatchers[0];
    verboseOption.MatchPattern.ShouldBe("--verbose");
    verboseOption.AlternateForm.ShouldBe("-v");
    verboseOption.ExpectsValue.ShouldBeFalse(); // Boolean flag
    verboseOption.IsOptional.ShouldBeTrue(); // Flag is optional (? applies to both forms)

    await Task.CompletedTask;
  }

  public static async Task Should_parse_optional_flag_with_alias_and_value()
  {
    // Arrange & Act - Optional flag with alias and required value
    // Pattern: --output,-o? {file} (per optional-flag-alias-syntax.md)
    // The ? after alias makes flag optional, but value is required IF flag is present
    CompiledRoute route = PatternParser.Parse("backup {source} --output,-o? {file}");

    // Assert
    route.ShouldNotBeNull();
    route.OptionMatchers.Count.ShouldBe(1);

    OptionMatcher outputOption = route.OptionMatchers[0];
    outputOption.MatchPattern.ShouldBe("--output");
    outputOption.AlternateForm.ShouldBe("-o");
    outputOption.ExpectsValue.ShouldBeTrue(); // Has parameter
    outputOption.ParameterName.ShouldBe("file");
    outputOption.IsOptional.ShouldBeTrue(); // Flag is optional (? applies to both forms)

    await Task.CompletedTask;
  }

  public static async Task Should_parse_optional_flag_with_alias_and_optional_value()
  {
    // Arrange & Act - Optional flag with alias and optional value
    // Pattern: --config,-c? {mode?} (per optional-flag-alias-syntax.md)
    // The ? after alias makes flag optional, the ? in parameter makes value optional
    CompiledRoute route = PatternParser.Parse("build --config,-c? {mode?}");

    // Assert
    route.ShouldNotBeNull();
    route.OptionMatchers.Count.ShouldBe(1);

    OptionMatcher configOption = route.OptionMatchers[0];
    configOption.MatchPattern.ShouldBe("--config");
    configOption.AlternateForm.ShouldBe("-c");
    configOption.ExpectsValue.ShouldBeTrue(); // Has parameter
    configOption.ParameterName.ShouldBe("mode");
    configOption.IsOptional.ShouldBeTrue(); // Flag is optional (? applies to both forms)
    // Note: Parameter optionality ({mode?}) is checked during binding, not stored here

    await Task.CompletedTask;
  }
}

} // namespace TimeWarp.Nuru.Tests.Parser
