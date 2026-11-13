#!/usr/bin/env -S dotnet --
#:project $(SourceDirectory)timewarp-nuru/timewarp-nuru.csproj
#:project $(SourceDirectory)timewarp-nuru-daemon/timewarp-nuru-daemon.csproj
#:project $(SourceDirectory)timewarp-nuru-mcphost/timewarp-nuru-mcphost.csproj
#:project $(SourceDirectory)timewarp-nuru-analyzers/timewarp-nuru-analyzers.csproj
#:package Microsoft.CodeAnalysis.CSharp
#:package Microsoft.Extensions.Http

#region Purpose
// Kanban 529: the consumer-compile lane. Generated code is part of the consumer's assembly, so it may
// name only the public surface of the TimeWarp.Nuru* runtime assemblies. Every repo test assembly is
// listed in internals-visible-to.g.cs, so a generated reference to an internal type or member
// compiles in every other repo test and still fails in a real consumer with CS0122 (kanban 528:
// AddCheckUpdatesRoute named the internal CheckUpdatesJsonSerializerContext).
#endregion

#region Design
// Each case compiles an app in memory with Roslyn under an assembly name that no InternalsVisibleTo
// attribute on a TimeWarp.Nuru* assembly lists, runs the Nuru generator on it, and requires zero
// compile errors. The cases between them reach every emitter under
// source/timewarp-nuru-analyzers/generators/emitters/: interceptor scaffold, route matcher (typed
// parameters, enums, options, aliases, catch-all, unknown options, custom converters), handler
// invoker (delegates, method groups, async, return kinds), command classes and Map<T> endpoints,
// operation endpoints with the problem renderer, verbatim renderer output and direct invoke,
// behaviors, service resolver and runtime DI with per-run scopes and typed HttpClients, configuration
// and IOptions, logging, telemetry, help, route help, version, capabilities, completion, REPL,
// check-updates, --json-args, and the daemon and MCP host routes from their own packages.
// Emitter markers in each case keep it reaching the paths it names. In a consumer an internal member
// of a referenced assembly reports CS0117 or CS0122 depending on whether its type is visible.
// The first case asserts the precondition: the consumer name is not granted. A new emitter that can
// name a runtime type belongs in a case here. Generator-65 keeps its narrower check-updates case.
// Roslyn-hosted (the analyzers library collides with timewarp-nuru's parsing types, CS0433), so it
// runs in the standalone phase.
#endregion

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.Generator.Gen75ConsumerSurfaceNoIvt
{
  using System.Collections.Immutable;
  using System.Reflection;
  using Microsoft.CodeAnalysis;
  using Microsoft.CodeAnalysis.CSharp;
  using TimeWarp.Nuru.Generators;

  [TestTag("generator")]
  public class ConsumerSurfaceNoIvtTests
  {
    [ModuleInitializer]
    internal static void Register() => RegisterTests<ConsumerSurfaceNoIvtTests>();

    private const string ConsumerAssemblyName = "Gen75ConsumerApp";

    private const string Usings = """
      using System;
      using System.Collections.Generic;
      using System.Linq;
      using System.Net.Http;
      using System.Threading;
      using System.Threading.Tasks;
      using Microsoft.Extensions.Configuration;
      using Microsoft.Extensions.DependencyInjection;
      using Microsoft.Extensions.Logging;
      using Microsoft.Extensions.Options;
      using TimeWarp.Mediator;
      using TimeWarp.Nuru;
      using TimeWarp.Terminal;

      """;

    private static (ImmutableArray<Diagnostic> Diagnostics, string Generated) CompileWithGenerator(string source, string[] jsonTypes, string[] jsonMemberTypes)
    {
      CSharpParseOptions parseOptions = CSharpParseOptions.Default
        .WithFeatures([new KeyValuePair<string, string>("InterceptorsNamespaces", "TimeWarp.Nuru.Generated")]);
      SyntaxTree tree = CSharpSyntaxTree.ParseText(Usings + source, parseOptions, path: "app.cs");
      SyntaxTree jsonContexts = CSharpSyntaxTree.ParseText(BuildTaskJsonContexts(jsonTypes, jsonMemberTypes), parseOptions, path: "NuruUserTypesJsonContext.g.cs");

      string tpa = (string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!;
      List<MetadataReference> references = [];
      foreach (string path in tpa.Split(Path.PathSeparator))
      {
        // A real app loads the analyzers assembly as an analyzer, not as a reference; referencing it
        // collides with timewarp-nuru's parsing types (CS0433).
        if (Path.GetFileNameWithoutExtension(path) == "TimeWarp.Nuru.Analyzers")
          continue;
        references.Add(MetadataReference.CreateFromFile(path));
      }

      CSharpCompilation compilation = CSharpCompilation.Create(
        assemblyName: ConsumerAssemblyName,
        syntaxTrees: [tree, jsonContexts],
        references: references,
        options: new CSharpCompilationOptions(OutputKind.ConsoleApplication, nullableContextOptions: NullableContextOptions.Enable));

      // The System.Text.Json generator fills the JSON contexts, as it does in a consumer build.
      CSharpGeneratorDriver driver = CSharpGeneratorDriver.Create(
        generators: [new NuruGenerator().AsSourceGenerator(), .. LoadSystemTextJsonGenerators()],
        parseOptions: parseOptions);
      _ = driver.RunGeneratorsAndUpdateCompilation(compilation, out Compilation output, out ImmutableArray<Diagnostic> generatorDiagnostics);

      generatorDiagnostics.Where(static d => d.Severity == DiagnosticSeverity.Error).ShouldBeEmpty(Describe(generatorDiagnostics));
      output.SyntaxTrees.Count().ShouldBeGreaterThan(2);
      string generated = string.Join(
        "\n",
        output.SyntaxTrees
          .Where(static t => t.FilePath.Contains("NuruGenerator", StringComparison.Ordinal))
          .Select(static t => t.GetText().ToString()));
      return (output.GetDiagnostics(), generated);
    }

    // In a consumer build the TimeWarp.Nuru.Build MSBuild task (GenerateNuruJsonContextTask) writes the
    // consumer's JSON contexts before compile. An in-memory compilation runs no MSBuild, so each case
    // supplies the same shape: internal partial contexts in TimeWarp.Nuru.Generated.
    private static string BuildTaskJsonContexts(string[] jsonTypes, string[] jsonMemberTypes)
    {
      System.Text.StringBuilder sb = new();
      sb.AppendLine("namespace TimeWarp.Nuru.Generated;");
      foreach (string type in jsonTypes)
        sb.Append("[global::System.Text.Json.Serialization.JsonSerializable(typeof(").Append(type).AppendLine("))]");
      if (jsonTypes.Length > 0)
        sb.AppendLine("internal partial class NuruUserTypesJsonContext : global::System.Text.Json.Serialization.JsonSerializerContext;");
      foreach (string type in jsonMemberTypes)
        sb.Append("[global::System.Text.Json.Serialization.JsonSerializable(typeof(").Append(type).AppendLine("))]");
      if (jsonMemberTypes.Length > 0)
        sb.AppendLine("internal partial class NuruJsonMemberContext : global::System.Text.Json.Serialization.JsonSerializerContext;");
      return sb.ToString();
    }

    // The System.Text.Json generator ships in the targeting pack that matches the running runtime.
    private static IEnumerable<ISourceGenerator> LoadSystemTextJsonGenerators()
    {
      string runtimeDirectory = System.Runtime.InteropServices.RuntimeEnvironment.GetRuntimeDirectory();
      string dotnetRoot = Path.GetFullPath(Path.Combine(runtimeDirectory, "..", "..", ".."));
      string refPacks = Path.Combine(dotnetRoot, "packs", "Microsoft.NETCore.App.Ref");
      string major = Environment.Version.Major.ToString(System.Globalization.CultureInfo.InvariantCulture) + ".";
      string generatorPath = Directory.GetDirectories(refPacks)
        .Where(directory => Path.GetFileName(directory).StartsWith(major, StringComparison.Ordinal))
        .Order(StringComparer.Ordinal)
        .Select(static directory => Path.Combine(directory, "analyzers", "dotnet", "cs", "System.Text.Json.SourceGeneration.dll"))
        .Last(File.Exists);

      Assembly generatorAssembly = Assembly.LoadFrom(generatorPath);
      return generatorAssembly.GetTypes()
        .Where(static type => !type.IsAbstract && typeof(IIncrementalGenerator).IsAssignableFrom(type))
        .Select(static type => ((IIncrementalGenerator)Activator.CreateInstance(type)!).AsSourceGenerator());
    }

    // Errors in generated code carry the offending line so a failure names what the emitter wrote.
    private static string Describe(IEnumerable<Diagnostic> diagnostics) =>
      string.Join("\n", diagnostics.Select(static d =>
      {
        SyntaxTree? tree = d.Location.SourceTree;
        if (tree is null || !d.Location.IsInSource)
          return d.ToString();

        int line = d.Location.GetLineSpan().StartLinePosition.Line;
        return d + "\n    > " + tree.GetText().Lines[line].ToString().Trim();
      }));

    // markers: text each case's generated code must contain, so the case keeps reaching the emitters it
    // names (a builder change that silently drops a path would otherwise leave the case green).
    private static void ShouldCompileAsConsumer(string source, string[] jsonTypes, string[] jsonMemberTypes, string[] markers)
    {
      (ImmutableArray<Diagnostic> diagnostics, string generated) = CompileWithGenerator(source, jsonTypes, jsonMemberTypes);
      Diagnostic[] errors = [.. diagnostics.Where(static d => d.Severity == DiagnosticSeverity.Error)];
      errors.ShouldBeEmpty(Describe(errors));
      foreach (string marker in markers)
        generated.ShouldContain(marker, Case.Sensitive, $"generated code lacks {marker}");
    }

    public static async Task Consumer_assembly_has_no_internals_visible_to_grant()
    {
      static string[] Granted(Assembly assembly) =>
        [.. assembly.GetCustomAttributes<System.Runtime.CompilerServices.InternalsVisibleToAttribute>().Select(static a => a.AssemblyName)];

      Granted(typeof(NuruApp).Assembly).ShouldNotBeEmpty();
      Assembly[] runtimeAssemblies =
        [typeof(NuruApp).Assembly, typeof(TimeWarp.Nuru.Daemon.NuruDaemonServer).Assembly, typeof(TimeWarp.Nuru.McpHost.NuruMcpServer).Assembly];
      foreach (Assembly assembly in runtimeAssemblies)
        Granted(assembly).ShouldNotContain(ConsumerAssemblyName, assembly.GetName().Name);

      await Task.CompletedTask;
    }

    // Interceptor scaffold, route matcher, handler invoker, help, route help, version, capabilities,
    // --json-args, unknown options, custom type converters.
    public static async Task Should_compile_fluent_routes_with_public_surface_only()
    {
      const string Source = """
        NuruApp app = NuruApp.CreateBuilder()
          .WithName("gen75")
          .WithDescription("Consumer surface fluent routes")
          .AddTypeConverter(new Gen75PointConverter())
          .Map("ping").WithHandler(() => "pong").WithDescription("Ping").AsQuery().Done()
          .Map("greet {name} {title?}")
            .WithHandler((string name, string? title) => $"Hello {title} {name}")
            .WithExample("greet Ada", "Greets Ada")
            .AsQuery().Done()
          .Map("delay {ms:int} {factor:double} {at:DateTime} {id:Guid} {on:bool}")
            .WithHandler((int ms, double factor, DateTime at, Guid id, bool on) => ms * factor)
            .AsCommand().Done()
          .Map("color {shade:Gen75Shade}")
            .WithHandler((Gen75Shade shade) => shade.ToString())
            .AsIdempotentCommand().Done()
          .Map("docker {*args}").WithHandler((string[] args) => args.Length).AsCommand().Done()
          .Map("sum {*values:int}").WithHandler((int[] values) => values.Sum()).AsQuery().Done()
          .Map("build --config,-c {mode} --verbose,-v --level {level:int?} --tag {tags}*")
            .WithHandler((string mode, bool verbose, int? level, string[] tags) => $"{mode}{verbose}{level}{tags.Length}")
            .AsCommand().Done()
          .Map("deploy {env} --shade {shade:Gen75Shade?}")
            .WithHandler((string env, Gen75Shade? shade) => $"{env}{shade}")
            .AsCommand().Done()
          .Map("point {at:Gen75Point}").WithHandler((Gen75Point at) => at.X + at.Y).AsQuery().Done()
          .Map("async-run {count:int}")
            .WithHandler(async (int count, CancellationToken cancellationToken) =>
            {
              await Task.Delay(0, cancellationToken);
              return count;
            })
            .AsCommand().Done()
          .Map("method {value}").WithHandler(Gen75Handlers.Echo).AsQuery().Done()
          .Map("term").WithHandler((ITerminal terminal) => terminal.WriteLine("t")).AsCommand().Done()
          .Map("exit-code").WithHandler(() => 3).AsCommand().Done()
          .Map("void-task").WithHandler(async () => await Task.Yield()).AsCommand().Done()
          .WithGroupPrefix("admin")
            .Map("users list --all").WithHandler((bool all) => all).AsQuery().Done()
            .Done()
          .Build();

        return await app.RunAsync(args);

        public enum Gen75Shade { Light, Dark }

        public readonly record struct Gen75Point(int X, int Y);

        public sealed class Gen75PointConverter : IRouteTypeConverter
        {
          public Type TargetType => typeof(Gen75Point);
          public string? ConstraintAlias => "point";

          public bool TryConvert(string value, out object? result) =>
            (result = new Gen75Point(int.Parse(value.Split(',')[0]), int.Parse(value.Split(',')[1]))) is not null;
        }

        public static class Gen75Handlers { public static string Echo(string value) => value; }
        """;

      ShouldCompileAsConsumer(Source, ["global::Gen75Point"], [],
        [
          "global::TimeWarp.Nuru.NuruUnknownOption.IsOptionLike",
          "global::TimeWarp.Nuru.CapabilitiesJsonSerializerContext.Default.CapabilitiesResponse",
          "global::TimeWarp.Nuru.NuruBindError", "Gen75PointConverter", "global::Gen75Shade",
          "global::Gen75Handlers.Echo"
        ]);
      await Task.CompletedTask;
    }

    // Command classes and Map<T> endpoints, typed properties, nested handlers with constructor DI,
    // behaviors (global, filtered, Implements<T>), service resolver and runtime DI with per-run scopes,
    // configuration and IOptions, logging, telemetry.
    public static async Task Should_compile_endpoints_services_and_behaviors_with_public_surface_only()
    {
      const string Source = """
        NuruApp app = NuruApp.CreateBuilder()
          .WithName("gen75-endpoints")
          .AddConfiguration()
          .UseTelemetry()
          .ConfigureServices(services =>
          {
            services.AddLogging(builder => builder.AddConsole());
            services.AddSingleton<IGen75Clock, Gen75Clock>();
            services.AddTransient<Gen75Formatter>();
            services.AddScoped<Gen75Unit>();
            services.AddCommandScoped<Gen75PerCommand>();
            services.AddSessionScoped<Gen75PerSession>();
            services.AddHttpClient<Gen75Http>(client => client.BaseAddress = new Uri("https://example.invalid/"));
            services.Configure<Gen75Settings>(_ => { });
          })
          .AddBehavior(typeof(Gen75TimingBehavior))
          .AddBehavior(typeof(Gen75AuditBehavior))
          .Map<Gen75DeployCommand>()
          .Map<Gen75StatusQuery>()
          .Map("audited {action}")
            .Implements<IGen75Audited>(x => x.Reason = "test")
            .WithHandler((string action, IGen75Clock clock, ILogger<Gen75Formatter> logger) =>
            {
              logger.LogInformation("{Action}", action);
              return clock.Now;
            })
            .AsCommand().Done()
          .Map("settings").WithHandler((IOptions<Gen75Settings> options) => options.Value.Name).AsQuery().Done()
          .Build();

        return await app.RunAsync(args);

        public interface IGen75Clock { DateTimeOffset Now { get; } }
        public sealed class Gen75Clock : IGen75Clock { public DateTimeOffset Now => DateTimeOffset.UnixEpoch; }
        public sealed class Gen75Formatter { public string Format(string value) => value; }
        public sealed class Gen75Unit { }
        public sealed class Gen75PerCommand { }
        public sealed class Gen75PerSession { }
        public sealed class Gen75Http(HttpClient client) { public HttpClient Client { get; } = client; }
        public sealed class Gen75Settings { public string Name { get; set; } = "n"; }

        public interface IGen75Audited { string Reason { get; set; } }

        public sealed class Gen75TimingBehavior(ILogger<Gen75TimingBehavior> logger) : INuruBehavior
        {
          public ValueTask HandleAsync(BehaviorContext context, Func<ValueTask> proceed)
          {
            logger.LogInformation("{Command}", context.CommandName);
            return proceed();
          }
        }

        public sealed class Gen75AuditBehavior : INuruBehavior<IGen75Audited>
        {
          public ValueTask HandleAsync(BehaviorContext<IGen75Audited> context, Func<ValueTask> proceed) =>
            context.Command.Reason.Length >= 0 ? proceed() : ValueTask.CompletedTask;
        }

        public enum Gen75Stage { Dev, Prod }

        [NuruRoute("deploy", Description = "Deploy")]
        public sealed class Gen75DeployCommand : ICommand<Unit>
        {
          [Parameter(Description = "Environment")]
          public string Env { get; set; } = "";
          [Option("stage", "s", Description = "Stage")]
          public Gen75Stage Stage { get; set; }
          [Option("force", "f", Description = "Force")]
          public bool Force { get; set; }
          [Option("retries", Description = "Retries")]
          public int? Retries { get; set; }

          public sealed class Handler(IGen75Clock clock, Gen75Formatter formatter, Gen75PerCommand perCommand, Gen75Http http, ITerminal terminal)
            : IRequestHandler<Gen75DeployCommand, Unit>
          {
            public ValueTask<Unit> Handle(Gen75DeployCommand command, CancellationToken cancellationToken)
            {
              _ = perCommand;
              _ = http;
              terminal.WriteLine(formatter.Format($"{command.Env}{command.Stage}{command.Force}{command.Retries}{clock.Now}"));
              return default;
            }
          }
        }

        [NuruRoute("status", Description = "Status")]
        public sealed class Gen75StatusQuery : IQuery<Gen75Status>
        {
          [Parameter(Description = "Names", IsCatchAll = true)]
          public string[] Names { get; set; } = [];

          public sealed class Handler(IOptions<Gen75Settings> settings) : IRequestHandler<Gen75StatusQuery, Gen75Status>
          {
            public ValueTask<Gen75Status> Handle(Gen75StatusQuery query, CancellationToken cancellationToken) =>
              ValueTask.FromResult(new Gen75Status(settings.Value.Name, query.Names.Length));
          }
        }

        public sealed record Gen75Status(string Name, int Count);
        """;

      ShouldCompileAsConsumer(Source, ["global::Gen75Status"], [],
        [
          "global::TimeWarp.Nuru.BehaviorContext", "global::TimeWarp.Nuru.TelemetryBehavior",
          "global::Gen75AuditBehavior", "global::Gen75DeployCommand",
          "global::TimeWarp.Nuru.NuruAsyncLease.For", "global::TimeWarp.Nuru.NuruOtlpEndpoint.Configure", "Gen75Settings"
        ]);
      await Task.CompletedTask;
    }

    // REPL, shell completion, check-updates, runtime Microsoft DI (NuruCommandLease).
    public static async Task Should_compile_repl_completion_and_check_updates_with_public_surface_only()
    {
      const string Source = """
        NuruApp app = NuruApp.CreateBuilder()
          .WithName("gen75-repl")
          .UseMicrosoftDependencyInjection()
          .AddRepl(options => options.Prompt = "gen75> ")
          .EnableCompletion()
          .AddCheckUpdatesRoute()
          .Map("mode {shade:Gen75ReplShade}").WithHandler((Gen75ReplShade shade) => shade.ToString()).AsQuery().Done()
          .Map("tag --name {name} --on").WithHandler((string name, bool on) => name).AsCommand().Done()
          .Build();

        return await app.RunAsync(args);

        public enum Gen75ReplShade { Light, Dark }
        """;

      ShouldCompileAsConsumer(Source, ["global::Gen75ReplShade"], [],
        [
          "global::TimeWarp.Nuru.ReplSession.RunAsync", "global::TimeWarp.Nuru.NuruCommandLease.Enter",
          "global::TimeWarp.Nuru.DynamicCompletionHandler.HandleCompletion",
          "global::TimeWarp.Nuru.CheckUpdatesGitHubRelease.ParseReleases"
        ]);
      await Task.CompletedTask;
    }

    // Operation endpoints ([Operation], OneOf results), the problem renderer, verbatim renderer output,
    // and direct invoke.
    public static async Task Should_compile_operation_endpoints_and_renderers_with_public_surface_only()
    {
      const string Source = """
        NuruApp app = NuruApp.CreateBuilder()
          .WithName("gen75-operations")
          .UseProblemRendererForErrors()
          .UseVerbatimRendererOutput()
          .UseDirectInvoke()
          .Map<Gen75OperationQuery>()
          .Map<Gen75DirectQuery>()
          .Build();

        return await app.RunAsync(args);

        [TimeWarp.Contracts.Operation("gen75op")]
        public sealed class Gen75OperationQuery : IQuery<OneOf.OneOf<Gen75Report, TimeWarp.Foundation.Types.SharedProblemDetails>>
        {
          public string Name { get; set; } = "";

          public sealed class Handler : IRequestHandler<Gen75OperationQuery, OneOf.OneOf<Gen75Report, TimeWarp.Foundation.Types.SharedProblemDetails>>
          {
            public ValueTask<OneOf.OneOf<Gen75Report, TimeWarp.Foundation.Types.SharedProblemDetails>> Handle(Gen75OperationQuery query, CancellationToken cancellationToken) =>
              ValueTask.FromResult<OneOf.OneOf<Gen75Report, TimeWarp.Foundation.Types.SharedProblemDetails>>(new Gen75Report(query.Name));
          }
        }

        public sealed record Gen75Report(string Name);

        public sealed class Gen75ReportRenderer : TimeWarp.Contracts.IResponseRenderer<Gen75Report>
        {
          public string Render(Gen75Report response) => "report=" + response.Name;
        }

        public sealed class Gen75ProblemRenderer : TimeWarp.Contracts.IResponseRenderer<TimeWarp.Foundation.Types.SharedProblemDetails>
        {
          public string Render(TimeWarp.Foundation.Types.SharedProblemDetails response) => "problem=" + response.Title;
        }

        [NuruRoute("gen75direct")]
        public sealed class Gen75DirectQuery : IQuery<string>
        {
          public sealed class Handler : IRequestHandler<Gen75DirectQuery, string>
          {
            public ValueTask<string> Handle(Gen75DirectQuery query, CancellationToken cancellationToken) =>
              ValueTask.FromResult("direct");
          }
        }
        """;

      ShouldCompileAsConsumer(Source, ["global::Gen75Report"], ["global::Gen75Report"],
        [
          "global::Gen75ProblemRenderer", "global::Gen75OperationQuery", "global::Gen75DirectQuery"
        ]);
      await Task.CompletedTask;
    }

    // Daemon serve route and MCP host route, emitted only when those packages are referenced.
    public static async Task Should_compile_daemon_and_mcp_routes_with_public_surface_only()
    {
      const string Source = """
        NuruApp app = NuruApp.CreateBuilder()
          .WithName("gen75-served")
          .Map("echo {text}").WithHandler((string text) => text).AsQuery().Done()
          .Map("local-echo {text}").WithHandler((string text) => text).AsQuery().LocalOnly().Done()
          .Map<Gen75PidQuery>()
          .AddServeRoute(idleTimeoutSeconds: 300)
          .AddMcpRoute(instructions: "gen75")
          .Build();

        return await app.RunAsync(args);

        [NuruRoute("pid", Description = "Process id")]
        public sealed class Gen75PidQuery : IQuery<int>
        {
          public sealed class Handler : IRequestHandler<Gen75PidQuery, int>
          {
            public ValueTask<int> Handle(Gen75PidQuery query, CancellationToken cancellationToken) =>
              ValueTask.FromResult(Environment.ProcessId);
          }
        }
        """;

      ShouldCompileAsConsumer(Source, [], [],
        [
          "global::TimeWarp.Nuru.Daemon.NuruDaemonServer.RunAsync",
          "global::TimeWarp.Nuru.McpHost.NuruMcpServer.RunStdioAsync",
          "global::TimeWarp.Nuru.NuruForwarding.TryForwardAsync"
        ]);
      await Task.CompletedTask;
    }
  }
}
