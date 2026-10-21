#!/usr/bin/env -S dotnet --

// A singleton that injects a Scoped service which injects a command-scoped service (508-007 review M1).
// The Scoped service lives for one run, in the run's command slot. The singleton (NURU056) gets its own
// copy, so it never keeps an instance the run disposes, and the next call's handler gets new, live instances.

#pragma warning disable CA1063 // Probes only record Dispose calls.
#pragma warning disable CA1816

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.Generator.SingletonOverPerRunScoped
{
using Microsoft.Extensions.DependencyInjection;

public interface IG57Token
{
  int Id { get; }

  bool IsDisposed { get; }
}

public interface IG57Holder
{
  IG57Token Token { get; }
}

public interface IG57Root
{
  IG57Holder Holder { get; }
}

public sealed class G57Token : IG57Token, IDisposable
{
  private static int NextId;

  public int Id { get; } = Interlocked.Increment(ref NextId);

  public bool IsDisposed { get; private set; }

  public void Dispose() => IsDisposed = true;
}

public sealed class G57Holder(IG57Token token) : IG57Holder
{
  public IG57Token Token { get; } = token;
}

public sealed class G57Root(IG57Holder holder) : IG57Root
{
  public IG57Holder Holder { get; } = holder;
}

/// <summary>What one handler call saw.</summary>
public sealed record G57Seen(IG57Root Root, IG57Holder Holder, IG57Token Token, bool TokenDisposed, bool RootTokenDisposed);

internal static class G57Handlers
{
  internal static readonly List<G57Seen> Seen = [];

  internal static string Go(IG57Root root, IG57Holder holder, IG57Token token)
  {
    Seen.Add(new G57Seen(root, holder, token, token.IsDisposed, root.Holder.Token.IsDisposed));
    return "go";
  }

  internal static void Register(IServiceCollection services)
  {
    services.AddCommandScoped<IG57Token, G57Token>();
    services.AddScoped<IG57Holder, G57Holder>();
#pragma warning disable NURU056 // The singleton outlives the per-run holder on purpose: this is the case under test.
    services.AddSingleton<IG57Root, G57Root>();
#pragma warning restore NURU056
  }
}

[TestTag("Generator")]
[TestTag("DI")]
public class SingletonOverPerRunScopedTests
{
  [ModuleInitializer]
  internal static void Register() => RegisterTests<SingletonOverPerRunScopedTests>();

  public static async Task Should_give_each_run_live_per_run_services_and_keep_the_singleton_off_them()
  {
    G57Handlers.Seen.Clear();
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .ConfigureServices(G57Handlers.Register)
      .Map("g57-go").WithHandler(G57Handlers.Go).AsQuery().Done()
      .Build();

    (await app.ExecuteAsync(["g57-go"])).ShouldBe(0);
    (await app.ExecuteAsync(["g57-go"])).ShouldBe(0);

    G57Handlers.Seen.Count.ShouldBe(2);
    G57Seen first = G57Handlers.Seen[0];
    G57Seen second = G57Handlers.Seen[1];

    // Each run: the handler's holder carries the run's token, and the token is live during the handler.
    first.Holder.Token.ShouldBeSameAs(first.Token);
    second.Holder.Token.ShouldBeSameAs(second.Token);
    first.TokenDisposed.ShouldBeFalse();
    second.TokenDisposed.ShouldBeFalse();

    // The next run builds new per-run instances; the first run's token was disposed when it ended.
    second.Token.Id.ShouldNotBe(first.Token.Id);
    second.Holder.ShouldNotBeSameAs(first.Holder);
    first.Token.IsDisposed.ShouldBeTrue();

    // One singleton, and it keeps its own holder: never a run's instance, never a disposed token.
    second.Root.ShouldBeSameAs(first.Root);
    first.Root.Holder.ShouldNotBeSameAs(first.Holder);
    first.Root.Holder.Token.ShouldNotBeSameAs(first.Token);
    first.RootTokenDisposed.ShouldBeFalse();
    second.RootTokenDisposed.ShouldBeFalse();
  }
}
}
