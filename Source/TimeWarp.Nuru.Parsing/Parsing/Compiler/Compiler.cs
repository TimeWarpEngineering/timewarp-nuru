namespace TimeWarp.Nuru.Parsing;

/// <summary>
/// Compiler that converts route pattern syntax tree to the runtime CompiledRoute structure.
/// This maintains backward compatibility with the current system.
/// </summary>
internal sealed class Compiler : SyntaxVisitor<object?>
{
  // Specificity scoring constants
  // Higher values indicate more specific routes that should be tried first
  private const int SpecificityLiteralSegment = 15;
  private const int SpecificityPositionalParameter = 2;
  private const int SpecificityOption = 10;
  private const int SpecificityOptionParameter = 5;
  private const int SpecificityCatchAllPenalty = -20;

  private readonly ILogger<Compiler> Logger;
  private readonly List<RouteMatcher> Segments = [];
  private readonly List<string> RequiredOptionPatterns = [];
  private readonly List<OptionMatcher> OptionMatchers = [];
  private string? CatchAllParameterName;
  private int Specificity;

  public Compiler() : this(null) { }

  public Compiler(ILogger<Compiler>? logger = null)
  {
    Logger = logger ?? NullLogger<Compiler>.Instance;
  }

  /// <summary>
  /// Compiles a route pattern syntax tree to a CompiledRoute.
  /// </summary>
  /// <param name="syntax">The syntax tree to compile.</param>
  /// <returns>A CompiledRoute compatible with the existing system.</returns>
  public CompiledRoute Compile(Syntax syntax)
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
    Specificity += SpecificityLiteralSegment;
    return null;
  }

  public override object? VisitParameter(ParameterSyntax parameter)
  {
    if (parameter.IsCatchAll)
    {
      CatchAllParameterName = parameter.Name;
      Specificity += SpecificityCatchAllPenalty;
    }
    else
    {
      Specificity += SpecificityPositionalParameter;
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

    // Prefer long form as primary pattern, short form as fallback
    string optionSyntax =
      optionLongSyntax
      ?? optionShortSyntax
      ?? throw new ParseException("Option must have a syntax");

    // Alternate form is short form when both exist
    string? alternateForm =
      (optionLongSyntax is not null && optionShortSyntax is not null)
      ? optionShortSyntax
      : null;

    RequiredOptionPatterns.Add(optionSyntax);
    Specificity += SpecificityOption;

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
        Specificity += SpecificityOptionParameter;
    }

    // Create option matcher
    var optionMatcher =
      new OptionMatcher
      (
        matchPattern: optionSyntax,
        expectsValue: expectsValue,
        parameterName: valueParameterName,
        alternateForm: alternateForm,
        description: option.Description,
        isOptional: option.IsOptional || !expectsValue,  // Boolean flags are always optional
        isRepeated: option.Parameter?.IsRepeated ?? false,
        parameterIsOptional: option.Parameter?.IsOptional ?? false
      );

    OptionMatchers.Add(optionMatcher);
    return null;
  }
}
