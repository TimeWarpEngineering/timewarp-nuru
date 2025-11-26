namespace TimeWarp.Nuru;

/// <summary>
/// Syntax highlighting colors for REPL terminal output.
/// Based on PSReadline color scheme for PowerShell.
/// </summary>
public static class SyntaxColors
{
  /// <summary>
  /// Color for command names and keywords.
  /// </summary>
  public const string CommandColor = AnsiColors.BrightYellow;

  /// <summary>
  /// Color for comments and documentation.
  /// </summary>
  public const string CommentColor = AnsiColors.Green;

  /// <summary>
  /// Color for continuation prompts in multi-line input.
  /// </summary>
  public const string ContinuationPromptColor = AnsiColors.White;

  /// <summary>
  /// Default color for tokens and general text.
  /// </summary>
  public const string DefaultTokenColor = AnsiColors.White;

  /// <summary>
  /// Color for emphasized or highlighted text.
  /// </summary>
  public const string EmphasisColor = AnsiColors.BrightCyan;

  /// <summary>
  /// Color for error messages and error highlighting.
  /// </summary>
  public const string ErrorColor = AnsiColors.BrightRed;

  /// <summary>
  /// Color for inline prediction suggestions.
  /// </summary>
  public const string InlinePredictionColor = "\x1b[97;2;3m";

  /// <summary>
  /// Color for language keywords.
  /// </summary>
  public const string KeywordColor = AnsiColors.BrightGreen;

  /// <summary>
  /// Color for completion list items.
  /// </summary>
  public const string ListPredictionColor = AnsiColors.Yellow;

  /// <summary>
  /// Background color for selected completion list items.
  /// </summary>
  public const string ListPredictionSelectedColor = "\x1b[48;5;238m";

  /// <summary>
  /// Color for completion list tooltips.
  /// </summary>
  public const string ListPredictionTooltipColor = "\x1b[97;2;3m";

  /// <summary>
  /// Color for member names (properties, methods, fields).
  /// </summary>
  public const string MemberColor = AnsiColors.White;

  /// <summary>
  /// Color for numeric literals.
  /// </summary>
  public const string NumberColor = AnsiColors.BrightWhite;

  /// <summary>
  /// Color for operators and symbols.
  /// </summary>
  public const string OperatorColor = AnsiColors.Gray;

  /// <summary>
  /// Color for parameter names and values.
  /// </summary>
  public const string ParameterColor = AnsiColors.Gray;

  /// <summary>
  /// Color for selected text (foreground and background).
  /// </summary>
  public const string SelectionColor = "\x1b[30;47m";

  /// <summary>
  /// Color for string literals.
  /// </summary>
  public const string StringColor = AnsiColors.Cyan;

  /// <summary>
  /// Color for type names and class names.
  /// </summary>
  public const string TypeColor = AnsiColors.White;

  /// <summary>
  /// Color for variable names.
  /// </summary>
  public const string VariableColor = AnsiColors.BrightGreen;
}
