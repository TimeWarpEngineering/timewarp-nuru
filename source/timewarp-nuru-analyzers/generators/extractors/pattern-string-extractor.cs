// Extracts route segments from pattern strings using the existing PatternParser.
//
// Integrates with timewarp-nuru-parsing project:
// - Parses mini-language pattern strings like "deploy {env|Target} --force,-f|Skip"
// - Converts Syntax nodes (LiteralSyntax, ParameterSyntax, OptionSyntax) to SegmentDefinitions
// - Handles both simple patterns ("status") and complex patterns with parameters/options

namespace TimeWarp.Nuru.Generators;

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
    if (string.IsNullOrWhiteSpace(pattern))
      return [];

    // Use the existing PatternParser to parse the pattern
    Parser parser = new();
    ParseResult<Syntax> result = parser.Parse(pattern);

    if (!result.Success || result.Value is null)
    {
      // If parsing fails, treat the whole pattern as a single literal
      return [new LiteralDefinition(0, pattern)];
    }

    // Convert Syntax segments to SegmentDefinitions
    return ConvertSegments(result.Value.Segments);
  }

  /// <summary>
  /// Converts parsed Syntax segments to SegmentDefinitions.
  /// </summary>
  private static ImmutableArray<SegmentDefinition> ConvertSegments(IReadOnlyList<SegmentSyntax> segments)
  {
    ImmutableArray<SegmentDefinition>.Builder builder = ImmutableArray.CreateBuilder<SegmentDefinition>(segments.Count);

    for (int i = 0; i < segments.Count; i++)
    {
      SegmentDefinition? converted = ConvertSegment(segments[i], i);
      if (converted is not null)
        builder.Add(converted);
    }

    return builder.ToImmutable();
  }

  /// <summary>
  /// Converts a single Syntax segment to a SegmentDefinition.
  /// </summary>
  private static SegmentDefinition? ConvertSegment(SegmentSyntax segment, int position)
  {
    return segment switch
    {
      LiteralSyntax literal => ConvertLiteral(literal, position),
      ParameterSyntax parameter => ConvertParameter(parameter, position),
      OptionSyntax option => ConvertOption(option, position),
      _ => null
    };
  }

  /// <summary>
  /// Converts a LiteralSyntax to a LiteralDefinition.
  /// </summary>
  private static LiteralDefinition ConvertLiteral(LiteralSyntax literal, int position)
  {
    return new LiteralDefinition(position, literal.Value);
  }

  /// <summary>
  /// Converts a ParameterSyntax to a ParameterDefinition.
  /// </summary>
  private static ParameterDefinition ConvertParameter(ParameterSyntax parameter, int position)
  {
    return new ParameterDefinition(
      Position: position,
      Name: parameter.Name,
      TypeConstraint: parameter.Type,
      Description: parameter.Description,
      IsOptional: parameter.IsOptional,
      IsCatchAll: parameter.IsCatchAll,
      ResolvedClrTypeName: ResolveClrTypeName(parameter.Type));
  }

  /// <summary>
  /// Converts an OptionSyntax to an OptionDefinition.
  /// </summary>
  private static OptionDefinition ConvertOption(OptionSyntax option, int position)
  {
    return new OptionDefinition(
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
      ResolvedClrTypeName: ResolveClrTypeName(option.Parameter?.Type));
  }

  /// <summary>
  /// Resolves a type constraint string to a fully qualified CLR type name.
  /// </summary>
  private static string? ResolveClrTypeName(string? typeConstraint)
  {
    if (string.IsNullOrEmpty(typeConstraint))
      return null;

    return typeConstraint.ToLowerInvariant() switch
    {
      "int" or "int32" => "global::System.Int32",
      "long" or "int64" => "global::System.Int64",
      "short" or "int16" => "global::System.Int16",
      "byte" => "global::System.Byte",
      "float" or "single" => "global::System.Single",
      "double" => "global::System.Double",
      "decimal" => "global::System.Decimal",
      "bool" or "boolean" => "global::System.Boolean",
      "char" => "global::System.Char",
      "string" => "global::System.String",
      "guid" => "global::System.Guid",
      "datetime" => "global::System.DateTime",
      "datetimeoffset" => "global::System.DateTimeOffset",
      "timespan" => "global::System.TimeSpan",
      "uri" => "global::System.Uri",
      "version" => "global::System.Version",
      _ => $"global::{typeConstraint}" // Assume it's a fully qualified type
    };
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
