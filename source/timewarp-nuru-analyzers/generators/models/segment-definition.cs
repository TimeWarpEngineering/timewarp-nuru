namespace TimeWarp.Nuru.Generators;

/// <summary>
/// Abstract base for all route segment types.
/// Segments are the parsed components of a route pattern.
/// </summary>
/// <param name="Position">Zero-based position of this segment in the route</param>
public abstract record SegmentDefinition(int Position)
{
  /// <summary>
  /// Gets the specificity contribution of this segment for route matching.
  /// Higher values mean more specific matches.
  /// </summary>
  public abstract int SpecificityContribution { get; }
}

/// <summary>
/// Represents a literal (fixed text) segment in a route pattern.
/// Example: "deploy" in "deploy {env}"
/// </summary>
/// <param name="Position">Zero-based position of this segment in the route</param>
/// <param name="Value">The literal text value</param>
public sealed record LiteralDefinition(int Position, string Value)
  : SegmentDefinition(Position)
{
  /// <summary>
  /// Literals have the highest specificity (1000 per segment).
  /// </summary>
  public override int SpecificityContribution => 1000;

  /// <summary>
  /// Gets the literal value normalized to lowercase for matching.
  /// </summary>
  public string NormalizedValue => Value.ToLowerInvariant();
}

/// <summary>
/// Represents a parameter (variable) segment in a route pattern.
/// Examples: "{env}", "{count:int}", "[optional]", "{*catchAll}"
/// </summary>
/// <param name="Position">Zero-based position of this segment in the route</param>
/// <param name="Name">The parameter name</param>
/// <param name="TypeConstraint">Optional type constraint (e.g., "int", "guid")</param>
/// <param name="Description">Optional description for help text</param>
/// <param name="IsOptional">Whether the parameter is optional (wrapped in [])</param>
/// <param name="IsCatchAll">Whether this is a catch-all parameter (*)</param>
/// <param name="ResolvedClrTypeName">The resolved CLR type name (e.g., "global::System.Int32")</param>
/// <param name="DefaultValue">Default value expression, if any</param>
public sealed record ParameterDefinition(
  int Position,
  string Name,
  string? TypeConstraint,
  string? Description,
  bool IsOptional,
  bool IsCatchAll,
  string? ResolvedClrTypeName,
  string? DefaultValue = null)
  : SegmentDefinition(Position)
{
  /// <summary>
  /// Parameters have medium specificity.
  /// Required parameters (100) are more specific than optional (50).
  /// Catch-all parameters have lowest specificity (10).
  /// </summary>
  public override int SpecificityContribution =>
    IsCatchAll ? 10 : (IsOptional ? 50 : 100);

  /// <summary>
  /// Gets the parameter name in camelCase for code generation.
  /// </summary>
  public string CamelCaseName =>
    string.IsNullOrEmpty(Name) ? Name : char.ToLowerInvariant(Name[0]) + Name[1..];

  /// <summary>
  /// Gets whether this parameter has a type constraint.
  /// </summary>
  public bool HasTypeConstraint => !string.IsNullOrEmpty(TypeConstraint);

  /// <summary>
  /// Gets the pattern syntax for this parameter.
  /// </summary>
  public string PatternSyntax
  {
    get
    {
      string typeSpec = HasTypeConstraint ? $":{TypeConstraint}" : "";
      if (IsCatchAll)
        return $"{{*{Name}{typeSpec}}}";
      if (IsOptional)
        return $"[{Name}{typeSpec}]";
      return $"{{{Name}{typeSpec}}}";
    }
  }
}

/// <summary>
/// Represents an option/flag segment in a route pattern.
/// Examples: "--verbose", "-v", "--output {path}", "--format {fmt?}"
/// </summary>
/// <param name="Position">Zero-based position of this segment in the route</param>
/// <param name="LongForm">The long form name (without --), or null for short-only options</param>
/// <param name="ShortForm">The short form name (without -), if any</param>
/// <param name="ParameterName">Name of the value parameter, if this option takes a value</param>
/// <param name="TypeConstraint">Optional type constraint for the value</param>
/// <param name="Description">Optional description for help text</param>
/// <param name="ExpectsValue">Whether this option expects a value (vs being a flag)</param>
/// <param name="IsOptional">Whether the option itself is optional</param>
/// <param name="IsRepeated">Whether the option can be specified multiple times</param>
/// <param name="ParameterIsOptional">Whether the option's value parameter is optional</param>
/// <param name="ResolvedClrTypeName">The resolved CLR type name for the value</param>
/// <param name="DefaultValueLiteral">The literal representation of the property's default value, if any (e.g., "1", "\"default\"")</param>
public sealed record OptionDefinition(
  int Position,
  string? LongForm,
  string? ShortForm,
  string? ParameterName,
  string? TypeConstraint,
  string? Description,
  bool ExpectsValue,
  bool IsOptional,
  bool IsRepeated,
  bool ParameterIsOptional,
  string? ResolvedClrTypeName,
  string? DefaultValueLiteral = null)
  : SegmentDefinition(Position)
{
  /// <summary>
  /// Options have lower specificity than literals but higher than catch-all.
  /// Required options (75) are more specific than optional (25).
  /// </summary>
  public override int SpecificityContribution =>
    IsOptional ? 25 : 75;

  /// <summary>
  /// Gets whether this option is a boolean flag (no value).
  /// </summary>
  public bool IsFlag => !ExpectsValue;

  /// <summary>
  /// Gets the long form with prefix (e.g., "--verbose"), or null if no long form.
  /// </summary>
  public string? LongFormWithPrefix => LongForm is not null ? $"--{LongForm}" : null;

  /// <summary>
  /// Gets the short form with prefix (e.g., "-v"), or null if no short form.
  /// </summary>
  public string? ShortFormWithPrefix => ShortForm is not null ? $"-{ShortForm}" : null;

  /// <summary>
  /// Gets all valid forms for this option.
  /// </summary>
  public IEnumerable<string> AllForms
  {
    get
    {
      if (LongFormWithPrefix is not null)
        yield return LongFormWithPrefix;
      if (ShortFormWithPrefix is not null)
        yield return ShortFormWithPrefix;
    }
  }

  /// <summary>
  /// Gets the pattern syntax for this option.
  /// </summary>
  public string PatternSyntax
  {
    get
    {
      string forms = ShortForm is not null
        ? $"--{LongForm},-{ShortForm}"
        : $"--{LongForm}";

      if (!ExpectsValue)
        return IsOptional ? $"{forms}?" : forms;

      string paramPart = ParameterIsOptional
        ? $"{{{ParameterName}?}}"
        : $"{{{ParameterName}}}";

      return $"{forms} {paramPart}";
    }
  }
}
