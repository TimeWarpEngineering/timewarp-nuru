namespace TimeWarp.Nuru;

#pragma warning disable CA1031 // Do not catch general exception types - clipboard access can fail for many OS-specific reasons
#pragma warning disable RCS1075 // Avoid empty catch clause - clipboard failures are intentionally swallowed

/// <summary>
/// Platform-specific clipboard operations for the REPL console reader.
/// </summary>
/// <remarks>
/// Provides cross-platform clipboard access using native tools:
/// <list type="bullet">
/// <item>Windows: PowerShell Get-Clipboard/Set-Clipboard</item>
/// <item>macOS: pbpaste/pbcopy</item>
/// <item>Linux: pwsh, powershell.exe (WSL), xclip, xsel, wl-clipboard</item>
/// </list>
/// Tool availability is cached on first use to avoid repeated process spawning.
/// </remarks>
public sealed partial class ReplConsoleReader
{
  /// <summary>
  /// Gets text from the system clipboard.
  /// </summary>
  /// <returns>The clipboard text, or null if unavailable.</returns>
  private static string? GetClipboardText()
  {
    // Platform-independent clipboard access
    // For now, return null - clipboard integration is platform-specific
    // Future: Use TextCopy or similar cross-platform library
    try
    {
      // Try xclip/xsel on Linux, pbpaste on macOS, or PowerShell on Windows
      if (OperatingSystem.IsWindows())
      {
        return GetWindowsClipboard();
      }
      else if (OperatingSystem.IsMacOS())
      {
        return GetMacOSClipboard();
      }
      else if (OperatingSystem.IsLinux())
      {
        return GetLinuxClipboard();
      }
    }
    catch (Exception)
    {
      // Ignore clipboard errors - clipboard access may fail in headless environments
    }

    return null;
  }

  /// <summary>
  /// Sets text to the system clipboard.
  /// </summary>
  /// <param name="text">The text to copy to clipboard.</param>
  private static void SetClipboardText(string text)
  {
    if (string.IsNullOrEmpty(text))
      return;

    try
    {
      if (OperatingSystem.IsWindows())
      {
        SetWindowsClipboard(text);
      }
      else if (OperatingSystem.IsMacOS())
      {
        SetMacOSClipboard(text);
      }
      else if (OperatingSystem.IsLinux())
      {
        SetLinuxClipboard(text);
      }
    }
    catch (Exception)
    {
      // Ignore clipboard errors - clipboard access may fail in headless environments
    }
  }

  // ============================================================================
  // Windows Clipboard
  // ============================================================================

  private static string? GetWindowsClipboard()
  {
    using System.Diagnostics.Process process = new()
    {
      StartInfo = new System.Diagnostics.ProcessStartInfo
      {
        FileName = "powershell",
        Arguments = "-command \"Get-Clipboard\"",
        RedirectStandardOutput = true,
        UseShellExecute = false,
        CreateNoWindow = true
      }
    };
    process.Start();
    string result = process.StandardOutput.ReadToEnd().TrimEnd('\r', '\n');
    process.WaitForExit();
    return result;
  }

  private static void SetWindowsClipboard(string text)
  {
    using System.Diagnostics.Process process = new()
    {
      StartInfo = new System.Diagnostics.ProcessStartInfo
      {
        FileName = "powershell",
        Arguments = $"-command \"Set-Clipboard -Value '{text.Replace("'", "''", StringComparison.Ordinal)}'\"",
        UseShellExecute = false,
        CreateNoWindow = true
      }
    };
    process.Start();
    process.WaitForExit();
  }

  // ============================================================================
  // macOS Clipboard
  // ============================================================================

  private static string? GetMacOSClipboard()
  {
    using System.Diagnostics.Process process = new()
    {
      StartInfo = new System.Diagnostics.ProcessStartInfo
      {
        FileName = "pbpaste",
        RedirectStandardOutput = true,
        UseShellExecute = false,
        CreateNoWindow = true
      }
    };
    process.Start();
    string result = process.StandardOutput.ReadToEnd();
    process.WaitForExit();
    return result;
  }

  private static void SetMacOSClipboard(string text)
  {
    using System.Diagnostics.Process process = new()
    {
      StartInfo = new System.Diagnostics.ProcessStartInfo
      {
        FileName = "pbcopy",
        RedirectStandardInput = true,
        UseShellExecute = false,
        CreateNoWindow = true
      }
    };
    process.Start();
    process.StandardInput.Write(text);
    process.StandardInput.Close();
    process.WaitForExit();
  }

  // ============================================================================
  // Linux Clipboard
  // ============================================================================

  private static string? GetLinuxClipboard()
  {
    // Try methods in order of preference:
    // 1. pwsh (PowerShell Core - cross-platform)
    // 2. powershell.exe (WSL → Windows clipboard)
    // 3. xclip (X11)
    // 4. xsel (X11 fallback)

    // Try pwsh (PowerShell Core)
    if (ClipboardToolCache.HasPwsh)
    {
      try
      {
        using System.Diagnostics.Process process = new()
        {
          StartInfo = new System.Diagnostics.ProcessStartInfo
          {
            FileName = "pwsh",
            Arguments = "-NoProfile -Command \"Get-Clipboard\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
          }
        };
        process.Start();
        string result = process.StandardOutput.ReadToEnd().TrimEnd('\r', '\n');
        process.WaitForExit();
        if (process.ExitCode == 0 && !string.IsNullOrEmpty(result))
          return result;
      }
      catch (Exception)
      {
        // pwsh failed, continue to next method
      }
    }

    // Try powershell.exe (WSL → Windows clipboard)
    if (ClipboardToolCache.HasPowershellExe)
    {
      try
      {
        using System.Diagnostics.Process process = new()
        {
          StartInfo = new System.Diagnostics.ProcessStartInfo
          {
            FileName = "powershell.exe",
            Arguments = "-NoProfile -Command \"Get-Clipboard\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
          }
        };
        process.Start();
        string result = process.StandardOutput.ReadToEnd().TrimEnd('\r', '\n');
        process.WaitForExit();
        if (process.ExitCode == 0 && !string.IsNullOrEmpty(result))
          return result;
      }
      catch (Exception)
      {
        // powershell.exe failed, continue to next method
      }
    }

    // Try xclip
    if (ClipboardToolCache.HasXclip)
    {
      try
      {
        using System.Diagnostics.Process process = new()
        {
          StartInfo = new System.Diagnostics.ProcessStartInfo
          {
            FileName = "xclip",
            Arguments = "-selection clipboard -o",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
          }
        };
        process.Start();
        string result = process.StandardOutput.ReadToEnd();
        process.WaitForExit();
        if (process.ExitCode == 0)
          return result;
      }
      catch (Exception)
      {
        // xclip failed, continue to next method
      }
    }

    // Try xsel as final fallback
    if (ClipboardToolCache.HasXsel)
    {
      try
      {
        using System.Diagnostics.Process process = new()
        {
          StartInfo = new System.Diagnostics.ProcessStartInfo
          {
            FileName = "xsel",
            Arguments = "--clipboard --output",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
          }
        };
        process.Start();
        string result = process.StandardOutput.ReadToEnd();
        process.WaitForExit();
        if (process.ExitCode == 0)
          return result;
      }
      catch (Exception)
      {
        // xsel failed
      }
    }

    return null;
  }

  private static void SetLinuxClipboard(string text)
  {
    // Try methods in order of preference:
    // 1. pwsh (PowerShell Core - cross-platform)
    // 2. clip.exe (WSL → Windows clipboard)
    // 3. xclip (X11)
    // 4. xsel (X11 fallback)

    // Try pwsh (PowerShell Core)
    if (ClipboardToolCache.HasPwsh)
    {
      try
      {
        // Use stdin piping to handle special characters safely
        using System.Diagnostics.Process process = new()
        {
          StartInfo = new System.Diagnostics.ProcessStartInfo
          {
            FileName = "pwsh",
            Arguments = "-NoProfile -Command \"$input | Set-Clipboard\"",
            RedirectStandardInput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
          }
        };
        process.Start();
        process.StandardInput.Write(text);
        process.StandardInput.Close();
        process.WaitForExit();
        if (process.ExitCode == 0)
          return;
      }
      catch (Exception)
      {
        // pwsh failed, continue to next method
      }
    }

    // Try clip.exe (WSL → Windows clipboard)
    if (ClipboardToolCache.HasClipExe)
    {
      try
      {
        using System.Diagnostics.Process process = new()
        {
          StartInfo = new System.Diagnostics.ProcessStartInfo
          {
            FileName = "clip.exe",
            RedirectStandardInput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
          }
        };
        process.Start();
        process.StandardInput.Write(text);
        process.StandardInput.Close();
        process.WaitForExit();
        if (process.ExitCode == 0)
          return;
      }
      catch (Exception)
      {
        // clip.exe failed, continue to next method
      }
    }

    // Try xclip
    if (ClipboardToolCache.HasXclip)
    {
      try
      {
        using System.Diagnostics.Process process = new()
        {
          StartInfo = new System.Diagnostics.ProcessStartInfo
          {
            FileName = "xclip",
            Arguments = "-selection clipboard",
            RedirectStandardInput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
          }
        };
        process.Start();
        process.StandardInput.Write(text);
        process.StandardInput.Close();
        process.WaitForExit();
        if (process.ExitCode == 0)
          return;
      }
      catch (Exception)
      {
        // xclip failed, continue to next method
      }
    }

    // Try xsel as final fallback
    if (ClipboardToolCache.HasXsel)
    {
      try
      {
        using System.Diagnostics.Process process = new()
        {
          StartInfo = new System.Diagnostics.ProcessStartInfo
          {
            FileName = "xsel",
            Arguments = "--clipboard --input",
            RedirectStandardInput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
          }
        };
        process.Start();
        process.StandardInput.Write(text);
        process.StandardInput.Close();
        process.WaitForExit();
      }
      catch (Exception)
      {
        // xsel failed - no more fallbacks
      }
    }
  }

  // ============================================================================
  // Clipboard Tool Cache
  // ============================================================================

  /// <summary>
  /// Caches clipboard tool availability to avoid repeated process spawning.
  /// </summary>
  private static class ClipboardToolCache
  {
    private static bool Initialized;
    private static bool _hasPwsh;
    private static bool _hasPowershellExe;
    private static bool _hasClipExe;
    private static bool _hasXclip;
    private static bool _hasXsel;

    public static bool HasPwsh { get { EnsureInitialized(); return _hasPwsh; } }
    public static bool HasPowershellExe { get { EnsureInitialized(); return _hasPowershellExe; } }
    public static bool HasClipExe { get { EnsureInitialized(); return _hasClipExe; } }
    public static bool HasXclip { get { EnsureInitialized(); return _hasXclip; } }
    public static bool HasXsel { get { EnsureInitialized(); return _hasXsel; } }

    private static void EnsureInitialized()
    {
      if (Initialized)
        return;

      // Only check Linux-specific tools when running on Linux
      if (OperatingSystem.IsLinux())
      {
        _hasPwsh = IsCommandAvailable("pwsh");
        _hasPowershellExe = IsCommandAvailable("powershell.exe");
        _hasClipExe = IsCommandAvailable("clip.exe");
        _hasXclip = IsCommandAvailable("xclip");
        _hasXsel = IsCommandAvailable("xsel");
      }

      Initialized = true;
    }

    private static bool IsCommandAvailable(string command)
    {
      try
      {
        using System.Diagnostics.Process process = new()
        {
          StartInfo = new System.Diagnostics.ProcessStartInfo
          {
            FileName = "which",
            Arguments = command,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
          }
        };
        process.Start();
        process.WaitForExit();
        return process.ExitCode == 0;
      }
      catch
      {
        return false;
      }
    }
  }
}
