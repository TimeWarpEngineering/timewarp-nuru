namespace TimeWarp.Nuru.Parsing;

/// <summary>
/// Base class for all AST nodes in route pattern parsing.
/// </summary>
public abstract record SyntaxNode;
/// <summary>
/// Root node representing a complete route pattern.
/// </summary>
/// <param name="Segments">The segments that make up this route pattern.</param>
public record RouteSyntax(IReadOnlyList<SegmentSyntax> Segments) : SyntaxNode;

/// <summary>
/// Base class for route segments (literals, parameters, options).
/// </summary>
public abstract record SegmentSyntax : SyntaxNode
{
  /// <summary>
  /// Position in the original input string where this segment starts.
  /// </summary>
  public int Position { get; init; }

  /// <summary>
  /// Length of this segment in the original input string.
  /// </summary>
  public int Length { get; init; }
}

/// <summary>
/// A literal segment that must match exactly.
/// Examples: "git", "status", "commit"
/// </summary>
/// <param name="Value">The literal text that must match.</param>
public record LiteralSyntax(string Value) : SegmentSyntax
{
  public override string ToString() => $"Literal: '{Value}'";
}

/// <summary>
/// A parameter segment that captures a value from command line arguments.
/// Examples: {name}, {count:int}, {file?}, {*args}
/// </summary>
/// <param name="Name">The parameter name.</param>
/// <param name="IsCatchAll">True if this is a catch-all parameter ({*args}).</param>
/// <param name="IsOptional">True if this parameter is optional ({name?}).</param>
/// <param name="Type">The type constraint (e.g., "int", "double"), if specified.</param>
/// <param name="Description">Parameter description from pipe syntax, if specified.</param>
public record ParameterSyntax
(
  string Name,
  bool IsCatchAll = false,
  bool IsOptional = false,
  string? Type = null,
  string? Description = null
) : SegmentSyntax
{
  public override string ToString()
  {
    var sb = new System.Text.StringBuilder();
    sb.Append("Parameter: name='").Append(Name).Append('\'');
    if (IsCatchAll) sb.Append(", catchAll=true");
    if (IsOptional) sb.Append(", optional=true");
    if (Type is not null) sb.Append(", type='").Append(Type).Append('\'');
    if (Description is not null) sb.Append(", desc='").Append(Description).Append('\'');
    return sb.ToString();
  }
}

/// <summary>
/// An option segment that represents a command line option.
/// Examples: --verbose, -v, --config {mode}, --force?
/// </summary>
/// <param name="LongForm">The long form option name (without --).</param>
/// <param name="ShortForm">The short form option name (without -), if specified.</param>
/// <param name="Description">Option description from pipe syntax, if specified.</param>
/// <param name="Parameter">Associated parameter for options that take values.</param>
/// <param name="IsOptional">True if this option is optional (--flag?).</param>
public record OptionSyntax
(
  string? LongForm = null,
  string? ShortForm = null,
  string? Description = null,
  ParameterSyntax? Parameter = null,
  bool IsOptional = false
) : SegmentSyntax
{
  public override string ToString()
  {
    var sb = new StringBuilder();
    sb.Append("Option:");
    if (LongForm is not null) sb.Append(" longName='").Append(LongForm).Append('\'');
    if (ShortForm is not null) sb.Append(" shortName='").Append(ShortForm).Append('\'');
    if (IsOptional) sb.Append(" optional=true");
    if (Parameter is not null) sb.Append(" hasParam=true");
    if (Description is not null) sb.Append(" desc='").Append(Description).Append('\'');
    return sb.ToString();
  }
}

