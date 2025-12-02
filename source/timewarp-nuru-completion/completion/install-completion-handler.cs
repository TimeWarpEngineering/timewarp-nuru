namespace TimeWarp.Nuru;

using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

/// <summary>
/// Handles installation of completion scripts to standard shell-specific locations.
/// </summary>
[SuppressMessage("Globalization", "CA1303:Do not pass literals as localized parameters", Justification = "CLI tool output")]
public static class InstallCompletionHandler
{
  private const string NuruMarkerBegin = "# BEGIN nuru-completion";
  private const string NuruMarkerEnd = "# END nuru-completion";

  /// <summary>
  /// Installs the completion script for the specified shell.
  /// </summary>
  /// <param name="appName">The application name.</param>
  /// <param name="shell">The shell to install for (bash, zsh, fish, pwsh). If null, installs for all shells.</param>
  /// <param name="dryRun">If true, shows what would be installed without writing files.</param>
  /// <returns>Exit code (0 for success, 1 for error).</returns>
  public static int Install(string appName, string? shell, bool dryRun = false)
  {
    // If no shell specified, install for all shells
    if (string.IsNullOrEmpty(shell))
    {
      return InstallAll(appName, dryRun);
    }

    return shell.ToLowerInvariant() switch
    {
      "bash" => InstallBash(appName, dryRun),
      "zsh" => InstallZsh(appName, dryRun),
      "fish" => InstallFish(appName, dryRun),
      "pwsh" or "powershell" => InstallPowerShell(appName, dryRun),
      _ => ReportUnknownShell(shell)
    };
  }

  private static int InstallAll(string appName, bool dryRun)
  {
    Console.WriteLine("Installing completions for all shells...");
    Console.WriteLine();

    int failures = 0;

    // Install for each shell, collecting results
    if (InstallBash(appName, dryRun) != 0)
    {
      failures++;
    }

    Console.WriteLine();

    if (InstallZsh(appName, dryRun) != 0)
    {
      failures++;
    }

    Console.WriteLine();

    if (InstallFish(appName, dryRun) != 0)
    {
      failures++;
    }

    Console.WriteLine();

    if (InstallPowerShell(appName, dryRun) != 0)
    {
      failures++;
    }

    if (failures > 0)
    {
      Console.WriteLine();
      Console.WriteLine("⚠️  " + failures + " shell(s) had issues during installation.");
      return 1;
    }

    Console.WriteLine();
    Console.WriteLine("✅ All shell completions installed successfully!");
    return 0;
  }

  /// <summary>
  /// Auto-detects the current shell from environment variables.
  /// Checks shell-specific variables first, then falls back to $SHELL.
  /// </summary>
  private static string DetectShell()
  {
    // Check for PowerShell-specific environment variables first
    // These are set when running inside PowerShell
    if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("PSModulePath")))
    {
      return "pwsh";
    }

    // Check for Zsh-specific variable
    if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("ZSH_VERSION")))
    {
      return "zsh";
    }

    // Check for Fish-specific variable
    if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("FISH_VERSION")))
    {
      return "fish";
    }

    // Check for Bash-specific variable
    if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("BASH_VERSION")))
    {
      return "bash";
    }

    // Fall back to $SHELL (login shell, not necessarily current shell)
    string? shellPath = Environment.GetEnvironmentVariable("SHELL");
    if (string.IsNullOrEmpty(shellPath))
    {
      // On Windows without $SHELL, assume PowerShell
      if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
      {
        return "pwsh";
      }

      return string.Empty;
    }

    string shellName = Path.GetFileName(shellPath);
    return shellName switch
    {
      "bash" => "bash",
      "zsh" => "zsh",
      "fish" => "fish",
      _ => shellName
    };
  }

  private static int InstallBash(string appName, bool dryRun)
  {
    // Use XDG-compliant location that bash-completion auto-loads from
    string homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
    string completionDir = Path.Combine(homeDir, ".local", "share", "bash-completion", "completions");
    string completionFile = Path.Combine(completionDir, appName);

    string script = DynamicCompletionScriptGenerator.GenerateBash(appName);

    if (dryRun)
    {
      ShowDryRun("Bash", completionFile, script, null);
      return 0;
    }

    return WriteCompletionFile(completionDir, completionFile, script, "Bash", autoLoads: true);
  }

  private static int InstallZsh(string appName, bool dryRun)
  {
    // Use XDG-compliant location for Zsh completions
    string homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
    string completionDir = Path.Combine(homeDir, ".local", "share", "zsh", "site-functions");
    string completionFile = Path.Combine(completionDir, "_" + appName);

    string script = DynamicCompletionScriptGenerator.GenerateZsh(appName);

    string? profileSetup = null;
    string zshrcPath = Path.Combine(homeDir, ".zshrc");
    if (!File.Exists(zshrcPath) || !File.ReadAllText(zshrcPath).Contains(".local/share/zsh/site-functions", StringComparison.Ordinal))
    {
      profileSetup = GetZshProfileSetup(completionDir);
    }

    if (dryRun)
    {
      ShowDryRun("Zsh", completionFile, script, profileSetup);
      return 0;
    }

    int result = WriteCompletionFile(completionDir, completionFile, script, "Zsh", autoLoads: false);
    if (result != 0)
    {
      return result;
    }

    // Check if we need to add fpath setup to .zshrc
    if (profileSetup is not null)
    {
      AppendToProfileIfNeeded(zshrcPath, profileSetup, "Zsh", "~/.zshrc");
    }

    return 0;
  }

  private static int InstallFish(string appName, bool dryRun)
  {
    // Fish auto-loads from this standard location
    string homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
    string completionDir = Path.Combine(homeDir, ".config", "fish", "completions");
    string completionFile = Path.Combine(completionDir, appName + ".fish");

    string script = DynamicCompletionScriptGenerator.GenerateFish(appName);

    if (dryRun)
    {
      ShowDryRun("Fish", completionFile, script, null);
      return 0;
    }

    return WriteCompletionFile(completionDir, completionFile, script, "Fish", autoLoads: true);
  }

  private static int InstallPowerShell(string appName, bool dryRun)
  {
    // Use a dedicated Nuru completions directory for PowerShell
    string homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
    string completionDir = Path.Combine(homeDir, ".local", "share", "nuru", "completions");
    string completionFile = Path.Combine(completionDir, appName + ".ps1");

    string script = DynamicCompletionScriptGenerator.GeneratePowerShell(appName);

    string? profileSetup = null;
    string profilePath = GetPowerShellProfilePath();
    if (!string.IsNullOrEmpty(profilePath) && (!File.Exists(profilePath) || !File.ReadAllText(profilePath).Contains("nuru/completions", StringComparison.Ordinal)))
    {
      profileSetup = GetPowerShellProfileSetup(completionDir);
    }

    if (dryRun)
    {
      ShowDryRun("PowerShell", completionFile, script, profileSetup);
      return 0;
    }

    int result = WriteCompletionFile(completionDir, completionFile, script, "PowerShell", autoLoads: false);
    if (result != 0)
    {
      return result;
    }

    // Check if we need to add lazy loader to profile
    if (profileSetup is not null)
    {
      AppendToProfileIfNeeded(profilePath, profileSetup, "PowerShell", "$PROFILE");
    }

    return 0;
  }

  private static void AppendToProfileIfNeeded(string profilePath, string setupContent, string shellName, string profileDisplayName)
  {
    try
    {
      // Check if the marker already exists
      if (File.Exists(profilePath) && File.ReadAllText(profilePath).Contains(NuruMarkerBegin, StringComparison.Ordinal))
      {
        Console.WriteLine("✅ " + shellName + " profile already configured (found marker in " + profileDisplayName + ")");
        return;
      }

      // Create profile directory if needed
      string? profileDir = Path.GetDirectoryName(profilePath);
      if (!string.IsNullOrEmpty(profileDir) && !Directory.Exists(profileDir))
      {
        Directory.CreateDirectory(profileDir);
        Console.WriteLine("📁 Created profile directory: " + profileDir);
      }

      // Append the setup content
      string contentToAppend = Environment.NewLine + setupContent + Environment.NewLine;
      File.AppendAllText(profilePath, contentToAppend);
      Console.WriteLine("✅ Added Nuru completion loader to " + profileDisplayName);
      Console.WriteLine("   Restart your shell or source your profile to activate.");
    }
    catch (IOException ex)
    {
      Console.Error.WriteLine("⚠️  Could not auto-configure " + profileDisplayName + ": " + ex.Message);
      Console.Error.WriteLine("   Please add the following to your " + profileDisplayName + " manually:");
      Console.Error.WriteLine();
      Console.Error.WriteLine(setupContent);
    }
    catch (UnauthorizedAccessException ex)
    {
      Console.Error.WriteLine("⚠️  Could not auto-configure " + profileDisplayName + ": " + ex.Message);
      Console.Error.WriteLine("   Please add the following to your " + profileDisplayName + " manually:");
      Console.Error.WriteLine();
      Console.Error.WriteLine(setupContent);
    }
  }

  private static int WriteCompletionFile(string directory, string filePath, string content, string shellName, bool autoLoads)
  {
    try
    {
      // Create directory if it doesn't exist
      if (!Directory.Exists(directory))
      {
        Directory.CreateDirectory(directory);
        Console.WriteLine("📁 Created directory: " + directory);
      }

      // Write the completion script
      File.WriteAllText(filePath, content);
      Console.WriteLine("✅ " + shellName + " completion installed to: " + filePath);

      if (autoLoads)
      {
        Console.WriteLine("   This location auto-loads - restart your shell or source the file.");
      }
      else
      {
        Console.WriteLine("   See one-time setup instructions below (if any).");
      }

      return 0;
    }
    catch (IOException ex)
    {
      Console.Error.WriteLine("❌ Failed to install " + shellName + " completion: " + ex.Message);
      return 1;
    }
    catch (UnauthorizedAccessException ex)
    {
      Console.Error.WriteLine("❌ Failed to install " + shellName + " completion: " + ex.Message);
      return 1;
    }
  }

  private static void ShowDryRun(string shellName, string filePath, string script, string? profileSetup)
  {
    Console.WriteLine("🔍 Dry run for " + shellName + ":");
    Console.WriteLine("   Would write to: " + filePath);
    Console.WriteLine("   Script size: " + script.Length + " bytes");

    if (profileSetup is not null)
    {
      Console.WriteLine();
      Console.WriteLine("   Would require one-time profile setup:");
      Console.WriteLine(profileSetup);
    }
  }

  private static string GetZshProfileSetup(string completionDir)
  {
    return $"""
    {NuruMarkerBegin}
    # Add Nuru completions to fpath
    fpath=({completionDir} $fpath)
    autoload -Uz compinit && compinit
    {NuruMarkerEnd}
    """;
  }

  private static string GetPowerShellProfileSetup(string completionDir)
  {
    // Lazy loader pattern - doesn't source files at startup, only registers completers
    return $$"""
    # Nuru completions - lazy load pattern
    {{NuruMarkerBegin}}
    $nuruCompletionPath = "{{completionDir}}"
    if (Test-Path $nuruCompletionPath) {
        Get-ChildItem "$nuruCompletionPath/*.ps1" -ErrorAction SilentlyContinue | ForEach-Object {
            . $_.FullName
        }
    }
    {{NuruMarkerEnd}}
    """;
  }

  private static string GetPowerShellProfilePath()
  {
    // PowerShell Core on Linux/macOS uses XDG_CONFIG_HOME or ~/.config
    // This matches what $PROFILE resolves to in PowerShell
    string homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

    if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
    {
      // Linux/macOS: Use XDG_CONFIG_HOME or default to ~/.config
      string? xdgConfig = Environment.GetEnvironmentVariable("XDG_CONFIG_HOME");
      string configDir = !string.IsNullOrEmpty(xdgConfig)
        ? xdgConfig
        : Path.Combine(homeDir, ".config");
      return Path.Combine(configDir, "powershell", "Microsoft.PowerShell_profile.ps1");
    }

    // Windows: Use Documents/PowerShell for PowerShell Core
    string corePath = Path.Combine(homeDir, "Documents", "PowerShell", "Microsoft.PowerShell_profile.ps1");
    if (File.Exists(corePath))
    {
      return corePath;
    }

    // Fall back to Windows PowerShell
    return Path.Combine(homeDir, "Documents", "WindowsPowerShell", "Microsoft.PowerShell_profile.ps1");
  }

  private static int ReportUnknownShell(string shell)
  {
    Console.Error.WriteLine("❌ Unknown shell: " + shell);
    Console.Error.WriteLine("   Supported shells: bash, zsh, fish, pwsh");
    return 1;
  }
}
