#!/usr/bin/dotnet --

#if !JARIBU_MULTI
return await RunAllTests();
#endif

[TestTag("Lexer")]
public class DescriptionTokenizationTests
{
  [ModuleInitializer]
  internal static void Register() => RegisterTests<DescriptionTokenizationTests>();

  /// <summary>
  /// Test 1: Simple description after pipe character
  /// Validates lexer continues normal tokenization after pipe
  /// Description extraction is parser's responsibility, not lexer's
  /// </summary>
  public static async Task Should_tokenize_simple_description()
  {
    string pattern = "command | help text";
    Lexer lexer = CreateLexer(pattern);
    IReadOnlyList<Token> tokens = lexer.Tokenize();

    // Lexer treats description as normal identifiers after pipe
    tokens.Count.ShouldBe(5);
    tokens[0].Type.ShouldBe(RouteTokenType.Identifier);
    tokens[0].Value.ShouldBe("command");
    tokens[1].Type.ShouldBe(RouteTokenType.Pipe);
    tokens[1].Value.ShouldBe("|");
    tokens[2].Type.ShouldBe(RouteTokenType.Identifier);
    tokens[2].Value.ShouldBe("help");
    tokens[3].Type.ShouldBe(RouteTokenType.Identifier);
    tokens[3].Value.ShouldBe("text");
    tokens[4].Type.ShouldBe(RouteTokenType.EndOfInput);

    await Task.CompletedTask;
  }

  /// <summary>
  /// Test 2: Description containing option-like special characters
  /// Validates that text after pipe is tokenized normally (including --)
  /// Parser will handle extracting description, lexer just produces tokens
  /// </summary>
  public static async Task Should_tokenize_description_with_special_chars()
  {
    string pattern = "cmd | use --force carefully";
    Lexer lexer = CreateLexer(pattern);
    IReadOnlyList<Token> tokens = lexer.Tokenize();

    // Special characters in description are tokenized normally
    tokens.Count.ShouldBe(7);
    tokens[0].Type.ShouldBe(RouteTokenType.Identifier);
    tokens[0].Value.ShouldBe("cmd");
    tokens[1].Type.ShouldBe(RouteTokenType.Pipe);
    tokens[2].Type.ShouldBe(RouteTokenType.Identifier);
    tokens[2].Value.ShouldBe("use");
    tokens[3].Type.ShouldBe(RouteTokenType.DoubleDash);  // -- tokenized normally
    tokens[3].Value.ShouldBe("--");
    tokens[4].Type.ShouldBe(RouteTokenType.Identifier);
    tokens[4].Value.ShouldBe("force");
    tokens[5].Type.ShouldBe(RouteTokenType.Identifier);
    tokens[5].Value.ShouldBe("carefully");
    tokens[6].Type.ShouldBe(RouteTokenType.EndOfInput);

    await Task.CompletedTask;
  }

  /// <summary>
  /// Test 3: Description at end of complex pattern with parameters and options
  /// Validates pipe works correctly after other route elements
  /// Real-world pattern: "deploy {env} --dry-run | Deploy to environment"
  /// </summary>
  public static async Task Should_tokenize_description_at_end_of_complex_pattern()
  {
    string pattern = "deploy {env} --dry-run | Deploy to environment";
    Lexer lexer = CreateLexer(pattern);
    IReadOnlyList<Token> tokens = lexer.Tokenize();

    // All tokens before pipe, then pipe, then description tokens
    tokens.Count.ShouldBe(11);

    // Before pipe: deploy {env} --dry-run
    tokens[0].Type.ShouldBe(RouteTokenType.Identifier);
    tokens[0].Value.ShouldBe("deploy");
    tokens[1].Type.ShouldBe(RouteTokenType.LeftBrace);
    tokens[2].Type.ShouldBe(RouteTokenType.Identifier);
    tokens[2].Value.ShouldBe("env");
    tokens[3].Type.ShouldBe(RouteTokenType.RightBrace);
    tokens[4].Type.ShouldBe(RouteTokenType.DoubleDash);
    tokens[5].Type.ShouldBe(RouteTokenType.Identifier);
    tokens[5].Value.ShouldBe("dry-run");

    // Pipe separator
    tokens[6].Type.ShouldBe(RouteTokenType.Pipe);

    // After pipe: Deploy to environment
    tokens[7].Type.ShouldBe(RouteTokenType.Identifier);
    tokens[7].Value.ShouldBe("Deploy");
    tokens[8].Type.ShouldBe(RouteTokenType.Identifier);
    tokens[8].Value.ShouldBe("to");
    tokens[9].Type.ShouldBe(RouteTokenType.Identifier);
    tokens[9].Value.ShouldBe("environment");
    tokens[10].Type.ShouldBe(RouteTokenType.EndOfInput);

    await Task.CompletedTask;
  }

  /// <summary>
  /// Test 4: Multiple pipe characters in pattern
  /// Validates lexer tokenizes all pipes as Pipe tokens
  /// Parser determines which pipe is the description separator (likely first at pattern level)
  /// </summary>
  public static async Task Should_tokenize_multiple_pipes()
  {
    // Pattern with pipe in parameter AND at pattern level
    string pattern = "cmd {a|param desc} | pattern desc";
    Lexer lexer = CreateLexer(pattern);
    IReadOnlyList<Token> tokens = lexer.Tokenize();

    // Both pipes should be tokenized as Pipe tokens
    Token[] pipeTokens = [.. tokens.Where(t => t.Type == RouteTokenType.Pipe)];
    pipeTokens.Length.ShouldBe(2, "Should find two pipe tokens");

    // First pipe is inside parameter (after 'a')
    tokens[3].Type.ShouldBe(RouteTokenType.Pipe);
    tokens[3].Position.ShouldBe(6);  // Position of first |

    // Second pipe is at pattern level (after '}')
    tokens[7].Type.ShouldBe(RouteTokenType.Pipe);
    tokens[7].Position.ShouldBe(19);  // Position of second |

    await Task.CompletedTask;
  }

  /// <summary>
  /// Test 5: Empty description after pipe (pipe at end of pattern)
  /// Edge case: pipe with nothing following it
  /// </summary>
  public static async Task Should_tokenize_empty_description_after_pipe()
  {
    string pattern = "command |";
    Lexer lexer = CreateLexer(pattern);
    IReadOnlyList<Token> tokens = lexer.Tokenize();

    // Pipe followed immediately by EndOfInput
    tokens.Count.ShouldBe(3);
    tokens[0].Type.ShouldBe(RouteTokenType.Identifier);
    tokens[0].Value.ShouldBe("command");
    tokens[1].Type.ShouldBe(RouteTokenType.Pipe);
    tokens[1].Value.ShouldBe("|");
    tokens[2].Type.ShouldBe(RouteTokenType.EndOfInput);

    await Task.CompletedTask;
  }

  /// <summary>
  /// Test 6: Description containing brace characters
  /// Validates braces in description text are tokenized as normal tokens
  /// Description text can reference parameter syntax like {syntax}
  /// </summary>
  public static async Task Should_tokenize_description_with_braces()
  {
    string pattern = "cmd | use {syntax} here";
    Lexer lexer = CreateLexer(pattern);
    IReadOnlyList<Token> tokens = lexer.Tokenize();

    // Braces in description are tokenized normally
    tokens.Count.ShouldBe(8);
    tokens[0].Type.ShouldBe(RouteTokenType.Identifier);
    tokens[0].Value.ShouldBe("cmd");
    tokens[1].Type.ShouldBe(RouteTokenType.Pipe);

    // Description tokens: use {syntax} here
    tokens[2].Type.ShouldBe(RouteTokenType.Identifier);
    tokens[2].Value.ShouldBe("use");
    tokens[3].Type.ShouldBe(RouteTokenType.LeftBrace);
    tokens[4].Type.ShouldBe(RouteTokenType.Identifier);
    tokens[4].Value.ShouldBe("syntax");
    tokens[5].Type.ShouldBe(RouteTokenType.RightBrace);
    tokens[6].Type.ShouldBe(RouteTokenType.Identifier);
    tokens[6].Value.ShouldBe("here");
    tokens[7].Type.ShouldBe(RouteTokenType.EndOfInput);

    await Task.CompletedTask;
  }

  /// <summary>
  /// Test 7: Trailing whitespace in description
  /// Validates whitespace handling is consistent with rest of lexer
  /// Whitespace should be trimmed/ignored as token separator
  /// </summary>
  public static async Task Should_handle_trailing_whitespace_in_description()
  {
    // Pattern with trailing spaces after description text
    string pattern = "cmd | text   ";
    Lexer lexer = CreateLexer(pattern);
    IReadOnlyList<Token> tokens = lexer.Tokenize();

    // Trailing whitespace should not create additional tokens
    tokens.Count.ShouldBe(4);
    tokens[0].Type.ShouldBe(RouteTokenType.Identifier);
    tokens[0].Value.ShouldBe("cmd");
    tokens[1].Type.ShouldBe(RouteTokenType.Pipe);
    tokens[2].Type.ShouldBe(RouteTokenType.Identifier);
    tokens[2].Value.ShouldBe("text");
    tokens[3].Type.ShouldBe(RouteTokenType.EndOfInput);

    // Verify last token before EndOfInput is the identifier, not whitespace
    tokens[tokens.Count - 2].Type.ShouldBe(RouteTokenType.Identifier);

    await Task.CompletedTask;
  }
}
