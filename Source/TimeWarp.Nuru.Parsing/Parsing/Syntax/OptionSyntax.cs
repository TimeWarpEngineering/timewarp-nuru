namespace TimeWarp.Nuru.Parsing;

using System.Text;

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