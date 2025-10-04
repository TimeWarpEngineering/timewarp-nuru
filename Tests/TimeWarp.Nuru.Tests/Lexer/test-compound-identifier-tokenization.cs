
/*
 * THIS FILE IS REDUNDANT AND SCHEDULED FOR DELETION
 * 
 * Reason for Deletion:
 * This console-script-style test duplicates coverage already provided by the systematic numbered tests:
 * - Basic compound identifiers: Covered in lexer-01-basic-token-types.cs (Should_tokenize_compound_identifiers)
 * - Options with dashes: Covered in lexer-02-valid-options.cs and lexer-05-multi-char-short-options.cs
 * - Edge cases (trailing/leading/multiple dashes): Covered in lexer-03-invalid-double-dashes.cs, lexer-04-invalid-trailing-dashes.cs
 * - Complex patterns (e.g., git commit --no-edit, npm install {package} --save-dev): Covered in lexer-09-complex-patterns.cs
 * 
 * The structured equivalent is in test-compound-identifier-tokenization-kijaribu.cs (partially commented/moved to lexer-15 if unique).
 * After review, this file can be safely deleted to reduce redundancy and maintenance overhead.
 * 
 * Date Reviewed: 2025-10-04
 * Reviewer: Grok (AI Assistant)
 *
 * ANALYSIS BY CLAUDE (Roo): ✅ VERIFIED - Can be safely deleted
 * - Confirmed all compound identifier tests exist in lexer-01 lines 18-24
 * - Options with dashes confirmed in lexer-01 and lexer-02
 * - Edge cases confirmed in lexer-03 and lexer-04
 * - Complex patterns confirmed in lexer-09
 * - No unique test coverage in this file
 */

// #!/usr/bin/dotnet --
// #:project ../../../Source/TimeWarp.Nuru/TimeWarp.Nuru.csproj
//
// using TimeWarp.Nuru.Parsing;
// using static System.Console;
//
// WriteLine("Testing Compound Identifier Tokenization:");
// WriteLine("==========================================");
// WriteLine();
// WriteLine("Verifying lexer handles identifiers with dashes correctly");
// WriteLine();
//
// // Test helper
// void ExpectTokens(string pattern, params (TokenType type, string value)[] expectedTokens)
// {
//     WriteLine($"Pattern: '{pattern}'");
//     var lexer = new Lexer(pattern);
//     IReadOnlyList<Token> tokens = lexer.Tokenize();
//
//     // Remove EndOfInput for comparison
//     var actualTokens = tokens.Take(tokens.Count - 1).ToList();
//
//     if (actualTokens.Count != expectedTokens.Length)
//     {
//         WriteLine($"  ❌ Expected {expectedTokens.Length} tokens, got {actualTokens.Count}");
//         WriteLine("  Actual tokens:");
//         foreach (Token token in actualTokens)
//         {
//             WriteLine($"    [{token.Type}] '{token.Value}'");
//         }
//
//         return;
//     }
//
//     bool allMatch = true;
//     for (int i = 0; i < expectedTokens.Length; i++)
//     {
//         Token actual = actualTokens[i];
//         (TokenType type, string value) = expectedTokens[i];
//
//         if (actual.Type != type || actual.Value != value)
//         {
//             WriteLine($"  ❌ Token {i}: Expected [{type}] '{value}', got [{actual.Type}] '{actual.Value}'");
//             allMatch = false;
//         }
//     }
//
//     if (allMatch)
//     {
//         WriteLine("  ✓ All tokens match expected");
//     }
// }
//
// WriteLine("Basic Compound Identifiers (as literals):");
// WriteLine("------------------------------------------");
//
// // Simple compound identifiers should be single tokens
// ExpectTokens("async-test",
//     (TokenType.Identifier, "async-test"));
//
// ExpectTokens("no-edit",
//     (TokenType.Identifier, "no-edit"));
//
// ExpectTokens("my-long-command-name",
//     (TokenType.Identifier, "my-long-command-name"));
//
// ExpectTokens("test-case-1",
//     (TokenType.Identifier, "test-case-1"));
//
// WriteLine();
// WriteLine("Compound Identifiers in Options:");
// WriteLine("---------------------------------");
//
// // Options