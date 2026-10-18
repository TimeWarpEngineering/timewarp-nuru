namespace TimeWarp.Nuru;

using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

public partial class NuruApp
{
  public ITerminal Terminal { get; }
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
  /// Calls run one at a time: generated route execution uses process-wide service state and
  /// <see cref="Environment.ExitCode"/>, as <see cref="RunAsync"/> does. Handler exceptions propagate.
  /// </remarks>
  /// <exception cref="ArgumentException"><paramref name="request"/> was bound by another app.</exception>
  /// <exception cref="InvalidOperationException">
  /// No generated code is attached to this app, or route execution stopped before the handler ran
  /// (for example, no handler is registered); the reason is on the app's terminal error output.
  /// </exception>
  public async Task<object?> InvokeAsync(NuruBoundRequest request, CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(request);
    Func<NuruApp, NuruInvocation, CancellationToken, Task<int>> executor = InvocationExecutor ?? throw MissingGeneratedCode();
    NuruInvocation invocation = new(request);
    int exitCode = await executor(this, invocation, cancellationToken).ConfigureAwait(false);
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
  public Task<int> RenderAsync(NuruBoundRequest request, object? result, ITerminal terminal)
  {
    ArgumentNullException.ThrowIfNull(request);
    ArgumentNullException.ThrowIfNull(terminal);
    Func<NuruApp, NuruInvocation, CancellationToken, Task<int>> executor = InvocationExecutor ?? throw MissingGeneratedCode();
    NuruInvocation invocation = new(request) { RenderTerminal = terminal, Result = result };
    return executor(this, invocation, CancellationToken.None);
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
    Terminal = terminal ?? TimeWarpTerminal.Default;
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
  /// To signal failure with a non-zero exit code, set <c>Environment.ExitCode</c>
  /// in your handler. The value will be returned as the process exit code.
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
