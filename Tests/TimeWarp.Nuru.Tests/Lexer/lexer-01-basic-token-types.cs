#!/usr/bin/dotnet --

return await RunTests<BasicTokenTypesTests>();

[TestTag("Lexer")]
[ClearRunfileCache]
public class BasicTokenTypesTests
{
  // Plain identifiers
  [Input("status")]
  [Input("version")]
  [Input("help")]
  [Input("build")]
  public static async Task Should_tokenize_plain_identifiers(string pattern)
    => await TokenizeSingleIdentifier(pattern);

  // Compound identifiers with dashes
  [Input("dry-run")]
  [Input("no-edit")]
  [Input("save-dev")]
  [Input("my-long-command-name")]
  [Input("this-is-a-long-name")]
  public static async Task Should_tokenize_compound_identifiers(string pattern)
    => await TokenizeSingleIdentifier(pattern);

  // Special characters
  [Input("{")]
  public static async Task Should_tokenize_left_brace(string pattern)
  {
    // Arrange
    Lexer lexer = CreateLexer(pattern);
    IReadOnlyList<Token> tokens = lexer.Tokenize();

    // Assert
    tokens.Count.ShouldBe(2);
    tokens[0].Type.ShouldBe(TokenType.LeftBrace);
    tokens[0].Value.ShouldBe("{");
    tokens[1].Type.ShouldBe(TokenType.EndOfInput);

    await Task.CompletedTask;
  }

  [Input("}")]
  public static async Task Should_tokenize_right_brace(string pattern)
  {
    // Arrange
    Lexer lexer = CreateLexer(pattern);
    IReadOnlyList<Token> tokens = lexer.Tokenize();

    // Assert
    tokens.Count.ShouldBe(2);
    tokens[0].Type.ShouldBe(TokenType.RightBrace);
    tokens[0].Value.ShouldBe("}");
    tokens[1].Type.ShouldBe(TokenType.EndOfInput);

    await Task.CompletedTask;
  }

  [Input(":")]
  public static async Task Should_tokenize_colon(string pattern)
  {
    // Arrange
    Lexer lexer = CreateLexer(pattern);
    IReadOnlyList<Token> tokens = lexer.Tokenize();

    // Assert
    tokens.Count.ShouldBe(2);
    tokens[0].Type.ShouldBe(TokenType.Colon);
    tokens[0].Value.ShouldBe(":");
    tokens[1].Type.ShouldBe(TokenType.EndOfInput);

    await Task.CompletedTask;
  }

  [Input("?")]
  public static async Task Should_tokenize_question(string pattern)
  {
    // Arrange
    Lexer lexer = CreateLexer(pattern);
    IReadOnlyList<Token> tokens = lexer.Tokenize();

    // Assert
    tokens.Count.ShouldBe(2);
    tokens[0].Type.ShouldBe(TokenType.Question);
    tokens[0].Value.ShouldBe("?");
    tokens[1].Type.ShouldBe(TokenType.EndOfInput);

    await Task.CompletedTask;
  }

  [Input("*")]
  public static async Task Should_tokenize_asterisk(string pattern)
  {
    // Arrange
    Lexer lexer = CreateLexer(pattern);
    IReadOnlyList<Token> tokens = lexer.Tokenize();

    // Assert
    tokens.Count.ShouldBe(2);
    tokens[0].Type.ShouldBe(TokenType.Asterisk);
    tokens[0].Value.ShouldBe("*");
    tokens[1].Type.ShouldBe(TokenType.EndOfInput);

    await Task.CompletedTask;
  }

  [Input("|")]
  public static async Task Should_tokenize_pipe(string pattern)
  {
    // Arrange
    Lexer lexer = CreateLexer(pattern);
    IReadOnlyList<Token> tokens = lexer.Tokenize();

    // Assert
    tokens.Count.ShouldBe(2);
    tokens[0].Type.ShouldBe(TokenType.Pipe);
    tokens[0].Value.ShouldBe("|");
    tokens[1].Type.ShouldBe(TokenType.EndOfInput);

    await Task.CompletedTask;
  }

  [Input(",")]
  public static async Task Should_tokenize_comma(string pattern)
  {
    // Arrange
    Lexer lexer = CreateLexer(pattern);
    IReadOnlyList<Token> tokens = lexer.Tokenize();

    // Assert
    tokens.Count.ShouldBe(2);
    tokens[0].Type.ShouldBe(TokenType.Comma);
    tokens[0].Value.ShouldBe(",");
    tokens[1].Type.ShouldBe(TokenType.EndOfInput);

    await Task.CompletedTask;
  }

  [Input("--option")]
  public static async Task Should_tokenize_double_dash(string pattern)
  {
    // Arrange
    Lexer lexer = CreateLexer(pattern);
    IReadOnlyList<Token> tokens = lexer.Tokenize();

    // Assert
    tokens.Count.ShouldBe(3);
    tokens[0].Type.ShouldBe(TokenType.DoubleDash);
    tokens[0].Value.ShouldBe("--");
    tokens[1].Type.ShouldBe(TokenType.Identifier);
    tokens[1].Value.ShouldBe("option");
    tokens[2].Type.ShouldBe(TokenType.EndOfInput);

    await Task.CompletedTask;
  }

  [Input("-h")]
  public static async Task Should_tokenize_single_dash(string pattern)
  {
    // Arrange
    Lexer lexer = CreateLexer(pattern);
    IReadOnlyList<Token> tokens = lexer.Tokenize();

    // Assert
    tokens.Count.ShouldBe(3);
    tokens[0].Type.ShouldBe(TokenType.SingleDash);
    tokens[0].Value.ShouldBe("-");
    tokens[1].Type.ShouldBe(TokenType.Identifier);
    tokens[1].Value.ShouldBe("h");
    tokens[2].Type.ShouldBe(TokenType.EndOfInput);

    await Task.CompletedTask;
  }

  [Input("--")]
  public static async Task Should_tokenize_end_of_options(string pattern)
  {
    // Arrange
    Lexer lexer = CreateLexer(pattern);
    IReadOnlyList<Token> tokens = lexer.Tokenize();

    // Assert
    tokens.Count.ShouldBe(2);
    tokens[0].Type.ShouldBe(TokenType.EndOfOptions);
    tokens[0].Value.ShouldBe("--");
    tokens[1].Type.ShouldBe(TokenType.EndOfInput);

    await Task.CompletedTask;
  }

  [Input("greet {name} | Say hello")]
  public static async Task Should_tokenize_description(string pattern)
  {
    // Arrange
    Lexer lexer = CreateLexer(pattern);
    IReadOnlyList<Token> tokens = lexer.Tokenize();

    // Assert
    // Lexer continues normal tokenization after pipe - parser handles description
    tokens.Count.ShouldBe(8);
    tokens[0].Type.ShouldBe(TokenType.Identifier);
    tokens[0].Value.ShouldBe("greet");
    tokens[1].Type.ShouldBe(TokenType.LeftBrace);
    tokens[2].Type.ShouldBe(TokenType.Identifier);
    tokens[2].Value.ShouldBe("name");
    tokens[3].Type.ShouldBe(TokenType.RightBrace);
    tokens[4].Type.ShouldBe(TokenType.Pipe);
    tokens[5].Type.ShouldBe(TokenType.Identifier);
    tokens[5].Value.ShouldBe("Say");
    tokens[6].Type.ShouldBe(TokenType.Identifier);
    tokens[6].Value.ShouldBe("hello");
    tokens[7].Type.ShouldBe(TokenType.EndOfInput);

    await Task.CompletedTask;
  }

  [Input("")]
  public static async Task Should_tokenize_end_of_input(string pattern)
  {
    // Arrange
    Lexer lexer = CreateLexer(pattern);
    IReadOnlyList<Token> tokens = lexer.Tokenize();

    // Assert
    tokens.Count.ShouldBe(1);
    tokens[0].Type.ShouldBe(TokenType.EndOfInput);
    tokens[0].Value.ShouldBe("");

    await Task.CompletedTask;
  }

  private static async Task TokenizeSingleIdentifier(string pattern)
  {
    // Arrange
    Lexer lexer = CreateLexer(pattern);
    IReadOnlyList<Token> tokens = lexer.Tokenize();

    // Assert
    tokens.Count.ShouldBe(2); // identifier + EndOfInput
    tokens[0].Type.ShouldBe(TokenType.Identifier);
    tokens[0].Value.ShouldBe(pattern);
    tokens[1].Type.ShouldBe(TokenType.EndOfInput);

    await Task.CompletedTask;
  }

}

