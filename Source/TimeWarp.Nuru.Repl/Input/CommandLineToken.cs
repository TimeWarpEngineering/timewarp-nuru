namespace TimeWarp.Nuru.Repl.Input;

/// <summary>
/// Represents a token in the command line input with position information.
/// </summary>
public sealed class CommandLineToken
{
  public string Text { get; }
  public TokenType Type { get; }
  public int Start { get; }
  public int End { get; }

  public CommandLineToken(string text, TokenType type, int start, int end)
  {
    Text = text;
    Type = type;
    Start = start;
    End = end;
  }

  public int Length => End - Start;
}

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
