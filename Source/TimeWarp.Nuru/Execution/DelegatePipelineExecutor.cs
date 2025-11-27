namespace TimeWarp.Nuru;

/// <summary>
/// Executes delegate routes through a pipeline of behaviors when DI is enabled.
/// This enables cross-cutting concerns (logging, metrics, validation, etc.) to apply
/// uniformly to both delegate and explicit command routes.
/// </summary>
/// <remarks>
/// Unlike explicit IRequest commands that flow through IMediator.Send(), delegate routes
/// use this custom executor that manually invokes IPipelineBehavior instances.
///
/// This approach is necessary because martinothamar/Mediator uses source generation
/// and doesn't support handlers defined in external libraries.
///
/// The executor follows the same pattern as Mediator's pipeline:
/// 1. Resolve all IPipelineBehavior&lt;DelegateRequest, DelegateResponse&gt; from DI
/// 2. Chain them together with the delegate invocation as the innermost handler
/// 3. Execute the pipeline
/// </remarks>
public sealed class DelegatePipelineExecutor
{
  private readonly IServiceProvider ServiceProvider;
  private readonly RouteExecutionContext ExecutionContext;

  /// <summary>
  /// Initializes a new instance of the <see cref="DelegatePipelineExecutor"/> class.
  /// </summary>
  /// <param name="serviceProvider">The service provider for resolving pipeline behaviors.</param>
  /// <param name="executionContext">The route execution context for sharing metadata with behaviors.</param>
  public DelegatePipelineExecutor(IServiceProvider serviceProvider, RouteExecutionContext executionContext)
  {
    ServiceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    ExecutionContext = executionContext ?? throw new ArgumentNullException(nameof(executionContext));
  }

  /// <summary>
  /// Executes a delegate request through the pipeline of behaviors.
  /// </summary>
  /// <param name="request">The delegate request to execute.</param>
  /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
  /// <returns>The response from the delegate execution.</returns>
  public ValueTask<DelegateResponse> ExecuteAsync(DelegateRequest request, CancellationToken cancellationToken)
  {
    ArgumentNullException.ThrowIfNull(request);

    // Populate the execution context for pipeline behaviors
    ExecutionContext.RoutePattern = request.RoutePattern;
    ExecutionContext.Parameters = request.Endpoint.CompiledRoute.PositionalMatchers
      .OfType<ParameterMatcher>()
      .Select((matcher, index) => new { matcher.Name, Index = index })
      .Where(x => x.Index < request.BoundArguments.Length)
      .ToDictionary(x => x.Name, x => request.BoundArguments[x.Index]?.ToString() ?? "");
    ExecutionContext.StartedAt = DateTimeOffset.UtcNow;
    ExecutionContext.Strategy = ExecutionStrategy.Delegate;
    ExecutionContext.IsWrappedDelegate = true;

    // Get all registered pipeline behaviors for DelegateRequest/DelegateResponse
    IEnumerable<IPipelineBehavior<DelegateRequest, DelegateResponse>> behaviors =
      ServiceProvider.GetServices<IPipelineBehavior<DelegateRequest, DelegateResponse>>();

    // Build the pipeline from innermost (delegate invocation) to outermost (first behavior)
    MessageHandlerDelegate<DelegateRequest, DelegateResponse> handler = InvokeDelegateAsync;

    // Chain behaviors in reverse order so first registered behavior is outermost
    foreach (IPipelineBehavior<DelegateRequest, DelegateResponse> behavior in behaviors.Reverse())
    {
      MessageHandlerDelegate<DelegateRequest, DelegateResponse> next = handler;
      handler = (msg, ct) => behavior.Handle(msg, next, ct);
    }

    // Execute the pipeline
    return handler(request, cancellationToken);
  }

  /// <summary>
  /// The innermost handler that actually invokes the delegate.
  /// </summary>
  private static async ValueTask<DelegateResponse> InvokeDelegateAsync(
    DelegateRequest request,
    CancellationToken cancellationToken)
  {
    _ = cancellationToken; // Currently unused but kept for API consistency with MessageHandlerDelegate
    object? result = await request.Invoker(request.BoundArguments).ConfigureAwait(false);
    return DelegateResponse.Success(result);
  }
}
