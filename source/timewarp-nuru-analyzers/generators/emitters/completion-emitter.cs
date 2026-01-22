// Emits shell completion support code.
// Generates the GeneratedShellCompletionProvider class for __complete callback.

namespace TimeWarp.Nuru.Generators;

using System.Text;

/// <summary>
/// Emits code to support shell completion functionality.
/// Creates the GeneratedShellCompletionProvider for the __complete callback protocol.
/// </summary>
internal static class CompletionEmitter
{
  /// <summary>
  /// Emits all shell completion-related code for an application.
  /// </summary>
  /// <param name="sb">The StringBuilder to append to.</param>
  /// <param name="app">The application model containing route definitions.</param>
  /// <param name="methodSuffix">Suffix for per-app methods (e.g., "_0" for multi-app assemblies).</param>
  /// <param name="endpoints">Routes from [NuruRoute] endpoint classes.</param>
  /// <param name="compilation">The Roslyn compilation for resolving enum types.</param>
  public static void Emit(StringBuilder sb, AppModel app, string methodSuffix, ImmutableArray<RouteDefinition> endpoints, Compilation compilation)
  {
    sb.AppendLine("  // ═══════════════════════════════════════════════════════════════════════════════");
    sb.AppendLine("  // SHELL COMPLETION SUPPORT (source-generated static data)");
    sb.AppendLine("  // ═══════════════════════════════════════════════════════════════════════════════");
    sb.AppendLine();

    EmitGeneratedShellCompletionProvider(sb, app, methodSuffix, endpoints, compilation);
  }

  /// <summary>
  /// Emits the GeneratedShellCompletionProvider class implementing IShellCompletionProvider.
  /// </summary>
  private static void EmitGeneratedShellCompletionProvider(StringBuilder sb, AppModel app, string methodSuffix, ImmutableArray<RouteDefinition> endpoints, Compilation compilation)
  {
    // Collect all routes for this app (excluding completion-related routes)
    IEnumerable<RouteDefinition> allRoutes = app.Routes
      .Concat(endpoints)
      .Where(r => !r.OriginalPattern.StartsWith("__complete", StringComparison.Ordinal) &&
                  !r.OriginalPattern.StartsWith("--generate-completion", StringComparison.Ordinal) &&
                  !r.OriginalPattern.StartsWith("--install-completion", StringComparison.Ordinal));

    // Extract completion data using shared helpers
    List<string> commandPrefixes = CompletionDataExtractor.ExtractCommandPrefixes(allRoutes);
    List<CompletionDataExtractor.OptionInfo> options = CompletionDataExtractor.ExtractOptions(allRoutes);
    List<CompletionDataExtractor.RouteOptionInfo> routeOptions = CompletionDataExtractor.ExtractRouteOptions(allRoutes);
    List<CompletionDataExtractor.ParameterInfo> parameters = CompletionDataExtractor.ExtractParameters(allRoutes);
    List<CompletionDataExtractor.EnumParameterInfo> enumParameters = CompletionDataExtractor.ExtractEnumParameters(allRoutes, compilation);

    string className = $"GeneratedShellCompletionProvider{methodSuffix}";

    sb.AppendLine($"  private sealed class {className} : global::TimeWarp.Nuru.IShellCompletionProvider");
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

    // GetCompletions method
    EmitGetCompletionsMethod(sb, commandPrefixes, options, routeOptions, parameters, enumParameters);
    sb.AppendLine();

    // TryGetParameterInfo method
    EmitTryGetParameterInfoMethod(sb, parameters, enumParameters);

    sb.AppendLine("  }");
    sb.AppendLine();

    // Emit static instance field for easy access
    sb.AppendLine($"  private static readonly global::TimeWarp.Nuru.IShellCompletionProvider __shellCompletionProvider{methodSuffix} = new {className}();");
  }

  /// <summary>
  /// Emits the GetCompletions method for the shell completion provider.
  /// </summary>
  private static void EmitGetCompletionsMethod(
    StringBuilder sb,
    List<string> commandPrefixes,
    List<CompletionDataExtractor.OptionInfo> options,
    List<CompletionDataExtractor.RouteOptionInfo> routeOptions,
    List<CompletionDataExtractor.ParameterInfo> parameters,
    List<CompletionDataExtractor.EnumParameterInfo> enumParameters)
  {
    sb.AppendLine("    public global::System.Collections.Generic.IEnumerable<global::TimeWarp.Nuru.CompletionCandidate> GetCompletions(int cursorIndex, string[] words)");
    sb.AppendLine("    {");
    sb.AppendLine("      // Convert shell completion args to REPL-style format");
    sb.AppendLine("      // Shell: cursorIndex is 1-based position in words array (including app name)");
    sb.AppendLine("      // Words array: [appName, word1, word2, ...]");
    sb.AppendLine("      string[] args = words.Length > 1 ? words[1..] : [];");
    sb.AppendLine("      bool hasTrailingSpace = cursorIndex >= words.Length;");
    sb.AppendLine();
    sb.AppendLine("      // Determine current input context");
    sb.AppendLine("      string currentInput = args.Length > 0 && !hasTrailingSpace ? args[^1] : \"\";");
    sb.AppendLine("      string prefix = args.Length > 0 ? string.Join(\" \", hasTrailingSpace ? args : args[..^1]) : \"\";");
    sb.AppendLine();
    sb.AppendLine("      // Track yielded completions to prevent duplicates");
    sb.AppendLine("      global::System.Collections.Generic.HashSet<string> yielded = new(global::System.StringComparer.OrdinalIgnoreCase);");
    sb.AppendLine();

    // Command completions
    sb.AppendLine("      // Command prefix completions");
    sb.AppendLine("      foreach (string cmd in CommandPrefixes)");
    sb.AppendLine("      {");
    sb.AppendLine("        if (!string.IsNullOrEmpty(prefix))");
    sb.AppendLine("        {");
    sb.AppendLine("          string prefixWithSpace = prefix + \" \";");
    sb.AppendLine("          if (cmd.StartsWith(prefixWithSpace, global::System.StringComparison.OrdinalIgnoreCase))");
    sb.AppendLine("          {");
    sb.AppendLine("            string nextWord = cmd[prefixWithSpace.Length..];");
    sb.AppendLine("            int spaceIdx = nextWord.IndexOf(' ');");
    sb.AppendLine("            if (spaceIdx >= 0) nextWord = nextWord[..spaceIdx];");
    sb.AppendLine();
    sb.AppendLine("            if (nextWord.StartsWith(currentInput, global::System.StringComparison.OrdinalIgnoreCase) && yielded.Add(nextWord))");
    sb.AppendLine("            {");
    sb.AppendLine("              yield return new global::TimeWarp.Nuru.CompletionCandidate(nextWord, null, global::TimeWarp.Nuru.CompletionType.Command);");
    sb.AppendLine("            }");
    sb.AppendLine("          }");
    sb.AppendLine("        }");
    sb.AppendLine("        else if (args.Length <= 1)");
    sb.AppendLine("        {");
    sb.AppendLine("          string firstWord = cmd;");
    sb.AppendLine("          int spaceIdx = cmd.IndexOf(' ');");
    sb.AppendLine("          if (spaceIdx >= 0) firstWord = cmd[..spaceIdx];");
    sb.AppendLine();
    sb.AppendLine("          if (firstWord.StartsWith(currentInput, global::System.StringComparison.OrdinalIgnoreCase) && yielded.Add(firstWord))");
    sb.AppendLine("          {");
    sb.AppendLine("            yield return new global::TimeWarp.Nuru.CompletionCandidate(firstWord, null, global::TimeWarp.Nuru.CompletionType.Command);");
    sb.AppendLine("          }");
    sb.AppendLine("        }");
    sb.AppendLine("      }");
    sb.AppendLine();

    // --help completion
    sb.AppendLine("      // --help is always available");
    sb.AppendLine("      if (string.IsNullOrEmpty(currentInput) || \"--help\".StartsWith(currentInput, global::System.StringComparison.OrdinalIgnoreCase))");
    sb.AppendLine("        yield return new global::TimeWarp.Nuru.CompletionCandidate(\"--help\", \"Show help for this command\", global::TimeWarp.Nuru.CompletionType.Option);");
    sb.AppendLine();

    // Enum parameter completions
    if (enumParameters.Count > 0)
    {
      sb.AppendLine("      // Enum parameter completions (position-aware)");
      foreach (CompletionDataExtractor.EnumParameterInfo enumParam in enumParameters)
      {
        int cmdWordCount = enumParam.CommandPrefix.Split(' ').Length;
        sb.AppendLine($"      // Enum completions for '{EscapeString(enumParam.CommandPrefix)}' parameter '{enumParam.ParameterName}' at position {enumParam.Position}");
        sb.AppendLine($"      if (string.Equals(prefix, \"{EscapeString(enumParam.CommandPrefix)}\", global::System.StringComparison.OrdinalIgnoreCase) ||");
        sb.AppendLine($"          prefix.StartsWith(\"{EscapeString(enumParam.CommandPrefix)} \", global::System.StringComparison.OrdinalIgnoreCase))");
        sb.AppendLine("      {");
        sb.AppendLine($"        int paramPos = args.Length - (hasTrailingSpace ? 0 : 1) - {cmdWordCount};");
        sb.AppendLine($"        if (paramPos == {enumParam.Position})");
        sb.AppendLine("        {");
        foreach (string value in enumParam.Values)
        {
          sb.AppendLine($"          if (\"{value}\".StartsWith(currentInput, global::System.StringComparison.OrdinalIgnoreCase))");
          sb.AppendLine($"            yield return new global::TimeWarp.Nuru.CompletionCandidate(\"{value}\", null, global::TimeWarp.Nuru.CompletionType.Enum);");
        }

        sb.AppendLine("        }");
        sb.AppendLine("      }");
      }

      sb.AppendLine();
    }

    // Context-aware route option completions
    if (routeOptions.Count > 0)
    {
      sb.AppendLine("      // Context-aware route option completions");
      foreach (CompletionDataExtractor.RouteOptionInfo routeOpt in routeOptions)
      {
        sb.AppendLine($"      // Options for '{EscapeString(routeOpt.CommandPrefix)}'");
        sb.AppendLine($"      if (string.Equals(prefix, \"{EscapeString(routeOpt.CommandPrefix)}\", global::System.StringComparison.OrdinalIgnoreCase) ||");
        sb.AppendLine($"          prefix.StartsWith(\"{EscapeString(routeOpt.CommandPrefix)} \", global::System.StringComparison.OrdinalIgnoreCase))");
        sb.AppendLine("      {");
        foreach (CompletionDataExtractor.OptionInfo opt in routeOpt.Options)
        {
          if (opt.LongForm is not null)
          {
            string description = opt.Description is not null ? $"\"{EscapeString(opt.Description)}\"" : "null";
            sb.AppendLine($"        if (string.IsNullOrEmpty(currentInput) || \"--{opt.LongForm}\".StartsWith(currentInput, global::System.StringComparison.OrdinalIgnoreCase))");
            sb.AppendLine($"          yield return new global::TimeWarp.Nuru.CompletionCandidate(\"--{opt.LongForm}\", {description}, global::TimeWarp.Nuru.CompletionType.Option);");
          }

          if (opt.ShortForm is not null)
          {
            string description = opt.Description is not null ? $"\"{EscapeString(opt.Description)}\"" : "null";
            sb.AppendLine($"        if (string.IsNullOrEmpty(currentInput) || \"-{opt.ShortForm}\".StartsWith(currentInput, global::System.StringComparison.OrdinalIgnoreCase))");
            sb.AppendLine($"          yield return new global::TimeWarp.Nuru.CompletionCandidate(\"-{opt.ShortForm}\", {description}, global::TimeWarp.Nuru.CompletionType.Option);");
          }
        }

        sb.AppendLine("      }");
      }

      sb.AppendLine();
    }

    // Global option completions
    if (options.Count > 0)
    {
      sb.AppendLine("      // Global option completions (when typing - or --)");
      sb.AppendLine("      if (currentInput.StartsWith(\"-\", global::System.StringComparison.Ordinal))");
      sb.AppendLine("      {");

      foreach (CompletionDataExtractor.OptionInfo opt in options.DistinctBy(o => o.LongForm ?? o.ShortForm))
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
  /// Emits the TryGetParameterInfo method for looking up custom completion sources.
  /// </summary>
  private static void EmitTryGetParameterInfoMethod(
    StringBuilder sb,
    List<CompletionDataExtractor.ParameterInfo> parameters,
    List<CompletionDataExtractor.EnumParameterInfo> enumParameters)
  {
    sb.AppendLine("    public bool TryGetParameterInfo(int cursorIndex, string[] words, out string? parameterName, out string? parameterTypeName)");
    sb.AppendLine("    {");
    sb.AppendLine("      parameterName = null;");
    sb.AppendLine("      parameterTypeName = null;");
    sb.AppendLine();
    sb.AppendLine("      if (words.Length < 2) return false;");
    sb.AppendLine();
    sb.AppendLine("      // Get command words (excluding app name)");
    sb.AppendLine("      string[] cmdWords = words[1..];");
    sb.AppendLine("      string prefix = string.Join(\" \", cmdWords.Take(cursorIndex - 1));");
    sb.AppendLine();

    // Generate parameter detection based on extracted parameter info
    foreach (CompletionDataExtractor.ParameterInfo param in parameters)
    {
      if (string.IsNullOrEmpty(param.CommandPrefix))
        continue;

      int cmdWordCount = param.CommandPrefix.Split(' ').Length;

      sb.AppendLine($"      // Parameter '{param.Name}' for command '{EscapeString(param.CommandPrefix)}'");
      sb.AppendLine($"      if (prefix.StartsWith(\"{EscapeString(param.CommandPrefix)}\", global::System.StringComparison.OrdinalIgnoreCase))");
      sb.AppendLine("      {");
      sb.AppendLine($"        int paramPos = cursorIndex - 1 - {cmdWordCount};");
      sb.AppendLine("        if (paramPos == 0) // First parameter position");
      sb.AppendLine("        {");
      sb.AppendLine($"          parameterName = \"{param.Name}\";");
      string typeConstraint = param.TypeConstraint is not null ? $"\"{param.TypeConstraint}\"" : "null";
      sb.AppendLine($"          parameterTypeName = {typeConstraint};");
      sb.AppendLine("          return true;");
      sb.AppendLine("        }");
      sb.AppendLine("      }");
    }

    sb.AppendLine();
    sb.AppendLine("      return false;");
    sb.AppendLine("    }");
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
}
