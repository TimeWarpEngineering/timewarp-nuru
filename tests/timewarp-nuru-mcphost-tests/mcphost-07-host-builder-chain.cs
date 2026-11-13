#!/usr/bin/env -S dotnet --
#:project $(SourceDirectory)timewarp-nuru-mcphost/timewarp-nuru-mcphost.csproj

#region Purpose
// Pins task 531: WithNuruTools on an ASP.NET-shaped host builder chain compiles and registers the tools,
// in the same method body that builds the Nuru app.
#endregion

#region Design
// The generator interprets the whole method body that holds a NuruApp Build() call. Before task 531 it
// took any CreateBuilder call for NuruApp.CreateBuilder, so WebApplication.CreateBuilder(args) became a
// Nuru builder and builder.Services.AddMcpServer() failed the build with NURU_S999. This file compiling
// is the main assertion. The host builder here is a stand-in with the same static CreateBuilder shape as
// WebApplication (the tests do not reference ASP.NET Core); AddMcpServer and WithNuruTools are the real
// ones, so the test also checks that the tools land in the container.
#endregion

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.McpHost.HostBuilderChain
{
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Server;
using TimeWarp.Nuru;
using TimeWarp.Nuru.McpHost;

/// <summary>Stand-in for <c>WebApplication</c>: a non-Nuru type with a static <c>CreateBuilder</c>.</summary>
public static class HbcWebApplication
{
  public static HbcWebApplicationBuilder CreateBuilder(string[] args) => new(args);
}

/// <summary>Stand-in for <c>WebApplicationBuilder</c>: exposes <c>Services</c> and a non-Nuru <c>Build</c>.</summary>
public sealed class HbcWebApplicationBuilder(string[] args)
{
  public int ArgCount { get; } = args.Length;

  public IServiceCollection Services { get; } = new ServiceCollection();

  public ServiceProvider Build() => Services.BuildServiceProvider();
}

[TestTag("McpHost")]
public class McpHostHostBuilderChainTests
{
  [ModuleInitializer]
  internal static void Register() => RegisterTests<McpHostHostBuilderChainTests>();

  public static async Task Should_register_nuru_tools_on_a_host_builder_chain_in_the_same_method()
  {
    // Arrange
    NuruApp nuruApp = NuruApp.CreateBuilder()
      .Map("hbc-ping").WithHandler(() => "pong").WithDescription("Ping").AsQuery().Done()
      .Map("hbc-greet {name}").WithHandler((string name) => $"Hello, {name}").AsQuery().Done()
      .Build();

    HbcWebApplicationBuilder builder = HbcWebApplication.CreateBuilder([]);
    builder.Services.AddMcpServer()
      .WithNuruTools(nuruApp);

    // Act
    await using ServiceProvider web = builder.Build();
    string[] toolNames = [.. web.GetServices<McpServerTool>().Select(tool => tool.ProtocolTool.Name)];

    // Assert
    toolNames.ShouldContain("hbc-ping");
    toolNames.ShouldContain("hbc-greet");
  }

  public static async Task Should_still_dispatch_the_nuru_app_built_beside_the_host_builder()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruApp nuruApp = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("hbc-run").WithHandler(() => "ran").AsQuery().Done()
      .Build();

    HbcWebApplicationBuilder builder = HbcWebApplication.CreateBuilder([]);
    builder.Services.AddMcpServer().WithNuruTools(nuruApp);
    await using ServiceProvider web = builder.Build();

    // Act
    int exitCode = await nuruApp.RunAsync(["hbc-run"]);

    // Assert
    exitCode.ShouldBe(0);
    terminal.Output.ShouldContain("ran");
  }
}

}
