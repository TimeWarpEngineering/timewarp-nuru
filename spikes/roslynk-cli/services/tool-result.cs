#region Purpose
// Turns a Roslynk tool outline into the process exit code.
#endregion

#region Design
// Roslynk reports failure as an error= header, not an exception. The body can mention the
// same letters inside a diagnostic message, so only header lines count.
#endregion

namespace RoslynkNuru.Services;

/// <summary>
/// Reads a Roslynk outline and sets the process exit code when the headers report an error.
/// </summary>
internal static class ToolResult
{
  public static string Finish(string result)
  {
    Environment.ExitCode = HasErrorHeader(result) ? 1 : 0;
    return result;
  }

  private static bool HasErrorHeader(string result)
  {
    int blank = result.IndexOf("\n\n", StringComparison.Ordinal);
    string headers = blank >= 0 ? result[..blank] : result;
    foreach (string line in headers.Split('\n'))
    {
      if (line.StartsWith("error=", StringComparison.Ordinal))
      {
        return true;
      }
    }

    return false;
  }
}
