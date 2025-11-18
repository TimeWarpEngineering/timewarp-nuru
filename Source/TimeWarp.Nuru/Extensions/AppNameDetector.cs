namespace TimeWarp.Nuru;

/// <summary>
/// Utility class for detecting the effective application name.
/// Used consistently across REPL history and shell completion features.
/// </summary>
internal static class AppNameDetector
{
  /// <summary>
  /// Gets the effective application name using a robust detection chain.
  /// This method is used by both REPL history and shell completion features
  /// to ensure consistent app naming.
  /// </summary>
  /// <returns>The detected application name.</returns>
  /// <exception cref="InvalidOperationException">Thrown when application name cannot be determined through any detection method.</exception>
  public static string GetEffectiveAppName()
  {
    // Try to get the actual process name (works for published executables)
    string? processPath = Environment.ProcessPath;
    if (processPath is not null)
    {
      string fileName = Path.GetFileNameWithoutExtension(processPath);
      if (!string.IsNullOrEmpty(fileName))
        return fileName;
    }

    // Fallback: try Process.GetCurrentProcess()
    using var currentProcess = Process.GetCurrentProcess();
    if (!string.IsNullOrEmpty(currentProcess.ProcessName))
      return currentProcess.ProcessName;

    // Final attempt: assembly name
    string? assemblyName = System.Reflection.Assembly.GetEntryAssembly()?.GetName().Name;
    if (!string.IsNullOrEmpty(assemblyName))
      return assemblyName;

    // No valid name found - exceptional state
    throw new InvalidOperationException
    (
      "Could not determine application name through any detection method. " +
      "This indicates an unusual hosting environment or process configuration that restricts access to process and assembly information."
    );
  }
}