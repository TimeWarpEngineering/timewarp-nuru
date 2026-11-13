#!/usr/bin/env -S dotnet --
#:project $(SourceDirectory)timewarp-nuru/timewarp-nuru.csproj

#region Purpose
// Kanban 553: the generator inlines a delegate handler's body into TimeWarp.Nuru.Generated. The
// body used to be copied as syntax text, so a type that resolved only through the caller's
// namespace, a using inside the namespace block, or the enclosing class failed CS0103 there.
// Each test builds an app whose handler names such a type, with and without behaviors, and runs it.
#endregion

#region Design
// A compile error is the failure this file guards, so it runs in the CI multi-mode assembly: a
// regression breaks the whole CI build, not one test. Every route pattern carries a gen74 prefix
// and each app uses Map(), so no route collides with another file in that assembly.
#endregion

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.Generator.Gen74InlinedLambdaQualifiedNames
{
  using Microsoft.Extensions.DependencyInjection;
  using TimeWarp.Nuru.Tests.Generator.Gen74InlinedLambdaQualifiedNames.Helpers;

  [TestTag("Generator")]
  public class InlinedLambdaQualifiedNameTests
  {
    [ModuleInitializer]
    internal static void Register() => RegisterTests<InlinedLambdaQualifiedNameTests>();

    public static async Task Should_resolve_a_namespace_type_in_an_expression_body_under_a_behavior()
    {
      // Arrange
      Gen74Trace.Calls.Clear();
      using TestTerminal terminal = new();
      NuruApp app = NuruApp.CreateBuilder()
        .UseTerminal(terminal)
        .AddBehavior(typeof(Gen74Behavior))
        .Map("gen74-probe").WithHandler(() => Gen74Probe.Ping()).AsQuery().Done()
        .Build();

      // Act
      int exitCode = await app.RunAsync(["gen74-probe"]);

      // Assert
      exitCode.ShouldBe(0);
      terminal.OutputContains("pong").ShouldBeTrue();
      Gen74Trace.Calls.ShouldContain("behavior:gen74-probe");
    }

    public static async Task Should_resolve_a_namespace_type_in_an_expression_body_without_behaviors()
    {
      // Arrange
      using TestTerminal terminal = new();
      NuruApp app = NuruApp.CreateBuilder()
        .UseTerminal(terminal)
        .Map("gen74-plain {name}").WithHandler((string name) => Gen74Format.Shout(name)).AsQuery().Done()
        .Build();

      // Act
      int exitCode = await app.RunAsync(["gen74-plain", "ada"]);

      // Assert
      exitCode.ShouldBe(0);
      terminal.OutputContains("ADA!").ShouldBeTrue();
    }

    public static async Task Should_resolve_types_static_members_and_extensions_in_a_block_body()
    {
      // Arrange
      Gen74Trace.Calls.Clear();
      using TestTerminal terminal = new();
      NuruApp app = NuruApp.CreateBuilder()
        .UseTerminal(terminal)
        .AddBehavior(typeof(Gen74Behavior))
        .Map("gen74-block {count:int}").WithHandler((int count) =>
        {
          Gen74Kind kind = count > 1 ? Gen74Kind.Many : Gen74Kind.One;
          List<Gen74Probe.Reading> readings = [new Gen74Probe.Reading(count)];
          Gen74Trace.Calls.Add(typeof(Gen74Format).Name);
          return $"{kind}:{readings[0].Value}:{"x".Gen74Twice()}:{Gen74Probe.Limit}";
        }).AsCommand().Done()
        .Build();

      // Act
      int exitCode = await app.RunAsync(["gen74-block", "3"]);

      // Assert
      exitCode.ShouldBe(0);
      terminal.OutputContains("Many:3:xx:10").ShouldBeTrue();
      Gen74Trace.Calls.ShouldContain("Gen74Format");
      Gen74Trace.Calls.ShouldContain("behavior:gen74-block {count:int}");
    }

    public static async Task Should_resolve_a_namespace_type_in_a_handler_that_takes_a_service()
    {
      // Arrange
      using TestTerminal terminal = new();
      NuruApp app = NuruApp.CreateBuilder()
        .UseTerminal(terminal)
        .ConfigureServices(services => services.AddSingleton<IGen74Greeter, Gen74Greeter>())
        .AddBehavior(typeof(Gen74Behavior))
        .Map("gen74-greet {name}")
          .WithHandler((string name, IGen74Greeter greeter) => Gen74Format.Shout(greeter.Greet(name)))
          .AsQuery()
          .Done()
        .Build();

      // Act
      int exitCode = await app.RunAsync(["gen74-greet", "ada"]);

      // Assert
      exitCode.ShouldBe(0);
      terminal.OutputContains("HELLO ADA!").ShouldBeTrue();
    }

    public static async Task Should_resolve_an_unqualified_static_member_of_the_enclosing_class()
    {
      // Act
      (int exitCode, string output) = await Gen74Probe.RunInnerAsync();

      // Assert
      exitCode.ShouldBe(0);
      output.ShouldContain("pong");
    }
  }

  public static class Gen74Probe
  {
    public const int Limit = 10;

    public static string Ping() => "pong";

    /// <summary>
    /// Builds and runs an app whose handler calls <see cref="Ping"/> unqualified, as code inside
    /// this class may.
    /// </summary>
    public static async Task<(int ExitCode, string Output)> RunInnerAsync()
    {
      using TestTerminal terminal = new();
      NuruApp app = NuruApp.CreateBuilder()
        .UseTerminal(terminal)
        .AddBehavior(typeof(Gen74Behavior))
        .Map("gen74-inner").WithHandler(() => Ping()).AsQuery().Done()
        .Build();

      int exitCode = await app.RunAsync(["gen74-inner"]);
      return (exitCode, terminal.Output);
    }

    public sealed record Reading(int Value);
  }

  public enum Gen74Kind
  {
    One,
    Many
  }

  public static class Gen74Trace
  {
    public static System.Collections.ObjectModel.Collection<string> Calls { get; } = [];
  }

  public interface IGen74Greeter
  {
    string Greet(string name);
  }

  public sealed class Gen74Greeter : IGen74Greeter
  {
    public string Greet(string name) => $"hello {name}";
  }

  public sealed class Gen74Behavior : INuruBehavior
  {
    public async ValueTask HandleAsync(BehaviorContext context, Func<ValueTask> proceed)
    {
      ArgumentNullException.ThrowIfNull(context);
      ArgumentNullException.ThrowIfNull(proceed);

      Gen74Trace.Calls.Add($"behavior:{context.CommandName}");
      await proceed();
    }
  }
}

namespace TimeWarp.Nuru.Tests.Generator.Gen74InlinedLambdaQualifiedNames.Helpers
{
  public static class Gen74Format
  {
    public static string Shout(string value)
    {
      ArgumentNullException.ThrowIfNull(value);
      return $"{value.ToUpperInvariant()}!";
    }

    public static string Gen74Twice(this string value) => value + value;
  }
}
