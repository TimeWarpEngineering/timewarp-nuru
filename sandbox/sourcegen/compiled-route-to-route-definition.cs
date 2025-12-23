// sandbox/sourcegen/compiled-route-to-route-definition.cs
// Helper to convert CompiledRoute (from existing parser) to RouteDefinition (design-time model)
// This approach uses the public API, avoiding internal Syntax types.
//
// Agent: Amina
// Task: #242-step-3

namespace TimeWarp.Nuru.SourceGen;

using System.Collections.Immutable;
using TimeWarp.Nuru; // For CompiledRoute, LiteralMatcher, ParameterMatcher, OptionMatcher

/// <summary>
/// Converts CompiledRoute from the existing pattern parser into RouteDefinition (design-time model).
/// Uses only public API - no internal types required.
/// </summary>
public static class CompiledRouteToRouteDefinition
{
  /// <summary>
  /// Converts a CompiledRoute object to a RouteDefinition.
  /// </summary>
  /// <param name="compiledRoute">The compiled route from PatternParser.</param>
  /// <param name="originalPattern">The original pattern string.</param>
  /// <param name="handler">The handler definition (from delegate analysis).</param>
  /// <param name="description">Optional route description.</param>
  /// <returns>A RouteDefinition representing the route.</returns>
  public static RouteDefinition Convert
  (
    CompiledRoute compiledRoute,
    string originalPattern,
    HandlerDefinition handler,
    string? description = null
  )
  {
    ImmutableArray<SegmentDefinition> segments = ConvertSegments(compiledRoute.Segments);

    // Map MessageType enum to string
    string messageType = compiledRoute.MessageType.ToString();

    return RouteDefinition.Create
    (
      originalPattern: originalPattern,
      segments: segments,
      handler: handler,
      messageType: messageType,
      description: description
    );
  }

  /// <summary>
  /// Converts a list of RouteMatchers to SegmentDefinition array.
  /// </summary>
  public static ImmutableArray<SegmentDefinition> ConvertSegments(IReadOnlyList<RouteMatcher> matchers)
  {
    ImmutableArray<SegmentDefinition>.Builder builder = ImmutableArray.CreateBuilder<SegmentDefinition>(matchers.Count);

    for (int i = 0; i < matchers.Count; i++)
    {
      RouteMatcher matcher = matchers[i];
      SegmentDefinition definition = ConvertMatcher(matcher, i);
      builder.Add(definition);
    }

    return builder.ToImmutable();
  }

  /// <summary>
  /// Converts a single RouteMatcher to SegmentDefinition.
  /// </summary>
  public static SegmentDefinition ConvertMatcher(RouteMatcher matcher, int position)
  {
    return matcher switch
    {
      LiteralMatcher literal => new LiteralDefinition
      (
        Position: position,
        Value: literal.Value
      ),

      ParameterMatcher param => new ParameterDefinition
      (
        Position: position,
        Name: param.Name,
        TypeConstraint: param.Constraint,
        Description: param.Description,
        IsOptional: param.IsOptional,
        IsCatchAll: param.IsCatchAll,
        ResolvedClrTypeName: ResolveClrType(param.Constraint),
        DefaultValue: null
      ),

      OptionMatcher option => new OptionDefinition
      (
        Position: position,
        LongForm: option.MatchPattern,
        ShortForm: option.AlternateForm,
        ParameterName: option.ParameterName,
        TypeConstraint: null, // OptionMatcher doesn't expose the original type constraint
        Description: option.Description,
        ExpectsValue: option.ExpectsValue,
        IsOptional: option.IsOptional,
        IsRepeated: option.IsRepeated,
        ParameterIsOptional: option.ParameterIsOptional,
        ResolvedClrTypeName: option.ExpectsValue ? "global::System.String" : "global::System.Boolean"
      ),

      _ => throw new NotSupportedException($"Unknown matcher type: {matcher.GetType().Name}")
    };
  }

  /// <summary>
  /// Resolves a type constraint string to a CLR type name.
  /// </summary>
  public static string? ResolveClrType(string? typeConstraint)
  {
    if (typeConstraint is null)
    {
      return "global::System.String"; // Default to string
    }

    return typeConstraint.ToLowerInvariant() switch
    {
      "int" => "global::System.Int32",
      "long" => "global::System.Int64",
      "short" => "global::System.Int16",
      "byte" => "global::System.Byte",
      "float" => "global::System.Single",
      "double" => "global::System.Double",
      "decimal" => "global::System.Decimal",
      "bool" => "global::System.Boolean",
      "char" => "global::System.Char",
      "string" => "global::System.String",
      "guid" => "global::System.Guid",
      "datetime" => "global::System.DateTime",
      "datetimeoffset" => "global::System.DateTimeOffset",
      "timespan" => "global::System.TimeSpan",
      "uri" => "global::System.Uri",
      _ => $"global::{typeConstraint}" // Custom type - assume fully qualified
    };
  }

  /// <summary>
  /// Convenience method: Parse pattern string and convert to RouteDefinition.
  /// </summary>
  /// <param name="pattern">The route pattern string.</param>
  /// <param name="handler">The handler definition.</param>
  /// <param name="description">Optional route description.</param>
  /// <returns>A RouteDefinition representing the route.</returns>
  public static RouteDefinition FromPattern(string pattern, HandlerDefinition handler, string? description = null)
  {
    CompiledRoute compiledRoute = PatternParser.Parse(pattern);
    return Convert(compiledRoute, pattern, handler, description);
  }
}
