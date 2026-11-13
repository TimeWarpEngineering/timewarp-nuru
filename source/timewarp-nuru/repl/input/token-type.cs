#region Purpose
// Enum of the token kinds REPL input is classified into.
#endregion

namespace TimeWarp.Nuru;

/// <summary>
/// Types of tokens in command line input.
/// </summary>
internal enum ReplTokenType
{
  Whitespace,
  Command,
  StringLiteral,
  Number,
  LongOption,
  ShortOption,
  Argument
}
