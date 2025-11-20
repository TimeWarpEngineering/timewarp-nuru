namespace TimeWarp.Nuru.Repl.Input;

/// <summary>
/// Types of tokens in command line input.
/// </summary>
public enum TokenType
{
  Whitespace,
  Command,
  StringLiteral,
  Number,
  LongOption,
  ShortOption,
  Argument
}