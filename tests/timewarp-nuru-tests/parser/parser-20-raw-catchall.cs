#!/usr/bin/env -S dotnet --

#region Purpose
// Pins the raw catch-all marker {**args}: it parses as a catch-all that is also raw,
// and it cannot share the route with other parameters, options, '--', or a type constraint.
#endregion

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.Parser
{

[TestTag("Parser")]
public class RawCatchAllTests
{
  [ModuleInitializer]
  internal static void Register() => RegisterTests<RawCatchAllTests>();

  public static async Task Should_parse_raw_catchall_as_catch_all_and_raw()
  {
    CompiledRoute route = PatternParser.Parse("{**args}");

    route.HasCatchAll.ShouldBeTrue();
    route.CatchAllParameterName.ShouldBe("args");
    route.IsRawCatchAll.ShouldBeTrue();
    route.Specificity.ShouldBe(PatternParser.Parse("{*args}").Specificity);

    ParameterMatcher matcher = (ParameterMatcher)route.PositionalMatchers[0];
    matcher.IsCatchAll.ShouldBeTrue();
    matcher.IsRaw.ShouldBeTrue();
    matcher.ToDisplayString().ShouldBe("{**args}");

    await Task.CompletedTask;
  }

  public static async Task Should_parse_raw_catchall_after_literals_with_description()
  {
    CompiledRoute route = PatternParser.Parse("tail {**args|the rest}");

    route.PositionalMatchers.Count.ShouldBe(2);
    LiteralMatcher literal = (LiteralMatcher)route.PositionalMatchers[0];
    literal.Value.ShouldBe("tail");

    ParameterMatcher matcher = (ParameterMatcher)route.PositionalMatchers[1];
    matcher.Name.ShouldBe("args");
    matcher.IsRaw.ShouldBeTrue();
    matcher.Description.ShouldBe("the rest");
    matcher.Constraint.ShouldBeNull();

    await Task.CompletedTask;
  }

  public static async Task Should_leave_star_catchall_not_raw()
  {
    CompiledRoute route = PatternParser.Parse("exec {*args}");

    route.IsRawCatchAll.ShouldBeFalse();
    ParameterMatcher matcher = (ParameterMatcher)route.PositionalMatchers[1];
    matcher.IsCatchAll.ShouldBeTrue();
    matcher.IsRaw.ShouldBeFalse();
    matcher.ToDisplayString().ShouldBe("{*args}");

    await Task.CompletedTask;
  }

  public static async Task Should_reject_raw_catchall_with_another_parameter()
  {
    await AssertRawNotAlone("cmd {file} {**args}");
  }

  public static async Task Should_reject_raw_catchall_with_an_option()
  {
    await AssertRawNotAlone("cmd {**args} --verbose");
  }

  public static async Task Should_reject_raw_catchall_after_end_of_options()
  {
    await AssertRawNotAlone("cmd -- {**args}");
  }

  public static async Task Should_reject_raw_catchall_with_a_type_constraint()
  {
    await AssertRawNotAlone("{**args:int}");
    await AssertRawNotAlone("{**args:string}");
  }

  public static async Task Should_reject_optional_raw_catchall_as_a_modifier()
  {
    PatternException exception = Should.Throw<PatternException>(() =>
      PatternParser.Parse("{**args?}")
    );

    exception.ParseErrors.ShouldNotBeNull();
    exception.ParseErrors!.ShouldContain(error => error is InvalidModifierCombinationError);

    await Task.CompletedTask;
  }

  private static Task AssertRawNotAlone(string pattern)
  {
    PatternException exception = Should.Throw<PatternException>(() =>
      PatternParser.Parse(pattern)
    );

    exception.SemanticErrors.ShouldNotBeNull();
    exception.SemanticErrors!.ShouldContain(error => error is RawCatchAllNotAloneError);
    return Task.CompletedTask;
  }
}
}
