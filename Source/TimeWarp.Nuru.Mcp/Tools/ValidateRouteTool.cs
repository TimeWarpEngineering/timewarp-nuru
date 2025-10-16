namespace TimeWarp.Nuru.Mcp.Tools;

using TimeWarp.Nuru.Parsing;

/// <summary>
/// MCP tool for validating TimeWarp.Nuru route patterns.
/// </summary>
internal sealed class ValidateRouteTool
{
    [McpServerTool]
    [Description("Validate a TimeWarp.Nuru route pattern and get detailed information")]
    public static string ValidateRoute(
        [Description("The route pattern to validate (e.g., 'deploy {env} --dry-run')")] string pattern)
    {
        try
        {
            CompiledRoute route = PatternParser.Parse(pattern);

            List<string> details = [];

            // Basic info
            details.Add($"✅ Valid route pattern: '{pattern}'");
            details.Add("");

            // Positional matchers breakdown
            if (route.PositionalMatchers.Count > 0)
            {
                details.Add("**Positional Matchers:**");
                int position = 0;
                foreach (RouteMatcher matcher in route.PositionalMatchers)
                {
                    details.Add($"  [{position}] {matcher.ToDisplayString()}");
                    position++;
                }

                details.Add("");
            }

            // Catch-all info
            if (route.HasCatchAll)
            {
                details.Add($"**Catch-all Parameter:** {route.CatchAllParameterName}");
                details.Add("");
            }

            // Options breakdown
            if (route.OptionMatchers.Count > 0)
            {
                details.Add("**Options:**");
                foreach (OptionMatcher option in route.OptionMatchers)
                {
                    string optionInfo = $"  {option.ParameterName ?? "boolean"}";
                    details.Add(optionInfo);
                }

                details.Add("");
            }

            // Required options
            var requiredOptions = route.OptionMatchers.Where(o => !o.IsOptional).ToList();
            if (requiredOptions.Count > 0)
            {
                details.Add("**Required Options:**");
                foreach (OptionMatcher reqOption in requiredOptions)
                {
                    details.Add($"  {reqOption.ToDisplayString()}");
                }

                details.Add("");
            }

            // Summary
            details.Add("**Summary:**");
            details.Add($"  Specificity Score: {route.Specificity}");
            int minRequiredArgs = route.PositionalMatchers.Count(m => m is not ParameterMatcher { IsOptional: true });
            details.Add($"  Minimum Required Args: {minRequiredArgs}");

            return string.Join("\n", details);
        }
        catch (PatternException ex)
        {
            return $"❌ Invalid route pattern: '{pattern}'\n\nError: {ex.Message}\n\n" +
                   "**Common issues:**\n" +
                   "- Missing closing brace '}' for parameters\n" +
                   "- Invalid type constraint (use :int, :string, :double, etc.)\n" +
                   "- Catch-all parameter '*' must be last\n" +
                   "- Optional parameters must come after required ones\n" +
                   "- Invalid option format (use --option or -o)";
        }
    }
}