using Microsoft.Extensions.DependencyInjection;
using TimeWarp.Nuru.CommandResolver;
using TimeWarp.Nuru.Endpoints;

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
        
        var typeConverterRegistry = serviceProvider.GetRequiredService<ITypeConverterRegistry>();
        _resolver = new RouteBasedCommandResolver(_endpoints, typeConverterRegistry);
    }
    
    public async Task<int> RunAsync(string[] args)
    {
        try
        {
            // Parse and match route
            var result = _resolver.Resolve(args);
            
            if (!result.Success || result.MatchedEndpoint == null)
            {
                System.Console.Error.WriteLine(result.ErrorMessage ?? "No matching command found.");
                ShowAvailableCommands();
                return 1;
            }
            
            // Check if this is a Mediator command
            var commandType = result.MatchedEndpoint.CommandType;
            
            if (commandType != null && IsMediatrCommand(commandType))
            {
                // Execute through Mediator
                return await ExecuteMediatrCommand(commandType, result);
            }
            else
            {
                // Execute delegate directly
                return await ExecuteDelegate(result);
            }
        }
        catch (Exception ex)
        {
            System.Console.Error.WriteLine($"Error: {ex.Message}");
            return 1;
        }
    }
    
    private bool IsMediatrCommand(Type type)
    {
        return type.GetInterfaces().Any(i => 
            i.IsGenericType && i.GetGenericTypeDefinition() == typeof(TimeWarp.Mediator.IRequest<>) ||
            i == typeof(TimeWarp.Mediator.IRequest));
    }
    
    private async Task<int> ExecuteMediatrCommand(Type commandType, ResolverResult result)
    {
        var commandExecutor = _serviceProvider.GetRequiredService<CommandExecutor>();
        
        // Execute through Mediator
        var response = await commandExecutor.ExecuteCommandAsync(
            commandType, 
            result.ExtractedValues, 
            CancellationToken.None);
        
        // Display the result
        CommandExecutor.DisplayResponse(response);
        
        return 0;
    }
    
    private async Task<int> ExecuteDelegate(ResolverResult result)
    {
        if (result.MatchedEndpoint?.Handler is Delegate del)
        {
            try
            {
                var typeConverterRegistry = _serviceProvider.GetRequiredService<ITypeConverterRegistry>();
                var returnValue = ParameterBinding.DelegateParameterBinder.InvokeWithParameters(
                    del, 
                    result.ExtractedValues, 
                    typeConverterRegistry,
                    _serviceProvider);
                
                // Handle async delegates
                if (returnValue is Task task)
                {
                    await task;
                }
                
                return 0;
            }
            catch (Exception ex)
            {
                System.Console.Error.WriteLine($"Error executing delegate: {ex.Message}");
                return 1;
            }
        }
        
        return 1;
    }
    
    private void ShowAvailableCommands()
    {
        System.Console.WriteLine("\nAvailable commands:");
        foreach (var endpoint in _endpoints)
        {
            System.Console.WriteLine($"  {endpoint.RoutePattern}  {endpoint.Description ?? ""}");
        }
    }
}