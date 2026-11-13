#!/usr/bin/env -S dotnet --

#region Purpose
// Pins that UseMicrosoftDependencyInjection() keeps singleton and transient lifetimes for services with constructor
// dependencies at runtime.
#endregion

#region Design
// Split from generator-15 (#392): the counter types live with the lifetime tests so each file defines every type it uses.
#endregion

using System.Globalization;
using Microsoft.Extensions.DependencyInjection;

#if !JARIBU_MULTI
return await RunAllTests();
#endif

// ═══════════════════════════════════════════════════════════════════════════════
// SERVICE INTERFACES AND IMPLEMENTATIONS (global scope for generator discovery)
// ═══════════════════════════════════════════════════════════════════════════════

/// <summary>
/// Service that tracks instance count for transient lifetime testing.
/// Uses separate type from singleton counter to avoid shared ServiceProvider conflicts.
/// </summary>
public interface IRdi15TransientCounter
{
  int GetInstanceId();
}

public class Rdi15TransientCounter : IRdi15TransientCounter
{
  private static int s_instanceCount;
  private readonly int _instanceId;

  public Rdi15TransientCounter()
  {
    _instanceId = Interlocked.Increment(ref s_instanceCount);
  }

  public int GetInstanceId() => _instanceId;

  public static void Reset() => s_instanceCount = 0;
}

/// <summary>
/// Service that tracks instance count for singleton lifetime testing.
/// Uses separate type from transient counter to avoid shared ServiceProvider conflicts.
/// </summary>
public interface IRdi15SingletonCounter
{
  int GetInstanceId();
}

public class Rdi15SingletonCounter : IRdi15SingletonCounter
{
  private static int s_instanceCount;
  private readonly int _instanceId;

  public Rdi15SingletonCounter()
  {
    _instanceId = Interlocked.Increment(ref s_instanceCount);
  }

  public int GetInstanceId() => _instanceId;

  public static void Reset() => s_instanceCount = 0;
}

// ═══════════════════════════════════════════════════════════════════════════════
// JARIBU TESTS
// ═══════════════════════════════════════════════════════════════════════════════

namespace TimeWarp.Nuru.Tests.Generator.RuntimeDILifetimes
{
  /// <summary>
  /// Tests that verify UseMicrosoftDependencyInjection() keeps transient and singleton lifetimes.
  /// </summary>
  [TestTag("Generator")]
  [TestTag("DI")]
  [TestTag("RuntimeDI")]
  [TestTag("Task392")]
  public class RuntimeDILifetimeTests
  {
    [ModuleInitializer]
    internal static void Register() => RegisterTests<RuntimeDILifetimeTests>();

    /// <summary>
    /// Verify transient lifetime creates new instances each time.
    /// </summary>
    public static async Task Should_respect_transient_lifetime()
    {
      // Arrange
      Rdi15TransientCounter.Reset();
      using TestTerminal terminal = new();

      NuruApp app = NuruApp.CreateBuilder()
        .UseTerminal(terminal)
        .UseMicrosoftDependencyInjection()
        .ConfigureServices(services =>
        {
          services.AddTransient<IRdi15TransientCounter, Rdi15TransientCounter>();
        })
        .Map("rdi15-transient-counter")
          .WithHandler((IRdi15TransientCounter counter) => counter.GetInstanceId().ToString(CultureInfo.InvariantCulture))
          .AsQuery()
          .Done()
        .Build();

      // Act - call twice
      await app.RunAsync(["rdi15-transient-counter"]);
      string firstOutput = terminal.Output;
      terminal.ClearOutput();

      await app.RunAsync(["rdi15-transient-counter"]);
      string secondOutput = terminal.Output;

      // Assert - each call should get a new instance
      firstOutput.ShouldContain("1");
      secondOutput.ShouldContain("2");
    }

    /// <summary>
    /// Verify singleton lifetime returns same instance.
    /// </summary>
    public static async Task Should_respect_singleton_lifetime()
    {
      // Arrange
      Rdi15SingletonCounter.Reset();
      using TestTerminal terminal = new();

      NuruApp app = NuruApp.CreateBuilder()
        .UseTerminal(terminal)
        .UseMicrosoftDependencyInjection()
        .ConfigureServices(services =>
        {
          services.AddSingleton<IRdi15SingletonCounter, Rdi15SingletonCounter>();
        })
        .Map("rdi15-singleton-counter")
          .WithHandler((IRdi15SingletonCounter counter) => counter.GetInstanceId().ToString(CultureInfo.InvariantCulture))
          .AsQuery()
          .Done()
        .Build();

      // Act - call twice
      await app.RunAsync(["rdi15-singleton-counter"]);
      string firstOutput = terminal.Output;
      terminal.ClearOutput();

      await app.RunAsync(["rdi15-singleton-counter"]);
      string secondOutput = terminal.Output;

      // Assert - both calls should get same instance (id = 1)
      firstOutput.ShouldContain("1");
      secondOutput.ShouldContain("1");
    }
  }
}
