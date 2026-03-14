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
  private static async Task<string?> GetClipboardTextAsync()
  {
    try
    {
      if (OperatingSystem.IsWindows())
      {
        return await GetWindowsClipboardAsync().ConfigureAwait(false);
      }
      else if (OperatingSystem.IsMacOS())
      {
        return await GetMacOSClipboardAsync().ConfigureAwait(false);
      }
      else if (OperatingSystem.IsLinux())
      {
        return await GetLinuxClipboardAsync().ConfigureAwait(false);
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
  private static async Task SetClipboardTextAsync(string text)
  {
    if (string.IsNullOrEmpty(text))
      return;

    try
    {
      if (OperatingSystem.IsWindows())
      {
        await SetWindowsClipboardAsync(text).ConfigureAwait(false);
      }
      else if (OperatingSystem.IsMacOS())
      {
        await SetMacOSClipboardAsync(text).ConfigureAwait(false);
      }
      else if (OperatingSystem.IsLinux())
      {
        await SetLinuxClipboardAsync(text).ConfigureAwait(false);
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

  private static async Task<string?> GetWindowsClipboardAsync()
  {
    CommandOutput output = await Shell.Builder("powershell")
      .WithArguments("-command", "Get-Clipboard")
      .WithNoValidation()
      .CaptureAsync()
      .ConfigureAwait(false);
    return output.Success ? output.Stdout.TrimEnd('\r', '\n') : null;
  }

  private static async Task SetWindowsClipboardAsync(string text)
  {
    await Shell.Builder("powershell")
      .WithArguments($"-command \"Set-Clipboard -Value '{text.Replace("'", "''", StringComparison.Ordinal)}'\"")
      .WithNoValidation()
      .RunAsync()
      .ConfigureAwait(false);
  }

  // ============================================================================
  // macOS Clipboard
  // ============================================================================

  private static async Task<string?> GetMacOSClipboardAsync()
  {
    CommandOutput output = await Shell.Builder("pbpaste")
      .WithNoValidation()
      .CaptureAsync()
      .ConfigureAwait(false);
    return output.Success ? output.Stdout : null;
  }

  private static async Task SetMacOSClipboardAsync(string text)
  {
    await Shell.Builder("pbcopy")
      .WithStandardInput(text)
      .WithNoValidation()
      .RunAsync()
      .ConfigureAwait(false);
  }

  // ============================================================================
  // Linux Clipboard
  // ============================================================================

  private static async Task<string?> GetLinuxClipboardAsync()
  {
    // Try methods in order of preference:
    // 1. pwsh (PowerShell Core - cross-platform)
    // 2. powershell.exe (WSL → Windows clipboard)
    // 3. xclip (X11)
    // 4. xsel (X11 fallback)

    // Try pwsh (PowerShell Core)
    if (ClipboardToolCache.HasPwsh)
    {
      CommandOutput output = await Shell.Builder("pwsh")
        .WithArguments("-NoProfile", "-Command", "Get-Clipboard")
        .WithNoValidation()
        .CaptureAsync()
        .ConfigureAwait(false);
      if (output.Success && !string.IsNullOrEmpty(output.Stdout.TrimEnd('\r', '\n')))
        return output.Stdout.TrimEnd('\r', '\n');
    }

    // Try powershell.exe (WSL → Windows clipboard)
    if (ClipboardToolCache.HasPowershellExe)
    {
      CommandOutput output = await Shell.Builder("powershell.exe")
        .WithArguments("-NoProfile", "-Command", "Get-Clipboard")
        .WithNoValidation()
        .CaptureAsync()
        .ConfigureAwait(false);
      if (output.Success && !string.IsNullOrEmpty(output.Stdout.TrimEnd('\r', '\n')))
        return output.Stdout.TrimEnd('\r', '\n');
    }

    // Try xclip
    if (ClipboardToolCache.HasXclip)
    {
      CommandOutput output = await Shell.Builder("xclip")
        .WithArguments("-selection", "clipboard", "-o")
        .WithNoValidation()
        .CaptureAsync()
        .ConfigureAwait(false);
      if (output.Success)
        return output.Stdout;
    }

    // Try xsel as final fallback
    if (ClipboardToolCache.HasXsel)
    {
      CommandOutput output = await Shell.Builder("xsel")
        .WithArguments("--clipboard", "--output")
        .WithNoValidation()
        .CaptureAsync()
        .ConfigureAwait(false);
      if (output.Success)
        return output.Stdout;
    }

    return null;
  }

  private static async Task SetLinuxClipboardAsync(string text)
  {
    // Try methods in order of preference:
    // 1. pwsh (PowerShell Core - cross-platform)
    // 2. clip.exe (WSL → Windows clipboard)
    // 3. xclip (X11)
    // 4. xsel (X11 fallback)

    // Try pwsh (PowerShell Core)
    if (ClipboardToolCache.HasPwsh)
    {
      CommandOutput output = await Shell.Builder("pwsh")
        .WithArguments("-NoProfile", "-Command", "$input | Set-Clipboard")
        .WithStandardInput(text)
        .WithNoValidation()
        .CaptureAsync()
        .ConfigureAwait(false);
      if (output.Success)
        return;
    }

    // Try clip.exe (WSL → Windows clipboard)
    if (ClipboardToolCache.HasClipExe)
    {
      CommandOutput output = await Shell.Builder("clip.exe")
        .WithStandardInput(text)
        .WithNoValidation()
        .CaptureAsync()
        .ConfigureAwait(false);
      if (output.Success)
        return;
    }

    // Try xclip
    if (ClipboardToolCache.HasXclip)
    {
      CommandOutput output = await Shell.Builder("xclip")
        .WithArguments("-selection", "clipboard")
        .WithStandardInput(text)
        .WithNoValidation()
        .CaptureAsync()
        .ConfigureAwait(false);
      if (output.Success)
        return;
    }

    // Try xsel as final fallback
    if (ClipboardToolCache.HasXsel)
    {
      await Shell.Builder("xsel")
        .WithArguments("--clipboard", "--input")
        .WithStandardInput(text)
        .WithNoValidation()
        .RunAsync()
        .ConfigureAwait(false);
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
