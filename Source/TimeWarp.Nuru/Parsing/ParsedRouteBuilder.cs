namespace TimeWarp.Nuru.Parsing;

using TimeWarp.Nuru.Parsing.Ast;
using TimeWarp.Nuru.Parsing.Segments;

/// <summary>
/// Visitor that converts route pattern AST to the existing ParsedRoute structure.
/// This maintains backward compatibility with the current system.
/// </summary>
internal sealed class ParsedRouteBuilder : RoutePatternVisitor<object?>
{
  private readonly List<RouteSegment> Segments = [];
  private readonly List<string> RequiredOptions = [];
  private readonly List<OptionSegment> OptionSegments = [];
  private string? CatchAllParameterName;
  private int Specificity;

  /// <summary>
  /// Converts a route pattern AST to a ParsedRoute.
  /// </summary>
  /// <param name="ast">The AST to convert.</param>
  /// <returns>A ParsedRoute compatible with the existing system.</returns>
  public ParsedRoute Build(RoutePatternAst ast)
  {
    // Reset state
    Segments.Clear();
    RequiredOptions.Clear();
    OptionSegments.Clear();
    CatchAllParameterName = null;
    Specificity = 0;

    // Visit all segments
    VisitPattern(ast);

    // Build the ParsedRoute
    return new ParsedRoute
    {
      PositionalTemplate = Segments.ToArray(),
      RequiredOptions = RequiredOptions.ToArray(),
      OptionSegments = OptionSegments.ToArray(),
      CatchAllParameterName = CatchAllParameterName,
      Specificity = Specificity
    };
  }

  public override object? VisitLiteral(LiteralNode literal)
  {
    Segments.Add(new LiteralSegment(literal.Value));
    Specificity += 15; // Literal segments greatly increase specificity
    return null;
  }

  public override object? VisitParameter(ParameterNode parameter)
  {
    if (parameter.IsCatchAll)
    {
      CatchAllParameterName = parameter.Name;
      Specificity -= 20; // Catch-all reduces specificity
    }
    else
    {
      Specificity += 2; // Positional parameters slightly increase specificity
    }

    // Create parameter segment
    var parameterSegment = new ParameterSegment(
        parameter.Name,
        parameter.IsCatchAll,
        parameter.Type,
        parameter.Description,
        parameter.IsOptional);

    Segments.Add(parameterSegment);
    return null;
  }

  public override object? VisitOption(OptionNode option)
  {
    // Add to required options list
    string optionName = option.LongName.StartsWith("--", StringComparison.Ordinal) ? option.LongName : $"--{option.LongName}";
    RequiredOptions.Add(optionName);
    Specificity += 10; // Options increase specificity

    // Determine if this option expects a value
    bool expectsValue = option.Parameter is not null;
    string? valueParameterName = option.Parameter?.Name;

    if (option.Parameter is not null)
    {
      if (option.Parameter.IsCatchAll)
      {
        CatchAllParameterName = option.Parameter.Name;
      }
      else
      {
        Specificity += 5; // Option parameters increase specificity
      }
    }

    // Create option segment
    var optionSegment = new OptionSegment(
        optionName,
        expectsValue,
        valueParameterName,
        option.ShortName is not null ? $"-{option.ShortName}" : null,
        option.Description);

    OptionSegments.Add(optionSegment);
    return null;
  }
}

