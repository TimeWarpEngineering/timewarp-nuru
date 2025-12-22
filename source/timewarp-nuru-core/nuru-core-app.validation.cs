namespace TimeWarp.Nuru;

/// <summary>
/// Configuration validation and help display methods for NuruCoreApp.
/// </summary>
public partial class NuruCoreApp
{
  private void ShowAvailableCommands(ITerminal? terminal = null)
  {
    terminal ??= Terminal;
    bool useColor = terminal.SupportsColor;
    terminal.WriteLine(HelpProvider.GetHelpText(Endpoints, AppMetadata?.Name, AppMetadata?.Description, HelpOptions, HelpContext.Cli, useColor));
  }

  /// <summary>
  /// Validates configuration if validation is enabled and not skipped.
  /// </summary>
  /// <param name="args">Command line arguments.</param>
  /// <param name="terminal">Terminal for output.</param>
  /// <returns>True if validation passed or was skipped, false if validation failed.</returns>
  private async Task<bool> ValidateConfigurationAsync(string[] args, ITerminal terminal)
  {
    // Skip validation for help commands or if no ServiceProvider
    if (ShouldSkipValidation(args) || ServiceProvider is null)
      return true;

    try
    {
      IStartupValidator? validator = ServiceProvider.GetService<IStartupValidator>();
      validator?.Validate();
      return true;
    }
    catch (OptionsValidationException ex)
    {
      await DisplayValidationErrorsAsync(ex, terminal).ConfigureAwait(false);
      return false;
    }
  }

  /// <summary>
  /// Determines whether configuration validation should be skipped for the current command.
  /// Validation is skipped for help commands.
  /// </summary>
  private static bool ShouldSkipValidation(string[] args)
  {
    // Skip validation if help flag is present
    // Using loop instead of LINQ to avoid JIT overhead on cold start
    foreach (string arg in args)
    {
      if (arg == "--help" || arg == "-h")
        return true;
    }

    return false;
  }

  /// <summary>
  /// Filters out configuration override arguments (--Section:Key=value pattern).
  /// Uses loop instead of LINQ to avoid JIT overhead on cold start.
  /// </summary>
  private static string[] FilterConfigurationArgs(string[] args)
  {
    // Fast path: if no args, return empty
    if (args.Length == 0)
      return [];

    // Count non-config args first to avoid list resizing
    int count = 0;
    foreach (string arg in args)
    {
      if (!(arg.StartsWith("--", StringComparison.Ordinal) && arg.Contains(':', StringComparison.Ordinal)))
        count++;
    }

    // Fast path: if all args are kept, return original array
    if (count == args.Length)
      return args;

    // Build filtered array
    string[] result = new string[count];
    int index = 0;
    foreach (string arg in args)
    {
      if (!(arg.StartsWith("--", StringComparison.Ordinal) && arg.Contains(':', StringComparison.Ordinal)))
        result[index++] = arg;
    }

    return result;
  }

  /// <summary>
  /// Displays configuration validation errors in a clean, user-friendly format.
  /// </summary>
  private static async Task DisplayValidationErrorsAsync(OptionsValidationException exception, ITerminal terminal)
  {
    await terminal.WriteErrorLineAsync("❌ Configuration validation failed:").ConfigureAwait(false);
    await terminal.WriteErrorLineAsync("").ConfigureAwait(false);

    foreach (string failure in exception.Failures)
    {
      await terminal.WriteErrorLineAsync($"  • {failure}").ConfigureAwait(false);
    }

    await terminal.WriteErrorLineAsync("").ConfigureAwait(false);
  }

  /// <summary>
  /// Gets the default application name using the centralized app name detector.
  /// </summary>
  private static string GetDefaultAppName()
  {
    try
    {
      return AppNameDetector.GetEffectiveAppName();
    }
    catch (InvalidOperationException)
    {
      return "nuru-app";
    }
  }

  /// <summary>
  /// Gets the application name from metadata or falls back to default detection.
  /// </summary>
  private string GetEffectiveAppName()
  {
    return AppMetadata?.Name ?? GetDefaultAppName();
  }

  /// <summary>
  /// Gets the application description from metadata.
  /// </summary>
  private string? GetEffectiveDescription()
  {
    return AppMetadata?.Description;
  }
}
