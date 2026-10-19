#!/usr/bin/env -S dotnet --
#:project $(SourceDirectory)timewarp-nuru-daemon/timewarp-nuru-daemon.csproj

// TimeWarp.Nuru.Daemon (task 508-005): what AddServeRoute() generates. A matched route is offered to
// NuruApp.RouteForwarder after parsing and before behaviors and the handler; local-only routes and
// built-ins never are; --daemon / --no-daemon set the mode and are removed; --json-args forwards the
// resolved object; the catalog carries localOnly; help lists the serve routes. The tests call
// NuruApp.ExecuteAsync, the entry point for apps built in another method (RunAsync interception
// needs the builder chain at the call site).

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.Daemon.Forwarding
{
using TimeWarp.Nuru.Daemon;

[LocalOnly]
[NuruRoute("df-local-endpoint", Description = "Endpoint that never forwards")]
public sealed class DfLocalEndpointQuery : IQuery<string>
{
  public sealed class Handler : IQueryHandler<DfLocalEndpointQuery, string>
  {
    public Task<string> Handle(DfLocalEndpointQuery query, CancellationToken cancellationToken) => Task.FromResult("local endpoint");
  }
}

[NuruRoute("df-remote-endpoint", Description = "Endpoint that forwards")]
public sealed class DfRemoteEndpointQuery : IQuery<string>
{
  [Parameter(Description = "A value")]
  public string Value { get; set; } = "";

  public sealed class Handler : IQueryHandler<DfRemoteEndpointQuery, string>
  {
    public Task<string> Handle(DfRemoteEndpointQuery query, CancellationToken cancellationToken) => Task.FromResult($"local {query?.Value}");
  }
}

/// <summary>Records each request and answers with a fixed result (null: run locally).</summary>
public sealed class RecordingForwarder(NuruForwardResult? result) : INuruRouteForwarder
{
  public System.Collections.ObjectModel.Collection<NuruForwardRequest> Requests { get; } = [];

  public Task<NuruForwardResult?> ForwardAsync(NuruApp app, NuruForwardRequest request, CancellationToken cancellationToken)
  {
    Requests.Add(request);
    return Task.FromResult(result);
  }
}

[TestTag("Daemon")]
public class DaemonForwardingTests
{
  [ModuleInitializer]
  internal static void Register() => RegisterTests<DaemonForwardingTests>();

  private static readonly NuruForwardResult ServerResult = new(7, "from server\n", "server warning\n");

  private static NuruApp CreateApp(TestTerminal terminal)
  {
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .WithName("df-app")
      .Map("df-greet {name}").WithHandler((string name) => $"local {name}").AsQuery().Done()
      .Map("df-count {n:int}").WithHandler((int n) => n * 2).AsQuery().Done()
      .Map("df-here").WithHandler(() => "here").AsQuery().LocalOnly().Done()
      .Map<DfLocalEndpointQuery>()
      .Map<DfRemoteEndpointQuery>()
      .AddServeRoute()
      .Build();
    return app;
  }

  public static async Task Should_attach_the_daemon_client()
  {
    using TestTerminal terminal = new();
    NuruApp app = CreateApp(terminal);
    app.RouteForwarder.ShouldBeOfType<NuruDaemonClient>();
    await Task.CompletedTask;
  }

  public static async Task Should_forward_a_matched_route_and_print_the_server_result()
  {
    using TestTerminal terminal = new();
    NuruApp app = CreateApp(terminal);
    RecordingForwarder forwarder = new(ServerResult);
    app.RouteForwarder = forwarder;

    int exitCode = await app.ExecuteAsync(["df-greet", "bob"]);

    exitCode.ShouldBe(7);
    terminal.Output.ShouldContain("from server");
    terminal.Output.ShouldNotContain("local bob");
    terminal.ErrorOutput.ShouldContain("server warning");
    forwarder.Requests.Count.ShouldBe(1);
    NuruForwardRequest request = forwarder.Requests[0];
    request.Route.ShouldBe("df-greet {name}");
    request.Args.ShouldBe(["df-greet", "bob"]);
    request.JsonArguments.ShouldBeNull();
    request.Mode.ShouldBe(NuruForwardMode.Auto);
  }

  public static async Task Should_forward_endpoint_routes()
  {
    using TestTerminal terminal = new();
    NuruApp app = CreateApp(terminal);
    RecordingForwarder forwarder = new(ServerResult);
    app.RouteForwarder = forwarder;

    (await app.ExecuteAsync(["df-remote-endpoint", "x"])).ShouldBe(7);
    // The route is the catalog pattern (EndpointCapability.Pattern), the key TryBind uses.
    forwarder.Requests.Single().Route.ShouldBe(app.Capabilities.Endpoints.Single(e => e.Description == "Endpoint that forwards").Pattern);
  }

  public static async Task Should_run_locally_when_the_forwarder_declines()
  {
    using TestTerminal terminal = new();
    NuruApp app = CreateApp(terminal);
    RecordingForwarder forwarder = new(result: null);
    app.RouteForwarder = forwarder;

    (await app.ExecuteAsync(["df-greet", "bob"])).ShouldBe(0);
    terminal.Output.ShouldContain("local bob");
    forwarder.Requests.Count.ShouldBe(1);
  }

  public static async Task Should_pass_require_mode_for_daemon_flag_and_remove_it()
  {
    using TestTerminal terminal = new();
    NuruApp app = CreateApp(terminal);
    RecordingForwarder forwarder = new(ServerResult);
    app.RouteForwarder = forwarder;

    (await app.ExecuteAsync(["--daemon", "df-greet", "bob"])).ShouldBe(7);
    NuruForwardRequest request = forwarder.Requests.Single();
    request.Mode.ShouldBe(NuruForwardMode.Require);
    request.Args.ShouldBe(["df-greet", "bob"]);
  }

  public static async Task Should_run_locally_with_no_daemon_flag()
  {
    using TestTerminal terminal = new();
    NuruApp app = CreateApp(terminal);
    RecordingForwarder forwarder = new(ServerResult);
    app.RouteForwarder = forwarder;

    (await app.ExecuteAsync(["df-greet", "--no-daemon", "bob"])).ShouldBe(0);
    terminal.Output.ShouldContain("local bob");
    forwarder.Requests.ShouldBeEmpty();
  }

  public static async Task Should_never_forward_local_only_routes_or_built_ins()
  {
    using TestTerminal terminal = new();
    NuruApp app = CreateApp(terminal);
    RecordingForwarder forwarder = new(ServerResult);
    app.RouteForwarder = forwarder;

    (await app.ExecuteAsync(["df-here"])).ShouldBe(0);
    (await app.ExecuteAsync(["df-local-endpoint"])).ShouldBe(0);
    (await app.ExecuteAsync(["--version"])).ShouldBe(0);
    (await app.ExecuteAsync(["--capabilities"])).ShouldBe(0);
    (await app.ExecuteAsync(["--help"])).ShouldBe(0);

    forwarder.Requests.ShouldBeEmpty();
    terminal.Output.ShouldContain("here");
    terminal.Output.ShouldContain("local endpoint");
  }

  public static async Task Should_keep_parse_errors_local()
  {
    using TestTerminal terminal = new();
    NuruApp app = CreateApp(terminal);
    RecordingForwarder forwarder = new(ServerResult);
    app.RouteForwarder = forwarder;

    // "abc" is not an int: conversion fails here, before the forwarder is asked.
    (await app.ExecuteAsync(["df-count", "abc"])).ShouldNotBe(0);
    forwarder.Requests.ShouldBeEmpty();
  }

  public static async Task Should_forward_json_args_with_the_resolved_object()
  {
    using TestTerminal terminal = new();
    NuruApp app = CreateApp(terminal);
    RecordingForwarder forwarder = new(ServerResult);
    app.RouteForwarder = forwarder;

    (await app.ExecuteAsync(["df-greet", "--json-args", """{"name":"amy"}"""])).ShouldBe(7);
    NuruForwardRequest request = forwarder.Requests.Single();
    request.Route.ShouldBe("df-greet {name}");
    request.JsonArguments.ShouldBe("""{"name":"amy"}""");
    request.Args.ShouldBe(["df-greet", "--json-args", """{"name":"amy"}"""]);
  }

  public static async Task Should_run_every_route_locally_when_the_forwarder_is_removed()
  {
    using TestTerminal terminal = new();
    NuruApp app = CreateApp(terminal);
    app.RouteForwarder = null;

    (await app.ExecuteAsync(["df-greet", "bob"])).ShouldBe(0);
    terminal.Output.ShouldContain("local bob");
    app.RouteForwarder.ShouldBeNull();
  }

  public static async Task Should_carry_local_only_in_the_catalog()
  {
    using TestTerminal terminal = new();
    NuruApp app = CreateApp(terminal);
    CapabilitiesResponse catalog = app.Capabilities;

    catalog.Endpoints.Single(e => e.Pattern == "df-here").LocalOnly.ShouldBe(true);
    catalog.Endpoints.Single(e => e.Pattern == "df-local-endpoint").LocalOnly.ShouldBe(true);
    catalog.Endpoints.Single(e => e.Pattern == "df-greet {name}").LocalOnly.ShouldBeNull();
    catalog.Endpoints.Single(e => e.Pattern == "df-remote-endpoint").LocalOnly.ShouldBeNull();

    (await app.ExecuteAsync(["--capabilities"])).ShouldBe(0);
    terminal.Output.ShouldContain("\"localOnly\": true");
  }

  public static async Task Should_list_the_serve_routes_in_help()
  {
    using TestTerminal terminal = new();
    NuruApp app = CreateApp(terminal);
    (await app.ExecuteAsync(["--help"])).ShouldBe(0);
    terminal.Output.ShouldContain("serve --status | --stop");
    terminal.Output.ShouldContain("--daemon | --no-daemon");
  }

  public static async Task Should_report_a_stopped_server_from_the_serve_routes()
  {
    using TestTerminal terminal = new();
    NuruApp app = CreateApp(terminal);
    string socket = Path.Combine(Directory.CreateTempSubdirectory("nuru-df-").FullName, "df.sock");
    string? previous = Environment.GetEnvironmentVariable(NuruDaemonOptions.SocketPathEnvironmentVariable);
    Environment.SetEnvironmentVariable(NuruDaemonOptions.SocketPathEnvironmentVariable, socket);
    try
    {
      (await app.ExecuteAsync(["serve", "--status"])).ShouldBe(1);
      terminal.Output.ShouldContain("df-app server is not running");
      (await app.ExecuteAsync(["serve", "--stop"])).ShouldBe(0);
      terminal.Output.ShouldContain("df-app server is not running.");
    }
    finally
    {
      Environment.SetEnvironmentVariable(NuruDaemonOptions.SocketPathEnvironmentVariable, previous);
    }
  }

  public static async Task Should_peel_forwarding_flags_before_the_separator_only()
  {
    NuruForwarding.PeelFlags(["a", "--daemon", "b"], out NuruForwardMode require).ShouldBe(["a", "b"]);
    require.ShouldBe(NuruForwardMode.Require);

    NuruForwarding.PeelFlags(["--daemon", "a", "--no-daemon"], out NuruForwardMode never).ShouldBe(["a"]);
    never.ShouldBe(NuruForwardMode.Never);

    NuruForwarding.PeelFlags(["a", "--", "--daemon"], out NuruForwardMode auto).ShouldBe(["a", "--", "--daemon"]);
    auto.ShouldBe(NuruForwardMode.Auto);
    await Task.CompletedTask;
  }
}
}
