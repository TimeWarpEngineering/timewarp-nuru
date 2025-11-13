namespace TimeWarp.Nuru.Completion;

/// <summary>
/// Specifies the type of completion candidate.
/// </summary>
public enum CompletionType
{
  /// <summary>
  /// Literal command name from route pattern.
  /// Example: "deploy", "status", "createorder"
  /// </summary>
  Command,

  /// <summary>
  /// Option flag (long or short form).
  /// Example: "--force", "-f", "--config"
  /// </summary>
  Option,

  /// <summary>
  /// Parameter value (generic).
  /// </summary>
  Parameter,

  /// <summary>
  /// File path completion - delegates to shell's file completion.
  /// </summary>
  File,

  /// <summary>
  /// Directory path completion - delegates to shell's directory completion.
  /// </summary>
  Directory,

  /// <summary>
  /// Enum value completion - provides all enum member names.
  /// </summary>
  Enum,

  /// <summary>
  /// Custom completion from user-provided ICompletionSource (future).
  /// </summary>
  Custom
}
