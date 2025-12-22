namespace TimeWarp.Nuru;

using System.Reflection;
using System.Text.Json;

/// <summary>
/// Capabilities route handler for NuruAppBuilderExtensions.
/// </summary>
/// <remarks>
/// This partial class contains:
/// <list type="bullet">
/// <item><description><see cref="AddCapabilitiesRoute{TBuilder}"/> - Registers the --capabilities route</description></item>
/// <item><description><see cref="DisplayCapabilitiesAsync"/> - Handler that outputs JSON metadata for AI tool discovery</description></item>
/// </list>
/// </remarks>
public static partial class NuruAppBuilderExtensions
{
  /// <summary>
  /// Adds a <c>--capabilities</c> route that outputs machine-readable JSON metadata about all commands.
  /// Enables AI tools to discover CLI capabilities without MCP complexity.
  /// </summary>
  /// <typeparam name="TBuilder">The builder type.</typeparam>
  /// <param name="builder">The NuruCoreAppBuilder instance.</param>
  /// <returns>The builder for chaining.</returns>
  /// <remarks>
  /// <para>
  /// The output is a JSON object containing:
  /// <list type="bullet">
  ///   <item><description>name - The application name</description></item>
  ///   <item><description>version - The application version</description></item>
  ///   <item><description>description - The application description</description></item>
  ///   <item><description>commands - Array of command metadata with patterns, descriptions, message types, parameters, and options</description></item>
  /// </list>
  /// </para>
  /// <para>
  /// This route is hidden from <c>--help</c> output since it's intended for AI consumption.
  /// </para>
  /// </remarks>
  public static TBuilder AddCapabilitiesRoute<TBuilder>(this TBuilder builder)
    where TBuilder : NuruCoreAppBuilder<TBuilder>
  {
    ArgumentNullException.ThrowIfNull(builder);
    builder.Map("--capabilities")
      .WithHandler(DisplayCapabilitiesAsync)
      .WithDescription("Display machine-readable metadata for AI tool discovery")
      .AsQuery()
      .Done();
    return builder;
  }

  /// <summary>
  /// Handler for the capabilities route that displays JSON metadata.
  /// Uses Func&lt;NuruCoreAppHolder, Task&gt; signature to match existing invoker registration.
  /// </summary>
  /// <param name="appHolder">The NuruCoreAppHolder instance (injected via DI).</param>
  internal static Task DisplayCapabilitiesAsync(NuruCoreAppHolder appHolder)
  {
    ArgumentNullException.ThrowIfNull(appHolder);

    NuruCoreApp app = appHolder.App;
    CapabilitiesResponse response = BuildCapabilitiesResponse(
      app.Endpoints,
      app.AppMetadata,
      app.HelpOptions);

    string json = JsonSerializer.Serialize(response, CapabilitiesJsonSerializerContext.Default.CapabilitiesResponse);
    Console.WriteLine(json);

    return Task.CompletedTask;
  }

  /// <summary>
  /// Builds the capabilities response from the registered endpoints.
  /// </summary>
  private static CapabilitiesResponse BuildCapabilitiesResponse(
    EndpointCollection endpoints,
    ApplicationMetadata? appMetadata,
    HelpOptions helpOptions)
  {
    string appName = appMetadata?.Name ?? GetEntryAssemblyName() ?? "unknown";
    string version = GetCapabilitiesVersion() ?? "0.0.0";
    string? description = appMetadata?.Description;

    List<CommandCapability> commands = [];

    foreach (Endpoint endpoint in endpoints.Endpoints)
    {
      if (ShouldExcludeFromCapabilities(endpoint, helpOptions))
        continue;

      commands.Add(BuildCommandCapability(endpoint));
    }

    return new CapabilitiesResponse
    {
      Name = appName,
      Version = version,
      Description = description,
      Commands = commands
    };
  }

  /// <summary>
  /// Determines if an endpoint should be excluded from capabilities output.
  /// </summary>
  private static bool ShouldExcludeFromCapabilities(Endpoint endpoint, HelpOptions helpOptions)
  {
    string pattern = endpoint.RoutePattern;

    // Always exclude --capabilities itself
    if (pattern == "--capabilities")
      return true;

    // Exclude help routes
    if (pattern is "--help" or "--help?" or "help" || pattern.EndsWith(" --help?", StringComparison.Ordinal))
      return true;

    // Exclude version route
    if (pattern is "--version,-v" or "--version")
      return true;

    // Exclude REPL commands
    if (HelpOptions.ReplCommandPatterns.Contains(pattern))
      return true;

    // Exclude completion routes
    foreach (string prefix in HelpOptions.CompletionRoutePrefixes)
    {
      if (pattern.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        return true;
    }

    // Exclude interactive routes
    if (pattern is "--interactive,-i" or "--interactive" or "-i")
      return true;

    // Exclude check-updates route
    if (pattern == "--check-updates")
      return true;

    // Exclude custom patterns from HelpOptions
    if (helpOptions.ExcludePatterns is not null)
    {
      foreach (string excludePattern in helpOptions.ExcludePatterns)
      {
        if (MatchesWildcardPattern(pattern, excludePattern))
          return true;
      }
    }

    return false;
  }

  /// <summary>
  /// Matches a pattern against a wildcard pattern (supports * for any characters).
  /// </summary>
  private static bool MatchesWildcardPattern(string value, string pattern)
  {
    if (pattern == "*")
      return true;

    if (pattern.StartsWith('*') && pattern.EndsWith('*'))
    {
      string middle = pattern[1..^1];
      return value.Contains(middle, StringComparison.OrdinalIgnoreCase);
    }

    if (pattern.StartsWith('*'))
    {
      string suffix = pattern[1..];
      return value.EndsWith(suffix, StringComparison.OrdinalIgnoreCase);
    }

    if (pattern.EndsWith('*'))
    {
      string prefix = pattern[..^1];
      return value.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);
    }

    return string.Equals(value, pattern, StringComparison.OrdinalIgnoreCase);
  }

  /// <summary>
  /// Builds command capability metadata from an endpoint.
  /// </summary>
  private static CommandCapability BuildCommandCapability(Endpoint endpoint)
  {
    List<ParameterCapability> parameters = [];
    List<OptionCapability> options = [];

    foreach (RouteMatcher segment in endpoint.CompiledRoute.Segments)
    {
      switch (segment)
      {
        case ParameterMatcher param:
          parameters.Add(new ParameterCapability
          {
            Name = param.Name,
            Type = param.Constraint ?? "string",
            Required = !param.IsOptional,
            Description = param.Description,
            IsCatchAll = param.IsCatchAll
          });
          break;

        case OptionMatcher opt:
          options.Add(BuildOptionCapability(opt));
          break;
      }
    }

    return new CommandCapability
    {
      Pattern = endpoint.RoutePattern,
      Description = endpoint.Description,
      MessageType = ConvertMessageType(endpoint.MessageType),
      Parameters = parameters,
      Options = options
    };
  }

  /// <summary>
  /// Builds option capability metadata from an option matcher.
  /// </summary>
  private static OptionCapability BuildOptionCapability(OptionMatcher opt)
  {
    string name = opt.MatchPattern.TrimStart('-');
    string? alias = opt.AlternateForm?.TrimStart('-');
    string type = opt.ExpectsValue ? "string" : "bool";

    return new OptionCapability
    {
      Name = name,
      Alias = alias,
      Type = type,
      Required = !opt.IsOptional,
      Description = opt.Description,
      IsRepeated = opt.IsRepeated
    };
  }

  /// <summary>
  /// Converts MessageType enum to lowercase kebab-case string for JSON output.
  /// </summary>
  private static string ConvertMessageType(MessageType messageType) => messageType switch
  {
    MessageType.Query => "query",
    MessageType.Command => "command",
    MessageType.IdempotentCommand => "idempotent-command",
    MessageType.Unspecified => "unspecified",
    _ => "unspecified"
  };

  /// <summary>
  /// Gets the entry assembly name.
  /// </summary>
  private static string? GetEntryAssemblyName()
  {
    Assembly? entryAssembly = Assembly.GetEntryAssembly();
    return entryAssembly?.GetName().Name;
  }

  /// <summary>
  /// Gets the entry assembly version for capabilities output.
  /// </summary>
  private static string? GetCapabilitiesVersion()
  {
    Assembly? entryAssembly = Assembly.GetEntryAssembly();
    if (entryAssembly is null)
      return null;

    string? version = entryAssembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
      ?? entryAssembly.GetName().Version?.ToString();

    if (version is null)
      return null;

    // Strip build metadata suffix (+<hash>) if present
    int plusIndex = version.IndexOf('+', StringComparison.Ordinal);
    return plusIndex >= 0 ? version[..plusIndex] : version;
  }
}
