#!/usr/bin/env -S dotnet --

// ═══════════════════════════════════════════════════════════════════════════════
// GENERATOR TEST: Lower in-project AddX into source-gen DI (task 395)
// ═══════════════════════════════════════════════════════════════════════════════
//
// PURPOSE: Verify pure public closed-type AddX scripts in this compilation are
// inlined into source-gen DI (new Impl / Lazy<T>), with TryAdd order preserved.
//
// WHAT THIS TESTS:
// - Simple public AddX wrapping AddSingleton<IFoo, Foo>
// - Nested same-assembly helpers (A → B → AddScoped)
// - Mix of inline AddSingleton and lowerable AddX (TryAdd does not replace)
// - Generic AddX<T>() that registers T as a public closed type
// ═══════════════════════════════════════════════════════════════════════════════

using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection.Extensions;

#if !JARIBU_MULTI
return await RunAllTests();
#endif

public interface IEm41Greeter
{
  string Greet();
}

public sealed class Em41Greeter : IEm41Greeter
{
  public string Greet() => "simple-addx";
}

public interface IEm41Nested
{
  string Value();
}

public sealed class Em41Nested : IEm41Nested
{
  public string Value() => "nested-addx";
}

public interface IEm41Mix
{
  string Value();
}

public sealed class Em41MixPrimary : IEm41Mix
{
  public string Value() => "mix-primary";
}

public sealed class Em41MixOther : IEm41Mix
{
  public string Value() => "mix-other";
}

public interface IEm41Extra
{
  string Value();
}

public sealed class Em41Extra : IEm41Extra
{
  public string Value() => "mix-extra";
}

public interface IEm41Generic
{
  string Value();
}

public sealed class Em41GenericService : IEm41Generic
{
  public string Value() => "generic-addx";
}

public static class Em41ServiceCollectionExtensions
{
  public static IServiceCollection AddEm41Simple(this IServiceCollection services)
  {
    services.AddSingleton<IEm41Greeter, Em41Greeter>();
    return services;
  }

  public static IServiceCollection AddEm41A(this IServiceCollection services)
  {
    services.AddEm41B();
    return services;
  }

  public static IServiceCollection AddEm41B(this IServiceCollection services)
  {
    services.AddScoped<IEm41Nested, Em41Nested>();
    return services;
  }

  public static IServiceCollection AddEm41Mix(this IServiceCollection services)
  {
    services.TryAddSingleton<IEm41Mix, Em41MixOther>();
    services.AddSingleton<IEm41Extra, Em41Extra>();
    return services;
  }

  public static IServiceCollection AddEm41Generic<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T>(
    this IServiceCollection services)
    where T : class, IEm41Generic
  {
    services.AddSingleton<IEm41Generic, T>();
    return services;
  }
}

namespace TimeWarp.Nuru.Tests.Generator.ExtensionMethodLowering
{
  [TestTag("Generator")]
  [TestTag("DI")]
  [TestTag("Task395")]
  public class ExtensionMethodLoweringTests
  {
    [ModuleInitializer]
    internal static void Register() => RegisterTests<ExtensionMethodLoweringTests>();

    public static async Task Should_lower_simple_public_addx_wrapping_add_singleton()
    {
      using TestTerminal terminal = new();

      NuruApp app = NuruApp.CreateBuilder()
        .UseTerminal(terminal)
        .ConfigureServices(static services => services.AddEm41Simple())
        .Map("em41-simple")
          .WithHandler(static (IEm41Greeter greeter) => greeter.Greet())
          .AsQuery()
          .Done()
        .Build();

      int exitCode = await app.RunAsync(["em41-simple"]);

      exitCode.ShouldBe(0);
      terminal.OutputContains("simple-addx").ShouldBeTrue();
    }

    public static async Task Should_lower_nested_same_assembly_helpers()
    {
      using TestTerminal terminal = new();

      NuruApp app = NuruApp.CreateBuilder()
        .UseTerminal(terminal)
        .ConfigureServices(static services => services.AddEm41A())
        .Map("em41-nested")
          .WithHandler(static (IEm41Nested nested) => nested.Value())
          .AsQuery()
          .Done()
        .Build();

      int exitCode = await app.RunAsync(["em41-nested"]);

      exitCode.ShouldBe(0);
      terminal.OutputContains("nested-addx").ShouldBeTrue();
    }

    public static async Task Should_replay_tryadd_in_order_against_accumulated_model()
    {
      using TestTerminal terminal = new();

      NuruApp app = NuruApp.CreateBuilder()
        .UseTerminal(terminal)
        .ConfigureServices(static services =>
        {
          services.AddSingleton<IEm41Mix, Em41MixPrimary>();
          services.AddEm41Mix();
        })
        .Map("em41-mix")
          .WithHandler(static (IEm41Mix mix, IEm41Extra extra) => $"{mix.Value()}+{extra.Value()}")
          .AsQuery()
          .Done()
        .Build();

      int exitCode = await app.RunAsync(["em41-mix"]);

      exitCode.ShouldBe(0);
      terminal.OutputContains("mix-primary+mix-extra").ShouldBeTrue();
    }

    public static async Task Should_lower_generic_addx_registering_closed_type_argument()
    {
      using TestTerminal terminal = new();

      NuruApp app = NuruApp.CreateBuilder()
        .UseTerminal(terminal)
        .ConfigureServices(static services => services.AddEm41Generic<Em41GenericService>())
        .Map("em41-generic")
          .WithHandler(static (IEm41Generic generic) => generic.Value())
          .AsQuery()
          .Done()
        .Build();

      int exitCode = await app.RunAsync(["em41-generic"]);

      exitCode.ShouldBe(0);
      terminal.OutputContains("generic-addx").ShouldBeTrue();
    }
  }
}
