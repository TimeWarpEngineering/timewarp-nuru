using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

// Configure all logs to go to stderr (stdout is used for the MCP protocol messages).
builder.Logging.AddConsole(o => o.LogToStandardErrorThreshold = LogLevel.Trace);

// Add the MCP services: the transport to use (stdio) and the tools to register.
builder.Services
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithTools<RandomNumberTools>()
    .WithTools<TimeWarp.Nuru.Mcp.Tools.GetExampleTool>()
    .WithTools<TimeWarp.Nuru.Mcp.Tools.CacheManagementTool>()
    .WithTools<TimeWarp.Nuru.Mcp.Tools.ValidateRouteTool>()
    .WithTools<TimeWarp.Nuru.Mcp.Tools.GetSyntaxTool>()
    .WithTools<TimeWarp.Nuru.Mcp.Tools.GenerateHandlerTool>()
    .WithTools<TimeWarp.Nuru.Mcp.Tools.ErrorHandlingTool>();

await builder.Build().RunAsync().ConfigureAwait(false);
