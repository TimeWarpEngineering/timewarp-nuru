
namespace TimeWarp.Nuru;

/// <summary>
/// A simple CLI runner that matches routes and executes through Mediator.
/// No complex middleware, no convoluted abstractions - just route matching and Mediator.
/// </summary>
public class NuruCli
{
  private readonly IServiceProvider _serviceProvider;
  private readonly EndpointCollection _endpoints;
  private readonly RouteBasedCommandResolver _resolver;

  public NuruCli(IServiceProvider serviceProvider)
  {
    _serviceProvider = serviceProvider;
    _endpoints = serviceProvider.GetRequiredService<EndpointCollection>();

    ITypeConverterRegistry typeConverterRegistry = serviceProvider.GetRequiredService<ITypeConverterRegistry>();
    _resolver = new RouteBasedCommandResolver(_endpoints, typeConverterRegistry);
  }

  public async Task<int> RunAsync(string[] args)
  {
    try
    {
      // Parse and match route
      ResolverResult result = _resolver.Resolve(args);

      if (!result.Success || result.MatchedEndpoint is null)
      {
        await System.Console.Error.WriteLineAsync(result.ErrorMessage ?? "No matching command found.").ConfigureAwait(false);
        ShowAvailableCommands();
        return 1;
      }

      // Check if this is a Mediator command
      Type? commandType = result.MatchedEndpoint.CommandType;

      if (commandType is not null && IsMediatorCommand(commandType))
      {
        // Execute through Mediator
        return await ExecuteMediatorCommandAsync(commandType, result).ConfigureAwait(false);
      }
      else
      {
        // Execute delegate directly
        return await ExecuteDelegateAsync(result).ConfigureAwait(false);
      }
    }
    catch (Exception ex)
    {
      await System.Console.Error.WriteLineAsync($"Error: {ex.Message}").ConfigureAwait(false);
      return 1;
    }
  }

  private bool IsMediatorCommand(Type type)
  {
    return type.GetInterfaces().Any(i =>
        i.IsGenericType && i.GetGenericTypeDefinition() == typeof(TimeWarp.Mediator.IRequest<>) ||
        i == typeof(TimeWarp.Mediator.IRequest));
  }

  private async Task<int> ExecuteMediatorCommandAsync(Type commandType, ResolverResult result)
  {
    CommandExecutor commandExecutor = _serviceProvider.GetRequiredService<CommandExecutor>();

    // Execute through Mediator
    object? response = await commandExecutor.ExecuteCommandAsync(
            commandType,
            result.ExtractedValues,
            CancellationToken.None).ConfigureAwait(false);

    // Display the result
    CommandExecutor.DisplayResponse(response);

    return 0;
  }

  private async Task<int> ExecuteDelegateAsync(ResolverResult result)
  {
    if (result.MatchedEndpoint?.Handler is Delegate del)
    {
      try
      {
        ITypeConverterRegistry typeConverterRegistry = _serviceProvider.GetRequiredService<ITypeConverterRegistry>();
        object? returnValue = ParameterBinding.DelegateParameterBinder.InvokeWithParameters(
                    del,
                    result.ExtractedValues,
                    typeConverterRegistry,
                    _serviceProvider);

        // Handle async delegates
        if (returnValue is Task task)
        {
          await task.ConfigureAwait(false);
        }

        return 0;
      }
      catch (Exception ex)
      {
        await System.Console.Error.WriteLineAsync($"Error executing delegate: {ex.Message}").ConfigureAwait(false);
        return 1;
      }
    }

    return 1;
  }

  private void ShowAvailableCommands()
  {
    System.Console.WriteLine("\nAvailable commands:");
    foreach (RouteEndpoint endpoint in _endpoints)
    {
      System.Console.WriteLine($"  {endpoint.RoutePattern}  {endpoint.Description ?? ""}");
    }
  }
}
