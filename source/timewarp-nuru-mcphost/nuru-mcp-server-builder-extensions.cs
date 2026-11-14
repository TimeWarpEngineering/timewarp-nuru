#region Purpose
// Extension that adds a Nuru app's routes to an MCP server registered with AddMcpServer().
#endregion

#region Design
// Public API on IMcpServerBuilder so a host that already owns an MCP server (ASP.NET Core with
// streamable HTTP) can publish a Nuru app without the stdio-only built-in mcp route. It is a runtime
// extension, not a DSL stub: the source generator does not interpret it, and the generator must ignore
// the surrounding non-Nuru builder chain (AddMcpServer, builder.Services; kanban 531). The app is
// passed already built; tools and instructions come from app.Capabilities, the same catalog
// --capabilities prints. The tool list is fixed when the call runs. Instructions and server info use
// ??= so a value the host set earlier wins over the app's catalog. The invoker copies the options,
// so the caller's instance is not shared.
#endregion

namespace TimeWarp.Nuru.McpHost;

/// <summary>
/// Adds a Nuru app's routes to an MCP server registered with <c>AddMcpServer()</c>.
/// </summary>
public static class NuruMcpServerBuilderExtensions
{
  /// <summary>
  /// Registers one MCP tool per route of <paramref name="app"/>, and sets the server instructions
  /// and server info from the app's catalog when the host has not set them.
  /// </summary>
  /// <param name="builder">The MCP server builder.</param>
  /// <param name="app">The built Nuru app whose routes become tools.</param>
  /// <param name="configure">Configures the host options (group filter, instructions, content format).</param>
  /// <returns>The builder for chaining.</returns>
  /// <example>
  /// Streamable HTTP in an ASP.NET Core app (needs the ModelContextProtocol.AspNetCore package):
  /// <code>
  /// builder.Services.AddMcpServer()
  ///   .WithHttpTransport()
  ///   .WithNuruTools(nuruApp, options => options.GroupFilter = "admin");
  /// WebApplication web = builder.Build();
  /// web.MapMcp();
  /// </code>
  /// </example>
  public static IMcpServerBuilder WithNuruTools(this IMcpServerBuilder builder, NuruApp app, Action<NuruMcpHostOptions>? configure = null)
  {
    ArgumentNullException.ThrowIfNull(builder);
    ArgumentNullException.ThrowIfNull(app);

    NuruMcpHostOptions options = new();
    configure?.Invoke(options);

    CapabilitiesResponse catalog = app.Capabilities;
    string? instructions = NuruMcpToolCatalog.CreateInstructions(catalog, options);

    builder.WithTools(NuruMcpServer.CreateTools(app, options));
    builder.Services.Configure<McpServerOptions>(serverOptions =>
    {
      serverOptions.ServerInstructions ??= instructions;
      serverOptions.ServerInfo ??= new Implementation { Name = catalog.Name, Version = catalog.Version };
    });

    return builder;
  }
}
