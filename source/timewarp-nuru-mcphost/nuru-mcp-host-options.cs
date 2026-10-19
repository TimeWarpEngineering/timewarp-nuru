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
  /// <see cref="Console.Error"/> during a call is captured into the tool result instead of the
  /// process streams. Stdio hosting needs this: stdout carries the protocol.
  /// </summary>
  public bool CaptureConsoleOutput { get; set; } = true;

  /// <summary>Copies the options so a host can keep its own instance.</summary>
  internal NuruMcpHostOptions Clone() => new()
  {
    Instructions = Instructions,
    GroupFilter = GroupFilter,
    ContentFormat = ContentFormat,
    ToolNameSeparator = ToolNameSeparator,
    CaptureConsoleOutput = CaptureConsoleOutput
  };
}
