namespace TimeWarp.Nuru;

/// <summary>
/// Represents the different types of tokens in route patterns.
/// </summary>
internal enum RouteTokenType
{
  /// <summary>
  /// A literal string that must match exactly.
  /// </summary>
  Literal,
  /// <summary>
  /// Opening brace '{' for parameters.
  /// </summary>
  LeftBrace,
  /// <summary>
  /// Closing brace '}' for parameters.
  /// </summary>
  RightBrace,
  /// <summary>
  /// Colon ':' for type constraints.
  /// </summary>
  Colon,
  /// <summary>
  /// Question mark '?' for optional parameters.
  /// </summary>
  Question,
  /// <summary>
  /// Pipe '|' for descriptions.
  /// </summary>
  Pipe,
  /// <summary>
  /// Asterisk '*' for catch-all parameters.
  /// </summary>
  Asterisk,
  /// <summary>
  /// Double dash '--' for long options.
  /// </summary>
  DoubleDash,
  /// <summary>
  /// Single dash '-' for short options.
  /// </summary>
  SingleDash,
  /// <summary>
  /// Comma ',' for option aliases.
  /// </summary>
  Comma,
  /// <summary>
  /// End of options separator '--' (standalone double dash).
  /// </summary>
  EndOfOptions,
  /// <summary>
  /// An identifier (parameter name, option name, type name).
  /// </summary>
  Identifier,
  /// <summary>
  /// Description text following a pipe character.
  /// </summary>
  Description,
  /// <summary>
  /// End of input marker.
  /// </summary>
  EndOfInput,
  /// <summary>
  /// Invalid/unrecognized character.
  /// </summary>
  Invalid
}

/// <summary>
/// Represents a token in the route pattern input.
/// </summary>
/// <param name="Type">The type of token.</param>
/// <param name="Value">The text value of the token.</param>
/// <param name="Position">The starting position in the input string.</param>
/// <param name="Length">The length of the token in characters.</param>
internal record Token(RouteTokenType Type, string Value, int Position, int Length)
{
  /// <summary>
  /// Gets the end position of this token (Position + Length).
  /// </summary>
  public int EndPosition => Position + Length;

  /// <summary>
  /// Creates a token for end of input.
  /// </summary>
  /// <param name="position">The position at the end of input.</param>
  /// <returns>An end of input token.</returns>
  public static Token EndOfInput(int position) => new(RouteTokenType.EndOfInput, string.Empty, position, 0);

  /// <summary>
  /// Creates an invalid token.
  /// </summary>
  /// <param name="value">The invalid character(s).</param>
  /// <param name="position">The position of the invalid character.</param>
  /// <returns>An invalid token.</returns>
  public static Token Invalid(string value, int position)
  {
    ArgumentNullException.ThrowIfNull(value, nameof(value));
    return new(RouteTokenType.Invalid, value, position, value.Length);
  }

  /// <summary>
  /// Returns a diagnostic string representation of the token.
  /// </summary>
  public override string ToString()
  {
    return $"[{Type}] '{Value}' at position {Position}";
  }
}
