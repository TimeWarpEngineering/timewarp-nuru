namespace TimeWarp.Nuru;

/// <summary>
/// Token navigation helper methods for the parser.
/// </summary>
internal sealed partial class Parser
{
  private bool Match(params RouteTokenType[] types)
  {
    foreach (RouteTokenType type in types)
    {
      if (Check(type))
      {
        Advance();
        return true;
      }
    }

    return false;
  }

  private bool Check(RouteTokenType type)
  {
    return !IsAtEnd() && Peek().Type == type;
  }

  private Token Advance()
  {
    if (!IsAtEnd()) CurrentIndex++;
    return Previous();
  }

  private bool IsAtEnd()
  {
    return CurrentIndex >= Tokens.Count || Peek().Type == RouteTokenType.EndOfInput;
  }

  private Token Peek()
  {
    return CurrentIndex < Tokens.Count ? Tokens[CurrentIndex] : Token.EndOfInput(0);
  }

  private Token Previous()
  {
    return Tokens[CurrentIndex - 1];
  }

  private Token Current()
  {
    return Tokens[CurrentIndex];
  }

  private Token Consume(RouteTokenType type, string message)
  {
    if (Check(type)) return Advance();
    Token token = Peek();
    AddParseError(new UnexpectedTokenError(token.Position, token.Length, token.Value, message));
    throw new ParseException(message);
  }

  private void Synchronize()
  {
    // Skip tokens until we find a likely start of a new segment
    while (!IsAtEnd())
    {
      Token token = Peek();
      if (token.Type is RouteTokenType.LeftBrace or RouteTokenType.DoubleDash or
          RouteTokenType.SingleDash or RouteTokenType.Identifier)
      {
        break;
      }

      Advance();
    }
  }
}
