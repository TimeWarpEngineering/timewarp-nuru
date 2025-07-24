
namespace TimeWarp.Nuru;

/// <summary>
/// A simple CLI runner that matches routes and executes through Mediator.
/// No complex middleware, no convoluted abstractions - just route matching and Mediator.
/// </summary>
public class NuruCli
{
  private readonly IServiceProvider ServiceProvider;
  private readonly EndpointCollection Endpoints;
  private readonly RouteBasedCommandResolver Resolver;

  public NuruCli(IServiceProvider serviceProvider)
  {
    ServiceProvider = serviceProvider;
    Endpoints = serviceProvider.GetRequiredService<EndpointCollection>();

    ITypeConverterRegistry typeConverterRegistry = serviceProvider.GetRequiredService<ITypeConverterRegistry>();
    Resolver = new RouteBasedCommandResolver(Endpoints, typeConverterRegistry);
  }

  public async Task<int> RunAsync(string[] args)
  {
    ArgumentNullException.ThrowIfNull(args);

    try
    {
      // Parse and match route
      ResolverResult result = Resolver.Resolve(args);

      if (!result.Success || result.MatchedEndpoint is null)
      {
        await Console.Error.WriteLineAsync(result.ErrorMessage ?? "No matching command found.").ConfigureAwait(false);
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
#pragma warning disable CA1031 // Do not catch general exception types - This is intentional for CLI error handling
    catch (Exception ex)
#pragma warning restore CA1031 // Do not catch general exception types
    {
      await Console.Error.WriteLineAsync($"Error: {ex.Message}").ConfigureAwait(false);
      return 1;
    }
  }

  private static bool IsMediatorCommand(Type type)
  {
    return type.GetInterfaces().Any(i =>
        i.IsGenericType && i.GetGenericTypeDefinition() == typeof(TimeWarp.Mediator.IRequest<>) ||
        i == typeof(TimeWarp.Mediator.IRequest));
  }

  private async Task<int> ExecuteMediatorCommandAsync(Type commandType, ResolverResult result)
  {
    CommandExecutor commandExecutor = ServiceProvider.GetRequiredService<CommandExecutor>();

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
        ITypeConverterRegistry typeConverterRegistry = ServiceProvider.GetRequiredService<ITypeConverterRegistry>();
        object? returnValue = ParameterBinding.DelegateParameterBinder.InvokeWithParameters(
                    del,
                    result.ExtractedValues,
                    typeConverterRegistry,
                    ServiceProvider);

        // Handle async delegates
        if (returnValue is Task task)
        {
          await task.ConfigureAwait(false);
        }

        return 0;
      }
#pragma warning disable CA1031 // Do not catch general exception types - This is intentional for CLI error handling
      catch (Exception ex)
#pragma warning restore CA1031 // Do not catch general exception types
      {
        await Console.Error.WriteLineAsync($"Error executing delegate: {ex.Message}").ConfigureAwait(false);
        return 1;
      }
    }

    return 1;
  }

  private void ShowAvailableCommands()
  {
    Console.WriteLine("\nAvailable commands:");
    foreach (RouteEndpoint endpoint in Endpoints)
    {
      Console.WriteLine($"  {endpoint.RoutePattern}  {endpoint.Description ?? ""}");
    }
  }
}
