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

  /// <summary>
  /// Whether the completion registry has been configured.
  /// </summary>
  private bool completionRegistryConfigured;

  /// <summary>
  /// Configures the completion source registry if not already configured.
  /// Called by generated interceptor code at the start of RunAsync.
  /// </summary>
  public void ConfigureCompletionRegistry()
  {
    if (completionRegistryConfigured)
      return;

    completionRegistryConfigured = true;
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
  /// Binds a JSON object to a route with the same rules as <c>--json-args</c>, without argv.
  /// </summary>
  /// <param name="route">The route pattern, as in <see cref="EndpointCapability.Pattern"/> (ordinal match).</param>
  /// <param name="arguments">
  /// A JSON object whose keys are the route's parameter and option names. <c>default(JsonElement)</c>
  /// binds no arguments.
  /// </param>
  /// <param name="request">The bound request. Pass it to <see cref="InvokeAsync"/>.</param>
  /// <param name="error">Why binding failed; its message is what <c>--json-args</c> prints.</param>
  /// <returns>True when the arguments bind to the route.</returns>
  /// <remarks>
  /// Keys match parameter and option names exactly (ordinal). Unknown keys fail. Values must have the
  /// member's JSON kind; <c>null</c> is accepted only for nullable members; repeated options and
  /// catch-alls take arrays. Absent keys keep the member's default. The binder is generated, so this
  /// needs no reflection and works under AOT.
  /// </remarks>
  /// <exception cref="InvalidOperationException">No generated binder is attached to this app.</exception>
  public bool TryBind(
    string route,
    JsonElement arguments,
    [NotNullWhen(true)] out NuruBoundRequest? request,
    [NotNullWhen(false)] out NuruBindError? error)
  {
    ArgumentNullException.ThrowIfNull(route);
    NuruJsonBinder binder = JsonBinder ?? throw MissingGeneratedCode();
    if (binder(route, arguments, out NuruBoundRequest? bound, out NuruBindError? failure) && bound is not null)
    {
      request = bound;
      error = null;
      return true;
    }

    request = null;
    error = failure ?? new NuruBindError(NuruBindErrorKind.UnknownRoute, null, $"Error: Unknown route '{route}'.");
    return false;
  }

  /// <summary>
  /// Runs a request bound by <see cref="TryBind"/>: the same configuration, services, command scope,
  /// Nuru behaviors and handler as the CLI, without writing the result.
  /// </summary>
  /// <param name="request">A request bound by this app's <see cref="TryBind"/>.</param>
  /// <param name="cancellationToken">Token passed to the handler and behaviors.</param>
  /// <returns>
  /// The handler's result, boxed (for an <c>OneOf&lt;T, SharedProblemDetails&gt;</c> handler, the
  /// <c>OneOf</c> itself), or null for a void or <c>Unit</c> handler. Pass it to
  /// <see cref="RenderAsync"/> for the CLI's text.
  /// </returns>
  /// <remarks>
  /// Each call runs in its own <see cref="NuruCallContext"/> (the current call's terminal and directory,
  /// or this app's terminal outside a call), so calls may run concurrently: each has its own exit code
  /// and service scope. Handler exceptions propagate.
  /// </remarks>
  /// <exception cref="ArgumentException"><paramref name="request"/> was bound by another app.</exception>
  /// <exception cref="InvalidOperationException">
  /// No generated code is attached to this app, or route execution stopped before the handler ran
  /// (for example, no handler is registered); the reason is on the app's terminal error output.
  /// </exception>
  public Task<object?> InvokeAsync(NuruBoundRequest request, CancellationToken cancellationToken = default) =>
    InvokeAsync(request, NuruCallContext.ForNestedCall(OwnTerminal), cancellationToken);

  /// <summary>
  /// Runs a request bound by <see cref="TryBind"/> in <paramref name="context"/>: what the handler writes
  /// goes to the context's terminal, and relative paths resolve against its directory.
  /// </summary>
  /// <param name="request">A request bound by this app's <see cref="TryBind"/>.</param>
  /// <param name="context">The call's terminal, directory and exit code.</param>
  /// <param name="cancellationToken">Token passed to the handler and behaviors.</param>
  /// <returns>The handler's result, as <see cref="InvokeAsync(NuruBoundRequest, CancellationToken)"/> returns it.</returns>
  /// <exception cref="ArgumentException"><paramref name="request"/> was bound by another app.</exception>
  /// <exception cref="InvalidOperationException">
  /// No generated code is attached to this app, or route execution stopped before the handler ran;
  /// the reason is on the context terminal's error output.
  /// </exception>
  public async Task<object?> InvokeAsync(NuruBoundRequest request, NuruCallContext context, CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(request);
    ArgumentNullException.ThrowIfNull(context);
    Func<NuruApp, NuruInvocation, CancellationToken, Task<int>> executor = InvocationExecutor ?? throw MissingGeneratedCode();
    NuruInvocation invocation = new(request);
    int exitCode = await context.RunAsync(() => executor(this, invocation, cancellationToken)).ConfigureAwait(false);
    context.ExitCode = exitCode;
    if (!invocation.Dispatched)
    {
      throw new InvalidOperationException(
        $"'{request.Route}' exited with code {exitCode} before its handler ran. See the app's error output.");
    }

    return invocation.Result;
  }

  /// <summary>
  /// Writes a result from <see cref="InvokeAsync"/> as the CLI writes it for that route: a registered
  /// <c>IResponseRenderer&lt;T&gt;</c>, problem details to stderr, otherwise the CLI output strategy
  /// (raw text, ISO 8601, JSON).
  /// </summary>
  /// <param name="request">The request the result came from.</param>
  /// <param name="result">The value <see cref="InvokeAsync"/> returned.</param>
  /// <param name="terminal">Terminal to write to, for example a <c>TestTerminal</c> to capture the text.</param>
  /// <returns>The exit code the CLI returns for this result: problem details give their status, otherwise 0.</returns>
  /// <remarks>
  /// Rendering runs in its own <see cref="NuruCallContext"/> on <paramref name="terminal"/>, starting at
  /// exit code 0, so the exit code is this result's alone, never <see cref="Environment.ExitCode"/>. To
  /// render the result of <see cref="InvokeAsync(NuruBoundRequest, NuruCallContext, CancellationToken)"/>
  /// as part of that call, use <see cref="RenderAsync(NuruBoundRequest, object?, ITerminal, NuruCallContext)"/>.
  /// </remarks>
  public Task<int> RenderAsync(NuruBoundRequest request, object? result, ITerminal terminal)
  {
    ArgumentNullException.ThrowIfNull(terminal);
    NuruCallContext context = new(terminal, NuruCallContext.CurrentDirectory) { CaptureConsole = NuruCallContext.Current?.CaptureConsole ?? false };
    return RenderInContextAsync(request, result, context);
  }

  /// <summary>
  /// Writes a result from <see cref="InvokeAsync(NuruBoundRequest, NuruCallContext, CancellationToken)"/> as
  /// the rest of that call: as <see cref="RenderAsync(NuruBoundRequest, object?, ITerminal)"/> does, but
  /// starting from the exit code the handler left in <paramref name="call"/> and resolving relative paths
  /// against the call's working directory.
  /// </summary>
  /// <param name="request">The request the result came from.</param>
  /// <param name="result">The value <see cref="InvokeAsync(NuruBoundRequest, NuruCallContext, CancellationToken)"/> returned.</param>
  /// <param name="terminal">Terminal to write to, for example a <c>TestTerminal</c> to capture the text.</param>
  /// <param name="call">The context the request was invoked in. Its <see cref="NuruCallContext.ExitCode"/> is set to the result.</param>
  /// <returns>
  /// The call's exit code: problem details give their status, otherwise the code the handler set with
  /// <see cref="NuruCallContext.CurrentExitCode"/> (0 when it set none).
  /// </returns>
  public async Task<int> RenderAsync(NuruBoundRequest request, object? result, ITerminal terminal, NuruCallContext call)
  {
    ArgumentNullException.ThrowIfNull(terminal);
    ArgumentNullException.ThrowIfNull(call);
    NuruCallContext context = new(terminal, call.WorkingDirectory)
    {
      CaptureConsole = call.CaptureConsole,
      HoldsExclusive = call.HoldsExclusive,
      ExitCode = call.ExitCode
    };
    int exitCode = await RenderInContextAsync(request, result, context).ConfigureAwait(false);
    call.ExitCode = exitCode;
    return exitCode;
  }

  private Task<int> RenderInContextAsync(NuruBoundRequest request, object? result, NuruCallContext context)
  {
    ArgumentNullException.ThrowIfNull(request);
    Func<NuruApp, NuruInvocation, CancellationToken, Task<int>> executor = InvocationExecutor ?? throw MissingGeneratedCode();
    NuruInvocation invocation = new(request) { RenderTerminal = context.Terminal, Result = result };
    return context.RunAsync(() => executor(this, invocation, CancellationToken.None));
  }

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
  /// Renders <paramref name="problem"/> with the app's <c>IResponseRenderer&lt;SharedProblemDetails&gt;</c>: the
  /// renderer its problem-returning handlers use (runtime DI, a registered service, or the one visible renderer).
  /// </summary>
  /// <param name="problem">The problem, for example from <see cref="NuruProblems.FromBindError"/>.</param>
  /// <param name="text">The rendered text.</param>
  /// <returns>False when the app has no problem renderer.</returns>
  /// <remarks>Renderer exceptions propagate.</remarks>
  public bool TryRenderProblem(SharedProblemDetails problem, [NotNullWhen(true)] out string? text)
  {
    ArgumentNullException.ThrowIfNull(problem);
    text = ProblemRenderer?.Invoke(this, problem);
    return text is not null;
  }

  /// <summary>
  /// Writes an argv value error. Called by generated code. With <c>UseProblemRendererForErrors()</c> and a
  /// problem renderer, the error is rendered as <see cref="NuruProblems.Invalid"/> to stderr; otherwise
  /// <paramref name="message"/> is written to stdout as it always was.
  /// </summary>
  /// <param name="message">The CLI's message.</param>
  /// <param name="key">The parameter or option name.</param>
  [EditorBrowsable(EditorBrowsableState.Never)]
  public void WriteArgumentError(string message, string? key) =>
    WriteArgumentError(message, NuruBindErrorKind.InvalidValue, key);

  /// <summary>
  /// Writes an argv error of a given kind, as <see cref="WriteArgumentError(string, string?)"/>. Called by
  /// generated code, for example with <see cref="NuruBindErrorKind.MissingValue"/> for an option with no value.
  /// </summary>
  /// <param name="message">The CLI's message.</param>
  /// <param name="kind">Why the argument was rejected.</param>
  /// <param name="key">The parameter or option name.</param>
  [EditorBrowsable(EditorBrowsableState.Never)]
  public void WriteArgumentError(string message, NuruBindErrorKind kind, string? key)
  {
    if (!TryWriteProblemError(message, kind, key))
      Terminal.WriteLine(message);
  }

  /// <summary>
  /// Writes a <c>--json-args</c> error. Called by generated code. With <c>UseProblemRendererForErrors()</c> and a
  /// problem renderer, the error is rendered as <see cref="NuruProblems.Invalid"/>; otherwise
  /// <paramref name="message"/> is written as it always was. Both go to stderr.
  /// </summary>
  /// <param name="message">The CLI's message.</param>
  /// <param name="kind">Why binding failed, or null when no <see cref="NuruBindErrorKind"/> applies.</param>
  /// <param name="key">The key the error is about, or null.</param>
  [EditorBrowsable(EditorBrowsableState.Never)]
  public Task WriteBindErrorAsync(string message, NuruBindErrorKind? kind, string? key)
  {
    if (TryWriteProblemError(message, kind, key))
      return Task.CompletedTask;

    return Terminal.WriteErrorLineAsync(message);
  }

  /// <summary>
  /// Writes a <c>--json-args</c> bind failure. Called by generated code, as
  /// <see cref="WriteBindErrorAsync(string, NuruBindErrorKind?, string?)"/>.
  /// </summary>
  /// <param name="error">The bind failure.</param>
  [EditorBrowsable(EditorBrowsableState.Never)]
  public Task WriteBindErrorAsync(NuruBindError error)
  {
    ArgumentNullException.ThrowIfNull(error);
    return WriteBindErrorAsync(error.Message, error.Kind, error.Key);
  }

  private bool TryWriteProblemError(string message, NuruBindErrorKind? kind, string? key)
  {
    if (!ProblemRendererForErrors || !TryRenderProblem(NuruProblems.Invalid(message, kind, key), out string? text))
      return false;

    if (VerbatimRendererOutput)
      Terminal.Error.Write(text);
    else
      Terminal.WriteErrorLine(text);
    return true;
  }

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

  /// <summary>
  /// Tracer provider for distributed tracing. Set by generated telemetry code.
  /// </summary>
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
  /// <param name="delayMs">Delay in milliseconds to allow export to complete. Default: 1000ms.</param>
  public async Task FlushTelemetryAsync(int delayMs = 1000)
  {
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
