#!/usr/bin/dotnet --
#:project ../../../Source/TimeWarp.Nuru/TimeWarp.Nuru.csproj

using TimeWarp.Nuru.Parsing;
using static System.Console;

WriteLine("Testing Compound Identifier Tokenization:");
WriteLine("==========================================");
WriteLine();
WriteLine("Verifying lexer handles identifiers with dashes correctly");
WriteLine();

// Test helper
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
        WriteLine("  Actual tokens:");
        foreach (Token token in actualTokens)
        {
            WriteLine($"    [{token.Type}] '{token.Value}'");
        }

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

WriteLine("Basic Compound Identifiers (as literals):");
WriteLine("------------------------------------------");

// Simple compound identifiers should be single tokens
ExpectTokens("async-test",
    (TokenType.Identifier, "async-test"));

ExpectTokens("no-edit",
    (TokenType.Identifier, "no-edit"));

ExpectTokens("my-long-command-name",
    (TokenType.Identifier, "my-long-command-name"));

ExpectTokens("test-case-1",
    (TokenType.Identifier, "test-case-1"));

WriteLine();
WriteLine("Compound Identifiers in Options:");
WriteLine("---------------------------------");

// Options with dashes in their names
ExpectTokens("--no-edit",
    (TokenType.DoubleDash, "--"),
    (TokenType.Identifier, "no-edit"));

ExpectTokens("--dry-run",
    (TokenType.DoubleDash, "--"),
    (TokenType.Identifier, "dry-run"));

ExpectTokens("--max-count",
    (TokenType.DoubleDash, "--"),
    (TokenType.Identifier, "max-count"));

ExpectTokens("--save-dev",
    (TokenType.DoubleDash, "--"),
    (TokenType.Identifier, "save-dev"));

ExpectTokens("--enhance-logs",
    (TokenType.DoubleDash, "--"),
    (TokenType.Identifier, "enhance-logs"));

WriteLine();
WriteLine("Edge Cases with Dashes:");
WriteLine("------------------------");

// Trailing dash
ExpectTokens("test-",
    (TokenType.Identifier, "test-"));

// Leading dash (should be treated as single dash option)
ExpectTokens("-test",
    (TokenType.SingleDash, "-"),
    (TokenType.Identifier, "test"));

// Just a dash
ExpectTokens("-",
    (TokenType.SingleDash, "-"));

// Multiple consecutive dashes in identifier
ExpectTokens("test--case",
    (TokenType.Identifier, "test--case"));

WriteLine();
WriteLine("Complex Patterns with Compound Identifiers:");
WriteLine("--------------------------------------------");

ExpectTokens("git commit --no-edit",
    (TokenType.Identifier, "git"),
    (TokenType.Identifier, "commit"),
    (TokenType.DoubleDash, "--"),
    (TokenType.Identifier, "no-edit"));

ExpectTokens("docker run --save-dev {image}",
    (TokenType.Identifier, "docker"),
    (TokenType.Identifier, "run"),
    (TokenType.DoubleDash, "--"),
    (TokenType.Identifier, "save-dev"),
    (TokenType.LeftBrace, "{"),
    (TokenType.Identifier, "image"),
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

ExpectTokens("npm install {package} --save-dev",
    (TokenType.Identifier, "npm"),
    (TokenType.Identifier, "install"),
    (TokenType.LeftBrace, "{"),
    (TokenType.Identifier, "package"),
    (TokenType.RightBrace, "}"),
    (TokenType.DoubleDash, "--"),
    (TokenType.Identifier, "save-dev"));

WriteLine();
WriteLine("========================================");
WriteLine("Summary:");
WriteLine("Compound identifiers (with dashes) are correctly tokenized as:");
WriteLine("1. Single Identifier tokens when standalone");
WriteLine("2. Identifier tokens after DoubleDash when used as options");
WriteLine("3. Edge cases like trailing/leading dashes handled appropriately");
WriteLine("========================================");