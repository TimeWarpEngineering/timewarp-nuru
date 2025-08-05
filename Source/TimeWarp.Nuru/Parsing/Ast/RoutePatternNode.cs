namespace TimeWarp.Nuru.Parsing.Ast;

/// <summary>
/// Base class for all AST nodes in route pattern parsing.
/// </summary>
public abstract record RoutePatternNode;
/// <summary>
/// Root node representing a complete route pattern.
/// </summary>
/// <param name="Segments">The segments that make up this route pattern.</param>
public record RoutePatternAst(IReadOnlyList<SegmentNode> Segments) : RoutePatternNode;

/// <summary>
/// Base class for route segments (literals, parameters, options).
/// </summary>
public abstract record SegmentNode : RoutePatternNode
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
public record LiteralNode(string Value) : SegmentNode
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
public record ParameterNode(
  string Name,
  bool IsCatchAll = false,
  bool IsOptional = false,
  string? Type = null,
  string? Description = null) : SegmentNode
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
/// Examples: --verbose, -v, --config {mode}
/// </summary>
/// <param name="LongName">The long form option name (without --).</param>
/// <param name="ShortName">The short form option name (without -), if specified.</param>
/// <param name="Description">Option description from pipe syntax, if specified.</param>
/// <param name="Parameter">Associated parameter for options that take values.</param>
public record OptionNode(
  string? LongName = null,
  string? ShortName = null,
  string? Description = null,
  ParameterNode? Parameter = null) : SegmentNode
{
  public override string ToString()
  {
    var sb = new System.Text.StringBuilder();
    sb.Append("Option:");
    if (LongName is not null) sb.Append(" longName='").Append(LongName).Append('\'');
    if (ShortName is not null) sb.Append(" shortName='").Append(ShortName).Append('\'');
    if (Parameter is not null) sb.Append(" hasParam=true");
    if (Description is not null) sb.Append(" desc='").Append(Description).Append('\'');
    return sb.ToString();
  }
}

