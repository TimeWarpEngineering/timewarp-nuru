namespace TimeWarp.Nuru;

/// <summary>
/// Segment parsing methods for the parser.
/// </summary>
internal sealed partial class Parser
{
  private LiteralSyntax ParseLiteral()
  {
    Token token = Consume(RouteTokenType.Identifier, "Expected identifier");
    return new LiteralSyntax(token.Value)
    {
      Position = token.Position,
      Length = token.Length
    };
  }

  private LiteralSyntax ParseEndOfOptions()
  {
    Token token = Consume(RouteTokenType.EndOfOptions, "Expected '--'");

    // Create a literal segment for the -- separator
    return new LiteralSyntax("--")
    {
      Position = token.Position,
      Length = token.Length
    };
  }

  private ParameterSyntax ParseParameter()
  {
    Token leftBrace = Consume(RouteTokenType.LeftBrace, "Expected '{'");
    int startPos = leftBrace.Position;

    // Check for catch-all marker
    bool isCatchAll = false;
    if (Match(RouteTokenType.Asterisk))
    {
      isCatchAll = true;
    }

    // Parameter name
    Token nameToken = Consume(RouteTokenType.Identifier, "Expected parameter name");
    string paramName = nameToken.Value;

    // Validate identifier starts with letter or underscore
    if (!IsValidIdentifier(paramName))
    {
      AddParseError
      (
        new InvalidIdentifierError
        (
          nameToken.Position,
          nameToken.Length,
          paramName
        )
      );
    }

    // Optional marker
    bool isOptional = Match(RouteTokenType.Question);

    // Validate that catch-all and optional are not combined
    if (isCatchAll && isOptional)
    {
      AddParseError
      (
        new InvalidModifierCombinationError
        (
          startPos,
          nameToken.EndPosition - startPos,
          paramName
        )
      );
    }

    // Type constraint
    string? typeConstraint = null;
    if (Match(RouteTokenType.Colon))
    {
      Token typeToken = Consume(RouteTokenType.Identifier, "Expected type name after ':'");
      typeConstraint = typeToken.Value;

      // Type might also be optional (e.g., int?)
      if (Match(RouteTokenType.Question))
      {
        isOptional = true;
        typeConstraint += "?";
      }

      // Validate type constraint (NURU004)
      string baseType = typeConstraint.TrimEnd('?');
      if (!IsValidTypeConstraint(baseType))
      {
        AddParseError
        (
          new InvalidTypeConstraintError
          (
            typeToken.Position,
            typeToken.Length,
            baseType
          )
        );
      }
    }

    // Description
    string? description = null;
    if (Match(RouteTokenType.Pipe))
    {
      description = ConsumeDescription(stopAtRightBrace: true);
    }

    Token rightBrace = Consume(RouteTokenType.RightBrace, "Expected '}'");

    return new ParameterSyntax(paramName, isCatchAll, isOptional, false, typeConstraint, description)
    {
      Position = startPos,
      Length = rightBrace.EndPosition - startPos
    };
  }

  private OptionSyntax ParseOption()
  {
    Token optionToken = Current();
    bool isLong = optionToken.Type == RouteTokenType.DoubleDash;
    Advance(); // Consume the dash(es)

    int startPos = optionToken.Position;
    Token optionNameToken = Consume(RouteTokenType.Identifier, "Expected option name");

    // Parse option forms (long/short)
    (string? longForm, string? shortForm) = ParseOptionForms(isLong, optionNameToken);

    // Check for optional modifier (?)
    bool isOptional = Match(RouteTokenType.Question);

    // Parse description if present
    string? description = ParseOptionDescription();

    // Parse option parameter if present
    ParameterSyntax? parameter = ParseOptionParameter();

    int endPos = Previous().EndPosition;

    return new OptionSyntax(longForm, shortForm, description, parameter, isOptional)
    {
      Position = startPos,
      Length = endPos - startPos
    };
  }

  private (string? longForm, string? shortForm) ParseOptionForms(bool isLong, Token optionNameToken)
  {
    if (isLong)
    {
      string longForm = optionNameToken.Value;
      string? shortForm = null;

      // Check for short alias
      if (Match(RouteTokenType.Comma))
      {
        Consume(RouteTokenType.SingleDash, "Expected '-' after comma");
        Token shortToken = Consume(RouteTokenType.Identifier, "Expected short option name");
        shortForm = shortToken.Value;
      }

      return (longForm, shortForm);
    }
    else
    {
      string shortForm = optionNameToken.Value;

      // Validate single dash options should be single character
      if (shortForm.Length > 1)
      {
        AddParseError
        (
          new InvalidOptionFormatError
          (
            optionNameToken.Position - 1, // Include the dash
            optionNameToken.Length + 1,
            $"-{shortForm}")
          );
      }

      return (null, shortForm);
    }
  }

  private string? ParseOptionDescription()
  {
    if (Match(RouteTokenType.Pipe))
    {
      return ConsumeDescription(stopAtRightBrace: false);
    }

    return null;
  }

  private ParameterSyntax? ParseOptionParameter()
  {
    if (!Check(RouteTokenType.LeftBrace))
    {
      return null;
    }

    ParameterSyntax parameter = ParseParameter();

    // Validate that catch-all is not used in options
    if (parameter.IsCatchAll)
    {
      // Catch-all in options is invalid parameter syntax
      AddParseError
      (
        new InvalidParameterSyntaxError
        (
          parameter.Position,
          parameter.Length,
          $"{{*{parameter.Name}}}",
          parameter.Name
        )
      );
    }

    // Check for repeated modifier (*) after the parameter
    // e.g., --tag {value}* means the option can be repeated
    if (Match(RouteTokenType.Asterisk))
    {
      parameter = parameter with { IsRepeated = true };
    }

    return parameter;
  }

  private SegmentSyntax? ParseInvalidToken()
  {
    Token token = Advance();

    // Special handling for angle bracket syntax
    if (token.Value.StartsWith('<') && token.Value.EndsWith('>'))
    {
      string paramName = token.Value[1..^1]; // Remove < and >
      string suggestion = $"{{{paramName}}}";

      AddParseError
      (
        new InvalidParameterSyntaxError
        (
          token.Position,
          token.Length,
          token.Value,
          suggestion
        )
      );
    }
    else
    {
      AddParseError
      (
        new InvalidCharacterError
        (
          token.Position,
          token.Length,
          token.Value
        )
      );
    }

    return null; // Skip invalid tokens
  }

  private SegmentSyntax? HandleUnexpectedRightBrace()
  {
    Token token = Advance(); // Consume the right brace to avoid infinite loop
    AddParseError
    (
      new UnexpectedTokenError
      (
        token.Position,
        token.Length,
        "}",
        "Unexpected '}'"
      )
    );

    return null;
  }

  private SegmentSyntax? HandleUnexpectedToken()
  {
    Token token = Advance(); // Consume the unexpected token to avoid infinite loop
    // Unexpected token - truly unexpected case
    AddParseError
    (
      new InvalidCharacterError
      (
        token.Position,
        token.Length,
        token.Value
      )
    );

    return null;
  }
}
