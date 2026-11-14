#region Purpose
// NuruApp binding, invocation, rendering and problem/bind error reporting members.
#endregion

#region Design
// TryBind/InvokeAsync/RenderAsync give hosts (daemon, MCP) the same binding rules as
// --json-args without going through argv. Execution always flows through the generated
// InvocationExecutor, so these members stay AOT-safe and reuse the route's behaviors.
// InvokeAsync throws when the handler never ran, because an exit code alone cannot carry the
// failure to a caller that expects a result.
// Argument-error writers route through the problem renderer only when ProblemRendererForErrors is
// set, and otherwise write the plain message, so argv users keep the classic text.
// Bind error and bound-request types are in separate files; this partial only orchestrates them.
#endregion

namespace TimeWarp.Nuru;

using TimeWarp.Foundation.Types;

public partial class NuruApp
{
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
}
