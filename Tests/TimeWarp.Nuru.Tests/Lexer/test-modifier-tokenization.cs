/*
 * THIS FILE IS REDUNDANT AND SCHEDULED FOR DELETION
 * 
 * Reason for Deletion:
 * This console-script-style test duplicates coverage for modifier tokenization (? and * on flags/options), now centralized in lexer-15-advanced-features.cs.
 * Tests for optional flags (e.g., --verbose?, -v?), repeated params ({var}*), combined (e.g., --env? {var}*), complex CLI (deploy --force? --dry-run?) are covered in Should_tokenize_optional_verbose_flag, Should_tokenize_repeated_parameter, etc.
 * The structured equivalent is in test-modifier-tokenization-kijaribu.cs (commented/moved).
 * After review, this file can be safely deleted to reduce redundancy and maintenance overhead.
 * 
 * Date Reviewed: 2025-10-04
 * Reviewer: Grok (AI Assistant)
 *
 * ANALYSIS BY CLAUDE (Roo): ✅ VERIFIED - Can be safely deleted
 * - All optional flag tests (--verbose?, -v?) confirmed in lexer-15 lines 115-179
 * - All repeated parameter tests ({var}*, {p:int}*) confirmed in lexer-15 lines 245-343
 * - All combined modifier tests confirmed in lexer-15 lines 348-432
 * - All complex pattern tests confirmed in lexer-15 lines 437-510
 * - No unique test coverage in this file
 */

// #!/usr/bin/dotnet --
// #:project ../../../Source/TimeWarp.Nuru/TimeWarp.Nuru.csproj
//
// #pragma warning disable CA1031 // Do not catch general exception types - OK for tests
//
// using TimeWarp.Nuru.Parsing;
// using static System.Console;
// using System.Collections.Generic;
// using System.Linq;
//
// WriteLine
// (
//   """
//   Testing Lexer Support for Optional/Repeated Modifiers
//   ======================================================
//   Verifying lexer can tokenize ? and * in new positions
//   """
// );
//
// // Test helper
// void TestTokenization(string pattern, string expectedTokens)
// {
//     Write($"  {pattern,-35} ");
//     try
//     {
//         Lexer lexer = new(pattern);
//         IReadOnlyList<Token> tokens = lexer.Tokenize();
//
//         string tokenString = string.Join(" ", tokens
//             .Where(t => t.Type != TokenType.EndOfInput)
//             .Select(t => $"{t.Type}:{t.Value}"));
//
//         if (tokenString == expectedTokens)
//         {
//             WriteLine($"✓ {expectedTokens}");
//         }
//         else
//         {
//             WriteLine($"✗ Got: {tokenString}");
//         }
//     }
//     catch (Exception ex)
//     {
//         WriteLine($"✗ ERROR: {ex.Message}");
//     }
// }
//
// WriteLine
// (
//   """
//
//   Optional Flag Modifiers:
//   """
// );
//
// TestTokenization("--verbose?", "DoubleDash:-- Identifier:verbose Question:?");
// TestTokenization("--dry-run?", "DoubleDash:-- Identifier:dry-run Question:?");
// TestTokenization("-v?", "SingleDash:- Identifier:v Question:?");
// TestTokenization("--config? {mode}", "DoubleDash:-- Identifier:config Question:? LeftBrace:{ Identifier:mode RightBrace:}");
// TestTokenization("--env? {name?}", "DoubleDash:-- Identifier:env Question:? LeftBrace:{ Identifier:name Question:? RightBrace:}");
// 
// WriteLine
// (
//   """
//
//   Repeated Parameter Modifiers:
//   """
// );
//
// TestTokenization("--env {var}*", "DoubleDash:-- Identifier:env LeftBrace:{ Identifier:var RightBrace:} Asterisk:*");
// TestTokenization("--port {p:int}*", "DoubleDash:-- Identifier:port LeftBrace:{ Identifier:p Colon:: Identifier:int RightBrace:} Asterisk:*");
// TestTokenization("--label {l}* --tag {t}*", "DoubleDash:-- Identifier:label LeftBrace:{ Identifier:l RightBrace:} Asterisk:* DoubleDash:-- Identifier:tag LeftBrace:{ Identifier:t RightBrace:} Asterisk:*");
//
// WriteLine
// (
//   """
//
//   Combined Modifiers:
//   """
// );
//
// TestTokenization("--env? {var}*", "DoubleDash:-- Identifier:env Question:? LeftBrace:{ Identifier:var RightBrace:} Asterisk:*");
// TestTokenization("--opt? {val?}*", "DoubleDash:-- Identifier:opt Question:? LeftBrace:{ Identifier:val Question:? RightBrace:} Asterisk:*");
// TestTokenization("--flag?*", "DoubleDash:-- Identifier:flag Question:? Asterisk:*");
//
// WriteLine
// (
//   """
//
//   Complex Patterns:
//   """
// );
//
// TestTokenization("deploy {env} --force? --dry-run?", "Identifier:deploy LeftBrace:{ Identifier:env RightBrace:} DoubleDash:-- Identifier:force Question:? DoubleDash:-- Identifier:dry-run Question:?");
// TestTokenization("docker --env? {e}* {*cmd}", "Identifier:docker DoubleDash:-- Identifier:env Question:? LeftBrace:{ Identifier:e RightBrace:} Asterisk:* LeftBrace:{ Asterisk:* Identifier:cmd RightBrace:}");
//
// WriteLine
// (
//   """
//
//   ========================================
//   Summary:
//   The lexer already tokenizes ? and * correctly.
//   These tokens can appear after options and parameters.
//   The parser needs to interpret them in these new contexts.
//   """
// );