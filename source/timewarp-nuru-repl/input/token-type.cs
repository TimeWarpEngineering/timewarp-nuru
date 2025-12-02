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
