#region Purpose
// Best-effort cleanup of the previous Windows executable after a successful
// in-place replace (dev self-install leaves dev.exe.old; 470 M41).
#endregion
#region Design
// Windows allows renaming a running exe; publish then writes the new binary
// to the original path. The .old file is no longer needed after success and
// should be deleted when the OS will allow it. Delete failures (brief lock)
// are non-fatal — next self-install already deletes leftover .old as a
// pre-step. Kept free of NuruRoute/endpoint types so unit tests can compile
// this file without DiscoverEndpoints collisions.
#endregion

namespace DevCli;

using TimeWarp.Terminal;

#pragma warning disable CA1031 // Do not catch generally exception types — best-effort delete

/// <summary>
/// Helpers for Windows in-place executable replacement used by self-install.
/// </summary>
public static class WindowsExeReplace
{
  /// <summary>
  /// Best-effort delete of <paramref name="oldExe"/> after a successful
  /// Windows self-install. Returns <c>true</c> when the file is gone (or
  /// was already absent); <c>false</c> when delete failed (lock / IO).
  /// </summary>
  public static bool TryDeleteOldExecutable(string oldExe, ITerminal? terminal = null)
  {
    ArgumentNullException.ThrowIfNull(oldExe);

    if (!File.Exists(oldExe))
    {
      return true;
    }

    try
    {
      File.Delete(oldExe);
      return !File.Exists(oldExe);
    }
    catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
    {
      terminal?.WriteLine($"WARNING: Could not delete {oldExe}: {ex.Message}");
      return false;
    }
  }
}

#pragma warning restore CA1031
