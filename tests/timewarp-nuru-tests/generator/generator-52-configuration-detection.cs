#!/usr/bin/env -S dotnet --
#:project $(SourceDirectory)timewarp-nuru/timewarp-nuru.csproj
#:project $(SourceDirectory)timewarp-nuru-analyzers/timewarp-nuru-analyzers.csproj
#:package Microsoft.CodeAnalysis.CSharp

#region Purpose
// Task 321: full configuration sources are emitted only for AddConfiguration(),
// a constructor parameter of type IConfiguration, IConfigurationRoot, or IOptions<T>,
// or such a parameter on a handler.
#endregion

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.Generator.Gen52ConfigurationDetection
{
  using Microsoft.CodeAnalysis;
  using Microsoft.CodeAnalysis.CSharp;
  using TimeWarp.Nuru.Generators;

  [TestTag("generator")]
  public class ConfigurationDetectionTests
  {
    [ModuleInitializer]
    internal static void Register() => RegisterTests<ConfigurationDetectionTests>();

    private const string FullMarker = "CONFIGURATION (from AddConfiguration())";
    private const string MinimalMarker = "Minimal configuration for service initialization";

    private static GeneratorDriverRunResult RunNuruGenerator(string source)
    {
      SyntaxTree tree = CSharpSyntaxTree.ParseText(source);

      string tpa = (string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!;
      List<MetadataReference> references = [];
      foreach (string path in tpa.Split(Path.PathSeparator))
      {
        references.Add(MetadataReference.CreateFromFile(path));
      }

      CSharpCompilation compilation = CSharpCompilation.Create(
        assemblyName: "ConfigurationDetectionRepro",
        syntaxTrees: [tree],
        references: references,
        options: new CSharpCompilationOptions(OutputKind.ConsoleApplication));

      CSharpGeneratorDriver driver = CSharpGeneratorDriver.Create(new NuruGenerator());
      GeneratorDriver ran = driver.RunGenerators(compilation);
      return ran.GetRunResult();
    }

    private static string GeneratedSource(GeneratorDriverRunResult result)
    {
      result.Results.Length.ShouldBe(1);
      result.Results[0].Exception.ShouldBeNull();
      return string.Join(
        "\n",
        result.Results[0].GeneratedSources.Select(generated => generated.SourceText.ToString()));
    }

    private static void ShouldEmitFull(string source)
    {
      string generatedSource = GeneratedSource(RunNuruGenerator(source));
      generatedSource.ShouldContain(FullMarker);
      generatedSource.ShouldNotContain(MinimalMarker);
    }

    private static void ShouldEmitMinimal(string source)
    {
      string generatedSource = GeneratedSource(RunNuruGenerator(source));
      generatedSource.ShouldContain(MinimalMarker);
      generatedSource.ShouldNotContain(FullMarker);
    }

    public static async Task NoConfigurationUsage_Should_EmitMinimalConfiguration()
    {
      const string Source = """
        using TimeWarp.Nuru;

        NuruApp app = NuruApp.CreateBuilder()
          .Map("ping")
            .WithHandler(() => "pong")
            .AsQuery()
            .Done()
          .Build();

        return await app.RunAsync(args);
        """;

      ShouldEmitMinimal(Source);
      await Task.CompletedTask;
    }

    public static async Task AddConfigurationCall_Should_EmitFullConfiguration()
    {
      const string Source = """
        using TimeWarp.Nuru;

        NuruApp app = NuruApp.CreateBuilder()
          .AddConfiguration()
          .Map("ping")
            .WithHandler(() => "pong")
            .AsQuery()
            .Done()
          .Build();

        return await app.RunAsync(args);
        """;

      ShouldEmitFull(Source);
      await Task.CompletedTask;
    }

    public static async Task LambdaIConfiguration_Should_EmitFullConfiguration()
    {
      const string Source = """
        using Microsoft.Extensions.Configuration;
        using TimeWarp.Nuru;

        NuruApp app = NuruApp.CreateBuilder()
          .Map("show")
            .WithHandler((IConfiguration configuration) => configuration["k"] ?? "")
            .AsQuery()
            .Done()
          .Build();

        return await app.RunAsync(args);
        """;

      ShouldEmitFull(Source);
      await Task.CompletedTask;
    }

    public static async Task NullableIConfiguration_Should_EmitFullConfiguration()
    {
      const string Source = """
        using Microsoft.Extensions.Configuration;
        using TimeWarp.Nuru;

        NuruApp app = NuruApp.CreateBuilder()
          .Map("show")
            .WithHandler((IConfiguration? configuration) => configuration?["k"] ?? "")
            .AsQuery()
            .Done()
          .Build();

        return await app.RunAsync(args);
        """;

      ShouldEmitFull(Source);
      await Task.CompletedTask;
    }

    public static async Task QualifiedIConfiguration_Should_EmitFullConfiguration()
    {
      const string Source = """
        using TimeWarp.Nuru;

        NuruApp app = NuruApp.CreateBuilder()
          .Map("show")
            .WithHandler((Microsoft.Extensions.Configuration.IConfiguration configuration) => configuration["k"] ?? "")
            .AsQuery()
            .Done()
          .Build();

        return await app.RunAsync(args);
        """;

      ShouldEmitFull(Source);
      await Task.CompletedTask;
    }

    public static async Task LambdaIConfigurationRoot_Should_EmitFullConfiguration()
    {
      const string Source = """
        using Microsoft.Extensions.Configuration;
        using TimeWarp.Nuru;

        NuruApp app = NuruApp.CreateBuilder()
          .Map("show")
            .WithHandler((IConfigurationRoot root) => root["k"] ?? "")
            .AsQuery()
            .Done()
          .Build();

        return await app.RunAsync(args);
        """;

      ShouldEmitFull(Source);
      await Task.CompletedTask;
    }

    public static async Task LambdaIOptions_Should_EmitFullConfiguration()
    {
      const string Source = """
        using Microsoft.Extensions.Options;
        using TimeWarp.Nuru;

        NuruApp app = NuruApp.CreateBuilder()
          .Map("show")
            .WithHandler((IOptions<DatabaseOptions> options) => options.Value.Host)
            .AsQuery()
            .Done()
          .Build();

        return await app.RunAsync(args);

        public class DatabaseOptions
        {
          public string Host { get; set; } = "";
        }
        """;

      ShouldEmitFull(Source);
      await Task.CompletedTask;
    }

    public static async Task LambdaIOptionsSnapshot_Should_EmitMinimalConfiguration()
    {
      const string Source = """
        using Microsoft.Extensions.Options;
        using TimeWarp.Nuru;

        NuruApp app = NuruApp.CreateBuilder()
          .Map("show")
            .WithHandler((IOptionsSnapshot<DatabaseOptions> options) => options.Value.Host)
            .AsQuery()
            .Done()
          .Build();

        return await app.RunAsync(args);

        public class DatabaseOptions
        {
          public string Host { get; set; } = "";
        }
        """;

      ShouldEmitMinimal(Source);
      await Task.CompletedTask;
    }

    public static async Task MethodGroupIConfiguration_Should_EmitFullConfiguration()
    {
      const string Source = """
        using Microsoft.Extensions.Configuration;
        using TimeWarp.Nuru;

        NuruApp app = NuruApp.CreateBuilder()
          .Map("show")
            .WithHandler(Handlers.Show)
            .AsQuery()
            .Done()
          .Build();

        return await app.RunAsync(args);

        internal static class Handlers
        {
          internal static string Show(IConfiguration configuration) => configuration["k"] ?? "";
        }
        """;

      ShouldEmitFull(Source);
      await Task.CompletedTask;
    }

    public static async Task UnrelatedIConfigurationHelper_Should_EmitMinimalConfiguration()
    {
      const string Source = """
        using Microsoft.Extensions.Configuration;
        using TimeWarp.Nuru;

        static string Read(IConfiguration configuration) => configuration["k"] ?? "";

        NuruApp app = NuruApp.CreateBuilder()
          .Map("ping")
            .WithHandler(() => "pong")
            .AsQuery()
            .Done()
          .Build();

        return await app.RunAsync(args);
        """;

      ShouldEmitMinimal(Source);
      await Task.CompletedTask;
    }

    public static async Task AnonymousMethodIConfiguration_Should_EmitFullConfiguration()
    {
      const string Source = """
        using Microsoft.Extensions.Configuration;
        using TimeWarp.Nuru;

        NuruApp app = NuruApp.CreateBuilder()
          .Map("show")
            .WithHandler(delegate (IConfiguration configuration) { return configuration["k"] ?? ""; })
            .AsQuery()
            .Done()
          .Build();

        return await app.RunAsync(args);
        """;

      ShouldEmitFull(Source);
      await Task.CompletedTask;
    }

    public static async Task EndpointConstructorIConfiguration_Should_EmitFullConfiguration()
    {
      const string Source = """
        using Microsoft.Extensions.Configuration;
        using TimeWarp.Mediator;
        using TimeWarp.Nuru;

        [NuruRoute("show")]
        public sealed class ShowQuery : IQuery<Unit>
        {
          public sealed class Handler(IConfiguration configuration) : IQueryHandler<ShowQuery, Unit>
          {
            public Task<Unit> Handle(ShowQuery query, CancellationToken cancellationToken)
            {
              _ = configuration;
              _ = query;
              _ = cancellationToken;
              return Unit.Task;
            }
          }
        }

        NuruApp app = NuruApp.CreateBuilder()
          .DiscoverEndpoints()
          .Build();

        return await app.RunAsync(args);
        """;

      ShouldEmitFull(Source);
      await Task.CompletedTask;
    }

    public static async Task EndpointConstructorIOptions_Should_EmitFullConfiguration()
    {
      const string Source = """
        using Microsoft.Extensions.Options;
        using TimeWarp.Mediator;
        using TimeWarp.Nuru;

        public class DatabaseOptions
        {
          public string Host { get; set; } = "";
        }

        [NuruRoute("show")]
        public sealed class ShowQuery : IQuery<Unit>
        {
          public sealed class Handler(IOptions<DatabaseOptions> options) : IQueryHandler<ShowQuery, Unit>
          {
            public Task<Unit> Handle(ShowQuery query, CancellationToken cancellationToken)
            {
              _ = options;
              _ = query;
              _ = cancellationToken;
              return Unit.Task;
            }
          }
        }

        NuruApp app = NuruApp.CreateBuilder()
          .DiscoverEndpoints()
          .Build();

        return await app.RunAsync(args);
        """;

      ShouldEmitFull(Source);
      await Task.CompletedTask;
    }

    public static async Task EndpointExplicitConstructorIConfigurationRoot_Should_EmitFullConfiguration()
    {
      const string Source = """
        using Microsoft.Extensions.Configuration;
        using TimeWarp.Mediator;
        using TimeWarp.Nuru;

        [NuruRoute("show")]
        public sealed class ShowCommand : ICommand<Unit>
        {
          public sealed class Handler : ICommandHandler<ShowCommand, Unit>
          {
            public Handler(IConfigurationRoot root)
            {
              _ = root;
            }

            public Task<Unit> Handle(ShowCommand command, CancellationToken cancellationToken)
            {
              _ = command;
              _ = cancellationToken;
              return Unit.Task;
            }
          }
        }

        NuruApp app = NuruApp.CreateBuilder()
          .DiscoverEndpoints()
          .Build();

        return await app.RunAsync(args);
        """;

      ShouldEmitFull(Source);
      await Task.CompletedTask;
    }

    public static async Task RegisteredServiceConstructorIConfiguration_Should_EmitFullConfiguration()
    {
      const string Source = """
        using Microsoft.Extensions.Configuration;
        using Microsoft.Extensions.DependencyInjection;
        using TimeWarp.Nuru;

        NuruApp app = NuruApp.CreateBuilder()
          .ConfigureServices(services => services.AddSingleton<IGreeter, Greeter>())
          .Map("greet")
            .WithHandler((IGreeter greeter) => greeter.Greet())
            .AsQuery()
            .Done()
          .Build();

        return await app.RunAsync(args);

        public interface IGreeter
        {
          string Greet();
        }

        public sealed class Greeter(IConfiguration configuration) : IGreeter
        {
          public string Greet() => configuration["Greeting"] ?? "";
        }
        """;

      ShouldEmitFull(Source);
      await Task.CompletedTask;
    }

    public static async Task HandleMethodWithoutNuruInterface_Should_EmitMinimalConfiguration()
    {
      const string Source = """
        using Microsoft.Extensions.Configuration;
        using TimeWarp.Nuru;

        NuruApp app = NuruApp.CreateBuilder()
          .Map("ping")
            .WithHandler(() => "pong")
            .AsQuery()
            .Done()
          .Build();

        return await app.RunAsync(args);

        public sealed class Handler
        {
          public string Handle(IConfiguration configuration) => configuration["k"] ?? "";
        }
        """;

      ShouldEmitMinimal(Source);
      await Task.CompletedTask;
    }
  }
}
