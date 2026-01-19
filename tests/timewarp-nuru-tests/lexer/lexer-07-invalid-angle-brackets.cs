#!/usr/bin/dotnet --

#if !JARIBU_MULTI
return await RunAllTests();
#endif

[TestTag("Lexer")]
public class InvalidAngleBracketsTests
{
  [ModuleInitializer]
  internal static void Register() => RegisterTests<InvalidAngleBracketsTests>();

  public static async Task Should_reject_angle_brackets_after_identifier()
  {
    // Arrange
    string pattern = "test<param>";
    Lexer lexer = CreateLexer(pattern);
    IReadOnlyList<Token> tokens = lexer.Tokenize();

    // Assert
    tokens.Count.ShouldBe(3);
    tokens[0].Type.ShouldBe(RouteTokenType.Identifier);
    tokens[0].Value.ShouldBe("test");
    tokens[1].Type.ShouldBe(RouteTokenType.Invalid);
    tokens[2].Type.ShouldBe(RouteTokenType.EndOfInput);

    await Task.CompletedTask;
  }

  public static async Task Should_reject_angle_brackets_at_start()
  {
    // Arrange
    string pattern = "<param>";
    Lexer lexer = CreateLexer(pattern);
    IReadOnlyList<Token> tokens = lexer.Tokenize();

    // Assert
    tokens.Count.ShouldBe(2);
    tokens[0].Type.ShouldBe(RouteTokenType.Invalid);
    tokens[1].Type.ShouldBe(RouteTokenType.EndOfInput);

    await Task.CompletedTask;
  }

  public static async Task Should_accept_curly_braces()
  {
    // Arrange
    string pattern = "{param}";
    Lexer lexer = CreateLexer(pattern);
    IReadOnlyList<Token> tokens = lexer.Tokenize();

    // Assert - positive test: ensure valid syntax still works
    tokens.Count.ShouldBe(4);
    tokens[0].Type.ShouldBe(RouteTokenType.LeftBrace);
    tokens[1].Type.ShouldBe(RouteTokenType.Identifier);
    tokens[1].Value.ShouldBe("param");
    tokens[2].Type.ShouldBe(RouteTokenType.RightBrace);
    tokens[3].Type.ShouldBe(RouteTokenType.EndOfInput);

    await Task.CompletedTask;
  }

  public static async Task Should_reject_mixed_bracket_syntax()
  {
    // Arrange
    string pattern = "{param>";
    Lexer lexer = CreateLexer(pattern);
    IReadOnlyList<Token> tokens = lexer.Tokenize();

    // Assert
    tokens.Count.ShouldBe(4);
    tokens[0].Type.ShouldBe(RouteTokenType.LeftBrace);
    tokens[1].Type.ShouldBe(RouteTokenType.Identifier);
    tokens[1].Value.ShouldBe("param");
    tokens[2].Type.ShouldBe(RouteTokenType.Invalid);
    tokens[3].Type.ShouldBe(RouteTokenType.EndOfInput);

    await Task.CompletedTask;
  }
}
