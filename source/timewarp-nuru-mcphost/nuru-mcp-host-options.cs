namespace TimeWarp.Nuru.McpHost;

/// <summary>
/// What a <c>tools/call</c> result carries.
/// </summary>
public enum NuruMcpContentFormat
{
  /// <summary>The text the CLI writes for the route, as one text content block.</summary>
  Text = 0,

  /// <summary>
  /// The CLI text plus <c>structuredContent</c>: the JSON result for <c>json</c> routes, or
  /// <c>{"result": value}</c> for scalars and JSON arrays. Routes that return nothing carry text only.
  /// </summary>
  Json = 1
}

/// <summary>
/// Options for serving a Nuru app's routes as MCP tools.
/// </summary>
public sealed class NuruMcpHostOptions
{
  /// <summary>Separator between the literal words of a route in its tool name.</summary>
  public const string DefaultToolNameSeparator = "_";

  /// <summary>Tool name for a route with no literal words (the default route).</summary>
  public const string DefaultRouteToolName = "default";

  /// <summary>
  /// Text appended to the server instructions after the app description.
  /// </summary>
  public string? Instructions { get; set; }

  /// <summary>
  /// Dot-separated group prefix. Only routes in that group become tools, by the same rule as
  /// <c>--capabilities --group-filter</c> (<see cref="CapabilitiesResponse.FilterByGroup"/>).
  /// </summary>
  public string? GroupFilter { get; set; }

  /// <summary>What a <c>tools/call</c> result carries. Default <see cref="NuruMcpContentFormat.Text"/>.</summary>
  public NuruMcpContentFormat ContentFormat { get; set; } = NuruMcpContentFormat.Text;

  /// <summary>
  /// Separator between a route's literal words in its tool name. Default <c>_</c>, so
  /// <c>config set {key} {value}</c> is the tool <c>config_set</c>.
  /// </summary>
  public string ToolNameSeparator { get; set; } = DefaultToolNameSeparator;

  /// <summary>
  /// When true (the default), text a handler writes to <see cref="Console.Out"/> or
  /// <see cref="Console.Error"/> during a call is captured into that call's tool result instead of the
  /// process streams. Stdio hosting needs this: stdout carries the protocol. Writes through
  /// <see cref="ITerminal"/> always go to the call's result.
  /// </summary>
  public bool CaptureConsoleOutput { get; set; } = true;

  /// <summary>
  /// When true, tool calls run one at a time, as if every route were marked
  /// <see cref="ExclusiveAttribute"/>. When false (the default) calls run concurrently and only exclusive
  /// routes wait for each other. Applies to calls this process runs; a forwarded call follows the
  /// server's own options.
  /// </summary>
  public bool ExclusiveCalls { get; set; }

  /// <summary>
  /// Annotation overrides keyed by tool name, for an app that serves contracts it does not own.
  /// Every hint an entry sets (non-null) replaces the one derived from the catalog (endpoint kind,
  /// <c>[TimeWarp.Contracts.Destructive]</c>, <c>[TimeWarp.Contracts.OpenWorld]</c>, an operation's
  /// <c>Title</c>, or <c>[DisplayName]</c> on a route class with no operation); the
  /// others keep the derived value. An entry's <see cref="ModelContextProtocol.Protocol.ToolAnnotations.Title"/> is also the tool title.
  /// </summary>
  /// <example>
  /// <code>
  /// options.ToolAnnotations["find_references"] = new ToolAnnotations { OpenWorldHint = false, Title = "Find references" };
  /// </code>
  /// </example>
  public IDictionary<string, ToolAnnotations> ToolAnnotations { get; } = new Dictionary<string, ToolAnnotations>(StringComparer.Ordinal);

  /// <summary>Copies the options so a host can keep its own instance.</summary>
  internal NuruMcpHostOptions Clone()
  {
    NuruMcpHostOptions copy = new()
    {
      Instructions = Instructions,
      GroupFilter = GroupFilter,
      ContentFormat = ContentFormat,
      ToolNameSeparator = ToolNameSeparator,
      CaptureConsoleOutput = CaptureConsoleOutput,
      ExclusiveCalls = ExclusiveCalls
    };

    foreach (KeyValuePair<string, ToolAnnotations> entry in ToolAnnotations)
      copy.ToolAnnotations[entry.Key] = entry.Value;

    return copy;
  }
}
