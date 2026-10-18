namespace TimeWarp.Nuru.McpHost;

/// <summary>
/// Serves a Nuru app's routes as MCP tools.
/// </summary>
/// <remarks>
/// <see cref="RunStdioAsync"/> is what the built-in <c>mcp</c> route runs (<c>AddMcpRoute()</c>).
/// Apps that already run an MCP host (for example ASP.NET Core with streamable HTTP) use
/// <see cref="NuruMcpServerBuilderExtensions.WithNuruTools"/> instead.
/// </remarks>
public static class NuruMcpServer
{
  /// <summary>
  /// Creates one <see cref="NuruMcpTool"/> per route in the app's catalog, after the group filter.
  /// </summary>
  /// <param name="app">The app to publish.</param>
  /// <param name="options">Host options; null uses the defaults.</param>
  /// <returns>The tools, in catalog order.</returns>
  public static IReadOnlyList<NuruMcpTool> CreateTools(NuruApp app, NuruMcpHostOptions? options = null)
  {
    ArgumentNullException.ThrowIfNull(app);

    NuruMcpToolInvoker invoker = new(app, options);
    return
    [
      .. NuruMcpToolCatalog.CreateTools(app.Capabilities, options)
        .Select(definition => new NuruMcpTool(definition, invoker))
    ];
  }

  /// <summary>
  /// Server options with the app's tools, server name and version from the catalog, and instructions.
  /// </summary>
  /// <param name="app">The app to publish.</param>
  /// <param name="options">Host options; null uses the defaults.</param>
  /// <returns>Options for <see cref="McpServer.Create"/>.</returns>
  public static McpServerOptions CreateServerOptions(NuruApp app, NuruMcpHostOptions? options = null)
  {
    ArgumentNullException.ThrowIfNull(app);

    CapabilitiesResponse catalog = app.Capabilities;
    McpServerOptions serverOptions = new()
    {
      ServerInfo = new Implementation { Name = catalog.Name, Version = catalog.Version },
      ServerInstructions = NuruMcpToolCatalog.CreateInstructions(catalog, options),
      ToolCollection = []
    };

    foreach (NuruMcpTool tool in CreateTools(app, options))
      serverOptions.ToolCollection.Add(tool);

    return serverOptions;
  }

  /// <summary>
  /// Serves the app over stdio until the client closes stdin or <paramref name="cancellationToken"/> fires.
  /// </summary>
  /// <param name="app">The app to publish.</param>
  /// <param name="options">Host options; null uses the defaults.</param>
  /// <param name="cancellationToken">Stops the server.</param>
  /// <returns>Exit code 0.</returns>
  /// <remarks>
  /// Stdout carries the protocol. While the server runs, <see cref="Console.Out"/> is pointed at stderr so
  /// stray writes cannot corrupt the stream; text written during a call goes into that call's result
  /// (see <see cref="NuruMcpHostOptions.CaptureConsoleOutput"/>).
  /// </remarks>
  public static async Task<int> RunStdioAsync(NuruApp app, NuruMcpHostOptions? options = null, CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(app);

    McpServerOptions serverOptions = CreateServerOptions(app, options);
    (TextWriter Out, TextWriter Error) previous = ConsoleRedirect.RedirectOutToError();
    try
    {
      StdioServerTransport transport = new(serverOptions);
      await using (transport.ConfigureAwait(false))
      {
        await RunAsync(transport, serverOptions, cancellationToken).ConfigureAwait(false);
      }
    }
    finally
    {
      ConsoleRedirect.Restore(previous);
    }

    return 0;
  }

  /// <summary>
  /// Serves the app over a pair of streams (newline-delimited JSON-RPC, as stdio) until the input ends.
  /// </summary>
  /// <param name="app">The app to publish.</param>
  /// <param name="input">Stream the client writes requests to.</param>
  /// <param name="output">Stream the server writes responses to.</param>
  /// <param name="options">Host options; null uses the defaults.</param>
  /// <param name="cancellationToken">Stops the server.</param>
  /// <returns>A task that completes when the session ends.</returns>
  public static async Task RunAsync(NuruApp app, Stream input, Stream output, NuruMcpHostOptions? options = null, CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(app);
    ArgumentNullException.ThrowIfNull(input);
    ArgumentNullException.ThrowIfNull(output);

    McpServerOptions serverOptions = CreateServerOptions(app, options);
    StreamServerTransport transport = new(input, output, serverOptions.ServerInfo?.Name);
    await using (transport.ConfigureAwait(false))
    {
      await RunAsync(transport, serverOptions, cancellationToken).ConfigureAwait(false);
    }
  }

  private static async Task RunAsync(ITransport transport, McpServerOptions serverOptions, CancellationToken cancellationToken)
  {
    McpServer server = McpServer.Create(transport, serverOptions);
    await using (server.ConfigureAwait(false))
    {
      try
      {
        await server.RunAsync(cancellationToken).ConfigureAwait(false);
      }
      catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
      {
        // Stopping on request is a normal end of the session.
      }
    }
  }
}
