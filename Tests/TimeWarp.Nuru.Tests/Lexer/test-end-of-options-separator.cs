#!/usr/bin/dotnet --
#:project ../../../Source/TimeWarp.Nuru.Parsing/TimeWarp.Nuru.Parsing.csproj

using TimeWarp.Nuru.Parsing;
using static System.Console;

WriteLine("Testing Lexer Support for -- (End-of-Options) Separator:");
WriteLine("==========================================================");
WriteLine();

// Test that standalone -- is treated as a literal identifier, not DoubleDash
void TestPattern(string pattern, (TokenType type, string value)[] expected, string description)
{
  WriteLine($"Pattern: '{pattern}'");
  WriteLine($"Test: {description}");

  var lexer = new Lexer(pattern);
  IReadOnlyList<Token> tokens = lexer.Tokenize();

  // Remove EndOfInput for comparison
  var actualTokens = tokens.Take(tokens.Count - 1).ToList();

  if (actualTokens.Count != expected.Length)
  {
    WriteLine($"  ❌ FAILED: Expected {expected.Length} tokens, got {actualTokens.Count}");
    WriteLine("  Actual tokens:");
    foreach (Token token in actualTokens)
    {
      WriteLine($"    [{token.Type,-12}] '{token.Value}'");
    }

    return;
  }

  bool allMatch = true;
  for (int i = 0; i < expected.Length; i++)
  {
    Token actual = actualTokens[i];
    (TokenType type, string value) = expected[i];

    if (actual.Type != type || actual.Value != value)
    {
      WriteLine($"  ❌ Token {i}: Expected [{type,-12}] '{value}', got [{actual.Type,-12}] '{actual.Value}'");
      allMatch = false;
    }
    else
    {
      WriteLine($"  ✓ Token {i}: [{actual.Type,-12}] '{actual.Value}'");
    }
  }

  if (allMatch)
  {
    WriteLine("  ✅ PASSED");
  }
  else
  {
    WriteLine("  ❌ FAILED");
  }

  WriteLine();
}

// Test cases for -- separator
WriteLine("Testing standalone -- (should be EndOfOptions token):");
WriteLine("------------------------------------------------------");

TestPattern("--",
[
  (TokenType.EndOfOptions, "--")  // Standalone -- should be EndOfOptions
],
"Standalone -- should be EndOfOptions");

TestPattern("exec --",
[
  (TokenType.Identifier, "exec"),
  (TokenType.EndOfOptions, "--")  // -- at end should be EndOfOptions
],
"Command followed by --");

TestPattern("exec -- {*cmd}",
[
  (TokenType.Identifier, "exec"),
  (TokenType.EndOfOptions, "--"),  // -- before space should be EndOfOptions
  (TokenType.LeftBrace, "{"),
  (TokenType.Asterisk, "*"),
  (TokenType.Identifier, "cmd"),
  (TokenType.RightBrace, "}")
],
"-- separator with catch-all");

TestPattern("git log -- {*files}",
[
  (TokenType.Identifier, "git"),
  (TokenType.Identifier, "log"),
  (TokenType.EndOfOptions, "--"),  // -- before space should be EndOfOptions
  (TokenType.LeftBrace, "{"),
  (TokenType.Asterisk, "*"),
  (TokenType.Identifier, "files"),
  (TokenType.RightBrace, "}")
],
"Git log with -- separator");

WriteLine("Testing -- followed by option name (should remain DoubleDash):");
WriteLine("---------------------------------------------------------------");

TestPattern("--help",
[
  (TokenType.DoubleDash, "--"),
  (TokenType.Identifier, "help")
],
"--help should remain as option");

TestPattern("exec --env {e}",
[
  (TokenType.Identifier, "exec"),
  (TokenType.DoubleDash, "--"),
  (TokenType.Identifier, "env"),
  (TokenType.LeftBrace, "{"),
  (TokenType.Identifier, "e"),
  (TokenType.RightBrace, "}")
],
"--env should remain as option");

WriteLine("Testing complex patterns with both -- separator and options:");
WriteLine("--------------------------------------------------------------");

TestPattern("exec --env {e}* -- {*cmd}",
[
  (TokenType.Identifier, "exec"),
  (TokenType.DoubleDash, "--"),  // This is an option
  (TokenType.Identifier, "env"),
  (TokenType.LeftBrace, "{"),
  (TokenType.Identifier, "e"),
  (TokenType.RightBrace, "}"),
  (TokenType.Asterisk, "*"),
  (TokenType.EndOfOptions, "--"),  // This should be EndOfOptions separator
  (TokenType.LeftBrace, "{"),
  (TokenType.Asterisk, "*"),
  (TokenType.Identifier, "cmd"),
  (TokenType.RightBrace, "}")
],
"Option then -- separator");

WriteLine("========================================");
WriteLine("The lexer distinguishes between:");
WriteLine("1. Standalone -- (EndOfOptions token)");
WriteLine("2. -- followed by text (DoubleDash token for options)");