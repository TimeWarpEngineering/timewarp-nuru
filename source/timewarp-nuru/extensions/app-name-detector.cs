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
    // Try to get the actual process name (works for published/apphost executables).
    // When run framework-dependent (`dotnet myapp.dll`), both ProcessPath and ProcessName
    // resolve to the dotnet host ("dotnet"), not the app — so skip them and prefer the
    // entry assembly, which carries the real app name.
    string? processPath = Environment.ProcessPath;
    if (processPath is not null)
    {
      string fileName = Path.GetFileNameWithoutExtension(processPath);
      if (!string.IsNullOrEmpty(fileName) && !IsDotnetHost(fileName))
        return fileName;
    }

    // Entry assembly name (the real app name when hosted by the dotnet muxer).
    string? assemblyName = Assembly.GetEntryAssembly()?.GetName().Name;
    if (!string.IsNullOrEmpty(assemblyName))
      return assemblyName;

    // Last resort: the current process name (may be the dotnet host).
    using Process currentProcess = Process.GetCurrentProcess();
    if (!string.IsNullOrEmpty(currentProcess.ProcessName))
      return currentProcess.ProcessName;

    // No valid name found - exceptional state
    throw new InvalidOperationException
    (
      "Could not determine application name through any detection method. " +
      "This indicates an unusual hosting environment or process configuration that restricts access to process and assembly information."
    );
  }

  /// <summary>
  /// Whether the given process file name is the dotnet host/muxer rather than a real app.
  /// </summary>
  private static bool IsDotnetHost(string fileName) =>
    string.Equals(fileName, "dotnet", StringComparison.OrdinalIgnoreCase);
}
