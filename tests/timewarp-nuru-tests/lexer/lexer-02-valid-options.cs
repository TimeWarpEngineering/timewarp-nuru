#!/usr/bin/dotnet --

#if !JARIBU_MULTI
return await RunAllTests();
#endif

[TestTag("Lexer")]
public class ValidOptionsTests
{
  [ModuleInitializer]
  internal static void Register() => RegisterTests<ValidOptionsTests>();

  // Long options (with compound identifiers)
  [Input("--dry-run", "dry-run")]
  [Input("--no-edit", "no-edit")]
  [Input("--save-dev", "save-dev")]
  [Input("--v", "v")]
  public static async Task Should_tokenize_long_options(string pattern, string expectedOption)
  {
    // Arrange
    Lexer lexer = CreateLexer(pattern);
    IReadOnlyList<Token> tokens = lexer.Tokenize();

    // Assert
    tokens.Count.ShouldBe(3); // DoubleDash + Identifier + EndOfInput
    tokens[0].Type.ShouldBe(RouteTokenType.DoubleDash);
    tokens[0].Value.ShouldBe("--");
    tokens[1].Type.ShouldBe(RouteTokenType.Identifier);
    tokens[1].Value.ShouldBe(expectedOption);
    tokens[2].Type.ShouldBe(RouteTokenType.EndOfInput);

    await Task.CompletedTask;
  }

  // Short options (single character)
  [Input("-h", "h")]
  [Input("-v", "v")]
  [Input("-x", "x")]
  public static async Task Should_tokenize_short_options(string pattern, string expectedOption)
  {
    // Arrange
    Lexer lexer = CreateLexer(pattern);
    IReadOnlyList<Token> tokens = lexer.Tokenize();

    // Assert
    tokens.Count.ShouldBe(3); // SingleDash + Identifier + EndOfInput
    tokens[0].Type.ShouldBe(RouteTokenType.SingleDash);
    tokens[0].Value.ShouldBe("-");
    tokens[1].Type.ShouldBe(RouteTokenType.Identifier);
    tokens[1].Value.ShouldBe(expectedOption);
    tokens[2].Type.ShouldBe(RouteTokenType.EndOfInput);

    await Task.CompletedTask;
  }
}
