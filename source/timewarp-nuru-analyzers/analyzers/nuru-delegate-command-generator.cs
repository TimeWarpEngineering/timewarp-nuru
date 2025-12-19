namespace TimeWarp.Nuru;

/// <summary>
/// Source generator that creates Command classes from delegate signatures.
/// When using AsCommand() in the fluent API, generates a sealed Command class
/// with properties for each route parameter.
/// </summary>
/// <remarks>
/// Example input:
/// <code>
/// app.Map("deploy {env} --force")
///     .WithHandler((string env, bool force, ILogger logger) => { ... })
///     .AsCommand()
///     .Done();
/// </code>
///
/// Example output:
/// <code>
/// [GeneratedCode("TimeWarp.Nuru.Analyzers", "1.0.0")]
/// public sealed class Deploy_Generated_Command : ICommand&lt;Unit&gt;
/// {
///     public string Env { get; set; } = string.Empty;
///     public bool Force { get; set; }
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

    // Step 2: Find all AsCommand() invocations and extract route info
    IncrementalValuesProvider<DelegateCommandInfo?> commandInfos = context.SyntaxProvider
      .CreateSyntaxProvider(
        predicate: static (node, _) => IsAsCommandInvocation(node),
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
  /// Detects AsCommand() invocations in the fluent API pattern.
  /// </summary>
  private static bool IsAsCommandInvocation(Microsoft.CodeAnalysis.SyntaxNode node)
  {
    if (node is not InvocationExpressionSyntax invocation)
      return false;

    if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess)
      return false;

    return memberAccess.Name.Identifier.Text == "AsCommand";
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

    // Filter delegate parameters to only include route parameters (not DI parameters)
    List<CommandPropertyInfo> properties = [];
    foreach (DelegateParameterInfo param in signature.Parameters)
    {
      // Check if this parameter is a route parameter
      RouteParamMatch? match = FindRouteParameter(param.Name, routeParams);
      if (match is not null)
      {
        properties.Add(new CommandPropertyInfo(
          Name: ToPascalCase(param.Name),
          TypeName: param.Type.FullName,
          IsNullable: param.IsNullable,
          IsArray: param.IsArray,
          DefaultValue: GetDefaultValue(param)));
      }
      // If not a route parameter and it's an interface/class type, it's a DI parameter - skip
    }

    // Generate class name from pattern
    string className = GenerateClassName(chainInfo.Pattern);

    // Determine return type for ICommand<T>
    string commandReturnType = GetCommandReturnType(signature);

    return new DelegateCommandInfo(
      ClassName: className,
      Properties: [.. properties],
      ReturnType: commandReturnType);
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
  private static FluentChainInfo? WalkBackFluentChain(InvocationExpressionSyntax asCommandInvocation)
  {
    ExpressionSyntax? handlerExpression = null;
    string? pattern = null;

    // Start from AsCommand() and walk backwards through the chain
    ExpressionSyntax? current = asCommandInvocation.Expression;

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

    return new FluentChainInfo(pattern, handlerExpression);
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
  /// Generates a class name from the route pattern.
  /// </summary>
  private static string GenerateClassName(string pattern)
  {
    if (string.IsNullOrWhiteSpace(pattern))
      return "Default_Generated_Command";

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
      return "Default_Generated_Command";

    // Convert to PascalCase and join
    string prefix = string.Concat(literals.Select(ToPascalCase));
    return $"{prefix}_Generated_Command";
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
    sb.AppendLine("/// <summary>");
    sb.AppendLine("/// Generated Command class for delegate route.");

    sb.AppendLine("/// </summary>");
    sb.AppendLine("[global::System.CodeDom.Compiler.GeneratedCode(\"TimeWarp.Nuru.Analyzers\", \"1.0.0\")]");
    sb.Append(CultureInfo.InvariantCulture, $"public sealed class {command.ClassName} : global::Mediator.ICommand<{command.ReturnType}>");
    sb.AppendLine();
    sb.AppendLine("{");

    foreach (CommandPropertyInfo prop in command.Properties)
    {
      // Only add ? if nullable AND type doesn't already end with ?
      // (Nullable<T> types display as "T?" so we'd get "T??" otherwise)
      string nullableMarker = prop.IsNullable && !prop.TypeName.EndsWith('?') ? "?" : "";
      string defaultValue = prop.DefaultValue is not null ? $" = {prop.DefaultValue};" : "";

      sb.Append(CultureInfo.InvariantCulture, $"  public {prop.TypeName}{nullableMarker} {prop.Name} {{ get; set; }}{defaultValue}");
      sb.AppendLine();
    }

    sb.AppendLine("}");
  }

  // Internal record types for data passing
  private sealed record FluentChainInfo(string Pattern, ExpressionSyntax HandlerExpression);

  private sealed record RouteParameterInfo(
    List<string> PositionalParams,
    Dictionary<string, string> OptionParams);

  private sealed record RouteParamMatch(string Name, bool IsOption);

  private sealed record DelegateCommandInfo(
    string ClassName,
    ImmutableArray<CommandPropertyInfo> Properties,
    string ReturnType);

  private sealed record CommandPropertyInfo(
    string Name,
    string TypeName,
    bool IsNullable,
    bool IsArray,
    string? DefaultValue);
}
