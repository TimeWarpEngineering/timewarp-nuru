#!/usr/bin/dotnet --

#if !JARIBU_MULTI
return await RunAllTests();
#endif

[TestTag("Lexer")]
public class InvalidDoubleDashesTests
{
  [ModuleInitializer]
  internal static void Register() => RegisterTests<InvalidDoubleDashesTests>();

  // Double dashes embedded within identifiers are invalid
  [Input("test--case")]
  [Input("foo--bar--baz")]
  [Input("my--option")]
  [Input("a--b")]
  public static async Task Should_reject_double_dashes_within_identifiers(string pattern)
  {
    // Arrange
    Lexer lexer = CreateLexer(pattern);
    IReadOnlyList<Token> tokens = lexer.Tokenize();

    // Assert
    tokens.Count.ShouldBe(2); // Invalid + EndOfInput
    tokens[0].Type.ShouldBe(RouteTokenType.Invalid);
    tokens[0].Value.ShouldBe(pattern);
    tokens[1].Type.ShouldBe(RouteTokenType.EndOfInput);

    await Task.CompletedTask;
  }
}
