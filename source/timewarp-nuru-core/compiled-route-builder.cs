namespace TimeWarp.Nuru;

/// <summary>
/// Fluent builder for constructing <see cref="CompiledRoute"/> instances.
/// This provides a programmatic alternative to string-based route patterns.
/// </summary>
/// <remarks>
/// The builder produces routes identical to those created by <see cref="PatternParser.Parse"/>.
/// It uses the same specificity scoring constants as the pattern compiler.
/// </remarks>
internal sealed class CompiledRouteBuilder
{
  private readonly List<RouteMatcher> _segments = [];
  private string? _catchAllParameterName;
  private int _specificity;

  // Specificity scoring constants - must match Compiler exactly
  // See: documentation/developer/design/resolver/specificity-algorithm.md
  private const int SpecificityLiteralSegment = 100;
  private const int SpecificityRequiredOption = 50;
  private const int SpecificityOptionalOption = 25;
  private const int SpecificityTypedParameter = 20;
  private const int SpecificityUntypedParameter = 10;
  private const int SpecificityOptionalParameter = 5;
  private const int SpecificityCatchAll = 1;

  /// <summary>
  /// Adds a literal segment (e.g., "git", "commit").
  /// </summary>
  /// <param name="value">The literal value that must be matched exactly.</param>
  /// <returns>This builder for method chaining.</returns>
  public CompiledRouteBuilder WithLiteral(string value)
  {
    ArgumentException.ThrowIfNullOrWhiteSpace(value);
    _segments.Add(new LiteralMatcher(value));
    _specificity += SpecificityLiteralSegment;
    return this;
  }

  /// <summary>
  /// Adds a required positional parameter.
  /// </summary>
  /// <param name="name">The parameter name (e.g., "name" for {name}).</param>
  /// <param name="type">Optional type constraint (e.g., "int" for {id:int}).</param>
  /// <param name="description">Optional description for help text.</param>
  /// <returns>This builder for method chaining.</returns>
  public CompiledRouteBuilder WithParameter(
    string name,
    string? type = null,
    string? description = null)
  {
    ArgumentException.ThrowIfNullOrWhiteSpace(name);
    _segments.Add(new ParameterMatcher(name, isCatchAll: false, type, description, isOptional: false));
    _specificity += string.IsNullOrEmpty(type) ? SpecificityUntypedParameter : SpecificityTypedParameter;
    return this;
  }

  /// <summary>
  /// Adds an optional positional parameter.
  /// </summary>
  /// <param name="name">The parameter name (e.g., "name" for {name?}).</param>
  /// <param name="type">Optional type constraint (e.g., "int" for {id?:int}).</param>
  /// <param name="description">Optional description for help text.</param>
  /// <returns>This builder for method chaining.</returns>
  public CompiledRouteBuilder WithOptionalParameter(
    string name,
    string? type = null,
    string? description = null)
  {
    ArgumentException.ThrowIfNullOrWhiteSpace(name);
    _segments.Add(new ParameterMatcher(name, isCatchAll: false, type, description, isOptional: true));
    _specificity += SpecificityOptionalParameter;
    return this;
  }

  /// <summary>
  /// Adds an option (flag or option with value).
  /// </summary>
  /// <param name="longForm">Long form without dashes (e.g., "force" for --force).</param>
  /// <param name="shortForm">Optional short form without dash (e.g., "f" for -f).</param>
  /// <param name="parameterName">Parameter name for the option value (null for boolean flags).</param>
  /// <param name="expectsValue">True if the option expects a value argument.</param>
  /// <param name="parameterType">Type constraint for the value (e.g., "int" for --port {port:int}).</param>
  /// <param name="parameterIsOptional">True if the option value is optional (e.g., --config {file?}).</param>
  /// <param name="description">Optional description for help text.</param>
  /// <param name="isOptionalFlag">True if the option flag itself is optional (affects specificity scoring only).</param>
  /// <param name="isRepeated">True if the option can be specified multiple times.</param>
  /// <returns>This builder for method chaining.</returns>
  /// <remarks>
  /// <para>
  /// The <paramref name="isOptionalFlag"/> parameter controls specificity scoring only.
  /// Boolean flags and repeated options are always optional at runtime regardless of this setting.
  /// </para>
  /// <para>
  /// For boolean flags (expectsValue=false), the parameter name is automatically derived
  /// from the long form using camelCase conversion (e.g., "dry-run" becomes "dryRun").
  /// </para>
  /// </remarks>
  public CompiledRouteBuilder WithOption(
    string longForm,
    string? shortForm = null,
    string? parameterName = null,
    bool expectsValue = false,
    string? parameterType = null,
    bool parameterIsOptional = false,
    string? description = null,
    bool isOptionalFlag = false,
    bool isRepeated = false)
  {
    ArgumentException.ThrowIfNullOrWhiteSpace(longForm);

    string matchPattern = $"--{longForm}";
    string? alternateForm = shortForm is not null ? $"-{shortForm}" : null;

    // For boolean flags, derive parameter name from long form (matches Compiler behavior)
    string? resolvedParamName = parameterName;
    if (resolvedParamName is null && !expectsValue)
    {
      resolvedParamName = ToCamelCase(longForm);
    }

    // Determine runtime optionality: boolean flags and repeated options are always optional
    bool isOptionalAtRuntime = isOptionalFlag || !expectsValue || isRepeated;

    _segments.Add(new OptionMatcher(
      matchPattern: matchPattern,
      expectsValue: expectsValue,
      parameterName: resolvedParamName,
      alternateForm: alternateForm,
      description: description,
      isOptional: isOptionalAtRuntime,
      isRepeated: isRepeated,
      parameterIsOptional: parameterIsOptional
    ));

    // Score the option flag itself (only explicitly optional flags get lower specificity)
    _specificity += isOptionalFlag ? SpecificityOptionalOption : SpecificityRequiredOption;

    // Score the option's parameter value if present
    if (expectsValue)
    {
      if (parameterIsOptional)
      {
        _specificity += SpecificityOptionalParameter;
      }
      else if (!string.IsNullOrEmpty(parameterType))
      {
        _specificity += SpecificityTypedParameter;
      }
      else
      {
        _specificity += SpecificityUntypedParameter;
      }
    }

    return this;
  }

  /// <summary>
  /// Adds a catch-all parameter that captures all remaining arguments.
  /// </summary>
  /// <param name="name">The parameter name (e.g., "args" for {*args}).</param>
  /// <param name="type">Optional type constraint (e.g., "string[]" for {*args:string[]}).</param>
  /// <param name="description">Optional description for help text.</param>
  /// <returns>This builder for method chaining.</returns>
  /// <exception cref="InvalidOperationException">Thrown if a catch-all has already been added.</exception>
  /// <remarks>
  /// Only one catch-all parameter is allowed per route. It should typically be the last segment.
  /// </remarks>
  public CompiledRouteBuilder WithCatchAll(
    string name,
    string? type = null,
    string? description = null)
  {
    ArgumentException.ThrowIfNullOrWhiteSpace(name);

    if (_catchAllParameterName is not null)
    {
      throw new InvalidOperationException("Only one catch-all parameter is allowed per route.");
    }

    _catchAllParameterName = name;
    _segments.Add(new ParameterMatcher(name, isCatchAll: true, type, description, isOptional: false));
    _specificity += SpecificityCatchAll;
    return this;
  }

  /// <summary>
  /// Builds the <see cref="CompiledRoute"/> from the configured segments.
  /// </summary>
  /// <returns>A new <see cref="CompiledRoute"/> instance.</returns>
  public CompiledRoute Build()
  {
    return new CompiledRoute
    {
      Segments = _segments.ToArray(),
      CatchAllParameterName = _catchAllParameterName,
      Specificity = _specificity
    };
  }

  /// <summary>
  /// Converts a string to camelCase by removing dashes/underscores and lowercasing the first character.
  /// </summary>
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
