#!/usr/bin/env -S dotnet --

// Validate duplicate option alias detection (short and long forms).

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.Parser
{

[TestTag("Parser")]
public class DuplicateOptionAliasTests
{
  [ModuleInitializer]
  internal static void Register() => RegisterTests<DuplicateOptionAliasTests>();

  public static async Task Should_reject_duplicate_short_form_options()
  {
    // Arrange & Act
    PatternException exception = Should.Throw<PatternException>(() =>
      PatternParser.Parse("build -v -v")
    );

    // Assert
    exception.SemanticErrors.ShouldNotBeNull();
    exception.SemanticErrors.Count.ShouldBe(1);
    exception.SemanticErrors[0].ShouldBeOfType<DuplicateOptionAliasError>();

    DuplicateOptionAliasError error = (DuplicateOptionAliasError)exception.SemanticErrors[0];
    error.IsLongForm.ShouldBeFalse();
    error.Alias.ShouldBe("v");

    await Task.CompletedTask;
  }

  public static async Task Should_reject_duplicate_long_form_options()
  {
    // Arrange & Act
    PatternException exception = Should.Throw<PatternException>(() =>
      PatternParser.Parse("build --verbose --verbose")
    );

    // Assert
    exception.SemanticErrors.ShouldNotBeNull();
    exception.SemanticErrors.Count.ShouldBe(1);
    exception.SemanticErrors[0].ShouldBeOfType<DuplicateOptionAliasError>();

    DuplicateOptionAliasError error = (DuplicateOptionAliasError)exception.SemanticErrors[0];
    error.IsLongForm.ShouldBeTrue();
    error.Alias.ShouldBe("verbose");

    await Task.CompletedTask;
  }

  public static async Task Should_reject_duplicate_long_form_with_value_parameter()
  {
    // Arrange & Act
    PatternException exception = Should.Throw<PatternException>(() =>
      PatternParser.Parse("build --config {cfg} --config {cfg}")
    );

    // Assert - Duplicate option alias should be reported alongside duplicate parameter name
    exception.SemanticErrors.ShouldNotBeNull();

    List<DuplicateOptionAliasError> aliasErrors =
      [.. exception.SemanticErrors.OfType<DuplicateOptionAliasError>()];
    aliasErrors.ShouldNotBeEmpty();
    aliasErrors[0].IsLongForm.ShouldBeTrue();

    await Task.CompletedTask;
  }

  public static async Task Should_allow_distinct_short_and_long_forms()
  {
    // Arrange & Act
    CompiledRoute route = Should.NotThrow(() => PatternParser.Parse("build -v --verbose"));

    // Assert
    route.ShouldNotBeNull();

    await Task.CompletedTask;
  }

  public static async Task Should_allow_unique_long_forms()
  {
    // Arrange & Act
    CompiledRoute route = Should.NotThrow(() => PatternParser.Parse("build --verbose --quiet"));

    // Assert
    route.ShouldNotBeNull();

    await Task.CompletedTask;
  }

  public static async Task Should_reject_short_alias_colliding_across_options()
  {
    // Arrange & Act - two different long options declare the same -e alias via
    // the comma syntax; the short-form check must see through the alias form.
    Exception ex = Should.Throw<Exception>(() =>
      PatternParser.Parse("show --edit,-e --extract,-e")
    );

    // Assert
    ex.Message.ShouldContain("duplicate", Case.Insensitive);
    ex.Message.ShouldContain("e");

    await Task.CompletedTask;
  }

  public static async Task Should_reject_long_form_colliding_with_aliased_option()
  {
    // Arrange & Act - same long form, one bare and one carrying a short alias.
    Exception ex = Should.Throw<Exception>(() =>
      PatternParser.Parse("build --verbose,-v --verbose")
    );

    // Assert
    ex.Message.ShouldContain("duplicate", Case.Insensitive);
    ex.Message.ShouldContain("verbose");

    await Task.CompletedTask;
  }
}

} // namespace TimeWarp.Nuru.Tests.Parser
