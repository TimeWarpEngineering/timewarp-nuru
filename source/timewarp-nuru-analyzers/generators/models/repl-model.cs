namespace TimeWarp.Nuru.Generators;

/// <summary>
/// Configuration options for REPL (Read-Eval-Print Loop) mode.
/// </summary>
/// <param name="Prompt">The prompt string to display</param>
/// <param name="ContinuationPrompt">Prompt for multi-line input</param>
/// <param name="ExitCommands">Commands that exit the REPL</param>
/// <param name="HistorySize">Maximum history entries to retain</param>
/// <param name="EnableSyntaxHighlighting">Whether to highlight input</param>
/// <param name="EnableAutoComplete">Whether to enable tab completion</param>
public sealed record ReplModel(
  string Prompt,
  string ContinuationPrompt,
  ImmutableArray<string> ExitCommands,
  int HistorySize,
  bool EnableSyntaxHighlighting,
  bool EnableAutoComplete)
{
  /// <summary>
  /// Default REPL configuration.
  /// </summary>
  public static readonly ReplModel Default = new(
    Prompt: "> ",
    ContinuationPrompt: "... ",
    ExitCommands: ["exit", "quit", "q"],
    HistorySize: 100,
    EnableSyntaxHighlighting: true,
    EnableAutoComplete: true);
}
