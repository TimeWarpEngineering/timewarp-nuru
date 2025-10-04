/*
 * THIS FILE IS REDUNDANT AND SCHEDULED FOR DELETION
 * 
 * Reason for Deletion:
 * This console-script-style test duplicates coverage for description tokenization, now centralized in:
 * - lexer-12-description-tokenization.cs for pattern-level descriptions
 * - lexer-15-advanced-features.cs for element-level descriptions (e.g., {env|Environment}, --dry-run,-d|Preview)
 * The tests here (simple/complex patterns with internal pipes) are covered in Should_tokenize_parameter_with_description, Should_tokenize_option_with_description, and Should_tokenize_complex_pattern_with_descriptions.
 * After review, this file can be safely deleted to reduce redundancy and maintenance overhead.
 * 
 * Date Reviewed: 2025-10-04
 * Reviewer: Grok (AI Assistant)
 *
 * ANALYSIS BY CLAUDE (Roo): ✅ VERIFIED - Can be safely deleted
 * - Element-level descriptions ({env|Environment}) confirmed in lexer-15 lines 13-35
 * - Option descriptions (--dry-run,-d|Preview) confirmed in lexer-15 lines 41-67
 * - Pattern-level descriptions confirmed in lexer-12
 * - This was a debug/exploration script with no unique coverage
 */

// #!/usr/bin/dotnet --
// #:project ../../../Source/TimeWarp.Nuru/TimeWarp.Nuru.csproj
//
// #pragma warning disable CA1031 // Do not catch general exception types - OK for tests
//
// using TimeWarp.Nuru.Parsing;
// using static System.Console;
//
// WriteLine("Testing lexer with hanging pattern:");
//
// string pattern = "deploy {env|Environment} --dry-run,-d|Preview";
// WriteLine($"Pattern: {pattern}");
//
// // Test simpler patterns first
// TestLexer("deploy {env|Environment}");
// TestLexer("--dry-run,-d|Preview");
// TestLexer("deploy {env|Environment} --dry-run,-d|Preview");
//
// void TestLexer(string testPattern)
// {
//     try
//     {
//         WriteLine($"\nTesting: {testPattern}");
//         var lexer = new Lexer(testPattern);
//         IReadOnlyList<Token> tokens = lexer.Tokenize();
//         WriteLine($"Found {tokens.Count} tokens:");
//         foreach (Token token in tokens)
//         {
//             WriteLine($"  {token.Type}: '{token.Value}' at position {token.Position}");
//         }
//     }
//     catch (Exception ex)
//     {
//         WriteLine($"Error: {ex.Message}");
//     }
// }