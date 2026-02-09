#!/usr/bin/dotnet --
#:project ../../../source/timewarp-nuru/timewarp-nuru.csproj

// ═══════════════════════════════════════════════════════════════════════════════
// ENDPOINT DSL - COMPLETE PIPELINE MIDDLEWARE ⭐ RECOMMENDED
// ═══════════════════════════════════════════════════════════════════════════════
//
// This sample combines ALL pipeline behaviors into a complete enterprise-grade
// reference implementation.
//
// DSL: Endpoint with full behavior pipeline registered via .AddBehavior()
//
// BEHAVIORS INCLUDED:
//   1. TelemetryBehavior       - OpenTelemetry distributed tracing
//   2. PerformanceBehavior       - Timing and slow command warnings
//   3. LoggingBehavior           - Request/response logging
//   4. AuthorizationBehavior<T>  - Filtered authorization (admin only)
//   5. RetryBehavior<T>          - Exponential backoff for transient failures
//   6. ExceptionHandlingBehavior - Consistent error handling
//
// ORDER MATTERS: Behaviors execute in registration order (first = outermost)
// ═══════════════════════════════════════════════════════════════════════════════

using System.Diagnostics;
using TimeWarp.Nuru;
using static System.Console;

NuruApp app = NuruApp.CreateBuilder()
  // Outermost: Telemetry (captures everything)
  .AddBehavior(typeof(TelemetryBehavior))
  // Next: Performance monitoring
  .AddBehavior(typeof(PerformanceBehavior))
  // Next: Logging
  .AddBehavior(typeof(LoggingBehavior))
  // Next: Authorization (filtered to IRequireAuthorization)
  .AddBehavior(typeof(AuthorizationBehavior<IRequireAuthorization>))
  // Next: Retry (filtered to IRetryable)
  .AddBehavior(typeof(RetryBehavior<IRetryable>))
  // Innermost: Exception handling
  .AddBehavior(typeof(ExceptionHandlingBehavior))
  .DiscoverEndpoints()
  .Build();

await app.RunAsync(args);

// =============================================================================
// ALL PIPELINE BEHAVIORS
// =============================================================================

public sealed class TelemetryBehavior : INuruBehavior
{
  private static readonly ActivitySource Source = new("Nuru.CompletePipeline");

  public async ValueTask HandleAsync(BehaviorContext context, Func<ValueTask> proceed)
  {
    using Activity? activity = Source.StartActivity($"nuru.{context.CommandName}");
    activity?.SetTag("command", context.CommandName);
    activity?.SetTag("correlation", context.CorrelationId);

    try
    {
      await proceed();
      activity?.SetStatus(ActivityStatusCode.Ok);
    }
    catch (Exception ex)
    {
      activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
      activity?.RecordException(ex);
      throw;
    }
  }
}

public sealed class PerformanceBehavior : INuruBehavior
{
  private const int Threshold = 500;

  public async ValueTask HandleAsync(BehaviorContext context, Func<ValueTask> proceed)
  {
    Stopwatch sw = Stopwatch.StartNew();
    await proceed();
    sw.Stop();

    if (sw.ElapsedMilliseconds > Threshold)
      WriteLine($"[PERF] SLOW: {context.CommandName} took {sw.ElapsedMilliseconds}ms");
    else
      WriteLine($"[PERF] {context.CommandName} completed in {sw.ElapsedMilliseconds}ms");
  }
}

public sealed class LoggingBehavior : INuruBehavior
{
  public async ValueTask HandleAsync(BehaviorContext context, Func<ValueTask> proceed)
  {
    WriteLine($"[LOG] [{context.CorrelationId[..8]}] {context.CommandName} started");
    await proceed();
    WriteLine($"[LOG] [{context.CorrelationId[..8]}] {context.CommandName} completed");
  }
}

public interface IRequireAuthorization { }

public sealed class AuthorizationBehavior<T> : INuruBehavior<T> where T : IRequireAuthorization
{
  public async ValueTask HandleAsync(BehaviorContext context, Func<ValueTask> proceed)
  {
    string? role = Environment.GetEnvironmentVariable("USER_ROLE") ?? "user";
    if (role != "admin")
      throw new UnauthorizedAccessException($"Role '{role}' cannot execute {context.CommandName}");
    WriteLine($"[AUTH] ✓ Admin access granted");
    await proceed();
  }
}

public interface IRetryable { int MaxRetries { get; } }

public sealed class RetryBehavior<T> : INuruBehavior<T> where T : IRetryable
{
  public async ValueTask HandleAsync(BehaviorContext context, Func<ValueTask> proceed)
  {
    if (context.Command is not IRetryable r) { await proceed(); return; }

    for (int i = 1; i <= r.MaxRetries + 1; i++)
    {
      try { await proceed(); return; }
      catch (Exception ex) when (i <= r.MaxRetries && IsTransient(ex))
      {
        int delay = Math.Min((int)Math.Pow(2, i) * 50 + Random.Shared.Next(50), 3000);
        WriteLine($"[RETRY] Attempt {i} failed, waiting {delay}ms...");
        await Task.Delay(delay);
      }
    }
  }

  private static bool IsTransient(Exception ex) =>
    ex is TimeoutException or IOException or HttpRequestException;
}

public sealed class ExceptionHandlingBehavior : INuruBehavior
{
  public async ValueTask HandleAsync(BehaviorContext context, Func<ValueTask> proceed)
  {
    try { await proceed(); }
    catch (Exception ex)
    {
      WriteLine($"[ERROR] {context.CommandName} failed: {ex.Message}");
      throw;
    }
  }
}

// =============================================================================
// ENDPOINT DEFINITIONS
// =============================================================================

[NuruRoute("status", Description = "Check system status (public)")]
public sealed class StatusQuery : IQuery<Unit>
{
  public sealed class Handler : IQueryHandler<StatusQuery, Unit>
  {
    public ValueTask<Unit> Handle(StatusQuery q, CancellationToken ct)
    {
      WriteLine("✓ System operational");
      return default;
    }
  }
}

[NuruRoute("api", Description = "Call API (retries on failure)")]
public sealed class ApiCommand : ICommand<string>, IRetryable
{
  [Parameter] public string Endpoint { get; set; } = "";
  public int MaxRetries => 2;

  public sealed class Handler : ICommandHandler<ApiCommand, string>
  {
    private static int Fails = 0;
    public ValueTask<string> Handle(ApiCommand c, CancellationToken ct)
    {
      if (++Fails < 2) throw new TimeoutException("API timeout");
      Fails = 0;
      return new ValueTask<string>($"Response from {c.Endpoint}");
    }
  }
}

[NuruRoute("admin-config", Description = "Set config (admin only)")]
public sealed class AdminConfigCommand : ICommand<Unit>, IRequireAuthorization
{
  [Parameter] public string Key { get; set; } = "";
  [Parameter] public string Value { get; set; } = "";

  public sealed class Handler : ICommandHandler<AdminConfigCommand, Unit>
  {
    public ValueTask<Unit> Handle(AdminConfigCommand c, CancellationToken ct)
    {
      WriteLine($"Set {c.Key} = {c.Value}");
      return default;
    }
  }
}

[NuruRoute("slow", Description = "Slow operation (triggers performance warning)")]
public sealed class SlowCommand : ICommand<Unit>
{
  [Parameter] public int Ms { get; set; } = 600;

  public sealed class Handler : ICommandHandler<SlowCommand, Unit>
  {
    public async ValueTask<Unit> Handle(SlowCommand c, CancellationToken ct)
    {
      await Task.Delay(c.Ms, ct);
      WriteLine("Slow operation complete");
      return default;
    }
  }
}
