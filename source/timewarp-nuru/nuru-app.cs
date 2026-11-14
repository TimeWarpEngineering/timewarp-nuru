#region Purpose
// Runtime NuruApp partial: terminal, exclusivity gate, and the state a built app carries.
#endregion

#region Design
// A built NuruApp holds no route table. The generator attaches delegates (RouteExecutor,
// JsonBinder, InvocationExecutor, CapabilitiesProvider, ProblemRenderer) when it intercepts Build()
// or RunAsync(), and the members that need them throw a descriptive InvalidOperationException
// when absent, so an un-intercepted app fails loudly instead of silently doing nothing.
// RunAsync and RunReplAsync bodies throw by design: the interceptor replaces every call, so
// reaching the body means the generator did not run.
// The generator-facing members are public but EditorBrowsable.Never because generated code in the
// consumer's assembly cannot reach internals.
// Terminal prefers the ambient NuruCallContext terminal so concurrent daemon/MCP calls write to
// their own output; ExclusiveGate serializes routes marked Exclusive().
#endregion

namespace TimeWarp.Nuru;

using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using TimeWarp.Foundation.Types;

public partial class NuruApp
{
  private readonly ITerminal OwnTerminal;

  /// <summary>
  /// The terminal routes write to: the running call's terminal inside a <see cref="NuruCallContext"/>
  /// (a daemon or MCP host call), otherwise the terminal the app was built with.
  /// </summary>
  public ITerminal Terminal => NuruCallContext.Current?.Terminal ?? OwnTerminal;

  /// <summary>The gate exclusive calls of this app take (<see cref="NuruExclusiveLease"/>).</summary>
  internal SemaphoreSlim ExclusiveGate { get; } = new(1, 1);
  public ReplOptions? ReplOptions { get; init; }
  public ILoggerFactory? LoggerFactory { get; set; }

  /// <summary>
  /// Shell completion provider with source-generated static completion data.
  /// Set by generated interceptor code. Used by <see cref="DynamicCompletionHandler"/>.
  /// </summary>
  public IShellCompletionProvider? ShellCompletionProvider { get; set; }

  /// <summary>
  /// Registry for custom completion sources (databases, APIs, etc.).
  /// Configured via EnableCompletion(configure: registry => ...).
  /// </summary>
  public CompletionSourceRegistry CompletionSourceRegistry { get; } = new();

  /// <summary>
  /// Callback to configure completion sources. Set during Build() from builder configuration.
  /// Invoked once at the start of RunAsync by generated code.
  /// </summary>
  internal Action<CompletionSourceRegistry>? CompletionRegistryConfiguration { get; set; }

  /// <summary>Whether the completion registry has been configured.</summary>
  private bool CompletionRegistryConfigured;

  /// <summary>
  /// Configures the completion source registry if not already configured.
  /// Called by generated interceptor code at the start of RunAsync.
  /// </summary>
  public void ConfigureCompletionRegistry()
  {
    if (CompletionRegistryConfigured)
      return;

    CompletionRegistryConfigured = true;
    CompletionRegistryConfiguration?.Invoke(CompletionSourceRegistry);
  }

  /// <summary>
  /// Returns this app's generated capabilities catalog. Set by generated interceptor code when
  /// <see cref="NuruAppBuilder.Build"/> returns, or at the start of <see cref="RunAsync"/> when
  /// that Build() call was not intercepted.
  /// Use <see cref="Capabilities"/> instead of calling it.
  /// </summary>
  [EditorBrowsable(EditorBrowsableState.Never)]
  public Func<CapabilitiesResponse>? CapabilitiesProvider { get; set; }

  /// <summary>
  /// The full capabilities catalog for this app: the same document <c>--capabilities</c> prints.
  /// </summary>
  /// <remarks>
  /// The source generator builds the catalog at compile time, so this needs no reflection and works
  /// under AOT. The instance is built on first access and cached; treat it as read-only.
  /// <see cref="CapabilitiesResponse.FilterByGroup(string)"/> returns the document
  /// <c>--capabilities --group-filter {group}</c> prints.
  /// </remarks>
  /// <exception cref="InvalidOperationException">
  /// The source generator did not intercept this app's <c>Build()</c> or <c>RunAsync()</c> call.
  /// </exception>
  public CapabilitiesResponse Capabilities =>
    CapabilitiesProvider?.Invoke()
    ?? throw new InvalidOperationException(
      "No generated capabilities catalog is attached to this NuruApp. " +
      "The source generator attaches it to apps whose Build() or RunAsync() call it intercepts.");

  /// <summary>
  /// The generated JSON binder for this app. Set by generated interceptor code, like
  /// <see cref="CapabilitiesProvider"/>. Use <see cref="TryBind"/> instead of calling it.
  /// </summary>
  [EditorBrowsable(EditorBrowsableState.Never)]
  public NuruJsonBinder? JsonBinder { get; set; }

  /// <summary>
  /// Runs or renders a bound request through generated route execution. Set by generated
  /// interceptor code. Use <see cref="InvokeAsync"/> and <see cref="RenderAsync"/> instead of calling it.
  /// </summary>
  [EditorBrowsable(EditorBrowsableState.Never)]
  public Func<NuruApp, NuruInvocation, CancellationToken, Task<int>>? InvocationExecutor { get; set; }

  /// <summary>
  /// Renders problem details with the app's <c>IResponseRenderer&lt;SharedProblemDetails&gt;</c>, or returns null
  /// when the app has none. Set by generated interceptor code when one can exist. Use
  /// <see cref="TryRenderProblem"/> instead of calling it.
  /// </summary>
  [EditorBrowsable(EditorBrowsableState.Never)]
  public Func<NuruApp, SharedProblemDetails, string?>? ProblemRenderer { get; set; }

  /// <summary>
  /// True when the builder called <c>UseProblemRendererForErrors()</c>: CLI binding errors go through
  /// <see cref="TryRenderProblem"/>. Set by generated interceptor code.
  /// </summary>
  [EditorBrowsable(EditorBrowsableState.Never)]
  public bool ProblemRendererForErrors { get; set; }

  /// <summary>
  /// True when the builder called <c>UseVerbatimRendererOutput()</c>: rendered problems are written with no
  /// added newline. Set by generated interceptor code.
  /// </summary>
  [EditorBrowsable(EditorBrowsableState.Never)]
  public bool VerbatimRendererOutput { get; set; }

  /// <summary>
  /// Runs CLI arguments through generated route matching. Set by generated interceptor code, like
  /// <see cref="InvocationExecutor"/>. Use <see cref="ExecuteAsync"/> instead of calling it.
  /// </summary>
  [EditorBrowsable(EditorBrowsableState.Never)]
  public Func<NuruApp, string[], CancellationToken, Task<int>>? RouteExecutor { get; set; }

  /// <summary>
  /// Forwards matched routes to another process (a served app). Set by generated code when the
  /// builder calls <c>AddServeRoute()</c> (package TimeWarp.Nuru.Daemon); null runs every route here.
  /// </summary>
  public INuruRouteForwarder? RouteForwarder { get; set; }

  /// <summary>
  /// Runs CLI arguments as <see cref="RunAsync"/> does: the same route matching, built-ins, binding,
  /// behaviors and handler, writing to <see cref="Terminal"/>.
  /// </summary>
  /// <param name="args">Command-line arguments to parse and route.</param>
  /// <param name="cancellationToken">Token passed to the handler and behaviors.</param>
  /// <returns>The exit code <see cref="RunAsync"/> returns for the same arguments.</returns>
  /// <remarks>
  /// <see cref="RunAsync"/> works only at call sites the source generator intercepts. This entry point
  /// is for library code that runs an app it did not build. It skips the process-level telemetry setup
  /// <see cref="RunAsync"/> does. Like <see cref="RunAsync"/>, it uses process state
  /// (<see cref="Environment.ExitCode"/>, this app's terminal), so run one call at a time; use
  /// <see cref="ExecuteAsync(string[], NuruCallContext, CancellationToken)"/> to run calls concurrently.
  /// </remarks>
  /// <exception cref="InvalidOperationException">No generated code is attached to this app.</exception>
  public Task<int> ExecuteAsync(string[] args, CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(args);
    Func<NuruApp, string[], CancellationToken, Task<int>> executor = RouteExecutor ?? throw MissingGeneratedCode();
    return executor(this, args, cancellationToken);
  }

  /// <summary>
  /// Runs CLI arguments as <see cref="ExecuteAsync(string[], CancellationToken)"/> does, in
  /// <paramref name="context"/>: output goes to the context's terminal, relative paths resolve against
  /// its directory, and the exit code is the call's own (<see cref="NuruCallContext.ExitCode"/>).
  /// </summary>
  /// <param name="args">Command-line arguments to parse and route.</param>
  /// <param name="context">The call's terminal, directory and exit code.</param>
  /// <param name="cancellationToken">Token passed to the handler and behaviors.</param>
  /// <returns>The call's exit code.</returns>
  /// <remarks>
  /// Calls in separate contexts run concurrently: each gets its own service scope. Routes marked
  /// <see cref="ExclusiveAttribute"/> still run one at a time. A daemon host runs every forwarded call this way.
  /// </remarks>
  /// <exception cref="InvalidOperationException">No generated code is attached to this app.</exception>
  public async Task<int> ExecuteAsync(string[] args, NuruCallContext context, CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(args);
    ArgumentNullException.ThrowIfNull(context);
    Func<NuruApp, string[], CancellationToken, Task<int>> executor = RouteExecutor ?? throw MissingGeneratedCode();
    int exitCode = await context.RunAsync(() => executor(this, args, cancellationToken)).ConfigureAwait(false);
    context.ExitCode = exitCode;
    return exitCode;
  }

  private static InvalidOperationException MissingGeneratedCode() =>
    new(
      "No generated binder is attached to this NuruApp. " +
      "The source generator attaches it to apps whose Build() or RunAsync() call it intercepts.");

  /// <summary>Tracer provider for distributed tracing. Set by generated telemetry code.</summary>
  public TracerProvider? TracerProvider { get; set; }

  /// <summary>
  /// Meter provider for metrics. Set by generated telemetry code.
  /// </summary>
  public MeterProvider? MeterProvider { get; set; }

  public NuruApp(ITerminal? terminal = null)
  {
    OwnTerminal = terminal ?? TimeWarpTerminal.Default;
  }

  /// <summary>
  /// Flushes all telemetry data and disposes providers.
  /// Called automatically by generated RunAsync code when telemetry is enabled.
  /// </summary>
  /// <remarks>
  /// Returns immediately when no provider was created (no OTLP endpoint configured),
  /// so the grace delay is only paid when there is telemetry to export.
  /// </remarks>
  /// <param name="delayMs">Delay in milliseconds to allow export to complete. Default: 1000ms.</param>
  public async Task FlushTelemetryAsync(int delayMs = 1000)
  {
    if (TracerProvider is null && MeterProvider is null && LoggerFactory is null)
      return;

    TracerProvider?.ForceFlush();
    MeterProvider?.ForceFlush();

    if (delayMs > 0)
    {
      await Task.Delay(delayMs).ConfigureAwait(false);
    }

    TracerProvider?.Dispose();
    MeterProvider?.Dispose();
    LoggerFactory?.Dispose();
    TracerProvider = null;
    MeterProvider = null;
    LoggerFactory = null;
  }

  /// <summary>
  /// Runs the application with the given command-line arguments.
  /// This method is intercepted by generated code.
  /// </summary>
  /// <param name="args">Command-line arguments to parse and route.</param>
  /// <returns>
  /// Exit code: 0 for success, non-zero for failure.
  /// <para>
  /// <b>Important:</b> Handler return values are written to the terminal as output,
  /// they do NOT control the exit code. For example, <c>.WithHandler(() =&gt; 42)</c>
  /// outputs "42" to the terminal but still returns exit code 0.
  /// </para>
  /// <para>
  /// To signal failure with a non-zero exit code, set <see cref="NuruCallContext.CurrentExitCode"/>
  /// (or <c>Environment.ExitCode</c>) in your handler. The value will be returned as the process exit
  /// code. In a daemon or MCP host call only <see cref="NuruCallContext.CurrentExitCode"/> is per call.
  /// </para>
  /// </returns>
#pragma warning disable CA1822 // Member does not access instance data
  public Task<int> RunAsync(string[] args)
  {
    // This should never execute - interceptor replaces this call
    throw new InvalidOperationException(
        "RunAsync was not intercepted. Ensure the source generator is enabled.");
  }
#pragma warning restore CA1822

  /// <summary>
  /// Runs the application in REPL (interactive) mode.
  /// This method is intercepted by generated code when AddRepl() is called.
  /// </summary>
  /// <param name="cancellationToken">Optional cancellation token.</param>
  /// <returns>The exit code of the last executed command, or non-zero if the REPL could not run.</returns>
  /// <exception cref="InvalidOperationException">
  /// Thrown if the call was not intercepted. Ensure AddRepl() is called in the builder
  /// and the source generator is enabled.
  /// </exception>
#pragma warning disable CA1822 // Member does not access instance data
  public Task<int> RunReplAsync(CancellationToken cancellationToken = default)
  {
    // This should never execute - interceptor replaces this call
    throw new InvalidOperationException(
        "RunReplAsync was not intercepted. Ensure AddRepl() is called and the source generator is enabled.");
  }
#pragma warning restore CA1822

  /// <summary>
  /// Creates a builder for a Nuru application.
  /// Use fluent methods to configure individual features:
  /// <see cref="NuruAppBuilder.AddRepl(Action{ReplOptions})"/> for REPL,
  /// <see cref="NuruAppBuilder.ConfigureHelp(Action{HelpOptions})"/> for help,
  /// <see cref="NuruAppBuilder.AddConfiguration"/> for configuration.
  /// </summary>
  /// <returns>A configured NuruAppBuilder.</returns>
  /// <example>
  /// <code>
  /// NuruApp.CreateBuilder()
  ///     .AddRepl(options =>
  ///     {
  ///       options.Prompt = "myapp> ";
  ///       options.WelcomeMessage = "Welcome!";
  ///     })
  ///     .Map("greet {name}")
  ///       .WithHandler((string name) => Console.WriteLine($"Hello, {name}!"))
  ///       .Done()
  ///     .Build();
  /// </code>
  /// </example>
  [System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope",
    Justification = "Builder ownership is transferred to caller who is responsible for disposal")]
  public static NuruAppBuilder CreateBuilder()
  {
    return new NuruAppBuilder();
  }
}
