// Emits REPL (Read-Eval-Print Loop) support code.
// Generates the GeneratedReplRouteProvider class and RunReplAsync method.

namespace TimeWarp.Nuru.Generators;

using System.Text;

/// <summary>
/// Emits code to support REPL (interactive mode) functionality.
/// Creates the GeneratedReplRouteProvider for completions and RunReplAsync interceptor.
/// </summary>
internal static class ReplEmitter
{
  /// <summary>
  /// Emits all REPL-related code for an application.
  /// </summary>
  /// <param name="sb">The StringBuilder to append to.</param>
  /// <param name="app">The application model containing route definitions.</param>
  /// <param name="methodSuffix">Suffix for per-app methods (e.g., "_0" for multi-app assemblies).</param>
  /// <param name="attributedRoutes">Routes from [NuruRoute] attributed classes.</param>
  public static void Emit(StringBuilder sb, AppModel app, string methodSuffix, ImmutableArray<RouteDefinition> attributedRoutes)
  {
    sb.AppendLine("  // ═══════════════════════════════════════════════════════════════════════════════");
    sb.AppendLine("  // REPL (INTERACTIVE MODE) SUPPORT");
    sb.AppendLine("  // ═══════════════════════════════════════════════════════════════════════════════");
    sb.AppendLine();

    EmitGeneratedReplRouteProvider(sb, app, methodSuffix, attributedRoutes);
    sb.AppendLine();
    EmitRunReplAsyncMethod(sb, methodSuffix);
  }

  /// <summary>
  /// Emits the GeneratedReplRouteProvider class implementing IReplRouteProvider.
  /// </summary>
  private static void EmitGeneratedReplRouteProvider(StringBuilder sb, AppModel app, string methodSuffix, ImmutableArray<RouteDefinition> attributedRoutes)
  {
    // Collect all routes for this app
    IEnumerable<RouteDefinition> allRoutes = app.Routes.Concat(attributedRoutes);

    // Extract command prefixes (leading literal segments)
    List<string> commandPrefixes = ExtractCommandPrefixes(allRoutes);

    // Extract options for completion
    List<OptionInfo> options = ExtractOptions(allRoutes);

    // Extract parameters with their types for completion hints
    List<ParameterInfo> parameters = ExtractParameters(allRoutes);

    string className = $"GeneratedReplRouteProvider{methodSuffix}";

    sb.AppendLine($"  private sealed class {className} : global::TimeWarp.Nuru.IReplRouteProvider");
    sb.AppendLine("  {");

    // Static command prefixes array
    sb.AppendLine("    private static readonly string[] CommandPrefixes =");
    sb.AppendLine("    [");
    foreach (string prefix in commandPrefixes.OrderBy(p => p, StringComparer.OrdinalIgnoreCase))
    {
      sb.AppendLine($"      \"{EscapeString(prefix)}\",");
    }

    sb.AppendLine("    ];");
    sb.AppendLine();

    // GetCommandPrefixes method
    sb.AppendLine("    public global::System.Collections.Generic.IReadOnlyList<string> GetCommandPrefixes() => CommandPrefixes;");
    sb.AppendLine();

    // GetCompletions method
    EmitGetCompletionsMethod(sb, commandPrefixes, options, parameters);

    // IsKnownCommand method
    sb.AppendLine("    public bool IsKnownCommand(string token)");
    sb.AppendLine("    {");
    sb.AppendLine("      if (string.IsNullOrEmpty(token))");
    sb.AppendLine("        return false;");
    sb.AppendLine();
    sb.AppendLine("      foreach (string prefix in CommandPrefixes)");
    sb.AppendLine("      {");
    sb.AppendLine("        if (string.Equals(prefix, token, global::System.StringComparison.OrdinalIgnoreCase))");
    sb.AppendLine("          return true;");
    sb.AppendLine("        if (prefix.StartsWith(token + \" \", global::System.StringComparison.OrdinalIgnoreCase))");
    sb.AppendLine("          return true;");
    sb.AppendLine("      }");
    sb.AppendLine();
    sb.AppendLine("      return false;");
    sb.AppendLine("    }");

    sb.AppendLine("  }");
  }

  /// <summary>
  /// Emits the GetCompletions method for the route provider.
  /// </summary>
  private static void EmitGetCompletionsMethod(StringBuilder sb, List<string> commandPrefixes, List<OptionInfo> options, List<ParameterInfo> parameters)
  {
    sb.AppendLine("    public global::System.Collections.Generic.IEnumerable<global::TimeWarp.Nuru.CompletionCandidate> GetCompletions(string[] args, bool hasTrailingSpace)");
    sb.AppendLine("    {");
    sb.AppendLine("      // Determine current input context");
    sb.AppendLine("      string currentInput = args.Length > 0 && !hasTrailingSpace ? args[^1] : \"\";");
    sb.AppendLine("      string prefix = args.Length > 0 ? string.Join(\" \", hasTrailingSpace ? args : args[..^1]) : \"\";");
    sb.AppendLine();

    // Command completions (when at start or after partial command)
    sb.AppendLine("      // Command prefix completions");
    sb.AppendLine("      foreach (string cmd in CommandPrefixes)");
    sb.AppendLine("      {");
    sb.AppendLine("        // If we have a prefix, check if this command starts with prefix + space");
    sb.AppendLine("        if (!string.IsNullOrEmpty(prefix))");
    sb.AppendLine("        {");
    sb.AppendLine("          string prefixWithSpace = prefix + \" \";");
    sb.AppendLine("          if (cmd.StartsWith(prefixWithSpace, global::System.StringComparison.OrdinalIgnoreCase))");
    sb.AppendLine("          {");
    sb.AppendLine("            string nextWord = cmd[prefixWithSpace.Length..];");
    sb.AppendLine("            int spaceIdx = nextWord.IndexOf(' ');");
    sb.AppendLine("            if (spaceIdx >= 0) nextWord = nextWord[..spaceIdx];");
    sb.AppendLine();
    sb.AppendLine("            if (nextWord.StartsWith(currentInput, global::System.StringComparison.OrdinalIgnoreCase))");
    sb.AppendLine("            {");
    sb.AppendLine("              yield return new global::TimeWarp.Nuru.CompletionCandidate(nextWord, null, global::TimeWarp.Nuru.CompletionType.Command);");
    sb.AppendLine("            }");
    sb.AppendLine("          }");
    sb.AppendLine("        }");
    sb.AppendLine("        else if (args.Length <= 1)");
    sb.AppendLine("        {");
    sb.AppendLine("          // At the start - suggest first word of commands");
    sb.AppendLine("          string firstWord = cmd;");
    sb.AppendLine("          int spaceIdx = cmd.IndexOf(' ');");
    sb.AppendLine("          if (spaceIdx >= 0) firstWord = cmd[..spaceIdx];");
    sb.AppendLine();
    sb.AppendLine("          if (firstWord.StartsWith(currentInput, global::System.StringComparison.OrdinalIgnoreCase))");
    sb.AppendLine("          {");
    sb.AppendLine("            yield return new global::TimeWarp.Nuru.CompletionCandidate(firstWord, null, global::TimeWarp.Nuru.CompletionType.Command);");
    sb.AppendLine("          }");
    sb.AppendLine("        }");
    sb.AppendLine("      }");
    sb.AppendLine();

    // Option completions
    if (options.Count > 0)
    {
      sb.AppendLine("      // Option completions (when typing - or --)");
      sb.AppendLine("      if (currentInput.StartsWith(\"-\", global::System.StringComparison.Ordinal))");
      sb.AppendLine("      {");

      foreach (OptionInfo opt in options.DistinctBy(o => o.LongForm ?? o.ShortForm))
      {
        if (opt.LongForm is not null)
        {
          string description = opt.Description is not null ? $"\"{EscapeString(opt.Description)}\"" : "null";
          sb.AppendLine($"        if (\"--{opt.LongForm}\".StartsWith(currentInput, global::System.StringComparison.OrdinalIgnoreCase))");
          sb.AppendLine($"          yield return new global::TimeWarp.Nuru.CompletionCandidate(\"--{opt.LongForm}\", {description}, global::TimeWarp.Nuru.CompletionType.Option);");
        }

        if (opt.ShortForm is not null)
        {
          string description = opt.Description is not null ? $"\"{EscapeString(opt.Description)}\"" : "null";
          sb.AppendLine($"        if (\"-{opt.ShortForm}\".StartsWith(currentInput, global::System.StringComparison.OrdinalIgnoreCase))");
          sb.AppendLine($"          yield return new global::TimeWarp.Nuru.CompletionCandidate(\"-{opt.ShortForm}\", {description}, global::TimeWarp.Nuru.CompletionType.Option);");
        }
      }

      sb.AppendLine("      }");
    }

    sb.AppendLine("    }");
  }

  /// <summary>
  /// Emits the RunReplAsync method that starts the REPL session.
  /// </summary>
  private static void EmitRunReplAsyncMethod(StringBuilder sb, string methodSuffix)
  {
    string providerClassName = $"GeneratedReplRouteProvider{methodSuffix}";

    sb.AppendLine($"  private static async global::System.Threading.Tasks.Task RunReplAsync{methodSuffix}(global::TimeWarp.Nuru.NuruCoreApp app)");
    sb.AppendLine("  {");
    sb.AppendLine($"    global::TimeWarp.Nuru.IReplRouteProvider routeProvider = new {providerClassName}();");
    sb.AppendLine("    global::TimeWarp.Nuru.ReplOptions replOptions = app.ReplOptions ?? new global::TimeWarp.Nuru.ReplOptions();");
    sb.AppendLine();
    sb.AppendLine("    await global::TimeWarp.Nuru.ReplSession.RunAsync(");
    sb.AppendLine("      app,");
    sb.AppendLine("      replOptions,");
    sb.AppendLine("      routeProvider,");
    // Call ExecuteRouteAsync directly - the core route matching logic used by both RunAsync and REPL
    sb.AppendLine($"      static (nuruApp, args, ct) => ExecuteRouteAsync{methodSuffix}(nuruApp, args),");
    sb.AppendLine("      app.LoggerFactory");
    sb.AppendLine("    ).ConfigureAwait(false);");
    sb.AppendLine("  }");
  }

  /// <summary>
  /// Extracts command prefixes from route definitions.
  /// A command prefix is the sequence of leading literal segments.
  /// </summary>
  private static List<string> ExtractCommandPrefixes(IEnumerable<RouteDefinition> routes)
  {
    HashSet<string> prefixes = new(StringComparer.OrdinalIgnoreCase);

    foreach (RouteDefinition route in routes)
    {
      StringBuilder prefix = new();

      foreach (LiteralDefinition literal in route.Literals)
      {
        if (prefix.Length > 0)
          prefix.Append(' ');
        prefix.Append(literal.Value);
      }

      if (prefix.Length > 0)
        prefixes.Add(prefix.ToString());
    }

    return [.. prefixes];
  }

  /// <summary>
  /// Extracts option information from route definitions.
  /// </summary>
  private static List<OptionInfo> ExtractOptions(IEnumerable<RouteDefinition> routes)
  {
    List<OptionInfo> options = [];

    foreach (RouteDefinition route in routes)
    {
      foreach (OptionDefinition opt in route.Options)
      {
        options.Add(new OptionInfo(opt.LongForm, opt.ShortForm, opt.Description, opt.ExpectsValue, opt.TypeConstraint));
      }
    }

    return options;
  }

  /// <summary>
  /// Extracts parameter information from route definitions.
  /// </summary>
  private static List<ParameterInfo> ExtractParameters(IEnumerable<RouteDefinition> routes)
  {
    List<ParameterInfo> parameters = [];

    foreach (RouteDefinition route in routes)
    {
      // Get the command prefix for context
      string cmdPrefix = string.Join(" ", route.Literals.Select(l => l.Value));

      foreach (ParameterDefinition param in route.Parameters)
      {
        parameters.Add(new ParameterInfo(param.Name, param.TypeConstraint, param.Description, cmdPrefix));
      }
    }

    return parameters;
  }

  /// <summary>
  /// Escapes a string for use in generated C# code.
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

  /// <summary>
  /// Information about an option for completion generation.
  /// </summary>
  private sealed record OptionInfo(string? LongForm, string? ShortForm, string? Description, bool ExpectsValue, string? TypeConstraint);

  /// <summary>
  /// Information about a parameter for completion generation.
  /// </summary>
  private sealed record ParameterInfo(string Name, string? TypeConstraint, string? Description, string CommandPrefix);
}
