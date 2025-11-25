namespace TimeWarp.Nuru.Repl;

/// <summary>
/// Provides formatting utilities for REPL prompts and display elements.
/// </summary>
internal static class PromptFormatter
{
  /// <summary>
  /// Formats a prompt string with optional color based on REPL options.
  /// </summary>
  /// <param name="prompt">The prompt text to format.</param>
  /// <param name="enableColors">Whether to apply ANSI color codes.</param>
  /// <param name="promptColor">The ANSI color code to use (e.g., AnsiColors.Green).</param>
  /// <returns>The formatted prompt string.</returns>
  public static string Format(string prompt, bool enableColors, string promptColor)
  {
    ArgumentException.ThrowIfNullOrEmpty(prompt);
    ArgumentException.ThrowIfNullOrEmpty(promptColor);

    return enableColors
      ? promptColor + prompt + AnsiColors.Reset
      : prompt;
  }

  /// <summary>
  /// Formats a prompt string using ReplOptions settings.
  /// </summary>
  /// <param name="options">The REPL options containing prompt and color settings.</param>
  /// <returns>The formatted prompt string.</returns>
  public static string Format(ReplOptions options)
  {
    ArgumentNullException.ThrowIfNull(options);

    return Format(options.Prompt, options.EnableColors, options.PromptColor);
  }
}
