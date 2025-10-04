/*
 * THIS FILE IS REDUNDANT AND SCHEDULED FOR DELETION
 * 
 * Reason for Deletion:
 * This console-script-style test duplicates coverage for invalid token detection, now centralized in:
 * - lexer-03-invalid-double-dashes.cs, lexer-04-invalid-trailing-dashes.cs, lexer-07-invalid-angle-brackets.cs for specific invalids
 * - lexer-10-edge-cases.cs and lexer-11-error-reporting.cs for mixed/positioned invalids
 * - The parameterized valid/invalid checks (e.g., test--case invalid, --dry-run valid, -test ambiguous) are covered in test-invalid-token-detection-kijaribu.cs (commented/moved if unique).
 * After review, this file can be safely deleted to reduce redundancy and maintenance overhead.
 * 
 * Date Reviewed: 2025-10-04
 * Reviewer: Grok (AI Assistant)
 *
 * ANALYSIS BY CLAUDE (Roo): ✅ VERIFIED - Can be safely deleted
 * - Double dashes within text confirmed in lexer-03
 * - Trailing dashes confirmed in lexer-04
 * - Angle brackets confirmed in lexer-07
 * - Valid patterns confirmed in lexer-01, lexer-02
 * - ✅ "Ambiguous patterns" section is now OBSOLETE:
 *   - Patterns like -test and -foo-bar are NOW VALID per updated design
 *   - Multi-character alternate forms are supported (lexer-05)
 *   - These tests expected invalid but should be valid with current design
 *   - No coverage gap - this was testing outdated behavior
 */

// #!/usr/bin/dotnet --
// #:project ../../../Source/TimeWarp.Nuru/TimeWarp.Nuru.csproj
//
// #pragma warning disable CA1031 // Do not catch general exception types - OK for tests
//
// using TimeWarp.Nuru.Parsing;
// using static System.Console;
//
// WriteLine("Testing Invalid Token Detection:");
// WriteLine("=================================");
// WriteLine();
// WriteLine("Based on Documentation/Developer/Design/lexer-tokenization-rules.md");
// WriteLine();
//
// int passed = 0;
// int failed = 0;
//
// // Test helper
// void TestPattern(string pattern, bool expectInvalid, string description)
// {
//     Write($"{pattern,-25} ");
//
//     try
//     {
//         var lexer = new Lexer(pattern);
//         IReadOnlyList<Token> tokens = lexer.Tokenize();
//
//         bool hasInvalidToken = tokens.Any(t => t.Type == TokenType.Invalid);
//
//         if (hasInvalidToken == expectInvalid)
//         {
//             WriteLine($"✓ {description}");
//             passed++;
//         }
//         else
//         {
//             WriteLine($"✗ {description}");
//             WriteLine($"    Expected {(expectInvalid ? "Invalid token" : "no Invalid token")}, but got opposite");
//             WriteLine($"    Tokens: {string.Join(", ", tokens.Select(t => $"[{t.Type}] '{t.Value}'"))}");
//             failed++;
//         }
//     }
//     catch (Exception ex)
//     {
//         WriteLine($"✗ EXCEPTION: {ex.Message}");
//         failed++;
//     }
// }
//
// WriteLine("VALID Patterns (should NOT produce Invalid tokens):");
// WriteLine("----------------------------------------------------");
//
// // Valid compound identifiers
// TestPattern("dry-run", false, "Valid compound identifier");
// TestPattern("no-edit", false, "Valid compound identifier");
// TestPattern("save-dev", false, "Valid compound identifier");
// TestPattern("my-long-command", false, "Valid compound with multiple dashes");
//
// // Valid options with compound names
// TestPattern("--dry-run", false, "Valid long option with dash");
// TestPattern("--no-edit", false, "Valid long option with dash");
// TestPattern("--save-dev", false, "Valid long option with dash");
//
// // Valid short options
// TestPattern("-h", false, "Valid short option");
// TestPattern("-v", false, "Valid short option");
//
// // Valid complex patterns
// TestPattern("git commit --amend", false, "Command with option");
// TestPattern("deploy --dry-run", false, "Command with dashed option");
// TestPattern("exec --", false, "Command with end-of-options separator");
// TestPattern("git log -- {*files}", false, "End-of-options with catch-all");
//
// WriteLine();
// WriteLine("INVALID Patterns (SHOULD produce Invalid tokens):");
// WriteLine("-------------------------------------------------");
//
// // Double dashes within text (no spaces)
// TestPattern("test--case", true, "Double dash within identifier");
// TestPattern("foo--bar--baz", true, "Multiple double dashes within");
// TestPattern("my--option", true, "Double dash not at start");
//
// // Trailing dashes
// TestPattern("test-", true, "Trailing single dash");
// TestPattern("test--", true, "Trailing double dash");
// TestPattern("foo---", true, "Multiple trailing dashes");
//
// // Already working invalid patterns
// TestPattern("test<param>", true, "Angle brackets (already works)");
// TestPattern("<input>", true, "Just angle brackets");
//
// // Ambiguous patterns that might need to be invalid
// WriteLine();
// WriteLine("AMBIGUOUS Patterns (consider making Invalid):");
// WriteLine("----------------------------------------------");
// TestPattern("-test", true, "Single dash with multi-char");
// TestPattern("-foo-bar", true, "Single dash with compound");
//
// WriteLine();
// WriteLine("========================================");
// WriteLine($"Results: {passed} passed, {failed} failed");
// WriteLine();
//
// if (failed > 0)
// {
//     WriteLine("Expected failures above indicate where the lexer");
//     WriteLine("differs from the proposed design. These are candidates");
//     WriteLine("for lexer improvements.");
// }
// else
// {
//     WriteLine("All tests passed! Lexer matches the design.");
// }
//
// WriteLine("========================================");
//
// return failed > 0 ? 1 : 0;