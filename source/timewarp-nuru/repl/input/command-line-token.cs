namespace TimeWarp.Nuru;

/// <summary>
/// Represents a token in the command line input with position information.
/// </summary>
internal sealed class CommandLineToken
{
  public string Text { get; }
  public ReplTokenType Type { get; }
  public int Start { get; }
  public int End { get; }

  public CommandLineToken(string text, ReplTokenType type, int start, int end)
  {
    Text = text;
    Type = type;
    Start = start;
    End = end;
  }

  public int Length => End - Start;
}
