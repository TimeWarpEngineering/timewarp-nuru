#region Purpose
// App configuration dispatch handlers (help, REPL, MCP, serve, services) for the DSL interpreter.
#endregion

namespace TimeWarp.Nuru.Generators;

using RoslynSyntaxNode = Microsoft.CodeAnalysis.SyntaxNode;

public sealed partial class DslInterpreter
{
  /// <summary>
  /// Dispatches ConfigureHelp() call to IIrAppBuilder.
  /// Extracts options from the lambda when present.
  /// </summary>
  private object? DispatchConfigureHelp(InvocationExpressionSyntax invocation, object? receiver)
  {
    if (receiver is not IIrAppBuilder appBuilder)
    {
      throw new InvalidOperationException(
        $"ConfigureHelp() must be called on an app builder. Location: {invocation.GetLocation().GetLineSpan()}");
    }

    ArgumentListSyntax? args = invocation.ArgumentList;
    if (args?.Arguments.Count > 0)
    {
      HelpModel? customOptions = HelpOptionsExtractor.Extract(invocation, SemanticModel, CancellationToken);
      if (customOptions is not null)
      {
        return appBuilder.ConfigureHelp(customOptions);
      }
    }

    return appBuilder.ConfigureHelp(HelpModel.Default);
  }

  /// <summary>
  /// Dispatches AddRepl() call to IIrAppBuilder.
  /// Extracts options from lambda expression if present, otherwise uses defaults.
  /// </summary>
  private object? DispatchAddRepl(InvocationExpressionSyntax invocation, object? receiver)
  {
    if (receiver is not IIrAppBuilder appBuilder)
    {
      throw new InvalidOperationException(
        $"AddRepl() must be called on an app builder. Location: {invocation.GetLocation().GetLineSpan()}");
    }

    // Check if there's a lambda argument for options configuration
    ArgumentListSyntax? args = invocation.ArgumentList;
    if (args?.Arguments.Count > 0)
    {
      // Extract options from lambda
      ReplModel? customOptions = ReplOptionsExtractor.Extract(invocation, SemanticModel, CancellationToken);
      if (customOptions is not null)
      {
        return appBuilder.AddRepl(customOptions);
      }
    }

    // Use defaults if no lambda or extraction failed
    return appBuilder.AddRepl();
  }

  /// <summary>
  /// Dispatches AddConfiguration() call to IIrAppBuilder.
  /// </summary>
  private static object? DispatchAddConfiguration(object? receiver)
  {
    if (receiver is not IIrAppBuilder appBuilder)
    {
      throw new InvalidOperationException("AddConfiguration() must be called on an app builder.");
    }

    return appBuilder.AddConfiguration();
  }

  /// <summary>
  /// Dispatches AddCheckUpdatesRoute() call to IIrAppBuilder.
  /// </summary>
  private static object? DispatchAddCheckUpdatesRoute(object? receiver)
  {
    if (receiver is not IIrAppBuilder appBuilder)
    {
      throw new InvalidOperationException("AddCheckUpdatesRoute() must be called on an app builder.");
    }

    return appBuilder.AddCheckUpdatesRoute();
  }

  /// <summary>
  /// Dispatches AddMcpRoute() (TimeWarp.Nuru.McpHost) to IIrAppBuilder.
  /// Arguments are read as compile-time constants; the generated mcp route passes them to the host.
  /// </summary>
  private object? DispatchAddMcpRoute(InvocationExpressionSyntax invocation, object? receiver)
  {
    if (receiver is not IIrAppBuilder appBuilder)
    {
      throw new InvalidOperationException(
        $"AddMcpRoute() must be called on an app builder. Location: {invocation.GetLocation().GetLineSpan()}");
    }

    string? instructions = ExtractConstantArgument<string>(invocation, 0, "instructions", "AddMcpRoute");
    string? groupFilter = ExtractConstantArgument<string>(invocation, 1, "groupFilter", "AddMcpRoute");
    int contentFormat = ExtractConstantArgument<int?>(invocation, 2, "contentFormat", "AddMcpRoute") ?? 0;

    return appBuilder.AddMcpRoute(new McpRouteModel(instructions, groupFilter, contentFormat));
  }

  /// <summary>
  /// Dispatches AddServeRoute() (TimeWarp.Nuru.Daemon) to IIrAppBuilder.
  /// Arguments are read as compile-time constants; the generated serve route and forwarding client use them.
  /// </summary>
  private object? DispatchAddServeRoute(InvocationExpressionSyntax invocation, object? receiver)
  {
    if (receiver is not IIrAppBuilder appBuilder)
    {
      throw new InvalidOperationException(
        $"AddServeRoute() must be called on an app builder. Location: {invocation.GetLocation().GetLineSpan()}");
    }

    string? socketPath = ExtractConstantArgument<string>(invocation, 0, "socketPath", "AddServeRoute");
    int idleTimeoutSeconds = ExtractConstantArgument<int?>(invocation, 1, "idleTimeoutSeconds", "AddServeRoute") ?? DefaultServeIdleTimeoutSeconds;
    bool autoStart = ExtractConstantArgument<bool?>(invocation, 2, "autoStart", "AddServeRoute") ?? false;
    bool exclusiveCalls = ExtractConstantArgument<bool?>(invocation, 3, "exclusiveCalls", "AddServeRoute") ?? false;

    return appBuilder.AddServeRoute(new ServeRouteModel(socketPath, idleTimeoutSeconds, autoStart, exclusiveCalls));
  }

  /// <summary>
  /// Reads a compile-time constant argument by name or position. Absent arguments and
  /// <c>null</c> return default; any other non-constant expression is an error.
  /// </summary>
  private T? ExtractConstantArgument<T>(InvocationExpressionSyntax invocation, int index, string parameterName, string methodName)
  {
    ArgumentSyntax? argument = ResolveArgumentAt(invocation, index, parameterName);
    if (argument is null)
      return default;

    // A later positional slot may hold a named argument for another parameter.
    if (argument.NameColon is not null && argument.NameColon.Name.Identifier.Text != parameterName)
      return default;

    Optional<object?> constant = SemanticModel.GetConstantValue(argument.Expression, CancellationToken);
    if (!constant.HasValue)
    {
      throw new InvalidOperationException(
        $"{methodName}() argument '{parameterName}' must be a compile-time constant. Location: {argument.GetLocation().GetLineSpan()}");
    }

    return constant.Value switch
    {
      null => default,
      T value => value,
      int intValue when typeof(T) == typeof(int?) => (T)(object)intValue,
      _ => throw new InvalidOperationException(
        $"{methodName}() argument '{parameterName}' has an unexpected type. Location: {argument.GetLocation().GetLineSpan()}")
    };
  }

  /// <summary>
  /// Dispatches ConfigureServices() call to IIrAppBuilder.
  /// Extracts service registrations from the lambda and adds them to the IR model.
  /// Also captures the raw lambda body for runtime invocation when UseMicrosoftDependencyInjection is enabled.
  /// </summary>
  private object? DispatchConfigureServices(InvocationExpressionSyntax invocation, object? receiver)
  {
    if (receiver is not IIrAppBuilder appBuilder)
    {
      throw new InvalidOperationException(
        $"ConfigureServices() must be called on an app builder. Location: {invocation.GetLocation().GetLineSpan()}");
    }

    // Extract service registrations from the ConfigureServices lambda
    ServiceExtractionResult extraction = ServiceExtractor.Extract(
      invocation,
      SemanticModel,
      CancellationToken);

    // Add each service to the IR model
    foreach (ServiceDefinition service in extraction.Services)
    {
      appBuilder.AddService(service);
    }

    // Add extension method calls for validation (NURU052)
    foreach (ExtensionMethodCall extensionMethod in extraction.ExtensionMethods)
    {
      appBuilder.AddExtensionMethodCall(extensionMethod);
    }

    // Extract logging configuration from AddLogging() if present
    LoggingConfiguration? loggingConfig = ServiceExtractor.ExtractLoggingConfiguration(
      invocation,
      SemanticModel,
      CancellationToken);

    if (loggingConfig is not null)
    {
      appBuilder.SetLoggingConfiguration(loggingConfig);
    }

    // Extract HttpClient configurations from AddHttpClient() if present
    ImmutableArray<HttpClientConfiguration> httpClientConfigs = ServiceExtractor.ExtractHttpClientConfigurations(
      invocation,
      SemanticModel,
      CancellationToken);

    foreach (HttpClientConfiguration httpClientConfig in httpClientConfigs)
    {
      appBuilder.AddHttpClientConfiguration(httpClientConfig);
    }

    // Extract and store the lambda body for runtime invocation (used when UseMicrosoftDependencyInjection is enabled)
    string? lambdaBody = ExtractConfigureServicesLambdaBody(invocation);
    if (lambdaBody is not null)
    {
      appBuilder.SetConfigureServicesBody(lambdaBody);
    }

    return appBuilder;
  }

  /// <summary>
  /// Extracts the raw lambda body or method call from a ConfigureServices() invocation.
  /// Used to emit code that invokes the user's delegate at runtime.
  /// </summary>
  private string? ExtractConfigureServicesLambdaBody(InvocationExpressionSyntax invocation)
  {
    ArgumentListSyntax? args = invocation.ArgumentList;
    if (args is null || args.Arguments.Count == 0)
      return null;

    ExpressionSyntax configureExpression = args.Arguments[0].Expression;

    // Handle lambda expressions: services => { ... } or services => services.Add...
    if (configureExpression is LambdaExpressionSyntax lambda)
    {
      // Get the parameter name (usually "services" or "s")
      string? parameterName = lambda switch
      {
        SimpleLambdaExpressionSyntax simple => simple.Parameter.Identifier.Text,
        ParenthesizedLambdaExpressionSyntax paren when paren.ParameterList.Parameters.Count > 0
          => paren.ParameterList.Parameters[0].Identifier.Text,
        _ => null
      };

      if (parameterName is null)
        return null;

      // Get the lambda body
      CSharpSyntaxNode body = lambda.Body;

      // For block body: { services.AddLogging(); services.AddSingleton<...>(); }
      if (body is BlockSyntax block)
      {
        return block.ToFullString();
      }

      // For expression body: services.AddLogging().AddSingleton<...>()
      // Wrap in block for consistent emission
      if (body is ExpressionSyntax expr)
      {
        return $"{{ {expr.ToFullString()}; }}";
      }
    }

    // Handle anonymous method expressions: delegate(IServiceCollection s) { ... }
    // (Old C# 2.0 syntax, but still valid)
    if (configureExpression is AnonymousMethodExpressionSyntax anonymousMethod)
    {
      if (anonymousMethod.Block is not null)
      {
        return anonymousMethod.Block.ToFullString();
      }
    }

    // Handle method group references by finding the method definition and inlining the body.
    // This works for both local functions and static methods.
    if (configureExpression is IdentifierNameSyntax or MemberAccessExpressionSyntax)
    {
      SymbolInfo symbolInfo = SemanticModel.GetSymbolInfo(configureExpression);
      IMethodSymbol? methodSymbol = symbolInfo.Symbol as IMethodSymbol
        ?? symbolInfo.CandidateSymbols.OfType<IMethodSymbol>().FirstOrDefault();

      if (methodSymbol is not null)
      {
        // Find the method's syntax declaration and extract its body
        foreach (SyntaxReference syntaxRef in methodSymbol.DeclaringSyntaxReferences)
        {
          RoslynSyntaxNode syntax = syntaxRef.GetSyntax();

          // Handle local functions
          if (syntax is LocalFunctionStatementSyntax localFunction)
          {
            if (localFunction.Body is not null)
            {
              return localFunction.Body.ToFullString();
            }

            if (localFunction.ExpressionBody is not null)
            {
              return $"{{ {localFunction.ExpressionBody.Expression.ToFullString()}; }}";
            }
          }

          // Handle regular methods
          if (syntax is MethodDeclarationSyntax methodDecl)
          {
            if (methodDecl.Body is not null)
            {
              return methodDecl.Body.ToFullString();
            }

            if (methodDecl.ExpressionBody is not null)
            {
              return $"{{ {methodDecl.ExpressionBody.Expression.ToFullString()}; }}";
            }
          }
        }

        // Fallback: if we couldn't get the body, try to call the method
        // (only works for static methods on classes, not local functions)
        if (configureExpression is MemberAccessExpressionSyntax memberAccess)
        {
          string qualifiedName = memberAccess.ToFullString().Trim();
          return $"{{ {qualifiedName}(services); }}";
        }
      }
    }

    return null;
  }
}
