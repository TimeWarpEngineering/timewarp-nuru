namespace TimeWarp.Nuru;

/// <summary>
/// Mediator and delegate execution methods for NuruCoreApp.
/// </summary>
public partial class NuruCoreApp
{
  [UnconditionalSuppressMessage("Trimming", "IL2026:RequiresUnreferencedCode",
      Justification = "Command type reflection is necessary for mediator pattern - users must preserve command types")]
  [UnconditionalSuppressMessage("AOT", "IL3050:RequiresDynamicCode",
      Justification = "Command instantiation through mediator pattern requires reflection")]
  private async Task<int> ExecuteMediatorCommandAsync(
      [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)]
      Type commandType,
      EndpointResolutionResult result)
  {
    if (MediatorExecutor is null)
    {
      throw new InvalidOperationException("MediatorExecutor is not available. Ensure DI is configured.");
    }

    object? returnValue = await MediatorExecutor.ExecuteCommandAsync(
      commandType,
      result.ExtractedValues!,
      CancellationToken.None
    ).ConfigureAwait(false);

    // Display the response (if any)
    MediatorExecutor.DisplayResponse(returnValue);

    // Handle int return values
    if (returnValue is int exitCode)
    {
      return exitCode;
    }

    return 0;
  }

  [UnconditionalSuppressMessage("Trimming", "IL2026:RequiresUnreferencedCode",
      Justification = "Delegate execution requires reflection - delegate types are preserved through registration")]
  [UnconditionalSuppressMessage("AOT", "IL3050:RequiresDynamicCode",
      Justification = "Delegate invocation may require dynamic code generation")]
  private Task<int> ExecuteDelegateAsync(Delegate del, Dictionary<string, string> extractedValues, Endpoint endpoint)
  {
    // When full DI is enabled (MediatorExecutor exists), route through Mediator
    if (MediatorExecutor is not null)
    {
      return ExecuteDelegateWithPipelineAsync(del, extractedValues, endpoint);
    }

    // Direct execution path (no DI)
    return DelegateExecutor.ExecuteAsync(
      del,
      extractedValues,
      TypeConverterRegistry,
      ServiceProvider ?? EmptyServiceProvider.Instance,
      endpoint,
      Terminal
    );
  }

  /// <summary>
  /// Executes a delegate through Mediator when DI is enabled, allowing pipeline behaviors to apply.
  /// </summary>
  [UnconditionalSuppressMessage("Trimming", "IL2026:RequiresUnreferencedCode",
      Justification = "Delegate execution requires reflection - delegate types are preserved through registration")]
  [UnconditionalSuppressMessage("AOT", "IL3050:RequiresDynamicCode",
      Justification = "Delegate invocation may require dynamic code generation")]
  private async Task<int> ExecuteDelegateWithPipelineAsync(
    Delegate del,
    Dictionary<string, string> extractedValues,
    Endpoint endpoint)
  {
    // Create a scope for this request to get fresh RouteExecutionContext
    using IServiceScope scope = ServiceProvider!.CreateScope();

    // Populate RouteExecutionContext for pipeline behaviors
    RouteExecutionContext? executionContext = scope.ServiceProvider.GetService<RouteExecutionContext>();
    if (executionContext is not null)
    {
      executionContext.RoutePattern = endpoint.RoutePattern;
      executionContext.StartedAt = DateTimeOffset.UtcNow;
      executionContext.Strategy = ExecutionStrategy.Delegate;
      executionContext.IsWrappedDelegate = true;
    }

    // Bind parameters first (same as direct execution)
    object?[] boundArgs = BindDelegateParameters(del, extractedValues, endpoint);

    // Create the delegate request
    DelegateRequest request = new()
    {
      RoutePattern = endpoint.RoutePattern,
      BoundArguments = boundArgs,
      Handler = del,
      Endpoint = endpoint
    };

    // Populate parameters in execution context
    if (executionContext is not null)
    {
      executionContext.Parameters = extractedValues;
    }

    try
    {
      // Execute through Mediator - pipeline behaviors will be invoked automatically
      IMediator mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
      DelegateResponse response = await mediator.Send(request, CancellationToken.None).ConfigureAwait(false);

      // Display the response (if any)
      ResponseDisplay.Write(response.Result, Terminal);

      return response.ExitCode;
    }
#pragma warning disable CA1031 // Do not catch general exception types
    // We catch all exceptions here to provide consistent error handling for delegate execution.
    // The CLI should not crash due to handler exceptions.
    catch (Exception ex)
#pragma warning restore CA1031
    {
      await Terminal.WriteErrorLineAsync($"Error executing handler: {ex.Message}").ConfigureAwait(false);
      return 1;
    }
  }
}
