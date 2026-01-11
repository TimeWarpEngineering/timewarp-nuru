// Extracts route segments from pattern strings using the existing PatternParser.
//
// Integrates with timewarp-nuru-parsing project:
// - Parses mini-language pattern strings like "deploy {env|Target} --force,-f|Skip"
// - Converts Syntax nodes (LiteralSyntax, ParameterSyntax, OptionSyntax) to SegmentDefinitions
// - Handles both simple patterns ("status") and complex patterns with parameters/options

namespace TimeWarp.Nuru.Generators;

/// <summary>
/// Result of extracting segments from a pattern string.
/// Contains both the parsed segments and any parse/semantic errors encountered.
/// </summary>
/// <param name="Segments">The extracted segment definitions.</param>
/// <param name="ParseErrors">Any parse errors encountered.</param>
/// <param name="SemanticErrors">Any semantic errors encountered.</param>
internal sealed record PatternParseResult(
  ImmutableArray<SegmentDefinition> Segments,
  IReadOnlyList<ParseError>? ParseErrors,
  IReadOnlyList<SemanticError>? SemanticErrors)
{
  /// <summary>
  /// Whether parsing was successful (no errors).
  /// </summary>
  public bool Success => (ParseErrors is null || ParseErrors.Count == 0) &&
                         (SemanticErrors is null || SemanticErrors.Count == 0);

  /// <summary>
  /// Creates a successful result with no errors.
  /// </summary>
  public static PatternParseResult Ok(ImmutableArray<SegmentDefinition> segments) =>
    new(segments, null, null);

  /// <summary>
  /// Creates a failed result with errors.
  /// </summary>
  public static PatternParseResult Failed(
    ImmutableArray<SegmentDefinition> segments,
    IReadOnlyList<ParseError>? parseErrors,
    IReadOnlyList<SemanticError>? semanticErrors) =>
    new(segments, parseErrors, semanticErrors);
}

/// <summary>
/// Extracts route segments from pattern strings using the PatternParser.
/// </summary>
internal static class PatternStringExtractor
{
  /// <summary>
  /// Extracts segments from a pattern string.
  /// </summary>
  /// <param name="pattern">The route pattern string.</param>
  /// <returns>An array of segment definitions.</returns>
  public static ImmutableArray<SegmentDefinition> ExtractSegments(string pattern)
  {
    PatternParseResult result = ExtractSegmentsWithErrors(pattern);
    return result.Segments;
  }

  /// <summary>
  /// Extracts segments from a pattern string, returning any parse errors.
  /// </summary>
  /// <param name="pattern">The route pattern string.</param>
  /// <returns>A result containing segments and any errors encountered.</returns>
  public static PatternParseResult ExtractSegmentsWithErrors(string pattern)
  {
    if (string.IsNullOrWhiteSpace(pattern))
      return PatternParseResult.Ok([]);

    // Use PatternParser.TryParse to get error information
    bool success = PatternParser.TryParse(
      pattern,
      out _,
      out IReadOnlyList<ParseError>? parseErrors,
      out IReadOnlyList<SemanticError>? semanticErrors);

    if (!success)
    {
      // Return errors along with a fallback literal segment
      return PatternParseResult.Failed(
        [new LiteralDefinition(0, pattern)],
        parseErrors,
        semanticErrors);
    }

    // Use the Parser directly to get the Syntax tree which has full type info
    Parser parser = new();
    ParseResult<Syntax> result = parser.Parse(pattern);

    if (!result.Success || result.Value is null)
    {
      // Shouldn't happen if TryParse succeeded, but handle it anyway
      return PatternParseResult.Failed(
        [new LiteralDefinition(0, pattern)],
        parseErrors,
        semanticErrors);
    }

    // Convert Syntax segments to SegmentDefinitions (preserves type constraints)
    ImmutableArray<SegmentDefinition> segments = ConvertSyntaxSegments(result.Value.Segments);
    return PatternParseResult.Ok(segments);
  }

  /// <summary>
  /// Converts parsed Syntax segments to SegmentDefinitions.
  /// </summary>
  private static ImmutableArray<SegmentDefinition> ConvertSyntaxSegments(IReadOnlyList<SegmentSyntax> segments)
  {
    ImmutableArray<SegmentDefinition>.Builder builder = ImmutableArray.CreateBuilder<SegmentDefinition>(segments.Count);

    for (int i = 0; i < segments.Count; i++)
    {
      SegmentDefinition? converted = ConvertSyntaxSegment(segments[i], i);
      if (converted is not null)
        builder.Add(converted);
    }

    return builder.ToImmutable();
  }

  /// <summary>
  /// Converts a single Syntax segment to a SegmentDefinition.
  /// </summary>
  private static SegmentDefinition? ConvertSyntaxSegment(SegmentSyntax segment, int position)
  {
    return segment switch
    {
      LiteralSyntax literal => new LiteralDefinition(position, literal.Value),
      ParameterSyntax parameter => new ParameterDefinition(
        Position: position,
        Name: parameter.Name,
        TypeConstraint: parameter.Type,
        Description: parameter.Description,
        IsOptional: parameter.IsOptional,
        IsCatchAll: parameter.IsCatchAll,
        ResolvedClrTypeName: ResolveClrTypeName(parameter.Type)),
      OptionSyntax option => new OptionDefinition(
        Position: position,
        LongForm: option.LongForm,
        ShortForm: option.ShortForm,
        ParameterName: option.Parameter?.Name,
        TypeConstraint: option.Parameter?.Type,
        Description: option.Description,
        ExpectsValue: option.Parameter is not null,
        IsOptional: option.IsOptional,
        IsRepeated: option.Parameter?.IsRepeated ?? false,
        ParameterIsOptional: option.Parameter?.IsOptional ?? false,
        ResolvedClrTypeName: ResolveClrTypeName(option.Parameter?.Type)),
      _ => null
    };
  }

  /// <summary>
  /// Resolves a type constraint string to a fully qualified CLR type name.
  /// Uses the shared TypeConversionMap for built-in types.
  /// </summary>
  private static string? ResolveClrTypeName(string? typeConstraint)
  {
    if (string.IsNullOrEmpty(typeConstraint))
      return null;

    // Handle nullable types (e.g., "int?", "double?")
    bool isNullable = typeConstraint.EndsWith('?');
    string baseType = isNullable ? typeConstraint[..^1] : typeConstraint;

    // Use shared type conversion map for built-in types
    string? resolvedBase = TypeConversionMap.GetClrTypeName(baseType);

    // Fallback for unknown types (custom converters, etc.)
    resolvedBase ??= $"global::{baseType}";

    return isNullable ? $"{resolvedBase}?" : resolvedBase;
  }

  /// <summary>
  /// Extracts parameter bindings from parsed segments, matching against handler parameters.
  /// </summary>
  /// <param name="segments">The parsed route segments.</param>
  /// <param name="handlerParameters">Parameters from the handler lambda/method.</param>
  /// <returns>Parameter bindings for the handler.</returns>
  public static ImmutableArray<ParameterBinding> BuildBindings
  (
    ImmutableArray<SegmentDefinition> segments,
    ImmutableArray<(string Name, string TypeName, bool IsOptional)> handlerParameters
  )
  {
    ImmutableArray<ParameterBinding>.Builder bindings = ImmutableArray.CreateBuilder<ParameterBinding>();

    foreach ((string paramName, string typeName, bool isOptional) in handlerParameters)
    {
      string paramNameLower = paramName.ToLowerInvariant();

      // Try to find a matching segment
      ParameterBinding? binding = TryBindParameter(segments, paramName, paramNameLower, typeName, isOptional);
      if (binding is not null)
        bindings.Add(binding);
    }

    return bindings.ToImmutable();
  }

  /// <summary>
  /// Tries to bind a handler parameter to a route segment.
  /// </summary>
  private static ParameterBinding? TryBindParameter
  (
    ImmutableArray<SegmentDefinition> segments,
    string paramName,
    string paramNameLower,
    string typeName,
    bool isOptional
  )
  {
    foreach (SegmentDefinition segment in segments)
    {
      switch (segment)
      {
        case ParameterDefinition param
          when string.Equals(param.Name, paramNameLower, StringComparison.OrdinalIgnoreCase):
          if (param.IsCatchAll)
          {
            return ParameterBinding.FromCatchAll(paramName, typeName, param.Name);
          }

          return ParameterBinding.FromParameter(
            parameterName: paramName,
            typeName: typeName,
            segmentName: param.Name,
            isOptional: isOptional || param.IsOptional,
            requiresConversion: typeName != "global::System.String");

        case OptionDefinition option
          when string.Equals(option.LongForm, paramNameLower, StringComparison.OrdinalIgnoreCase)
            || string.Equals(option.ShortForm, paramNameLower, StringComparison.OrdinalIgnoreCase):
          string optionName = option.LongForm ?? option.ShortForm!;
          if (option.IsFlag)
          {
            return ParameterBinding.FromFlag(paramName, optionName);
          }

          return ParameterBinding.FromOption(
            parameterName: paramName,
            typeName: typeName,
            optionName: optionName,
            isOptional: isOptional || option.IsOptional,
            isArray: option.IsRepeated,
            requiresConversion: typeName != "global::System.String");

        case OptionDefinition option
          when option.ParameterName is not null &&
               string.Equals(option.ParameterName, paramNameLower, StringComparison.OrdinalIgnoreCase):
          return ParameterBinding.FromOption(
            parameterName: paramName,
            typeName: typeName,
            optionName: option.LongForm ?? option.ShortForm!,
            isOptional: isOptional || option.ParameterIsOptional,
            isArray: option.IsRepeated,
            requiresConversion: typeName != "global::System.String");
      }
    }

    return null;
  }
}
