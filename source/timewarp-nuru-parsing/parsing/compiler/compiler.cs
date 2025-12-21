namespace TimeWarp.Nuru;

/// <summary>
/// Compiler that converts route pattern syntax tree to the runtime CompiledRoute structure.
/// This maintains backward compatibility with the current system.
/// </summary>
internal sealed class Compiler : SyntaxVisitor<object?>
{
  // Specificity scoring constants
  // Higher values indicate more specific routes that should be tried first
  // See: documentation/developer/design/resolver/specificity-algorithm.md
  private const int SpecificityLiteralSegment = 100;
  private const int SpecificityRequiredOption = 50;
  private const int SpecificityOptionalOption = 25;
  private const int SpecificityTypedParameter = 20;
  private const int SpecificityUntypedParameter = 10;
  private const int SpecificityOptionalParameter = 5;
  private const int SpecificityCatchAll = 1;

#if !ANALYZER_BUILD
  private readonly ILogger<Compiler> Logger;
#endif
  private readonly List<RouteMatcher> Segments = [];
  private string? CatchAllParameterName;
  private int Specificity;

#if !ANALYZER_BUILD
  public Compiler() : this(null) { }

  public Compiler(ILogger<Compiler>? logger = null)
  {
    Logger = logger ?? NullLogger<Compiler>.Instance;
  }
#else
  public Compiler() { }
#endif

  /// <summary>
  /// Compiles a route pattern syntax tree to a CompiledRoute.
  /// </summary>
  /// <param name="syntax">The syntax tree to compile.</param>
  /// <returns>A CompiledRoute compatible with the existing system.</returns>
  public CompiledRoute Compile(Syntax syntax)
  {
    // Reset state
    Segments.Clear();
    CatchAllParameterName = null;
    Specificity = 0;

    // Visit all segments
    VisitPattern(syntax);

    // Build the CompiledRoute
    return new CompiledRoute
    {
      Segments = Segments.ToArray(),
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
      Specificity += SpecificityCatchAll;
    }
    else if (parameter.IsOptional)
    {
      Specificity += SpecificityOptionalParameter;
    }
    else if (!string.IsNullOrEmpty(parameter.Type))
    {
      Specificity += SpecificityTypedParameter;
    }
    else
    {
      Specificity += SpecificityUntypedParameter;
    }

    // Create parameter matcher
    ParameterMatcher parameterMatcher = new(
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

    // Determine if this option expects a value
    bool expectsValue = option.Parameter is not null;
    string? valueParameterName = option.Parameter?.Name;

    // For boolean options (no parameter), use the option name as the parameter name
    if (valueParameterName is null && !expectsValue)
    {
      // Extract the option name from the syntax (remove leading dashes) and convert to camelCase
      valueParameterName = ToCamelCase(option.LongForm ?? option.ShortForm ?? "");
#if !ANALYZER_BUILD
      ParsingLoggerMessages.SettingBooleanOptionParameter(Logger, valueParameterName!, null);
#endif
    }

    // Determine if option is optional (runtime behavior)
    bool isRepeated = option.Parameter?.IsRepeated ?? false;
    bool isOptional = option.IsOptional || !expectsValue || isRepeated;  // Boolean flags and repeated options are always optional

    // Score the option flag itself (specificity scoring)
    // Only explicitly optional flags (with ?) get lower specificity
    // Boolean flags and repeated options are optional at runtime but still score as required for specificity
    bool isOptionalForSpecificity = option.IsOptional;
    Specificity += isOptionalForSpecificity ? SpecificityOptionalOption : SpecificityRequiredOption;

    // Score the option's parameter value if present
    if (option.Parameter is not null)
    {
      if (option.Parameter.IsCatchAll)
      {
        CatchAllParameterName = option.Parameter.Name;
        Specificity += SpecificityCatchAll;
      }
      else if (option.Parameter.IsOptional)
      {
        Specificity += SpecificityOptionalParameter;
      }
      else if (!string.IsNullOrEmpty(option.Parameter.Type))
      {
        Specificity += SpecificityTypedParameter;
      }
      else
      {
        Specificity += SpecificityUntypedParameter;
      }
    }

    // Create option matcher and add to segments in position
    OptionMatcher optionMatcher =
      new(
        matchPattern: optionSyntax,
        expectsValue: expectsValue,
        parameterName: valueParameterName,
        alternateForm: alternateForm,
        description: option.Description,
        isOptional: isOptional,
        isRepeated: isRepeated,
        parameterIsOptional: option.Parameter?.IsOptional ?? false
      );

    Segments.Add(optionMatcher);
    return null;
  }

  private static string ToCamelCase(string input)
  {
    if (string.IsNullOrEmpty(input))
      return input;

    string cleaned = input.Replace("-", "", StringComparison.Ordinal).Replace("_", "", StringComparison.Ordinal);
    if (cleaned.Length == 0)
      return input;

    return char.ToLowerInvariant(cleaned[0]) + cleaned[1..];
  }
}
