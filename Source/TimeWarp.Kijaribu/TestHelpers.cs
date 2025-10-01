namespace TimeWarp.Kijaribu;

using System.Text.RegularExpressions;

/// <summary>
/// Helper utilities for test formatting and common patterns.
/// </summary>
public static partial class TestHelpers
{
  /// <summary>
  /// Converts PascalCase test method names to readable format.
  /// Example: "CatchAllInOptionShouldFail" → "Catch All In Option Should Fail"
  /// </summary>
  public static string FormatTestName(string name) =>
    PascalCaseRegex().Replace(name, " $1").Trim();

  /// <summary>
  /// Logs a test pass with formatted output.
  /// </summary>
  public static void TestPassed(string testName) =>
    Console.WriteLine($"✅ {FormatTestName(testName)}");

  /// <summary>
  /// Logs a test failure with formatted output and reason.
  /// </summary>
  public static void TestFailed(string testName, string reason) =>
    Console.WriteLine($"❌ {FormatTestName(testName)}: {reason}");

  [GeneratedRegex("([A-Z])")]
  private static partial Regex PascalCaseRegex();
}
