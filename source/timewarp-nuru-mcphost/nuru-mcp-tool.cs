namespace TimeWarp.Nuru.McpHost;

/// <summary>
/// An MCP server tool that calls one Nuru route. Created by <see cref="NuruMcpServer.CreateTools"/>.
/// </summary>
public sealed class NuruMcpTool : McpServerTool
{
  private readonly NuruMcpToolInvoker Invoker;

  /// <summary>
  /// Creates a tool for <paramref name="definition"/> that runs through <paramref name="invoker"/>.
  /// </summary>
  /// <param name="definition">The tool definition and the endpoint it calls.</param>
  /// <param name="invoker">Runs calls against the app.</param>
  public NuruMcpTool(NuruMcpToolDefinition definition, NuruMcpToolInvoker invoker)
  {
    ArgumentNullException.ThrowIfNull(definition);
    ArgumentNullException.ThrowIfNull(invoker);
    Definition = definition;
    Invoker = invoker;
  }

  /// <summary>The tool definition and the endpoint it calls.</summary>
  public NuruMcpToolDefinition Definition { get; }

  /// <inheritdoc />
  public override Tool ProtocolTool => Definition.Tool;

  /// <inheritdoc />
  public override IReadOnlyList<object> Metadata => [];

  /// <inheritdoc />
  public override async ValueTask<CallToolResult> InvokeAsync(
    RequestContext<CallToolRequestParams> request,
    CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(request);
    return await Invoker.CallAsync(Definition.Endpoint, request.Params?.Arguments, cancellationToken).ConfigureAwait(false);
  }
}
