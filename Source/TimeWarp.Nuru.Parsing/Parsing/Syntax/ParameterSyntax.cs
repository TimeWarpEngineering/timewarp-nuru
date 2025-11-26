namespace TimeWarp.Nuru.Parsing;

/// <summary>
/// A parameter segment that captures a value from command line arguments.
/// Examples: {name}, {count:int}, {file?}, {*args}, {value}*
/// </summary>
/// <param name="Name">The parameter name.</param>
/// <param name="IsCatchAll">True if this is a catch-all parameter ({*args}).</param>
/// <param name="IsOptional">True if this parameter is optional ({name?}).</param>
/// <param name="IsRepeated">True if this parameter can be repeated ({value}* in options).</param>
/// <param name="Type">The type constraint (e.g., "int", "double"), if specified.</param>
/// <param name="Description">Parameter description from pipe syntax, if specified.</param>
internal record ParameterSyntax
(
  string Name,
  bool IsCatchAll = false,
  bool IsOptional = false,
  bool IsRepeated = false,
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
    if (IsRepeated) sb.Append(", repeated=true");
    if (Type is not null) sb.Append(", type='").Append(Type).Append('\'');
    if (Description is not null) sb.Append(", desc='").Append(Description).Append('\'');
    return sb.ToString();
  }
}
