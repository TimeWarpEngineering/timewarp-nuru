namespace TimeWarp.Nuru;

/// <summary>
/// Machine-readable metadata about CLI capabilities for AI tool discovery.
/// Returned by the <c>--capabilities</c> flag.
/// </summary>
/// <remarks>
/// <para>
/// This enables AI tools (OpenCode, Claude, etc.) to discover CLI capabilities
/// without MCP complexity, similar to how <c>--help</c> and <c>--version</c> are well-known flags.
/// </para>
/// <para>
/// Commands are organized hierarchically: grouped commands appear only within their group,
/// while ungrouped commands appear at the top level. Commands are never duplicated.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// {
///   "name": "mytool",
///   "version": "1.0.0",
///   "groups": [
///     {
///       "name": "admin",
///       "groups": [
///         { "name": "config", "commands": [{"pattern": "admin config get {key}", ...}] }
///       ],
///       "commands": [{"pattern": "admin status", ...}]
///     }
///   ],
///   "commands": [{"pattern": "version", ...}]
/// }
/// </code>
/// </example>
internal sealed class CapabilitiesResponse
{
  /// <summary>
  /// Gets the application name.
  /// </summary>
  public required string Name { get; init; }

  /// <summary>
  /// Gets the application version.
  /// </summary>
  public required string Version { get; init; }

  /// <summary>
  /// Gets the application description.
  /// </summary>
  public string? Description { get; init; }

  /// <summary>
  /// Gets the top-level route groups with hierarchical nesting.
  /// Groups contain their own nested groups and direct commands.
  /// </summary>
  public IReadOnlyList<GroupCapability>? Groups { get; init; }

  /// <summary>
  /// Gets the list of ungrouped commands at the top level.
  /// Grouped commands appear only within their respective groups.
  /// </summary>
  public required IReadOnlyList<CommandCapability> Commands { get; init; }
}

/// <summary>
/// Metadata for a route group containing nested groups and commands.
/// Groups represent hierarchical command organization (e.g., "admin", "admin config").
/// </summary>
internal sealed class GroupCapability
{
  /// <summary>
  /// Gets the group name (single word, e.g., "admin", "config").
  /// </summary>
  public required string Name { get; init; }

  /// <summary>
  /// Gets nested groups within this group.
  /// For example, "admin" group may contain a "config" nested group.
  /// </summary>
  public IReadOnlyList<GroupCapability>? Groups { get; init; }

  /// <summary>
  /// Gets the commands directly within this group (not in nested groups).
  /// </summary>
  public IReadOnlyList<CommandCapability>? Commands { get; init; }
}

/// <summary>
/// Metadata for a single command/route.
/// </summary>
internal sealed class CommandCapability
{
  /// <summary>
  /// Gets the route pattern (e.g., "users list", "user create {name}").
  /// </summary>
  public required string Pattern { get; init; }

  /// <summary>
  /// Gets the command description.
  /// </summary>
  public string? Description { get; init; }

  /// <summary>
  /// Gets the message type indicating command safety for AI agents.
  /// Values: "query", "command", "idempotent-command", "unspecified"
  /// </summary>
  /// <remarks>
  /// <list type="bullet">
  ///   <item><description>"query" - No state change, safe to run freely</description></item>
  ///   <item><description>"command" - State change, confirm before running</description></item>
  ///   <item><description>"idempotent-command" - State change but repeatable, safe to retry</description></item>
  ///   <item><description>"unspecified" - Treated as "command" for safety</description></item>
  /// </list>
  /// </remarks>
  public required string MessageType { get; init; }

  /// <summary>
  /// Gets the list of positional parameters for this command.
  /// </summary>
  public required IReadOnlyList<ParameterCapability> Parameters { get; init; }

  /// <summary>
  /// Gets the list of options (flags and named arguments) for this command.
  /// </summary>
  public required IReadOnlyList<OptionCapability> Options { get; init; }
}

/// <summary>
/// Metadata for a positional parameter.
/// </summary>
internal sealed class ParameterCapability
{
  /// <summary>
  /// Gets the parameter name.
  /// </summary>
  public required string Name { get; init; }

  /// <summary>
  /// Gets the parameter type (e.g., "string", "int", "bool").
  /// Defaults to "string" if no type constraint is specified.
  /// </summary>
  public string Type { get; init; } = "string";

  /// <summary>
  /// Gets whether this parameter is required.
  /// </summary>
  public bool Required { get; init; } = true;

  /// <summary>
  /// Gets the parameter description.
  /// </summary>
  public string? Description { get; init; }

  /// <summary>
  /// Gets whether this is a catch-all parameter that captures remaining arguments.
  /// </summary>
  public bool IsCatchAll { get; init; }
}

/// <summary>
/// Metadata for an option (flag or named argument).
/// </summary>
internal sealed class OptionCapability
{
  /// <summary>
  /// Gets the option name (long form without dashes, e.g., "format").
  /// </summary>
  public required string Name { get; init; }

  /// <summary>
  /// Gets the option alias (short form without dash, e.g., "f").
  /// </summary>
  public string? Alias { get; init; }

  /// <summary>
  /// Gets the option type.
  /// "bool" for flags, "string" for value options.
  /// </summary>
  public string Type { get; init; } = "bool";

  /// <summary>
  /// Gets whether this option is required.
  /// </summary>
  public bool Required { get; init; }

  /// <summary>
  /// Gets the option description.
  /// </summary>
  public string? Description { get; init; }

  /// <summary>
  /// Gets whether this option can be repeated to collect multiple values.
  /// </summary>
  public bool IsRepeated { get; init; }
}
