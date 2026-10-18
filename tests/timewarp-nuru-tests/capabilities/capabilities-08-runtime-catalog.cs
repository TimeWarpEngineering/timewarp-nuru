#!/usr/bin/env -S dotnet --
#:project $(SourceDirectory)timewarp-nuru/timewarp-nuru.csproj

// Tests for the in-process capabilities catalog (NuruApp.Capabilities) and the returnType descriptor.

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.Core.CapabilitiesRuntimeCatalog
{

[TestTag("Capabilities")]
public class CapabilitiesRuntimeCatalogTests
{
  [ModuleInitializer]
  internal static void Register() => RegisterTests<CapabilitiesRuntimeCatalogTests>();

  public static async Task Should_expose_catalog_after_build_without_run()
  {
    // Arrange
    NuruApp app = NuruApp.CreateBuilder()
      .WithName("rtc-tool")
      .WithDescription("Runtime catalog tool")
      .Map("rtc-only {name}").WithHandler((string name) => name).WithDescription("Only route").AsQuery().Done()
      .Build();

    // Act
    CapabilitiesResponse catalog = app.Capabilities;

    // Assert
    catalog.Name.ShouldBe("rtc-tool");
    catalog.Description.ShouldBe("Runtime catalog tool");
    catalog.Filter.ShouldBeNull();
    catalog.Invocation.ShouldBe(InvocationCapability.Standard);
    catalog.Endpoints.Count.ShouldBe(1);
    catalog.Endpoints[0].Pattern.ShouldBe("rtc-only {name}");
    catalog.Endpoints[0].Description.ShouldBe("Only route");
    catalog.Endpoints[0].Kind.ShouldBe(EndpointKind.Query);
    catalog.Endpoints[0].Parameters.Count.ShouldBe(1);
    catalog.Endpoints[0].Parameters[0].Name.ShouldBe("name");

    await Task.CompletedTask;
  }

  public static async Task Should_expose_catalog_when_build_is_called_on_the_endpoint_builder()
  {
    // Arrange: Build() declared on EndpointBuilder<TBuilder>, not NuruAppBuilder.
    NuruApp app = NuruApp.CreateBuilder()
      .Map("rtc-endpoint-build").WithHandler(() => "x").AsQuery()
      .Build();

    // Act
    CapabilitiesResponse catalog = app.Capabilities;

    // Assert
    catalog.Endpoints.Select(e => e.Pattern).ShouldBe(["rtc-endpoint-build"]);

    await Task.CompletedTask;
  }

  public static async Task Should_return_the_same_cached_instance()
  {
    // Arrange
    NuruApp app = NuruApp.CreateBuilder()
      .Map("rtc-cached").WithHandler(() => "x").AsQuery().Done()
      .Build();

    // Act
    CapabilitiesResponse first = app.Capabilities;
    CapabilitiesResponse second = app.Capabilities;

    // Assert
    ReferenceEquals(first, second).ShouldBeTrue("The unfiltered catalog is built once and cached");

    await Task.CompletedTask;
  }

  public static async Task Should_print_the_catalog_instance()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .WithName("rtc-print")
      .WithGroupPrefix("ops")
        .Map("deploy {env}").WithHandler((string env) => env).WithDescription("Deploy").AsCommand().Done()
        .Done()
      .Map("rtc-status").WithHandler(() => 1).WithExample("rtc-status", "Show status").AsQuery().Done()
      .Map("rtc-flag --verbose").WithHandler((bool verbose) => verbose ? "loud" : "quiet").AsCommand().Done()
      .Build();

    string expected = System.Text.Json.JsonSerializer.Serialize(app.Capabilities, CapabilitiesJsonSerializerContext.Default.CapabilitiesResponse);

    // Act
    int exitCode = await app.RunAsync(["--capabilities"]);

    // Assert
    exitCode.ShouldBe(0);
    terminal.Output.TrimEnd('\r', '\n').ShouldBe(expected);
  }

  public static async Task Should_print_the_filtered_catalog()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .WithGroupPrefix("admin")
        .WithGroupPrefix("config")
          .Map("get").WithHandler(() => "get").AsQuery().Done()
          .Done()
        .WithGroupPrefix("users")
          .Map("list").WithHandler(() => "list").AsQuery().Done()
          .Done()
        .Done()
      .Map("rtc-top").WithHandler(() => "top").AsQuery().Done()
      .Build();

    CapabilitiesResponse filtered = app.Capabilities.FilterByGroup("ADMIN.config");
    string expected = System.Text.Json.JsonSerializer.Serialize(filtered, CapabilitiesJsonSerializerContext.Default.CapabilitiesResponse);

    // Act
    int exitCode = await app.RunAsync(["--capabilities", "--group-filter", "ADMIN.config"]);

    // Assert
    exitCode.ShouldBe(0);
    terminal.Output.TrimEnd('\r', '\n').ShouldBe(expected);
    filtered.Filter.ShouldNotBeNull();
    filtered.Filter.Group.ShouldBe("ADMIN.config");
    filtered.Endpoints.Count.ShouldBe(1);
    filtered.Endpoints[0].Pattern.ShouldBe("admin config get");
    app.Capabilities.Endpoints.Count.ShouldBe(3);
    app.Capabilities.Filter.ShouldBeNull();
  }

  public static async Task Should_filter_by_group_like_the_cli()
  {
    // Arrange
    NuruApp app = NuruApp.CreateBuilder()
      .WithGroupPrefix("kanban")
        .Map("list").WithHandler(() => "list").AsQuery().Done()
        .Map("add {title}").WithHandler((string title) => title).AsCommand().Done()
        .Done()
      .WithGroupPrefix("config")
        .Map("get").WithHandler(() => "get").AsQuery().Done()
        .Done()
      .Build();

    // Act
    CapabilitiesResponse kanban = app.Capabilities.FilterByGroup("kanban");
    CapabilitiesResponse none = app.Capabilities.FilterByGroup("missing");
    CapabilitiesResponse tooDeep = app.Capabilities.FilterByGroup("kanban.list");

    // Assert
    kanban.Endpoints.Select(e => e.Pattern).ShouldBe(["kanban list", "kanban add {title}"]);
    none.Endpoints.Count.ShouldBe(0);
    none.Filter!.Group.ShouldBe("missing");
    tooDeep.Endpoints.Count.ShouldBe(0);

    await Task.CompletedTask;
  }

  public static async Task Should_describe_delegate_return_types()
  {
    // Arrange
    NuruApp app = NuruApp.CreateBuilder()
      .Map("rt-void").WithHandler(() => { }).AsCommand().Done()
      .Map("rt-task").WithHandler(async () => await Task.CompletedTask).AsCommand().Done()
      .Map("rt-string").WithHandler(() => "text").AsQuery().Done()
      .Map("rt-int").WithHandler(() => 42).AsQuery().Done()
      .Map("rt-task-int").WithHandler(async () => { await Task.CompletedTask; return 7; }).AsQuery().Done()
      .Map("rt-date").WithHandler(() => new DateTime(2026, 10, 19, 0, 0, 0, DateTimeKind.Utc)).AsQuery().Done()
      .Map("rt-array").WithHandler(() => Enumerable.Range(1, 2).ToArray()).AsQuery().Done()
      .Build();

    // Act
    Dictionary<string, ReturnTypeCapability> returnTypes = app.Capabilities.Endpoints
      .ToDictionary(e => e.Pattern, e => e.ReturnType!);

    // Assert
    AssertReturnType(returnTypes["rt-void"], "void", ReturnTypeKind.Unit);
    AssertReturnType(returnTypes["rt-task"], "void", ReturnTypeKind.Unit);
    AssertReturnType(returnTypes["rt-string"], "string", ReturnTypeKind.Primitive);
    AssertReturnType(returnTypes["rt-int"], "int", ReturnTypeKind.Primitive);
    AssertReturnType(returnTypes["rt-task-int"], "int", ReturnTypeKind.Primitive);
    AssertReturnType(returnTypes["rt-date"], "System.DateTime", ReturnTypeKind.Primitive);
    AssertReturnType(returnTypes["rt-array"], "int[]", ReturnTypeKind.Json);

    await Task.CompletedTask;
  }

  public static async Task Should_describe_endpoint_return_types()
  {
    // Arrange
    NuruApp app = NuruApp.CreateBuilder()
      .Map<RtTextQuery>()
      .Map<RtUnitCommand>()
      .Map<RtReportQuery>()
      .Build();

    // Act
    Dictionary<string, ReturnTypeCapability> returnTypes = app.Capabilities.Endpoints
      .ToDictionary(e => e.Pattern, e => e.ReturnType!);

    // Assert
    AssertReturnType(returnTypes["rt-endpoint-text"], "string", ReturnTypeKind.Primitive);
    AssertReturnType(returnTypes["rt-endpoint-unit"], "TimeWarp.Mediator.Unit", ReturnTypeKind.Unit);
    AssertReturnType(returnTypes["rt-endpoint-report"], "TimeWarp.Nuru.Tests.Core.CapabilitiesRuntimeCatalog.RtReport", ReturnTypeKind.Json);

    await Task.CompletedTask;
  }

  public static async Task Should_describe_problem_pair_endpoints_by_their_success_type()
  {
    // Arrange: the invoker unwraps OneOf<TSuccess, SharedProblemDetails> and writes TSuccess.
    NuruApp app = NuruApp.CreateBuilder()
      .Map<RtProblemTextQuery>()
      .Map<RtProblemReportQuery>()
      .Build();

    // Act
    Dictionary<string, ReturnTypeCapability> returnTypes = app.Capabilities.Endpoints
      .ToDictionary(e => e.Pattern, e => e.ReturnType!);

    // Assert
    AssertReturnType(returnTypes["rt-problem-text"], "string", ReturnTypeKind.Primitive);
    AssertReturnType(returnTypes["rt-problem-report"], "TimeWarp.Nuru.Tests.Core.CapabilitiesRuntimeCatalog.RtReport", ReturnTypeKind.Json);

    await Task.CompletedTask;
  }

  public static async Task Should_serialize_return_type_last_in_camel_case()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("rt-json-shape").WithHandler(() => "x").AsQuery().Done()
      .Build();

    // Act
    await app.RunAsync(["--capabilities"]);

    // Assert
    using System.Text.Json.JsonDocument doc = System.Text.Json.JsonDocument.Parse(terminal.Output);
    System.Text.Json.JsonElement endpoint = doc.RootElement.GetProperty("endpoints")[0];
    System.Text.Json.JsonProperty last = endpoint.EnumerateObject().Last();
    last.Name.ShouldBe("returnType");
    last.Value.GetProperty("name").GetString().ShouldBe("string");
    last.Value.GetProperty("kind").GetString().ShouldBe("primitive");
  }

  public static async Task Should_roundtrip_return_type_and_tolerate_its_absence()
  {
    // Arrange
    const string legacyJson = """
      {
        "name": "old-tool",
        "version": "1.0.0",
        "endpoints": [
          { "pattern": "ping", "groupPath": [], "kind": "query", "parameters": [], "options": [] }
        ]
      }
      """;
    CapabilitiesResponse current = new()
    {
      Name = "new-tool",
      Version = "2.0.0",
      Endpoints =
      [
        new EndpointCapability
        {
          Pattern = "ping",
          GroupPath = [],
          Kind = EndpointKind.Query,
          Parameters = [],
          Options = [],
          ReturnType = new ReturnTypeCapability { Name = "MyApp.Pong", Kind = ReturnTypeKind.Json }
        }
      ]
    };

    // Act
    CapabilitiesResponse? legacy = System.Text.Json.JsonSerializer.Deserialize(legacyJson, CapabilitiesJsonSerializerContext.Default.CapabilitiesResponse);
    string json = System.Text.Json.JsonSerializer.Serialize(current, CapabilitiesJsonSerializerContext.Default.CapabilitiesResponse);
    CapabilitiesResponse? roundtripped = System.Text.Json.JsonSerializer.Deserialize(json, CapabilitiesJsonSerializerContext.Default.CapabilitiesResponse);

    // Assert
    legacy.ShouldNotBeNull();
    legacy.Endpoints[0].ReturnType.ShouldBeNull();
    json.ShouldContain("\"kind\": \"json\"");
    roundtripped.ShouldNotBeNull();
    roundtripped.Endpoints[0].ReturnType.ShouldNotBeNull();
    roundtripped.Endpoints[0].ReturnType!.Name.ShouldBe("MyApp.Pong");
    roundtripped.Endpoints[0].ReturnType!.Kind.ShouldBe(ReturnTypeKind.Json);

    await Task.CompletedTask;
  }

  public static async Task Should_throw_when_no_catalog_is_attached()
  {
    // Arrange: an app the generator never saw.
    NuruApp app = new();

    // Act
    InvalidOperationException ex = Should.Throw<InvalidOperationException>(() => app.Capabilities);

    // Assert
    ex.Message.ShouldContain("No generated capabilities catalog");

    await Task.CompletedTask;
  }

  private static void AssertReturnType(ReturnTypeCapability returnType, string name, ReturnTypeKind kind)
  {
    returnType.ShouldNotBeNull();
    returnType.Name.ShouldBe(name);
    returnType.Kind.ShouldBe(kind);
  }
}

public sealed record RtReport(string Title, int Count);

[NuruRoute("rt-endpoint-text", Description = "Text query")]
public sealed class RtTextQuery : IQuery<string>
{
  public sealed class Handler : IQueryHandler<RtTextQuery, string>
  {
    public Task<string> Handle(RtTextQuery query, CancellationToken cancellationToken) => Task.FromResult("text");
  }
}

[NuruRoute("rt-endpoint-unit", Description = "Unit command")]
public sealed class RtUnitCommand : ICommand<Unit>
{
  public sealed class Handler : ICommandHandler<RtUnitCommand, Unit>
  {
    public Task<Unit> Handle(RtUnitCommand command, CancellationToken cancellationToken) => Unit.Task;
  }
}

[NuruRoute("rt-endpoint-report", Description = "Report query")]
public sealed class RtReportQuery : IQuery<RtReport>
{
  public sealed class Handler : IQueryHandler<RtReportQuery, RtReport>
  {
    public Task<RtReport> Handle(RtReportQuery query, CancellationToken cancellationToken) => Task.FromResult(new RtReport("r", 2));
  }
}

[NuruRoute("rt-problem-text", Description = "Problem pair with a text success")]
public sealed class RtProblemTextQuery : IQuery<OneOf.OneOf<string, TimeWarp.Foundation.Types.SharedProblemDetails>>
{
  public sealed class Handler : IQueryHandler<RtProblemTextQuery, OneOf.OneOf<string, TimeWarp.Foundation.Types.SharedProblemDetails>>
  {
    public Task<OneOf.OneOf<string, TimeWarp.Foundation.Types.SharedProblemDetails>> Handle(RtProblemTextQuery query, CancellationToken cancellationToken)
    {
      OneOf.OneOf<string, TimeWarp.Foundation.Types.SharedProblemDetails> ok = "text";
      return Task.FromResult(ok);
    }
  }
}

[NuruRoute("rt-problem-report", Description = "Problem pair with a record success")]
public sealed class RtProblemReportQuery : IQuery<OneOf.OneOf<RtReport, TimeWarp.Foundation.Types.SharedProblemDetails>>
{
  public sealed class Handler : IQueryHandler<RtProblemReportQuery, OneOf.OneOf<RtReport, TimeWarp.Foundation.Types.SharedProblemDetails>>
  {
    public Task<OneOf.OneOf<RtReport, TimeWarp.Foundation.Types.SharedProblemDetails>> Handle(RtProblemReportQuery query, CancellationToken cancellationToken)
    {
      OneOf.OneOf<RtReport, TimeWarp.Foundation.Types.SharedProblemDetails> ok = new RtReport("r", 1);
      return Task.FromResult(ok);
    }
  }
}

} // namespace TimeWarp.Nuru.Tests.Core.CapabilitiesRuntimeCatalog
