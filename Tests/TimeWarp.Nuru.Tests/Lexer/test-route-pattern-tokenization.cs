#!/usr/bin/dotnet --
#:project ../../../Source/TimeWarp.Nuru/TimeWarp.Nuru.csproj

#pragma warning disable CA1031 // Do not catch general exception types - OK for tests

using TimeWarp.Nuru.Parsing;
using static System.Console;

WriteLine("Testing Lexer:");
WriteLine();

// Test cases for the lexer
(string pattern, string description)[] testCases =
[
    // Basic literals
    ("status", "Single literal"),
    ("git status", "Two literals"),
    ("git commit push", "Three literals"),

    // Compound identifiers with dashes
    ("async-test", "Literal with dash"),
    ("no-edit", "Another literal with dash"),
    ("my-long-command-name", "Multiple dashes in literal"),

    // Parameters
    ("{name}", "Simple parameter"),
    ("{name:string}", "Parameter with type"),
    ("{count:int}", "Parameter with int type"),
    ("{tag?}", "Optional parameter"),
    ("{seconds:int?}", "Optional parameter with type"),
    ("{*args}", "Catch-all parameter"),
    ("{name|Description}", "Parameter with description"),
    ("{count:int|Number of items}", "Parameter with type and description"),

    // Options
    ("--help", "Long option"),
    ("-h", "Short option"),
    ("--no-edit", "Long option with dash"),
    ("--max-count", "Another long option with dash"),

    // Complex patterns
    ("git commit --amend", "Command with option"),
    ("git commit --amend --no-edit", "Command with two options"),
    ("git commit -m {message}", "Command with short option and parameter"),
    ("git commit --message {message}", "Command with long option and parameter"),
    ("git log --max-count {count:int}", "Option with dash and typed parameter"),

    // Failing patterns from test suite
    ("docker run --enhance-logs {image}", "Docker with enhanced logs option"),
    ("kubectl apply -f {file}", "kubectl with short option and parameter"),
    ("npm install {package} --save-dev", "npm with parameter then option"),
    ("git commit -m {message} --amend", "Short option with value then boolean option"),
    ("git commit --amend -m {message}", "Boolean option then short option with value"),
    ("git commit --amend --message {message}", "Two long options"),
    ("git commit --message {message} --amend", "Long option with value then boolean option"),

    // Mixed patterns
    ("deploy {env} --dry-run", "Positional parameter and option"),
    ("deploy {env} --version {ver}", "Multiple parameters with option"),
    ("kubectl get {resource} --watch --enhanced", "Multiple options"),

    // Edge cases
    ("", "Empty string"),
    ("   ", "Only spaces"),
    ("--", "Just double dash"),
    ("-", "Just single dash"),
    ("test-", "Trailing dash"),
    ("-test", "Leading dash (should be option)"),

    // Special characters that might confuse the lexer
    ("test<input>", "Angle brackets (invalid parameter syntax)"),
    ("test{param}test", "Parameter in middle of text"),
    ("--option={value}", "Option with equals (not standard pattern)"),
];

int passed = 0;
int failed = 0;

foreach ((string pattern, string description) in testCases)
{
  WriteLine($"Test: {description}");
  WriteLine($"Pattern: '{pattern}'");

  try
  {
    var lexer = new Lexer(pattern);
    IReadOnlyList<Token> tokens = lexer.Tokenize();

    WriteLine($"Tokens ({tokens.Count}):");
    foreach (Token token in tokens)
    {
      WriteLine($"  [{token.Type,-12}] '{token.Value}' at position {token.Position} (length {token.Length})");
    }

    // Verify last token is always EndOfInput
    Token lastToken = tokens[tokens.Count - 1];
    if (lastToken.Type != TokenType.EndOfInput)
    {
      WriteLine($"  ❌ ERROR: Last token should be EndOfInput, but was {lastToken.Type}");
      failed++;
    }
    else
    {
      WriteLine("  ✓ Success");
      passed++;
    }
  }
  catch (Exception ex)
  {
    WriteLine($"  ❌ EXCEPTION: {ex.Message}");
    failed++;
  }

  WriteLine();
}

WriteLine($"Summary: {passed} passed, {failed} failed out of {testCases.Length} tests");

// Now let's test specific expectations
WriteLine("\n" + new string('=', 50));
WriteLine("Testing specific tokenization expectations:");
WriteLine(new string('=', 50) + "\n");

void ExpectTokens(string pattern, params (TokenType type, string value)[] expectedTokens)
{
  WriteLine($"Pattern: '{pattern}'");
  var lexer = new Lexer(pattern);
  IReadOnlyList<Token> tokens = lexer.Tokenize();

  // Remove EndOfInput for comparison
  var actualTokens = tokens.Take(tokens.Count - 1).ToList();

  if (actualTokens.Count != expectedTokens.Length)
  {
    WriteLine($"  ❌ Expected {expectedTokens.Length} tokens, got {actualTokens.Count}");
    return;
  }

  bool allMatch = true;
  for (int i = 0; i < expectedTokens.Length; i++)
  {
    Token actual = actualTokens[i];
    (TokenType type, string value) = expectedTokens[i];

    if (actual.Type != type || actual.Value != value)
    {
      WriteLine($"  ❌ Token {i}: Expected [{type}] '{value}', got [{actual.Type}] '{actual.Value}'");
      allMatch = false;
    }
  }

  if (allMatch)
  {
    WriteLine("  ✓ All tokens match expected");
  }
}

// Critical test cases
ExpectTokens("async-test",
    (TokenType.Identifier, "async-test"));

ExpectTokens("git commit --no-edit",
    (TokenType.Identifier, "git"),
    (TokenType.Identifier, "commit"),
    (TokenType.DoubleDash, "--"),
    (TokenType.Identifier, "no-edit"));

ExpectTokens("git commit -m {message}",
    (TokenType.Identifier, "git"),
    (TokenType.Identifier, "commit"),
    (TokenType.SingleDash, "-"),
    (TokenType.Identifier, "m"),
    (TokenType.LeftBrace, "{"),
    (TokenType.Identifier, "message"),
    (TokenType.RightBrace, "}"));

ExpectTokens("git log --max-count {count:int}",
    (TokenType.Identifier, "git"),
    (TokenType.Identifier, "log"),
    (TokenType.DoubleDash, "--"),
    (TokenType.Identifier, "max-count"),
    (TokenType.LeftBrace, "{"),
    (TokenType.Identifier, "count"),
    (TokenType.Colon, ":"),
    (TokenType.Identifier, "int"),
    (TokenType.RightBrace, "}"));

// Test the failing patterns
ExpectTokens("docker run --enhance-logs {image}",
    (TokenType.Identifier, "docker"),
    (TokenType.Identifier, "run"),
    (TokenType.DoubleDash, "--"),
    (TokenType.Identifier, "enhance-logs"),
    (TokenType.LeftBrace, "{"),
    (TokenType.Identifier, "image"),
    (TokenType.RightBrace, "}"));

ExpectTokens("kubectl apply -f {file}",
    (TokenType.Identifier, "kubectl"),
    (TokenType.Identifier, "apply"),
    (TokenType.SingleDash, "-"),
    (TokenType.Identifier, "f"),
    (TokenType.LeftBrace, "{"),
    (TokenType.Identifier, "file"),
    (TokenType.RightBrace, "}"));

ExpectTokens("npm install {package} --save-dev",
    (TokenType.Identifier, "npm"),
    (TokenType.Identifier, "install"),
    (TokenType.LeftBrace, "{"),
    (TokenType.Identifier, "package"),
    (TokenType.RightBrace, "}"),
    (TokenType.DoubleDash, "--"),
    (TokenType.Identifier, "save-dev"));

ExpectTokens("git commit -m {message} --amend",
    (TokenType.Identifier, "git"),
    (TokenType.Identifier, "commit"),
    (TokenType.SingleDash, "-"),
    (TokenType.Identifier, "m"),
    (TokenType.LeftBrace, "{"),
    (TokenType.Identifier, "message"),
    (TokenType.RightBrace, "}"),
    (TokenType.DoubleDash, "--"),
    (TokenType.Identifier, "amend"));

return failed == 0 ? 0 : 1;