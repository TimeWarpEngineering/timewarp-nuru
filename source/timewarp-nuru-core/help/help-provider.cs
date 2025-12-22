namespace TimeWarp.Nuru;

/// <summary>
/// Provides help display functionality for commands.
/// </summary>
/// <remarks>
/// This class is split into partial classes for maintainability:
/// - help-provider.cs: Core orchestration (GetHelpText entry point)
/// - help-provider.filtering.cs: Route filtering, wildcard matching, and endpoint grouping
/// - help-provider.formatting.cs: Pattern formatting and display conversion logic
/// - help-provider.ansi.cs: ANSI color formatting helpers for headers, usage, and descriptions
/// </remarks>
public static partial class HelpProvider
{
  /// <summary>
  /// Gets help text for all registered routes.
  /// </summary>
  /// <param name="endpoints">The endpoint collection.</param>
  /// <param name="appName">Optional application name.</param>
  /// <param name="appDescription">Optional application description.</param>
  /// <param name="options">Optional help options for filtering.</param>
  /// <param name="context">The help context (CLI or REPL).</param>
  /// <param name="useColor">Whether to include ANSI color codes in the output.</param>
  public static string GetHelpText(
    EndpointCollection endpoints,
    string? appName = null,
    string? appDescription = null,
    HelpOptions? options = null,
    HelpContext context = HelpContext.Cli,
    bool useColor = true)
  {
    ArgumentNullException.ThrowIfNull(endpoints);
    options ??= new HelpOptions();

    List<Endpoint> routes = FilterRoutes(endpoints.Endpoints, options, context);

    if (routes.Count == 0)
    {
      return "No routes are registered.";
    }

    StringBuilder sb = new();

    // Description section
    if (!string.IsNullOrEmpty(appDescription))
    {
      sb.AppendLine(FormatSectionHeader("Description", useColor));
      sb.AppendLine("  " + FormatDescription(appDescription, useColor));
      sb.AppendLine();
    }

    // Usage section
    sb.AppendLine(FormatSectionHeader("Usage", useColor));
    sb.AppendLine("  " + FormatUsage(appName ?? GetDefaultAppName(), useColor));
    sb.AppendLine();

    // Group endpoints by description for alias grouping
    List<EndpointGroup> groups = GroupByDescription(routes);

    // Separate commands and options
    List<EndpointGroup> commandGroups = [.. groups.Where(g => !g.FirstPattern.StartsWith('-'))];
    List<EndpointGroup> optionGroups = [.. groups.Where(g => g.FirstPattern.StartsWith('-'))];

    // Commands section
    if (commandGroups.Count > 0)
    {
      sb.AppendLine(FormatSectionHeader("Commands", useColor));
      foreach (EndpointGroup group in commandGroups.OrderBy(g => g.FirstPattern))
      {
        AppendGroup(sb, group, useColor);
      }

      sb.AppendLine();
    }

    // Options section
    if (optionGroups.Count > 0)
    {
      sb.AppendLine(FormatSectionHeader("Options", useColor));
      foreach (EndpointGroup group in optionGroups.OrderBy(g => g.FirstPattern))
      {
        AppendGroup(sb, group, useColor);
      }

      sb.AppendLine();
    }

    // Legend section
    sb.AppendLine(FormatMessageTypeLegend(useColor));

    return sb.ToString().TrimEnd();
  }
}
