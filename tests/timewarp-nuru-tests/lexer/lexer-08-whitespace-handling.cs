#!/usr/bin/dotnet --

#if !JARIBU_MULTI
return await RunAllTests();
#endif

[TestTag("Lexer")]
public class WhitespaceHandlingTests
{
  [ModuleInitializer]
  internal static void Register() => RegisterTests<WhitespaceHandlingTests>();

  // All whitespace variations should produce identical token sequences
  [Input("git log")]          // single space
  [Input("git   log")]         // multiple spaces
  [Input("git\tlog")]          // tab character
  [Input("  git log")]         // leading whitespace
  [Input("git log  ")]         // trailing whitespace
  public static async Task Should_produce_identical_tokens_regardless_of_whitespace(string pattern)
  {
    // Arrange
    Lexer lexer = CreateLexer(pattern);
    IReadOnlyList<Token> tokens = lexer.Tokenize();

    // Assert - whitespace quantity and type don't affect tokenization
    tokens.Count.ShouldBe(3);
    tokens[0].Type.ShouldBe(RouteTokenType.Identifier);
    tokens[0].Value.ShouldBe("git");
    tokens[1].Type.ShouldBe(RouteTokenType.Identifier);
    tokens[1].Value.ShouldBe("log");
    tokens[2].Type.ShouldBe(RouteTokenType.EndOfInput);

    await Task.CompletedTask;
  }

  public static async Task Should_handle_whitespace_around_special_characters()
  {
    // Arrange
    string pattern = "{ param }";
    Lexer lexer = CreateLexer(pattern);
    IReadOnlyList<Token> tokens = lexer.Tokenize();

    // Assert - whitespace separates special chars from identifiers
    tokens.Count.ShouldBe(4);
    tokens[0].Type.ShouldBe(RouteTokenType.LeftBrace);
    tokens[1].Type.ShouldBe(RouteTokenType.Identifier);
    tokens[1].Value.ShouldBe("param");
    tokens[2].Type.ShouldBe(RouteTokenType.RightBrace);
    tokens[3].Type.ShouldBe(RouteTokenType.EndOfInput);

    await Task.CompletedTask;
  }

  public static async Task Should_handle_no_whitespace_between_special_characters()
  {
    // Arrange
    string pattern = "{}";
    Lexer lexer = CreateLexer(pattern);
    IReadOnlyList<Token> tokens = lexer.Tokenize();

    // Assert - special chars are self-delimiting
    tokens.Count.ShouldBe(3);
    tokens[0].Type.ShouldBe(RouteTokenType.LeftBrace);
    tokens[1].Type.ShouldBe(RouteTokenType.RightBrace);
    tokens[2].Type.ShouldBe(RouteTokenType.EndOfInput);

    await Task.CompletedTask;
  }
}
