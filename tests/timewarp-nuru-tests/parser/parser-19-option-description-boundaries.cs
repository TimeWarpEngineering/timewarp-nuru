#!/usr/bin/env -S dotnet --

// Option-description boundaries, parameter-name shape, and built-in type error text.

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.Parser
{

[TestTag("Parser")]
public class OptionDescriptionBoundaryTests
{
  [ModuleInitializer]
  internal static void Register() => RegisterTests<OptionDescriptionBoundaryTests>();

  private static readonly string[] BuiltInTypes =
  [
    "string",
    "int",
    "byte",
    "sbyte",
    "short",
    "ushort",
    "uint",
    "ulong",
    "long",
    "float",
    "double",
    "decimal",
    "bool",
    "char",
    "DateTime",
    "Guid",
    "TimeSpan",
    "uri",
    "Uri",
    "fileinfo",
    "FileInfo",
    "directoryinfo",
    "DirectoryInfo",
    "ipaddress",
    "IPAddress",
    "dateonly",
    "DateOnly",
    "timeonly",
    "TimeOnly"
  ];

  public static async Task Should_keep_end_of_options_after_option_description()
  {
    // Arrange & Act — "|desc" must not swallow the following "--"
    CompiledRoute route = PatternParser.Parse("run --flag|desc -- {*args}");

    // Assert
    route.HasCatchAll.ShouldBeTrue();
    route.CatchAllParameterName.ShouldBe("args");
    route.OptionMatchers.Count.ShouldBe(1);

    OptionMatcher flag = route.OptionMatchers[0];
    flag.MatchPattern.ShouldBe("--flag");
    flag.Description.ShouldBe("desc");
    flag.ExpectsValue.ShouldBeFalse();
    flag.ParameterName.ShouldBe("flag");

    await Task.CompletedTask;
  }

  public static async Task Should_keep_multi_word_description_and_end_of_options()
  {
    // Arrange & Act
    CompiledRoute route = PatternParser.Parse("run --flag|hello world -- {*args}");

    // Assert
    route.HasCatchAll.ShouldBeTrue();
    route.CatchAllParameterName.ShouldBe("args");

    OptionMatcher flag = route.OptionMatchers.Single();
    flag.Description.ShouldBe("hello world");
    flag.ExpectsValue.ShouldBeFalse();

    await Task.CompletedTask;
  }

  public static async Task Should_not_swallow_end_of_options_into_option_description()
  {
    // The corrupt parse treated this as success: description "a b" and {x} as --opt's value.
    bool parsed = PatternParser.TryParse
    (
      "cmd --opt | a -- b {x}",
      out CompiledRoute? route,
      out IReadOnlyList<ParseError>? parseErrors,
      out IReadOnlyList<SemanticError>? semanticErrors
    );

    parsed.ShouldBeFalse();
    route.ShouldBeNull();
    parseErrors.ShouldBeNull();
    semanticErrors.ShouldNotBeNull();
    semanticErrors.ShouldContain(error => error is InvalidEndOfOptionsSeparatorError);

    await Task.CompletedTask;
  }

  public static async Task Should_reject_hyphenated_parameter_names()
  {
    PatternException exception = Should.Throw<PatternException>(() =>
      PatternParser.Parse("run {my-param}")
    );

    exception.ParseErrors.ShouldNotBeNull();
    InvalidIdentifierError identifierError = exception.ParseErrors.OfType<InvalidIdentifierError>().Single();
    identifierError.InvalidIdentifier.ShouldBe("my-param");

    await Task.CompletedTask;
  }

  public static async Task Should_reject_hyphenated_option_value_parameter_names()
  {
    PatternException exception = Should.Throw<PatternException>(() =>
      PatternParser.Parse("run --opt {my-param}")
    );

    exception.ParseErrors.ShouldNotBeNull();
    exception.ParseErrors.OfType<InvalidIdentifierError>().Single().InvalidIdentifier.ShouldBe("my-param");

    await Task.CompletedTask;
  }

  public static async Task Should_allow_hyphenated_option_names()
  {
    CompiledRoute route = PatternParser.Parse("run --my-param {file}");

    route.OptionMatchers.Count.ShouldBe(1);
    OptionMatcher option = route.OptionMatchers[0];
    option.MatchPattern.ShouldBe("--my-param");
    option.ExpectsValue.ShouldBeTrue();
    option.ParameterName.ShouldBe("file");

    await Task.CompletedTask;
  }

  public static async Task Should_allow_identifier_shaped_parameter_names()
  {
    CompiledRoute route = PatternParser.Parse("run {_ok} {name_2}");

    ParameterMatcher first = (ParameterMatcher)route.PositionalMatchers[1];
    ParameterMatcher second = (ParameterMatcher)route.PositionalMatchers[2];
    first.Name.ShouldBe("_ok");
    second.Name.ShouldBe("name_2");

    await Task.CompletedTask;
  }

  public static async Task Should_list_every_built_in_type_on_invalid_constraint()
  {
    foreach (string typeName in BuiltInTypes)
    {
      Should.NotThrow(() => PatternParser.Parse($"use {{value:{typeName}}}"));
    }

    PatternException exception = Should.Throw<PatternException>(() =>
      PatternParser.Parse("use {value:not-a-type}")
    );

    exception.ParseErrors.ShouldNotBeNull();
    InvalidTypeConstraintError typeError = exception.ParseErrors.OfType<InvalidTypeConstraintError>().Single();
    string message = typeError.ToString();
    const string Marker = "Built-in types: ";
    int start = message.IndexOf(Marker, StringComparison.Ordinal);
    start.ShouldBeGreaterThanOrEqualTo(0);
    string remainder = message[(start + Marker.Length)..];
    int end = remainder.IndexOf('.', StringComparison.Ordinal);
    end.ShouldBeGreaterThan(0);
    string[] listed = remainder[..end].Split(", ");
    listed.ShouldBe(BuiltInTypes);

    await Task.CompletedTask;
  }
}

} // namespace TimeWarp.Nuru.Tests.Parser
