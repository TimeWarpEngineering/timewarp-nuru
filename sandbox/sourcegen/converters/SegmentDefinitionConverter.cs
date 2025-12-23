// sandbox/sourcegen/converters/SegmentDefinitionConverter.cs
// Static converter for transforming parsed segments into SegmentDefinition array.
//
// Two approaches available for evaluation:
// - FromSyntax: Uses internal Syntax types (full fidelity)
// - FromCompiledRoute: Uses public CompiledRoute types (loses option type constraints)
//
// Agent: Amina
// Task: #242-step-3

namespace TimeWarp.Nuru.SourceGen;

using System.Collections.Immutable;
using TimeWarp.Nuru;

/// <summary>
/// Static converter for transforming parsed segments into SegmentDefinition array.
/// Pure transformation - no side effects.
/// </summary>
public static class SegmentDefinitionConverter
{
  #region FromSyntax (full fidelity - uses internal types)

  /// <summary>
  /// Convert from Syntax (internal parser AST).
  /// Full fidelity - preserves all type constraints including option parameters.
  /// Requires InternalsVisibleTo in the parsing project.
  /// </summary>
  public static ImmutableArray<SegmentDefinition> FromSyntax(IReadOnlyList<SegmentSyntax> segments)
  {
    ImmutableArray<SegmentDefinition>.Builder builder = ImmutableArray.CreateBuilder<SegmentDefinition>(segments.Count);

    for (int i = 0; i < segments.Count; i++)
    {
      SegmentSyntax segment = segments[i];
      SegmentDefinition definition = ConvertSyntaxSegment(segment, i);
      builder.Add(definition);
    }

    return builder.ToImmutable();
  }

  private static SegmentDefinition ConvertSyntaxSegment(SegmentSyntax segment, int position)
  {
    return segment switch
    {
      LiteralSyntax literal => new LiteralDefinition
      (
        Position: position,
        Value: literal.Value
      ),

      ParameterSyntax param => new ParameterDefinition
      (
        Position: position,
        Name: param.Name,
        TypeConstraint: param.Type,
        Description: param.Description,
        IsOptional: param.IsOptional,
        IsCatchAll: param.IsCatchAll,
        ResolvedClrTypeName: ResolveClrType(param.Type),
        DefaultValue: null
      ),

      OptionSyntax option => new OptionDefinition
      (
        Position: position,
        LongForm: option.LongForm ?? "",
        ShortForm: option.ShortForm,
        ParameterName: option.Parameter?.Name,
        TypeConstraint: option.Parameter?.Type,  // Full fidelity - type preserved!
        Description: option.Description,
        ExpectsValue: option.Parameter is not null,
        IsOptional: option.IsOptional,
        IsRepeated: option.Parameter?.IsRepeated ?? false,
        ParameterIsOptional: option.Parameter?.IsOptional ?? false,
        ResolvedClrTypeName: option.Parameter is not null
          ? ResolveClrType(option.Parameter.Type)
          : "global::System.Boolean"
      ),

      _ => throw new NotSupportedException($"Unknown segment type: {segment.GetType().Name}")
    };
  }

  #endregion

  #region FromCompiledRoute (public API only - loses option type constraints)

  /// <summary>
  /// Convert from CompiledRoute (public runtime type).
  /// NOTE: Loses option parameter type constraints - OptionMatcher doesn't expose them.
  /// Keeping for comparison. May remove after evaluation.
  /// </summary>
  public static ImmutableArray<SegmentDefinition> FromCompiledRoute(IReadOnlyList<RouteMatcher> matchers)
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

  private static SegmentDefinition ConvertMatcher(RouteMatcher matcher, int position)
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
        TypeConstraint: null,  // GAP: OptionMatcher doesn't expose the original type constraint
        Description: option.Description,
        ExpectsValue: option.ExpectsValue,
        IsOptional: option.IsOptional,
        IsRepeated: option.IsRepeated,
        ParameterIsOptional: option.ParameterIsOptional,
        ResolvedClrTypeName: option.ExpectsValue
          ? "global::System.String"  // GAP: Defaults to string since type unknown
          : "global::System.Boolean"
      ),

      _ => throw new NotSupportedException($"Unknown matcher type: {matcher.GetType().Name}")
    };
  }

  #endregion

  #region Shared Helpers

  /// <summary>
  /// Resolves a type constraint string to a fully-qualified CLR type name.
  /// Handles nullable types (e.g., "int?" -> "global::System.Int32?").
  /// </summary>
  public static string ResolveClrType(string? typeConstraint)
  {
    if (typeConstraint is null)
    {
      return "global::System.String"; // Default to string
    }

    // Check for nullable suffix
    bool isNullable = typeConstraint.EndsWith('?');
    string baseType = isNullable ? typeConstraint[..^1] : typeConstraint;

    string resolvedBase = baseType.ToLowerInvariant() switch
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
      _ => $"global::{baseType}" // Custom type - assume fully qualified
    };

    return isNullable ? $"{resolvedBase}?" : resolvedBase;
  }

  #endregion
}
