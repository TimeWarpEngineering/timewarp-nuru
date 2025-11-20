namespace TimeWarp.Nuru.Repl.Input;

/// <summary>
/// Types of tokens in command line input.
/// </summary>
internal enum TokenType
{
  Whitespace,
  Command,
  StringLiteral,
  Number,
  LongOption,
  ShortOption,
  Argument
}