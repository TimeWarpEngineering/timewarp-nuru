// Shared helper for building route pattern display strings.
// Extracted to avoid duplication between help-emitter.cs and route-help-emitter.cs

namespace TimeWarp.Nuru.Generators;

using System.Text;

/// <summary>
/// Shared helper for building route pattern display strings for help text.
/// </summary>
internal static class HelpPatternHelper
{
  /// <summary>
  /// Builds the display pattern for help text.
  /// Creates a human-readable pattern like "deploy {env} [--force]".
  /// </summary>
  public static string BuildPatternDisplay(RouteDefinition route)
  {
    StringBuilder pattern = new();

    // Add group prefix if present
    if (!string.IsNullOrEmpty(route.GroupPrefix))
    {
      pattern.Append(route.GroupPrefix);
      pattern.Append(' ');
    }

    // Add segments
    foreach (SegmentDefinition segment in route.Segments)
    {
      switch (segment)
      {
        case LiteralDefinition literal:
          pattern.Append(literal.Value);
          pattern.Append(' ');
          break;

        case ParameterDefinition param:
          if (param.IsCatchAll)
          {
            pattern.Append($"{{*{param.Name}}} ");
          }
          else if (param.IsOptional)
          {
            pattern.Append($"[{param.Name}] ");
          }
          else
          {
            pattern.Append($"{{{param.Name}}} ");
          }

          break;

        case OptionDefinition option:
          string optionDisplay = (option.LongForm, option.ShortForm) switch
          {
            (not null, not null) => $"--{option.LongForm},-{option.ShortForm}",
            (not null, null) => $"--{option.LongForm}",
            (null, not null) => $"-{option.ShortForm}",
            _ => "[invalid option]"
          };

          if (option.ExpectsValue)
          {
            optionDisplay += $" {{{option.ParameterName ?? "value"}}}";
          }

          if (option.IsOptional)
          {
            pattern.Append($"[{optionDisplay}] ");
          }
          else
          {
            pattern.Append($"{optionDisplay} ");
          }

          break;

        case EndOfOptionsSeparatorDefinition:
          pattern.Append("-- ");
          break;
      }
    }

    return pattern.ToString().Trim();
  }
}
