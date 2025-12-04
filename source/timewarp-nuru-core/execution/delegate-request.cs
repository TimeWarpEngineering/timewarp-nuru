namespace TimeWarp.Nuru;

/// <summary>
/// Wraps a delegate invocation as a Mediator request, enabling pipeline behaviors
/// to apply uniformly to delegate routes.
/// </summary>
/// <remarks>
/// When DI is enabled and pipeline behaviors are registered, delegate routes are wrapped
/// in this request and sent through <see cref="IMediator.Send"/>. This ensures cross-cutting
/// concerns (logging, metrics, validation, etc.) apply consistently to all routes.
///
/// The source generator in martinothamar/Mediator scans referenced assemblies by default,
/// so the <see cref="DelegateRequestHandler"/> defined in this library will be discovered
/// automatically when consuming applications call <c>services.AddMediator()</c>.
/// </remarks>
public sealed class DelegateRequest : IRequest<DelegateResponse>
{
  /// <summary>
  /// Gets the matched route pattern (e.g., "deploy {env} --dry-run").
  /// </summary>
  public required string RoutePattern { get; init; }

  /// <summary>
  /// Gets the arguments bound from route parameters.
  /// </summary>
  [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1819:Properties should not return arrays",
    Justification = "Array is intentional for delegate invocation - represents bound arguments")]
  public required object?[] BoundArguments { get; init; }

  /// <summary>
  /// Gets the delegate handler to invoke.
  /// </summary>
  public required Delegate Handler { get; init; }

  /// <summary>
  /// Gets the endpoint metadata for logging/tracing.
  /// </summary>
  public required Endpoint Endpoint { get; init; }
}

/// <summary>
/// Response from a delegate execution, containing the boxed result.
/// </summary>
public sealed class DelegateResponse
{
  /// <summary>
  /// Gets the result of the delegate execution (boxed, or null for void).
  /// </summary>
  public object? Result { get; init; }

  /// <summary>
  /// Gets the exit code for the command (0 for success, non-zero for failure).
  /// </summary>
  public int ExitCode { get; init; }

  /// <summary>
  /// Creates a successful response with the given result.
  /// </summary>
  public static DelegateResponse Success(object? result) => new()
  {
    Result = result,
    ExitCode = result is int code ? code : 0
  };

  /// <summary>
  /// Creates a failure response with the given exit code.
  /// </summary>
  public static DelegateResponse Failure(int exitCode = 1) => new()
  {
    Result = null,
    ExitCode = exitCode
  };
}

/// <summary>
/// Handles <see cref="DelegateRequest"/> by invoking the wrapped delegate.
/// </summary>
/// <remarks>
/// This handler is automatically discovered by martinothamar/Mediator's source generator
/// when consuming applications call <c>services.AddMediator()</c>, as the generator
/// scans referenced assemblies by default.
///
/// The handler uses source-generated typed invokers from <see cref="InvokerRegistry"/>
/// for AOT-compatible delegate invocation. Parameter binding and type conversion
/// happen before the request is created.
/// </remarks>
public sealed class DelegateRequestHandler : IRequestHandler<DelegateRequest, DelegateResponse>
{
  /// <summary>
  /// Invokes the wrapped delegate using source-generated typed invokers.
  /// </summary>
  public async ValueTask<DelegateResponse> Handle(DelegateRequest request, CancellationToken cancellationToken)
  {
    ArgumentNullException.ThrowIfNull(request);
    _ = cancellationToken; // Available for future use

    MethodInfo method = request.Handler.Method;
    string signatureKey = InvokerRegistry.ComputeSignatureKey(method);
    object? result;

    // Try async invoker first
    if (InvokerRegistry.TryGetAsyncInvoker(signatureKey, out AsyncInvoker? asyncInvoker))
    {
      result = await asyncInvoker(request.Handler, request.BoundArguments).ConfigureAwait(false);
    }
    // Try sync invoker
    else if (InvokerRegistry.TryGetSync(signatureKey, out SyncInvoker? syncInvoker))
    {
      result = syncInvoker(request.Handler, request.BoundArguments);
    }
    else
    {
      throw new InvalidOperationException(
        $"No source-generated invoker found for signature '{signatureKey}'. " +
        "Ensure the NuruInvokerGenerator source generator is running and the delegate signature is supported. " +
        $"Route: {request.RoutePattern}");
    }

    return DelegateResponse.Success(result);
  }
}
