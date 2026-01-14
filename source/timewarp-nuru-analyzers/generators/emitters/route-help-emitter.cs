// Emits per-route help text generation code.
// Generates inline help output for "command --help" scenarios.
// Task #356: Per-route help support

namespace TimeWarp.Nuru.Generators;

using System.Text;

/// <summary>
/// Emits code to generate help text for a specific route.
/// Used when a user types "command --help" to show help for just that command.
/// </summary>
internal static class RouteHelpEmitter
{
  /// <summary>
  /// Emits code to check for per-route help (command --help) and display route-specific help.
  /// This should be called at the start of each route's matching block.
  /// </summary>
  /// <param name="sb">The StringBuilder to append to.</param>
  /// <param name="route">The route definition to emit help for.</param>
  /// <param name="routeIndex">The index of this route (used for unique label names).</param>
  /// <param name="indent">Indentation level (number of spaces).</param>
  public static void EmitPerRouteHelpCheck(
    StringBuilder sb,
    RouteDefinition route,
    int routeIndex,
    int indent = 4)
  {
    string indentStr = new(' ', indent);

    // Get the literal prefix for this route (group prefix + pattern literals)
    List<string> literalPrefix = GetLiteralPrefix(route);

    // If no literals, this route can't have per-route help (e.g., "{*args}")
    if (literalPrefix.Count == 0)
      return;

    // Build the pattern match for: [literal1, literal2, ..., "--help" or "-h"]
    StringBuilder patternBuilder = new();
    patternBuilder.Append('[');
    foreach (string literal in literalPrefix)
    {
      patternBuilder.Append($"\"{EscapeString(literal)}\", ");
    }

    patternBuilder.Append("\"--help\" or \"-h\"]");
    string helpPattern = patternBuilder.ToString();

    // Emit the help check
    sb.AppendLine($"{indentStr}// Per-route help: {route.FullPattern} --help");
    sb.AppendLine($"{indentStr}if (routeArgs is {helpPattern})");
    sb.AppendLine($"{indentStr}{{");

    // Emit the help content inline
    EmitRouteHelpContent(sb, route, indent + 2);

    sb.AppendLine($"{indentStr}  return 0;");
    sb.AppendLine($"{indentStr}}}");
    sb.AppendLine();
  }

  /// <summary>
  /// Gets the literal prefix for a route (group prefix literals + pattern literals).
  /// These are the literals that must match before --help for per-route help.
  /// </summary>
  private static List<string> GetLiteralPrefix(RouteDefinition route)
  {
    List<string> literals = [];

    // Add group prefix literals if present
    if (!string.IsNullOrEmpty(route.GroupPrefix))
    {
      literals.AddRange(route.GroupPrefix.Split(' ', StringSplitOptions.RemoveEmptyEntries));
    }

    // Add pattern literals (stop at first non-literal segment)
    foreach (SegmentDefinition segment in route.Segments)
    {
      if (segment is LiteralDefinition literal)
      {
        literals.Add(literal.Value);
      }
      else
      {
        // Stop at first parameter/option - we only match the literal prefix
        break;
      }
    }

    return literals;
  }

  /// <summary>
  /// Emits the help content for a specific route.
  /// </summary>
  private static void EmitRouteHelpContent(StringBuilder sb, RouteDefinition route, int indent)
  {
    string indentStr = new(' ', indent);

    // Pattern line (e.g., "deploy {env} [--dry-run,-d] [--force,-f]")
    string pattern = BuildPatternDisplay(route);
    sb.AppendLine($"{indentStr}app.Terminal.WriteLine(\"{EscapeString(pattern)}\");");

    // Description (if present)
    if (!string.IsNullOrEmpty(route.Description))
    {
      sb.AppendLine($"{indentStr}app.Terminal.WriteLine();");
      sb.AppendLine($"{indentStr}app.Terminal.WriteLine(\"  {EscapeString(route.Description)}\");");
    }

    // Parameters section
    IEnumerable<ParameterDefinition> parameters = route.Parameters.ToList();
    if (parameters.Any())
    {
      sb.AppendLine($"{indentStr}app.Terminal.WriteLine();");
      sb.AppendLine($"{indentStr}app.Terminal.WriteLine(\"Parameters:\");");

      foreach (ParameterDefinition param in parameters)
      {
        string paramDisplay = BuildParameterHelpLine(param);
        sb.AppendLine($"{indentStr}app.Terminal.WriteLine(\"  {EscapeString(paramDisplay)}\");");
      }
    }

    // Options section
    IEnumerable<OptionDefinition> options = route.Options.ToList();
    if (options.Any())
    {
      sb.AppendLine($"{indentStr}app.Terminal.WriteLine();");
      sb.AppendLine($"{indentStr}app.Terminal.WriteLine(\"Options:\");");

      foreach (OptionDefinition option in options)
      {
        string optionDisplay = BuildOptionHelpLine(option);
        sb.AppendLine($"{indentStr}app.Terminal.WriteLine(\"  {EscapeString(optionDisplay)}\");");
      }
    }
  }

  /// <summary>
  /// Builds the display pattern for help text.
  /// </summary>
  private static string BuildPatternDisplay(RouteDefinition route)
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

  /// <summary>
  /// Builds a help line for a parameter.
  /// </summary>
  private static string BuildParameterHelpLine(ParameterDefinition param)
  {
    StringBuilder line = new();

    // Parameter name with type info
    string name = param.Name;
    if (param.IsCatchAll)
    {
      name = $"*{name}";
    }

    // Add type constraint if present
    if (param.HasTypeConstraint)
    {
      name += $":{param.TypeConstraint}";
    }

    // Pad for alignment
    string paddedName = name.PadRight(15);
    line.Append(paddedName);

    // Add description or required/optional indicator
    if (!string.IsNullOrEmpty(param.Description))
    {
      line.Append(param.Description);
    }
    else if (param.IsCatchAll)
    {
      line.Append("(catch-all)");
    }
    else if (param.IsOptional)
    {
      line.Append("(optional)");
    }
    else
    {
      line.Append("(required)");
    }

    return line.ToString();
  }

  /// <summary>
  /// Builds a help line for an option.
  /// </summary>
  private static string BuildOptionHelpLine(OptionDefinition option)
  {
    StringBuilder line = new();

    // Option forms
    string forms = (option.LongForm, option.ShortForm) switch
    {
      (not null, not null) => $"--{option.LongForm}, -{option.ShortForm}",
      (not null, null) => $"--{option.LongForm}",
      (null, not null) => $"-{option.ShortForm}",
      _ => "[invalid]"
    };

    // Add value placeholder if option takes a value
    if (option.ExpectsValue)
    {
      string valueName = option.ParameterName ?? "value";
      if (option.ParameterIsOptional)
      {
        forms += $" [{valueName}]";
      }
      else
      {
        forms += $" <{valueName}>";
      }
    }

    // Pad for alignment
    string paddedForms = forms.PadRight(25);
    line.Append(paddedForms);

    // Add description
    if (!string.IsNullOrEmpty(option.Description))
    {
      line.Append(option.Description);
    }
    else if (option.IsOptional)
    {
      line.Append("(optional)");
    }

    return line.ToString();
  }

  /// <summary>
  /// Escapes a string for use in C# source code.
  /// </summary>
  private static string EscapeString(string value)
  {
    return value
      .Replace("\\", "\\\\", StringComparison.Ordinal)
      .Replace("\"", "\\\"", StringComparison.Ordinal)
      .Replace("\n", "\\n", StringComparison.Ordinal)
      .Replace("\r", "\\r", StringComparison.Ordinal)
      .Replace("\t", "\\t", StringComparison.Ordinal);
  }
}
