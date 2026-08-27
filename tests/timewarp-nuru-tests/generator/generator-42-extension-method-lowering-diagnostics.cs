#!/usr/bin/env -S dotnet --
#:project $(SourceDirectory)timewarp-nuru/timewarp-nuru.csproj
#:project $(SourceDirectory)timewarp-nuru-analyzers/timewarp-nuru-analyzers.csproj
#:package Microsoft.CodeAnalysis.CSharp

#region Purpose
// Roslyn-hosted tests for task 395: referenced-assembly decompile, fail-closed NURU052,
// and NURU054 for internal impls lowered from AddX. Excluded from JARIBU_MULTI (CS0433).
#endregion

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.Generator.Gen42ExtensionMethodLoweringDiagnostics
{
  using System.Globalization;
  using Microsoft.CodeAnalysis;
  using Microsoft.CodeAnalysis.CSharp;
  using Microsoft.CodeAnalysis.Emit;
  using TimeWarp.Nuru.Generators;

  [TestTag("generator")]
  [TestTag("Task395")]
  public class ExtensionMethodLoweringDiagnosticsTests
  {
    [ModuleInitializer]
    internal static void Register() => RegisterTests<ExtensionMethodLoweringDiagnosticsTests>();

    private static List<MetadataReference> BuildReferences()
    {
      string tpa = (string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!;
      List<MetadataReference> references = [];
      foreach (string path in tpa.Split(Path.PathSeparator))
        references.Add(MetadataReference.CreateFromFile(path));

      return references;
    }

    private static GeneratorDriverRunResult RunNuruGenerator(string source, params MetadataReference[] extraReferences)
    {
      SyntaxTree tree = CSharpSyntaxTree.ParseText(source, path: "app.cs");
      List<MetadataReference> references = BuildReferences();
      references.AddRange(extraReferences);

      CSharpCompilation compilation = CSharpCompilation.Create(
        assemblyName: "Em42App",
        syntaxTrees: [tree],
        references: references,
        options: new CSharpCompilationOptions(OutputKind.ConsoleApplication));

      CSharpGeneratorDriver driver = CSharpGeneratorDriver.Create(new NuruGenerator());
      GeneratorDriver ran = driver.RunGenerators(compilation);
      return ran.GetRunResult();
    }

    private static PortableExecutableReference EmitLibrary(string source, string assemblyName, bool metadataOnly)
    {
      SyntaxTree tree = CSharpSyntaxTree.ParseText(source, path: assemblyName + ".cs");
      CSharpCompilation compilation = CSharpCompilation.Create(
        assemblyName: assemblyName,
        syntaxTrees: [tree],
        references: BuildReferences(),
        options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

      string path = Path.Combine(Path.GetTempPath(), $"{assemblyName}-{Guid.NewGuid():N}.dll");
      EmitOptions? emitOptions = metadataOnly ? new EmitOptions(metadataOnly: true) : null;
      EmitResult emitResult;
      using (FileStream fileStream = new(path, FileMode.Create, FileAccess.Write))
      {
        emitResult = compilation.Emit(fileStream, options: emitOptions);
      }

      emitResult.Success.ShouldBeTrue(
        string.Join("\n", emitResult.Diagnostics.Select(static d => d.ToString())));

      return MetadataReference.CreateFromFile(path);
    }

    private const string AppPreamble = """
      using TimeWarp.Nuru;
      using Microsoft.Extensions.DependencyInjection;

      """;

    public static async Task Should_lower_pure_addx_from_referenced_assembly()
    {
      const string Library = """
        using Microsoft.Extensions.DependencyInjection;

        public interface IEm42LibGreeter
        {
          string Greet();
        }

        public sealed class Em42LibGreeter : IEm42LibGreeter
        {
          public string Greet() => "from-lib";
        }

        public static class Em42LibExtensions
        {
          public static IServiceCollection AddEm42Lib(this IServiceCollection services)
          {
            services.AddSingleton<IEm42LibGreeter, Em42LibGreeter>();
            return services;
          }
        }
        """;

      MetadataReference library = EmitLibrary(Library, "Em42Lib", metadataOnly: false);

      const string App = AppPreamble + """
        NuruApp app = NuruApp.CreateBuilder()
          .ConfigureServices(static services => services.AddEm42Lib())
          .Map("em42-lib").WithHandler(static (IEm42LibGreeter g) => g.Greet()).AsQuery().Done()
          .Build();
        return await app.RunAsync([]);
        """;

      GeneratorDriverRunResult result = RunNuruGenerator(App, library);
      result.Results[0].Exception.ShouldBeNull();

      Diagnostic[] nuru052 = [.. result.Diagnostics.Where(static d => d.Id == "NURU052")];
      Diagnostic[] nuru050 = [.. result.Diagnostics.Where(static d => d.Id == "NURU050")];
      nuru052.ShouldBeEmpty();
      nuru050.ShouldBeEmpty();

      string generated = string.Join("\n", result.Results[0].GeneratedSources.Select(static g => g.SourceText.ToString()));
      generated.ShouldContain("Em42LibGreeter");

      await Task.CompletedTask;
    }

    public static async Task Should_report_nuru052_for_metadata_only_body()
    {
      const string Library = """
        using Microsoft.Extensions.DependencyInjection;

        public interface IEm42Stub { }
        public sealed class Em42Stub : IEm42Stub { }

        public static class Em42StubExtensions
        {
          public static IServiceCollection AddEm42Stub(this IServiceCollection services)
          {
            services.AddSingleton<IEm42Stub, Em42Stub>();
            return services;
          }
        }
        """;

      MetadataReference library = EmitLibrary(Library, "Em42StubLib", metadataOnly: true);

      const string App = AppPreamble + """
        NuruApp app = NuruApp.CreateBuilder()
          .ConfigureServices(static services => services.AddEm42Stub())
          .Map("em42-stub").WithHandler(static () => "ok").AsQuery().Done()
          .Build();
        return await app.RunAsync([]);
        """;

      GeneratorDriverRunResult result = RunNuruGenerator(App, library);
      result.Results[0].Exception.ShouldBeNull();
      result.Diagnostics.ShouldContain(static d => d.Id == "NURU052");

      await Task.CompletedTask;
    }

    public static async Task Should_report_nuru052_for_non_collection_side_effect()
    {
      const string App = AppPreamble + """
        NuruApp app = NuruApp.CreateBuilder()
          .ConfigureServices(static services => services.AddEm42Impure())
          .Map("em42-impure").WithHandler(static () => "ok").AsQuery().Done()
          .Build();
        return await app.RunAsync([]);

        public interface IEm42Impure { }
        public sealed class Em42Impure : IEm42Impure { }

        public static class Em42ImpureExtensions
        {
          public static bool CacheEnabled;

          public static IServiceCollection AddEm42Impure(this IServiceCollection services)
          {
            CacheEnabled = true;
            services.AddSingleton<IEm42Impure, Em42Impure>();
            return services;
          }
        }
        """;

      GeneratorDriverRunResult result = RunNuruGenerator(App);
      result.Results[0].Exception.ShouldBeNull();
      result.Diagnostics.ShouldContain(static d => d.Id == "NURU052");

      string generated = string.Join("\n", result.Results[0].GeneratedSources.Select(static g => g.SourceText.ToString()));
      generated.ShouldNotContain("Em42Impure");

      await Task.CompletedTask;
    }

    public static async Task Should_report_nuru052_for_builder_returning_addx()
    {
      const string App = AppPreamble + """
        NuruApp app = NuruApp.CreateBuilder()
          .ConfigureServices(static services => services.AddEm42Builder())
          .Map("em42-builder").WithHandler(static () => "ok").AsQuery().Done()
          .Build();
        return await app.RunAsync([]);

        public interface IEm42Builder { IEm42Builder More(); }

        public static class Em42BuilderExtensions
        {
          public static IEm42Builder AddEm42Builder(this IServiceCollection services)
          {
            return new Em42Builder();
          }
        }

        public sealed class Em42Builder : IEm42Builder
        {
          public IEm42Builder More() => this;
        }
        """;

      GeneratorDriverRunResult result = RunNuruGenerator(App);
      result.Results[0].Exception.ShouldBeNull();
      result.Diagnostics.ShouldContain(static d => d.Id == "NURU052");

      await Task.CompletedTask;
    }

    public static async Task Should_report_nuru052_for_factory_inside_addx()
    {
      const string App = AppPreamble + """
        NuruApp app = NuruApp.CreateBuilder()
          .ConfigureServices(static services => services.AddEm42Factory())
          .Map("em42-factory").WithHandler(static () => "ok").AsQuery().Done()
          .Build();
        return await app.RunAsync([]);

        public interface IEm42Factory { }
        public sealed class Em42Factory : IEm42Factory { }

        public static class Em42FactoryExtensions
        {
          public static IServiceCollection AddEm42Factory(this IServiceCollection services)
          {
            services.AddSingleton<IEm42Factory>(static _ => new Em42Factory());
            return services;
          }
        }
        """;

      GeneratorDriverRunResult result = RunNuruGenerator(App);
      result.Results[0].Exception.ShouldBeNull();
      result.Diagnostics.Any(static d => d.Id == "NURU052" || d.Id == "NURU053").ShouldBeTrue();

      await Task.CompletedTask;
    }

    public static async Task Should_report_nuru054_for_internal_impl_inside_referenced_addx()
    {
      const string Library = """
        using Microsoft.Extensions.DependencyInjection;

        public interface IEm42Hidden { }

        internal sealed class Em42Hidden : IEm42Hidden { }

        public static class Em42HiddenExtensions
        {
          public static IServiceCollection AddEm42Hidden(this IServiceCollection services)
          {
            services.AddSingleton<IEm42Hidden, Em42Hidden>();
            return services;
          }
        }
        """;

      MetadataReference library = EmitLibrary(Library, "Em42HiddenLib", metadataOnly: false);

      const string App = AppPreamble + """
        NuruApp app = NuruApp.CreateBuilder()
          .ConfigureServices(static services => services.AddEm42Hidden())
          .Map("em42-hidden").WithHandler(static () => "ok").AsQuery().Done()
          .Build();
        return await app.RunAsync([]);
        """;

      GeneratorDriverRunResult result = RunNuruGenerator(App, library);
      result.Results[0].Exception.ShouldBeNull();

      result.Diagnostics.ShouldContain(static d => d.Id == "NURU054");

      string generated = string.Join("\n", result.Results[0].GeneratedSources.Select(static g => g.SourceText.ToString()));
      generated.ShouldNotContain("new global::Em42Hidden");
      generated.ShouldNotContain("new Em42Hidden");

      await Task.CompletedTask;
    }

    public static async Task Should_not_emit_new_or_field_for_internal_impl_on_command_handler()
    {
      const string Library = """
        using Microsoft.Extensions.DependencyInjection;

        public interface IEm42HiddenCmd { }

        internal sealed class Em42HiddenCmdImpl : IEm42HiddenCmd { }

        public static class Em42HiddenCmdExtensions
        {
          public static IServiceCollection AddEm42HiddenCmd(this IServiceCollection services)
          {
            services.AddSingleton<IEm42HiddenCmd, Em42HiddenCmdImpl>();
            return services;
          }
        }
        """;

      MetadataReference library = EmitLibrary(Library, "Em42HiddenCmdLib", metadataOnly: false);

      const string App = AppPreamble + """
        NuruApp app = NuruApp.CreateBuilder()
          .ConfigureServices(static services => services.AddEm42HiddenCmd())
          .Map<Em42HiddenCmdApp.HiddenCommand>()
          .Build();
        return await app.RunAsync([]);

        namespace Em42HiddenCmdApp
        {
          [NuruRoute("em42-hidden-cmd")]
          public sealed class HiddenCommand : ICommand<Unit>
          {
            public sealed class Handler : ICommandHandler<HiddenCommand, Unit>
            {
              public Handler(IEm42HiddenCmd hidden)
              {
                _ = hidden;
              }

              public ValueTask<Unit> Handle(HiddenCommand command, CancellationToken ct) => default;
            }
          }
        }
        """;

      GeneratorDriverRunResult result = RunNuruGenerator(App, library);
      result.Results[0].Exception.ShouldBeNull();
      result.Diagnostics.ShouldContain(static d => d.Id == "NURU054");

      string generated = string.Join("\n", result.Results[0].GeneratedSources.Select(static g => g.SourceText.ToString()));
      generated.ShouldNotContain("new global::Em42HiddenCmdImpl");
      generated.ShouldNotContain("new Em42HiddenCmdImpl");
      generated.ShouldNotContain("__svc_Em42HiddenCmdImpl");

      await Task.CompletedTask;
    }

    public static async Task Should_include_hatch_guidance_in_nuru052_message()
    {
      const string App = AppPreamble + """
        NuruApp app = NuruApp.CreateBuilder()
          .ConfigureServices(static services => services.AddEm42BuilderMsg())
          .Map("em42-msg").WithHandler(static () => "ok").AsQuery().Done()
          .Build();
        return await app.RunAsync([]);

        public interface IEm42BuilderMsg { }

        public static class Em42BuilderMsgExtensions
        {
          public static IEm42BuilderMsg AddEm42BuilderMsg(this IServiceCollection services) => new Em42BuilderMsg();
        }

        public sealed class Em42BuilderMsg : IEm42BuilderMsg { }
        """;

      GeneratorDriverRunResult result = RunNuruGenerator(App);
      Diagnostic nuru052 = result.Diagnostics.First(static d => d.Id == "NURU052");
      string message = nuru052.GetMessage(CultureInfo.InvariantCulture);
      message.ShouldContain("UseMicrosoftDependencyInjection");
      message.ShouldContain("AddSingleton");

      await Task.CompletedTask;
    }
  }
}
