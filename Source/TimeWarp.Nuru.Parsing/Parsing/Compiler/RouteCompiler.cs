namespace TimeWarp.Nuru.Parsing;

using TimeWarp.Nuru.Parsing;

/// <summary>
/// Compiler that converts route pattern syntax tree to the runtime CompiledRoute structure.
/// This maintains backward compatibility with the current system.
/// </summary>
internal sealed class RouteCompiler : SyntaxVisitor<object?>
{
  private readonly ILogger<RouteCompiler> Logger;
  private readonly List<RouteMatcher> Segments = [];
  private readonly List<string> RequiredOptionPatterns = [];
  private readonly List<OptionMatcher> OptionMatchers = [];
  private string? CatchAllParameterName;
  private int Specificity;

  public RouteCompiler() : this(null) { }

  public RouteCompiler(ILogger<RouteCompiler>? logger = null)
  {
    Logger = logger ?? NullLogger<RouteCompiler>.Instance;
  }

  /// <summary>
  /// Compiles a route pattern syntax tree to a CompiledRoute.
  /// </summary>
  /// <param name="syntax">The syntax tree to compile.</param>
  /// <returns>A CompiledRoute compatible with the existing system.</returns>
  public CompiledRoute Compile(RouteSyntax syntax)
  {
    // Reset state
    Segments.Clear();
    RequiredOptionPatterns.Clear();
    OptionMatchers.Clear();
    CatchAllParameterName = null;
    Specificity = 0;

    // Visit all segments
    VisitPattern(syntax);

    // Build the CompiledRoute
    return new CompiledRoute
    {
      PositionalMatchers = Segments.ToArray(),
      RequiredOptionPatterns = RequiredOptionPatterns.ToArray(),
      OptionMatchers = OptionMatchers.ToArray(),
      CatchAllParameterName = CatchAllParameterName,
      Specificity = Specificity
    };
  }

  public override object? VisitLiteral(LiteralSyntax literal)
  {
    Segments.Add(new LiteralMatcher(literal.Value));
    Specificity += 15; // Literal segments greatly increase specificity
    return null;
  }

  public override object? VisitParameter(ParameterSyntax parameter)
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

    // Create parameter matcher
    var parameterMatcher = new ParameterMatcher(
        parameter.Name,
        parameter.IsCatchAll,
        parameter.Type,
        parameter.Description,
        parameter.IsOptional);

    Segments.Add(parameterMatcher);
    return null;
  }

  public override object? VisitOption(OptionSyntax option)
  {
    string? optionShortSyntax =
      option.ShortForm is not null
      ? $"-{option.ShortForm}"
      : null;

    string? optionLongSyntax =
      option.LongForm is not null
      ? $"--{option.LongForm}"
      : null;

    string optionSyntax =
      optionShortSyntax
      ?? optionLongSyntax
      ?? throw new ParseException("Option must have a syntax");

    RequiredOptionPatterns.Add(optionSyntax);
    Specificity += 10; // Options increase specificity

    // Determine if this option expects a value
    bool expectsValue = option.Parameter is not null;
    string? valueParameterName = option.Parameter?.Name;

    // For boolean options (no parameter), use the option name as the parameter name
    if (valueParameterName is null && !expectsValue)
    {
      // Extract the option name from the syntax (remove leading dashes)
      valueParameterName = option.LongForm ?? option.ShortForm;
      LoggerMessages.SettingBooleanOptionParameter(Logger, valueParameterName!, null);
    }

    if (option.Parameter is not null)
    {
      if (option.Parameter.IsCatchAll)
        CatchAllParameterName = option.Parameter.Name;
      else
        Specificity += 5; // Option parameters increase specificity
    }

    // Create option matcher
    var optionMatcher =
      new OptionMatcher
      (
        matchPattern: optionSyntax,
        expectsValue: expectsValue,
        parameterName: valueParameterName,
        alternateForm: optionShortSyntax,
        description: option.Description,
        isOptional: option.IsOptional
      );

    OptionMatchers.Add(optionMatcher);
    return null;
  }
}
