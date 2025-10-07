namespace TimeWarp.Kijaribu;

using System;
using System.IO;
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
  /// <summary>
  /// Logs a test pass status.
  /// </summary>
  public static void TestPassed() =>
    Console.WriteLine("  ✓ PASSED");

  /// <summary>
  /// Logs a test failure status with reason.
  /// </summary>
  public static void TestFailed(string reason) =>
    Console.WriteLine($"  ✗ FAILED: {reason}");

  /// <summary>
  /// Logs a test skipped status with reason.
  /// </summary>
  public static void TestSkipped(string reason) =>
    Console.WriteLine($"  ⚠ SKIPPED: {reason}");

  /// <summary>
  /// Clears the runfile cache entry(ies) for a specific file to ensure fresh compilation on the current run.
  /// Deletes top-level cache dirs prefixed with the filename (e.g., "kijaribu-05-cache-clearing-<hash>").
  /// </summary>
  /// <param name="filePath">Full path to the file (e.g., .cs script).</param>
  /// <param name="deleteAllPrefixed">If true, deletes all matching prefixed dirs (default: true, for completeness).</param>
  public static void ClearRunfileCache(string filePath, bool deleteAllPrefixed = true)
  {
    string runfileCacheRoot = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        ".local", "share", "dotnet", "runfile"
    );

    if (!Directory.Exists(runfileCacheRoot) || !File.Exists(filePath))
    {
        return;
    }

    string filePrefix = Path.GetFileNameWithoutExtension(filePath).ToLowerInvariant() + "-";
    bool clearedAny = false;

    foreach (string cacheDir in Directory.GetDirectories(runfileCacheRoot))
    {
        string cacheDirName = Path.GetFileName(cacheDir).ToLowerInvariant();
        if (cacheDirName.StartsWith(filePrefix, StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                Directory.Delete(cacheDir, recursive: true);
                Console.WriteLine($"✓ Cleared runfile cache for {Path.GetFileName(filePath)}: {Path.GetFileName(cacheDir)}");
                clearedAny = true;

                if (!deleteAllPrefixed)
                {
                    return; // Stop after first match (if not deleting all)
                }
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            {
                Console.WriteLine($"⚠ Skipped clearing {Path.GetFileName(cacheDir)} (locked/in use): {ex.Message}");
                // Continue to next; don't fail the whole op
            }
        }
    }

    if (!clearedAny)
    {
        Console.WriteLine($"⚠ No runfile cache prefixed with '{filePrefix}' found for {Path.GetFileName(filePath)}; proceeding.");
    }
  }

  /// <summary>
  /// Clears all runfile caches (broad fallback, as in original TestRunner).
  /// </summary>
  public static void ClearAllRunfileCaches()
  {
    string runfileCacheRoot = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        ".local", "share", "dotnet", "runfile"
    );

    if (!Directory.Exists(runfileCacheRoot))
    {
        return;
    }

    bool anyDeleted = false;
    foreach (string cacheDir in Directory.GetDirectories(runfileCacheRoot))
    {
        try
        {
            Directory.Delete(cacheDir, recursive: true);
            if (!anyDeleted)
            {
                Console.WriteLine("✓ Clearing all runfile caches:");
                anyDeleted = true;
            }

            Console.WriteLine($"  - {Path.GetFileName(cacheDir)}");
        }
        catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
        {
            Console.WriteLine($"  - Skipped {Path.GetFileName(cacheDir)} (locked): {ex.Message}");
        }
    }

    if (anyDeleted)
    {
        Console.WriteLine();
    }
  }

  [GeneratedRegex("([A-Z])")]
  private static partial Regex PascalCaseRegex();
}
