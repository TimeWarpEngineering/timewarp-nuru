namespace TimeWarp.Nuru;

/// <summary>
/// Source generator that creates Command/Query classes and Handler classes from delegate signatures.
/// Supports AsCommand(), AsIdempotentCommand(), and AsQuery() in the fluent API.
/// Generates a sealed class with properties for each route parameter, and a nested Handler class
/// that wraps the delegate body with parameter rewriting.
/// </summary>
/// <remarks>
/// <para>
/// Message type determines the generated interface:
/// <list type="bullet">
///   <item><description>AsCommand() → ICommand&lt;T&gt; + ICommandHandler</description></item>
///   <item><description>AsIdempotentCommand() → ICommand&lt;T&gt;, IIdempotent + ICommandHandler</description></item>
///   <item><description>AsQuery() → IQuery&lt;T&gt; + IQueryHandler</description></item>
/// </list>
/// </para>
///
/// <para>Example input:</para>
/// <code>
/// app.Map("deploy {env} --force")
///     .WithHandler((string env, bool force, ILogger logger) => {
///         logger.LogInformation("Deploying to {Env}", env);
///     })
///     .AsCommand()
///     .Done();
/// </code>
///
/// <para>Example output:</para>
/// <code>
/// [GeneratedCode("TimeWarp.Nuru.Analyzers", "1.0.0")]
/// public sealed class Deploy_Generated_Command : ICommand&lt;Unit&gt;
/// {
///   public string Env { get; set; } = string.Empty;
///   public bool Force { get; set; }
///
///   public sealed class Handler : ICommandHandler&lt;Deploy_Generated_Command, Unit&gt;
///   {
///     private readonly ILogger Logger;
///
///     public Handler(ILogger logger)
///     {
///       Logger = logger;
///     }
///
///     public ValueTask&lt;Unit&gt; Handle(Deploy_Generated_Command request, CancellationToken cancellationToken)
///     {
///       Logger.LogInformation("Deploying to {Env}", request.Env);
///       return default;
///     }
///   }
/// }
/// </code>
/// </remarks>
[Generator]
public class NuruDelegateCommandGenerator : IIncrementalGenerator
{
  private const string SuppressAttributeName = "TimeWarp.Nuru.SuppressNuruCommandGenerationAttribute";

  public void Initialize(IncrementalGeneratorInitializationContext context)
  {
    // Step 1: Check for [assembly: SuppressNuruCommandGeneration] attribute
    IncrementalValueProvider<bool> hasSuppressAttribute = context.CompilationProvider
      .Select(static (compilation, _) =>
      {
        foreach (AttributeData attribute in compilation.Assembly.GetAttributes())
        {
          if (attribute.AttributeClass?.ToDisplayString() == SuppressAttributeName)
            return true;
        }

        return false;
      });

    // Step 2: Find all message type invocations (AsCommand, AsIdempotentCommand, AsQuery) and extract route info
    IncrementalValuesProvider<DelegateCommandInfo?> commandInfos = context.SyntaxProvider
      .CreateSyntaxProvider(
        predicate: static (node, _) => IsMessageTypeInvocation(node),
        transform: static (ctx, ct) => ExtractCommandInfo(ctx, ct))
      .Where(static info => info is not null);

    // Step 3: Collect all command infos
    IncrementalValueProvider<ImmutableArray<DelegateCommandInfo?>> collectedInfos = commandInfos.Collect();

    // Step 4: Combine with suppress flag
    IncrementalValueProvider<(ImmutableArray<DelegateCommandInfo?> Commands, bool Suppress)> combined =
      collectedInfos.Combine(hasSuppressAttribute);

    // Step 5: Generate source code
    context.RegisterSourceOutput(combined, static (ctx, data) =>
    {
      if (data.Suppress)
        return;

      ImmutableArray<DelegateCommandInfo?> commands = data.Commands;

      if (commands.IsDefaultOrEmpty)
        return;

      // Filter nulls and get unique commands by class name
      HashSet<string> seenNames = [];
      List<DelegateCommandInfo> uniqueCommands = [];

      foreach (DelegateCommandInfo? cmd in commands)
      {
        if (cmd is null)
          continue;

        if (seenNames.Add(cmd.ClassName))
        {
          uniqueCommands.Add(cmd);
        }
      }

      if (uniqueCommands.Count == 0)
        return;

      string source = GenerateCommandClasses(uniqueCommands);
      ctx.AddSource("GeneratedDelegateCommands.g.cs", source);
    });
  }

  /// <summary>
  /// Detects message type invocations in the fluent API pattern.
  /// Matches AsCommand(), AsIdempotentCommand(), and AsQuery().
  /// </summary>
  private static bool IsMessageTypeInvocation(Microsoft.CodeAnalysis.SyntaxNode node)
  {
    if (node is not InvocationExpressionSyntax invocation)
      return false;

    if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess)
      return false;

    return memberAccess.Name.Identifier.Text is "AsCommand" or "AsIdempotentCommand" or "AsQuery";
  }

  /// <summary>
  /// Extracts command info from an AsCommand() invocation by walking back the fluent chain.
  /// </summary>
  private static DelegateCommandInfo? ExtractCommandInfo(
    GeneratorSyntaxContext context,
    CancellationToken cancellationToken)
  {
    if (context.Node is not InvocationExpressionSyntax asCommandInvocation)
      return null;

    // Walk back the fluent chain to find WithHandler and Map
    FluentChainInfo? chainInfo = WalkBackFluentChain(asCommandInvocation);
    if (chainInfo is null)
    {
      // Debug: chain info not found
      return null;
    }

    // Extract delegate signature from the handler
    DelegateSignature? signature = ExtractSignatureFromHandler(
      chainInfo.HandlerExpression,
      context.SemanticModel,
      cancellationToken);

    if (signature is null)
      return null;

    // Parse the route pattern to identify route parameters
    RouteParameterInfo routeParams = ParseRoutePattern(chainInfo.Pattern);

    // Classify all parameters as route params or DI params
    List<ParameterClassification> allParams = [];
    List<CommandPropertyInfo> properties = [];

    foreach (DelegateParameterInfo param in signature.Parameters)
    {
      // Check if this parameter is a route parameter
      RouteParamMatch? match = FindRouteParameter(param.Name, routeParams);
      bool isRouteParam = match is not null;

      allParams.Add(new ParameterClassification(
        Name: param.Name,
        TypeFullName: param.Type.FullName,
        IsRouteParam: isRouteParam,
        IsDiParam: !isRouteParam));

      if (isRouteParam)
      {
        properties.Add(new CommandPropertyInfo(
          Name: ToPascalCase(param.Name),
          TypeName: param.Type.FullName,
          IsNullable: param.IsNullable,
          IsArray: param.IsArray,
          DefaultValue: GetDefaultValue(param)));
      }
    }

    // Generate class name from pattern (suffix varies by message type)
    string className = GenerateClassName(chainInfo.Pattern, chainInfo.MessageType);

    // Determine return type for ICommand<T> or IQuery<T>
    string commandReturnType = GetCommandReturnType(signature);

    // Extract handler info if the handler is a lambda expression
    HandlerInfo? handlerInfo = ExtractHandlerInfo(
      chainInfo.HandlerExpression,
      allParams,
      signature,
      context.SemanticModel,
      cancellationToken);

    return new DelegateCommandInfo(
      ClassName: className,
      Properties: [.. properties],
      ReturnType: commandReturnType,
      MessageType: chainInfo.MessageType,
      Handler: handlerInfo);
  }

  /// <summary>
  /// Extracts handler information from a lambda expression, including rewritten body.
  /// </summary>
  private static HandlerInfo? ExtractHandlerInfo(
    ExpressionSyntax handlerExpression,
    List<ParameterClassification> parameters,
    DelegateSignature signature,
    SemanticModel semanticModel,
    CancellationToken cancellationToken)
  {
    // Only support lambda expressions for now (method groups deferred)
    if (handlerExpression is not LambdaExpressionSyntax lambda)
      return null;

    // Check for closures (captured external variables)
    if (DetectClosures(lambda, parameters, semanticModel, out List<string> capturedVariables))
    {
      // For now, skip handler generation if closures detected
      // In future, we could report diagnostics here
      return null;
    }

    // Detect if lambda is async
    bool isAsync = lambda.Modifiers.Any(SyntaxKind.AsyncKeyword);

    // Get lambda body
    CSharpSyntaxNode? body = lambda.Body;
    if (body is null)
      return null;

    // Build parameter mappings for rewriting
    Dictionary<string, string> routeParamMappings = [];
    Dictionary<string, string> diParamMappings = [];

    foreach (ParameterClassification param in parameters)
    {
      if (param.IsRouteParam)
      {
        // env → request.Env
        routeParamMappings[param.Name] = $"request.{ToPascalCase(param.Name)}";
      }
      else if (param.IsDiParam)
      {
        // logger → Logger (field reference)
        diParamMappings[param.Name] = ToPascalCase(param.Name);
      }
    }

    // Rewrite the lambda body
    ParameterRewriter rewriter = new(routeParamMappings, diParamMappings);
    Microsoft.CodeAnalysis.SyntaxNode rewrittenBody = rewriter.Visit(body);

    // Convert body to string
    string bodyString = RewrittenBodyToString(rewrittenBody, signature.ReturnType, isAsync);

    return new HandlerInfo(
      Parameters: [.. parameters],
      LambdaBody: bodyString,
      IsAsync: isAsync,
      ReturnType: signature.ReturnType);
  }

  /// <summary>
  /// Detects if a lambda captures variables from enclosing scope (closures).
  /// </summary>
  private static bool DetectClosures(
    LambdaExpressionSyntax lambda,
    List<ParameterClassification> parameters,
    SemanticModel semanticModel,
    out List<string> capturedVariables)
  {
    capturedVariables = [];
    HashSet<string> allowedNames = [.. parameters.Select(p => p.Name)];

    // Track local variables declared inside the lambda
    HashSet<string> localVariables = [];
    foreach (VariableDeclaratorSyntax declarator in lambda.Body.DescendantNodes()
      .OfType<VariableDeclaratorSyntax>())
    {
      localVariables.Add(declarator.Identifier.Text);
    }

    // Walk lambda body looking for identifiers
    foreach (IdentifierNameSyntax identifier in lambda.Body.DescendantNodes()
      .OfType<IdentifierNameSyntax>())
    {
      string name = identifier.Identifier.Text;

      // Skip if it's a parameter
      if (allowedNames.Contains(name))
        continue;

      // Skip if it's a local variable declared inside lambda
      if (localVariables.Contains(name))
        continue;

      // Skip if it's part of a member access on the right side (obj.name - we care about 'obj', not 'name')
      if (identifier.Parent is MemberAccessExpressionSyntax memberAccess &&
          memberAccess.Name == identifier)
        continue;

      // Get symbol info
      SymbolInfo symbolInfo = semanticModel.GetSymbolInfo(identifier);
      ISymbol? symbol = symbolInfo.Symbol;

      if (symbol is null)
      {
        // Symbol resolution failed - be conservative and treat as closure
        // This can happen for variables in top-level statements or outer scopes
        // where the semantic model doesn't resolve correctly in source generators
        capturedVariables.Add(name);
        continue;
      }

      // Check what kind of symbol it is
      switch (symbol)
      {
        case ILocalSymbol:
          // Local variable from outer scope - closure!
          capturedVariables.Add(name);
          break;

        case IParameterSymbol param:
          // If not our lambda's parameter, it's from outer method - closure!
          if (!allowedNames.Contains(param.Name))
          {
            capturedVariables.Add(name);
          }

          break;

        case IFieldSymbol field when !field.IsStatic:
          // Instance field access (via implicit 'this') - closure!
          capturedVariables.Add($"this.{name}");
          break;

        case IPropertySymbol prop when !prop.IsStatic:
          // Instance property access (via implicit 'this') - closure!
          capturedVariables.Add($"this.{name}");
          break;

        // Static members are OK
        case IMethodSymbol:
        case IFieldSymbol { IsStatic: true }:
        case IPropertySymbol { IsStatic: true }:
        case INamedTypeSymbol:
        case INamespaceSymbol:
          break;
      }
    }

    return capturedVariables.Count > 0;
  }

  /// <summary>
  /// Converts the rewritten lambda body to a string suitable for the Handle method.
  /// </summary>
  private static string RewrittenBodyToString(
    Microsoft.CodeAnalysis.SyntaxNode rewrittenBody,
    DelegateTypeInfo returnType,
    bool isAsync)
  {
    string bodyText = rewrittenBody.ToFullString().Trim();

    // Handle block body vs expression body
    if (rewrittenBody is BlockSyntax)
    {
      // Block body - strip the outer braces, we'll add our own
      if (bodyText.StartsWith('{') && bodyText.EndsWith('}'))
      {
        bodyText = bodyText[1..^1].Trim();
      }

      return bodyText;
    }
    else
    {
      // Expression body - wrap appropriately
      if (returnType.IsVoid)
      {
        // void return - just execute the expression
        return $"{bodyText};";
      }
      else if (returnType.IsTask)
      {
        if (isAsync)
        {
          // async Task or Task<T> - await and return
          if (returnType.TaskResultType is null)
          {
            // async Task → await, then return Unit
            return $"await {bodyText};";
          }
          else
          {
            // async Task<T> → return await result
            return $"return await {bodyText};";
          }
        }
        else
        {
          // Non-async Task return (rare) - return the task wrapped
          return $"return new global::System.Threading.Tasks.ValueTask<{returnType.TaskResultType?.FullName ?? "global::Mediator.Unit"}>({bodyText});";
        }
      }
      else
      {
        // Sync return value - wrap in ValueTask
        return $"return new global::System.Threading.Tasks.ValueTask<{returnType.FullName}>({bodyText});";
      }
    }
  }

  /// <summary>
  /// Walks back the fluent chain from AsCommand() to find WithHandler() and Map().
  /// </summary>
  /// <remarks>
  /// Chain structure for: app.Map("pattern").WithHandler(handler).AsCommand().Done()
  ///
  /// InvocationExpression (.AsCommand())
  ///   └── Expression: MemberAccessExpression
  ///         ├── Name: "AsCommand"
  ///         └── Expression: InvocationExpression (.WithHandler(handler))
  ///               ├── Expression: MemberAccessExpression
  ///               │     ├── Name: "WithHandler"
  ///               │     └── Expression: InvocationExpression (.Map("pattern"))
  ///               │           ├── Expression: MemberAccessExpression
  ///               │           │     ├── Name: "Map"
  ///               │           │     └── Expression: IdentifierName "app"
  ///               │           └── ArgumentList: ("pattern")
  ///               └── ArgumentList: (handler)
  /// </remarks>
  private static FluentChainInfo? WalkBackFluentChain(InvocationExpressionSyntax messageTypeInvocation)
  {
    ExpressionSyntax? handlerExpression = null;
    string? pattern = null;

    // Extract message type from the invocation (AsCommand, AsIdempotentCommand, AsQuery)
    GeneratedMessageType messageType = GeneratedMessageType.Command;
    if (messageTypeInvocation.Expression is MemberAccessExpressionSyntax msgTypeAccess)
    {
      messageType = msgTypeAccess.Name.Identifier.Text switch
      {
        "AsQuery" => GeneratedMessageType.Query,
        "AsIdempotentCommand" => GeneratedMessageType.IdempotentCommand,
        _ => GeneratedMessageType.Command
      };
    }

    // Start from message type invocation and walk backwards through the chain
    ExpressionSyntax? current = messageTypeInvocation.Expression;

    while (current is MemberAccessExpressionSyntax memberAccess)
    {
      // memberAccess.Expression is what we're calling the method on (the previous invocation result)
      if (memberAccess.Expression is not InvocationExpressionSyntax previousInvocation)
        break;

      // Check what method was called in the previous invocation
      if (previousInvocation.Expression is MemberAccessExpressionSyntax prevMethodAccess)
      {
        string methodName = prevMethodAccess.Name.Identifier.Text;

        if (methodName == "WithHandler")
        {
          // Extract handler from WithHandler(handler)
          if (previousInvocation.ArgumentList.Arguments.Count > 0)
          {
            handlerExpression = previousInvocation.ArgumentList.Arguments[0].Expression;
          }
        }
        else if (methodName == "Map")
        {
          // Extract pattern from Map("pattern")
          if (previousInvocation.ArgumentList.Arguments.Count > 0)
          {
            ArgumentSyntax patternArg = previousInvocation.ArgumentList.Arguments[0];
            if (patternArg.Expression is LiteralExpressionSyntax literal &&
                literal.IsKind(SyntaxKind.StringLiteralExpression))
            {
              pattern = literal.Token.ValueText;
            }
          }
        }
      }

      // Move to the previous step in the chain
      current = previousInvocation.Expression;
    }

    if (handlerExpression is null || pattern is null)
      return null;

    return new FluentChainInfo(pattern, handlerExpression, messageType);
  }

  /// <summary>
  /// Parses a route pattern to extract parameter names.
  /// </summary>
  private static RouteParameterInfo ParseRoutePattern(string pattern)
  {
    List<string> positionalParams = [];
    Dictionary<string, string> optionParams = []; // option long form -> param name

    if (string.IsNullOrWhiteSpace(pattern))
      return new RouteParameterInfo(positionalParams, optionParams);

    // Simple parsing - look for {paramName} and --option patterns
    int i = 0;
    while (i < pattern.Length)
    {
      // Skip whitespace
      while (i < pattern.Length && char.IsWhiteSpace(pattern[i]))
        i++;

      if (i >= pattern.Length)
        break;

      // Check for option (--name or -n)
      if (i < pattern.Length - 1 && pattern[i] == '-' && pattern[i + 1] == '-')
      {
        // Long option: --force, --config {mode}
        i += 2; // skip --
        int optStart = i;
        while (i < pattern.Length && (char.IsLetterOrDigit(pattern[i]) || pattern[i] == '-'))
          i++;

        string optName = pattern[optStart..i];

        // Check for aliases (--force,-f)
        while (i < pattern.Length && pattern[i] == ',')
        {
          i++; // skip comma
          if (i < pattern.Length && pattern[i] == '-')
          {
            i++; // skip -
            while (i < pattern.Length && char.IsLetterOrDigit(pattern[i]))
              i++;
          }
        }

        // Check for optional marker (?) - skip it
        if (i < pattern.Length && pattern[i] == '?')
        {
          i++;
        }

        // Check for parameter value: {paramName}
        while (i < pattern.Length && char.IsWhiteSpace(pattern[i]))
          i++;

        if (i < pattern.Length && pattern[i] == '{')
        {
          i++; // skip {

          // Check for catch-all
          if (i < pattern.Length && pattern[i] == '*')
            i++;

          int paramStart = i;
          while (i < pattern.Length && pattern[i] != '}' && pattern[i] != ':' && pattern[i] != '?')
            i++;

          string paramName = pattern[paramStart..i];

          // Skip type constraint and optional marker
          while (i < pattern.Length && pattern[i] != '}')
            i++;

          if (i < pattern.Length)
            i++; // skip }

          optionParams[optName] = paramName;
        }
        else
        {
          // Flag option (no value) - the option name itself becomes a bool parameter
          optionParams[optName] = optName;
        }
      }
      else if (pattern[i] == '{')
      {
        // Positional parameter: {name}, {name:type}, {name?}
        i++; // skip {

        // Check for catch-all
        if (i < pattern.Length && pattern[i] == '*')
          i++;

        int paramStart = i;
        while (i < pattern.Length && pattern[i] != '}' && pattern[i] != ':' && pattern[i] != '?')
          i++;

        string paramName = pattern[paramStart..i];
        positionalParams.Add(paramName);

        // Skip to end of parameter
        while (i < pattern.Length && pattern[i] != '}')
          i++;

        if (i < pattern.Length)
          i++; // skip }
      }
      else if (pattern[i] == '-')
      {
        // Short option only: -v (less common, skip for now)
        i++;
        while (i < pattern.Length && char.IsLetterOrDigit(pattern[i]))
          i++;
      }
      else
      {
        // Literal - skip
        while (i < pattern.Length && !char.IsWhiteSpace(pattern[i]) && pattern[i] != '{')
          i++;
      }
    }

    return new RouteParameterInfo(positionalParams, optionParams);
  }

  /// <summary>
  /// Finds if a delegate parameter matches a route parameter.
  /// </summary>
  private static RouteParamMatch? FindRouteParameter(string delegateParamName, RouteParameterInfo routeParams)
  {
    // Check positional parameters (case-insensitive)
    foreach (string positional in routeParams.PositionalParams)
    {
      if (string.Equals(positional, delegateParamName, StringComparison.OrdinalIgnoreCase))
        return new RouteParamMatch(positional, IsOption: false);
    }

    // Check option parameters (case-insensitive)
    foreach (KeyValuePair<string, string> option in routeParams.OptionParams)
    {
      if (string.Equals(option.Value, delegateParamName, StringComparison.OrdinalIgnoreCase))
        return new RouteParamMatch(option.Value, IsOption: true);
    }

    return null;
  }

  /// <summary>
  /// Generates a class name from the route pattern and message type.
  /// </summary>
  private static string GenerateClassName(string pattern, GeneratedMessageType messageType)
  {
    string suffix = messageType == GeneratedMessageType.Query ? "_Generated_Query" : "_Generated_Command";

    if (string.IsNullOrWhiteSpace(pattern))
      return $"Default{suffix}";

    // Extract first literal(s) from pattern
    List<string> literals = [];
    int i = 0;

    while (i < pattern.Length)
    {
      // Skip whitespace
      while (i < pattern.Length && char.IsWhiteSpace(pattern[i]))
        i++;

      if (i >= pattern.Length)
        break;

      // Skip options and parameters
      if (pattern[i] == '-' || pattern[i] == '{')
        break;

      // Extract literal
      int start = i;
      while (i < pattern.Length && !char.IsWhiteSpace(pattern[i]) && pattern[i] != '{' && pattern[i] != '-')
        i++;

      if (i > start)
        literals.Add(pattern[start..i]);
    }

    if (literals.Count == 0)
      return $"Default{suffix}";

    // Convert to PascalCase and join
    string prefix = string.Concat(literals.Select(ToPascalCase));
    return $"{prefix}{suffix}";
  }

  /// <summary>
  /// Gets the return type for ICommand&lt;T&gt;.
  /// </summary>
  private static string GetCommandReturnType(DelegateSignature signature)
  {
    // void or Task → Unit
    if (signature.ReturnType.IsVoid)
      return "global::Mediator.Unit";

    if (signature.ReturnType.IsTask)
    {
      // Task → Unit
      if (signature.ReturnType.TaskResultType is null)
        return "global::Mediator.Unit";

      // Task<T> → T
      return signature.ReturnType.TaskResultType.FullName;
    }

    // T → T
    return signature.ReturnType.FullName;
  }

  /// <summary>
  /// Gets the default value expression for a property type.
  /// </summary>
  private static string? GetDefaultValue(DelegateParameterInfo param)
  {
    // Nullable types don't need explicit default
    if (param.IsNullable)
      return null;

    // Arrays need empty array
    if (param.IsArray)
      return "[]";

    // String needs string.Empty
    if (param.Type.FullName == "global::System.String" || param.Type.FullName == "string")
      return "string.Empty";

    // Value types have implicit defaults (int = 0, bool = false, etc.)
    return null;
  }

  private static string ToPascalCase(string input)
  {
    if (string.IsNullOrEmpty(input))
      return input;

    // Handle kebab-case: some-name → SomeName
    if (input.Contains('-', StringComparison.Ordinal))
    {
      return string.Concat(input.Split('-')
        .Where(s => s.Length > 0)
        .Select(s => char.ToUpperInvariant(s[0]) + s[1..]));
    }

    return char.ToUpperInvariant(input[0]) + input[1..];
  }

  /// <summary>
  /// Extracts delegate signature from a handler expression (reused from NuruInvokerGenerator).
  /// </summary>
  private static DelegateSignature? ExtractSignatureFromHandler(
    ExpressionSyntax handlerExpression,
    SemanticModel semanticModel,
    CancellationToken cancellationToken)
  {
    return handlerExpression switch
    {
      LambdaExpressionSyntax lambda => ExtractFromLambda(lambda, semanticModel, cancellationToken),
      IdentifierNameSyntax identifier => ExtractFromMethodGroup(identifier, semanticModel, cancellationToken),
      MemberAccessExpressionSyntax memberAccess => ExtractFromMethodGroup(memberAccess, semanticModel, cancellationToken),
      ObjectCreationExpressionSyntax creation => ExtractFromDelegateCreation(creation, semanticModel, cancellationToken),
      _ => ExtractFromExpression(handlerExpression, semanticModel, cancellationToken)
    };
  }

  private static DelegateSignature? ExtractFromLambda(
    LambdaExpressionSyntax lambda,
    SemanticModel semanticModel,
    CancellationToken cancellationToken)
  {
    ISymbol? symbol = semanticModel.GetSymbolInfo(lambda, cancellationToken).Symbol;
    if (symbol is IMethodSymbol methodSymbol)
      return CreateSignatureFromMethod(methodSymbol);

    Microsoft.CodeAnalysis.TypeInfo typeInfo = semanticModel.GetTypeInfo(lambda, cancellationToken);
    if (typeInfo.ConvertedType is INamedTypeSymbol delegateType &&
        delegateType.DelegateInvokeMethod is IMethodSymbol invokeMethod)
    {
      return CreateSignatureFromMethod(invokeMethod);
    }

    return null;
  }

  private static DelegateSignature? ExtractFromMethodGroup(
    ExpressionSyntax expression,
    SemanticModel semanticModel,
    CancellationToken cancellationToken)
  {
    SymbolInfo symbolInfo = semanticModel.GetSymbolInfo(expression, cancellationToken);

    if (symbolInfo.Symbol is IMethodSymbol methodSymbol)
      return CreateSignatureFromMethod(methodSymbol);

    if (symbolInfo.CandidateSymbols.Length > 0 &&
        symbolInfo.CandidateSymbols[0] is IMethodSymbol candidateMethod)
    {
      return CreateSignatureFromMethod(candidateMethod);
    }

    return null;
  }

  private static DelegateSignature? ExtractFromDelegateCreation(
    ObjectCreationExpressionSyntax creation,
    SemanticModel semanticModel,
    CancellationToken cancellationToken)
  {
    Microsoft.CodeAnalysis.TypeInfo typeInfo = semanticModel.GetTypeInfo(creation, cancellationToken);

    if (typeInfo.Type is INamedTypeSymbol delegateType &&
        delegateType.DelegateInvokeMethod is IMethodSymbol invokeMethod)
    {
      return CreateSignatureFromMethod(invokeMethod);
    }

    return null;
  }

  private static DelegateSignature? ExtractFromExpression(
    ExpressionSyntax expression,
    SemanticModel semanticModel,
    CancellationToken cancellationToken)
  {
    Microsoft.CodeAnalysis.TypeInfo typeInfo = semanticModel.GetTypeInfo(expression, cancellationToken);

    if (typeInfo.Type is INamedTypeSymbol namedType &&
        namedType.DelegateInvokeMethod is IMethodSymbol invokeMethod)
    {
      return CreateSignatureFromMethod(invokeMethod);
    }

    if (typeInfo.ConvertedType is INamedTypeSymbol convertedType &&
        convertedType.DelegateInvokeMethod is IMethodSymbol convertedInvokeMethod)
    {
      return CreateSignatureFromMethod(convertedInvokeMethod);
    }

    return null;
  }

  private static DelegateSignature CreateSignatureFromMethod(IMethodSymbol method)
  {
    ImmutableArray<DelegateParameterInfo>.Builder parameters =
      ImmutableArray.CreateBuilder<DelegateParameterInfo>(method.Parameters.Length);

    foreach (IParameterSymbol param in method.Parameters)
    {
      ITypeSymbol paramType = param.Type;
      bool isArray = paramType is IArrayTypeSymbol;
      bool isNullable = param.NullableAnnotation == NullableAnnotation.Annotated ||
                        IsNullableValueType(paramType);

      DelegateTypeInfo typeInfo = DelegateTypeInfo.FromSymbol(paramType);

      parameters.Add(new DelegateParameterInfo(
        param.Name,
        typeInfo,
        isArray,
        isNullable));
    }

    ImmutableArray<DelegateParameterInfo> paramArray = parameters.ToImmutable();
    DelegateTypeInfo returnType = DelegateTypeInfo.FromSymbol(method.ReturnType);
    bool isAsync = returnType.IsTask;
    string identifier = DelegateSignature.CreateIdentifier(paramArray, returnType);

    return new DelegateSignature(paramArray, returnType, isAsync, identifier);
  }

  private static bool IsNullableValueType(ITypeSymbol type) =>
    type.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T;

  /// <summary>
  /// Generates the source code for all Command classes.
  /// </summary>
  private static string GenerateCommandClasses(List<DelegateCommandInfo> commands)
  {
    System.Text.StringBuilder sb = new();

    sb.AppendLine("// <auto-generated/>");
    sb.AppendLine("// Generated by TimeWarp.Nuru.Analyzers - NuruDelegateCommandGenerator");
    sb.AppendLine("// DO NOT EDIT");
    sb.AppendLine();
    sb.AppendLine("#nullable enable");
    sb.AppendLine();
    sb.AppendLine("namespace TimeWarp.Nuru.Generated;");
    sb.AppendLine();

    foreach (DelegateCommandInfo command in commands)
    {
      GenerateCommandClass(sb, command);
      sb.AppendLine();
    }

    return sb.ToString();
  }

  private static void GenerateCommandClass(System.Text.StringBuilder sb, DelegateCommandInfo command)
  {
    // Determine the interface based on message type
    string interfaceType = command.MessageType switch
    {
      GeneratedMessageType.Query => $"global::Mediator.IQuery<{command.ReturnType}>",
      GeneratedMessageType.IdempotentCommand => $"global::Mediator.ICommand<{command.ReturnType}>, global::TimeWarp.Nuru.IIdempotent",
      _ => $"global::Mediator.ICommand<{command.ReturnType}>"
    };

    string classDescription = command.MessageType == GeneratedMessageType.Query
      ? "Generated Query class for delegate route."
      : "Generated Command class for delegate route.";

    sb.AppendLine("/// <summary>");
    sb.Append(CultureInfo.InvariantCulture, $"/// {classDescription}");
    sb.AppendLine();
    sb.AppendLine("/// </summary>");
    sb.AppendLine("[global::System.CodeDom.Compiler.GeneratedCode(\"TimeWarp.Nuru.Analyzers\", \"1.0.0\")]");
    sb.Append(CultureInfo.InvariantCulture, $"public sealed class {command.ClassName} : {interfaceType}");
    sb.AppendLine();
    sb.AppendLine("{");

    // Generate properties
    foreach (CommandPropertyInfo prop in command.Properties)
    {
      // Only add ? if nullable AND type doesn't already end with ?
      // (Nullable<T> types display as "T?" so we'd get "T??" otherwise)
      string nullableMarker = prop.IsNullable && !prop.TypeName.EndsWith('?') ? "?" : "";
      string defaultValue = prop.DefaultValue is not null ? $" = {prop.DefaultValue};" : "";

      sb.Append(CultureInfo.InvariantCulture, $"  public {prop.TypeName}{nullableMarker} {prop.Name} {{ get; set; }}{defaultValue}");
      sb.AppendLine();
    }

    // Generate nested Handler class if handler info is available
    if (command.Handler is not null)
    {
      sb.AppendLine();
      GenerateHandlerClass(sb, command);
    }

    sb.AppendLine("}");
  }

  /// <summary>
  /// Generates the nested Handler class inside the Command/Query class.
  /// </summary>
  private static void GenerateHandlerClass(System.Text.StringBuilder sb, DelegateCommandInfo command)
  {
    HandlerInfo handler = command.Handler!;

    // Get DI parameters (non-route params)
    ImmutableArray<ParameterClassification> diParams =
      [.. handler.Parameters.Where(p => p.IsDiParam)];

    // Determine handler interface based on message type
    string handlerInterface = command.MessageType == GeneratedMessageType.Query
      ? $"global::Mediator.IQueryHandler<{command.ClassName}, {command.ReturnType}>"
      : $"global::Mediator.ICommandHandler<{command.ClassName}, {command.ReturnType}>";

    string handlerDescription = command.MessageType == GeneratedMessageType.Query
      ? "Generated Handler for this query."
      : "Generated Handler for this command.";

    // Handler class declaration
    sb.AppendLine("  /// <summary>");
    sb.Append(CultureInfo.InvariantCulture, $"  /// {handlerDescription}");
    sb.AppendLine();
    sb.AppendLine("  /// </summary>");
    sb.Append(CultureInfo.InvariantCulture, $"  public sealed class Handler : {handlerInterface}");
    sb.AppendLine();
    sb.AppendLine("  {");

    // Generate DI fields (PascalCase per coding standard)
    foreach (ParameterClassification diParam in diParams)
    {
      string fieldName = ToPascalCase(diParam.Name);
      sb.Append(CultureInfo.InvariantCulture, $"    private readonly {diParam.TypeFullName} {fieldName};");
      sb.AppendLine();
    }

    // Generate constructor if there are DI params
    if (diParams.Length > 0)
    {
      sb.AppendLine();
      sb.AppendLine("    public Handler");
      sb.AppendLine("    (");

      for (int i = 0; i < diParams.Length; i++)
      {
        ParameterClassification p = diParams[i];
        string comma = i < diParams.Length - 1 ? "," : "";
        sb.Append(CultureInfo.InvariantCulture, $"      {p.TypeFullName} {p.Name}{comma}");
        sb.AppendLine();
      }

      sb.AppendLine("    )");
      sb.AppendLine("    {");

      foreach (ParameterClassification p in diParams)
      {
        sb.Append(CultureInfo.InvariantCulture, $"      {ToPascalCase(p.Name)} = {p.Name};");
        sb.AppendLine();
      }

      sb.AppendLine("    }");
    }

    // Generate Handle method
    sb.AppendLine();
    string asyncKeyword = handler.IsAsync ? "async " : "";
    sb.Append(CultureInfo.InvariantCulture, $"    public {asyncKeyword}global::System.Threading.Tasks.ValueTask<{command.ReturnType}> Handle");
    sb.AppendLine();
    sb.AppendLine("    (");
    sb.Append(CultureInfo.InvariantCulture, $"      {command.ClassName} request,");
    sb.AppendLine();
    sb.AppendLine("      global::System.Threading.CancellationToken cancellationToken");
    sb.AppendLine("    )");
    sb.AppendLine("    {");

    // Add the rewritten lambda body
    string[] bodyLines = handler.LambdaBody.Split('\n');
    foreach (string line in bodyLines)
    {
      string trimmedLine = line.TrimEnd('\r');
      if (!string.IsNullOrWhiteSpace(trimmedLine))
      {
        sb.Append(CultureInfo.InvariantCulture, $"      {trimmedLine.TrimStart()}");
        sb.AppendLine();
      }
    }

    // Add return statement for void returns if not already present
    if (command.ReturnType == "global::Mediator.Unit" && !handler.LambdaBody.Contains("return", StringComparison.Ordinal))
    {
      if (handler.IsAsync)
      {
        sb.AppendLine("      return global::Mediator.Unit.Value;");
      }
      else
      {
        sb.AppendLine("      return default;");
      }
    }

    sb.AppendLine("    }");
    sb.AppendLine("  }");
  }

  // Internal record types for data passing
  private sealed record FluentChainInfo(string Pattern, ExpressionSyntax HandlerExpression, GeneratedMessageType MessageType);

  /// <summary>
  /// Message type for generated commands/queries.
  /// </summary>
  private enum GeneratedMessageType
  {
    Command,
    IdempotentCommand,
    Query
  }

  private sealed record RouteParameterInfo(
    List<string> PositionalParams,
    Dictionary<string, string> OptionParams);

  private sealed record RouteParamMatch(string Name, bool IsOption);

  private sealed record DelegateCommandInfo(
    string ClassName,
    ImmutableArray<CommandPropertyInfo> Properties,
    string ReturnType,
    GeneratedMessageType MessageType,
    HandlerInfo? Handler);

  private sealed record CommandPropertyInfo(
    string Name,
    string TypeName,
    bool IsNullable,
    bool IsArray,
    string? DefaultValue);

  /// <summary>
  /// Classifies a delegate parameter as either a route parameter or DI parameter.
  /// </summary>
  private sealed record ParameterClassification(
    string Name,
    string TypeFullName,
    bool IsRouteParam,
    bool IsDiParam);

  /// <summary>
  /// Contains information needed to generate the Handler class.
  /// </summary>
  private sealed record HandlerInfo(
    ImmutableArray<ParameterClassification> Parameters,
    string LambdaBody,
    bool IsAsync,
    DelegateTypeInfo ReturnType);

  /// <summary>
  /// Rewrites parameter references in lambda body:
  /// - Route params: env → request.Env
  /// - DI params: logger → Logger
  /// </summary>
  private sealed class ParameterRewriter
  {
    private readonly Dictionary<string, string> RouteParamMappings;
    private readonly Dictionary<string, string> DiParamMappings;

    public ParameterRewriter(
      Dictionary<string, string> routeParamMappings,
      Dictionary<string, string> diParamMappings)
    {
      RouteParamMappings = routeParamMappings;
      DiParamMappings = diParamMappings;
    }

    /// <summary>
    /// Rewrites the given syntax node by replacing parameter references.
    /// </summary>
    public Microsoft.CodeAnalysis.SyntaxNode Visit(Microsoft.CodeAnalysis.SyntaxNode node)
    {
      // Collect all local variable names to avoid rewriting them
      HashSet<string> localVariables = [];

      foreach (VariableDeclaratorSyntax declarator in node.DescendantNodes().OfType<VariableDeclaratorSyntax>())
      {
        localVariables.Add(declarator.Identifier.Text);
      }

      foreach (ForEachStatementSyntax forEach in node.DescendantNodes().OfType<ForEachStatementSyntax>())
      {
        localVariables.Add(forEach.Identifier.Text);
      }

      foreach (CatchDeclarationSyntax catchDecl in node.DescendantNodes().OfType<CatchDeclarationSyntax>())
      {
        if (catchDecl.Identifier != default)
        {
          localVariables.Add(catchDecl.Identifier.Text);
        }
      }

      // Replace identifiers (use DescendantNodesAndSelf to handle expression-bodied lambdas
      // where the body itself is a single identifier like: (text) => text)
      return node.ReplaceNodes(
        node.DescendantNodesAndSelf().OfType<IdentifierNameSyntax>(),
        (original, _) => RewriteIdentifier(original, localVariables));
    }

    private Microsoft.CodeAnalysis.SyntaxNode RewriteIdentifier(IdentifierNameSyntax node, HashSet<string> localVariables)
    {
      string name = node.Identifier.Text;

      // Skip if it's a local variable declared in the lambda
      if (localVariables.Contains(name))
        return node;

      // Skip if it's on the right side of a member access (obj.name - don't rewrite 'name')
      if (node.Parent is MemberAccessExpressionSyntax memberAccess &&
          memberAccess.Name == node)
        return node;

      // Skip if it's part of a declaration (int name = ...)
      if (node.Parent is VariableDeclaratorSyntax)
        return node;

      // Skip if it's a type name in a declaration (List<T> name = ...)
      if (node.Parent is GenericNameSyntax ||
          node.Parent is QualifiedNameSyntax ||
          node.Parent is TypeArgumentListSyntax)
        return node;

      // Route param: env → request.Env
      if (RouteParamMappings.TryGetValue(name, out string? routeReplacement))
      {
        return SyntaxFactory.ParseExpression(routeReplacement)
          .WithTriviaFrom(node);
      }

      // DI param: logger → Logger (field access)
      if (DiParamMappings.TryGetValue(name, out string? diReplacement))
      {
        return SyntaxFactory.IdentifierName(diReplacement)
          .WithTriviaFrom(node);
      }

      return node;
    }
  }
}
