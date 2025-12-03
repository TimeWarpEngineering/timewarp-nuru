namespace TimeWarp.Nuru;

/// <summary>
/// Source generator that creates typed invoker methods for route handlers.
/// Eliminates the need for DynamicInvoke and enables AOT compatibility.
/// </summary>
[Generator]
public class NuruInvokerGenerator : IIncrementalGenerator
{
  public void Initialize(IncrementalGeneratorInitializationContext context)
  {
    // Step 1: Find all Map invocations with their delegate signatures
    IncrementalValuesProvider<RouteWithSignature?> routeSignatures = context.SyntaxProvider
      .CreateSyntaxProvider(
        predicate: static (node, _) => IsMapInvocation(node),
        transform: static (ctx, ct) => GetRouteWithSignature(ctx, ct))
      .Where(static info => info?.Signature is not null);

    // Step 2: Collect all unique signatures
    IncrementalValueProvider<ImmutableArray<RouteWithSignature?>> collectedSignatures =
      routeSignatures.Collect();

    // Step 3: Generate source code
    context.RegisterSourceOutput(collectedSignatures, static (ctx, routes) =>
    {
      if (routes.IsDefaultOrEmpty)
        return;

      // Get unique signatures (by identifier)
      HashSet<string> seenIdentifiers = [];
      List<DelegateSignature> uniqueSignatures = [];

      foreach (RouteWithSignature? route in routes)
      {
        if (route?.Signature is null)
          continue;

        if (seenIdentifiers.Add(route.Signature.UniqueIdentifier))
        {
          uniqueSignatures.Add(route.Signature);
        }
      }

      if (uniqueSignatures.Count == 0)
        return;

      string source = GenerateInvokersClass(uniqueSignatures);
      ctx.AddSource("GeneratedRouteInvokers.g.cs", source);
    });
  }

  private static bool IsMapInvocation(Microsoft.CodeAnalysis.SyntaxNode node)
  {
    if (node is not InvocationExpressionSyntax invocation)
      return false;

    if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess)
      return false;

    return memberAccess.Name.Identifier.Text == "Map";
  }

  private static RouteWithSignature? GetRouteWithSignature(
    GeneratorSyntaxContext context,
    CancellationToken cancellationToken)
  {
    if (context.Node is not InvocationExpressionSyntax invocation)
      return null;

    ArgumentListSyntax? argumentList = invocation.ArgumentList;
    if (argumentList is null || argumentList.Arguments.Count < 2)
      return null;

    // Find the pattern argument (may be positional or named)
    ArgumentSyntax? patternArgument = FindPatternArgument(argumentList);
    if (patternArgument is null)
      return null;

    if (patternArgument.Expression is not LiteralExpressionSyntax literal ||
        !literal.IsKind(SyntaxKind.StringLiteralExpression))
      return null;

    string? pattern = literal.Token.ValueText;
    if (string.IsNullOrEmpty(pattern))
      return null;

    Location location = literal.GetLocation();

    // Extract delegate signature using the handler argument
    DelegateSignature? signature = ExtractSignatureFromMapCall(
      invocation,
      argumentList,
      context.SemanticModel,
      cancellationToken);

    return new RouteWithSignature(pattern, location, signature);
  }

  /// <summary>
  /// Finds the pattern argument, handling both positional and named arguments.
  /// </summary>
  private static ArgumentSyntax? FindPatternArgument(ArgumentListSyntax argumentList)
  {
    // Check for named argument "pattern"
    foreach (ArgumentSyntax arg in argumentList.Arguments)
    {
      if (arg.NameColon?.Name.Identifier.Text == "pattern")
        return arg;
    }

    // Fall back to first positional argument
    return argumentList.Arguments.Count > 0 ? argumentList.Arguments[0] : null;
  }

  /// <summary>
  /// Extracts the delegate signature from a Map() call, handling both positional and named arguments.
  /// </summary>
  private static DelegateSignature? ExtractSignatureFromMapCall(
    InvocationExpressionSyntax invocation,
    ArgumentListSyntax argumentList,
    SemanticModel semanticModel,
    CancellationToken cancellationToken)
  {
    // Find the handler argument (may be positional or named)
    ArgumentSyntax? handlerArgument = null;

    // Check for named argument "handler"
    foreach (ArgumentSyntax arg in argumentList.Arguments)
    {
      if (arg.NameColon?.Name.Identifier.Text == "handler")
      {
        handlerArgument = arg;
        break;
      }
    }

    // Fall back to second positional argument if no named argument found
    if (handlerArgument is null && argumentList.Arguments.Count >= 2)
    {
      // Only use positional if no named arguments are used for the first arg
      ArgumentSyntax firstArg = argumentList.Arguments[0];
      if (firstArg.NameColon is null)
      {
        handlerArgument = argumentList.Arguments[1];
      }
    }

    if (handlerArgument is null)
      return null;

    // Now extract the signature from the handler expression
    return ExtractSignatureFromHandler(
      handlerArgument.Expression,
      semanticModel,
      cancellationToken);
  }

  /// <summary>
  /// Extracts delegate signature from a handler expression.
  /// </summary>
  private static DelegateSignature? ExtractSignatureFromHandler(
    ExpressionSyntax handlerExpression,
    SemanticModel semanticModel,
    CancellationToken cancellationToken)
  {
    return handlerExpression switch
    {
      // Lambda expression: (int x, string y) => ...
      LambdaExpressionSyntax lambda => ExtractFromLambda(lambda, semanticModel, cancellationToken),

      // Method group: MyHandler (referencing a method)
      IdentifierNameSyntax identifier => ExtractFromMethodGroup(identifier, semanticModel, cancellationToken),

      // Qualified method group: SomeClass.MyHandler
      MemberAccessExpressionSyntax memberAccess => ExtractFromMethodGroup(memberAccess, semanticModel, cancellationToken),

      // Delegate creation: new Action<int>(...)
      ObjectCreationExpressionSyntax creation => ExtractFromDelegateCreation(creation, semanticModel, cancellationToken),

      // Anything else - try to get the delegate type from the symbol
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

    // Try to get the converted type for implicitly typed lambdas
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

  /// <summary>
  /// Creates a DelegateSignature from a method symbol.
  /// </summary>
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

  private static string GenerateInvokersClass(List<DelegateSignature> signatures)
  {
    System.Text.StringBuilder sb = new();

    sb.AppendLine("// <auto-generated/>");
    sb.AppendLine("// Generated by TimeWarp.Nuru.Analyzers - NuruInvokerGenerator");
    sb.AppendLine("// DO NOT EDIT");
    sb.AppendLine();
    sb.AppendLine("#nullable enable");
    sb.AppendLine();
    sb.AppendLine("namespace TimeWarp.Nuru.Generated;");
    sb.AppendLine();
    sb.AppendLine("/// <summary>");
    sb.AppendLine("/// Generated typed invoker methods for route handlers.");
    sb.AppendLine("/// These methods enable reflection-free delegate invocation for AOT compatibility.");
    sb.AppendLine("/// </summary>");
    sb.AppendLine("[global::System.CodeDom.Compiler.GeneratedCode(\"TimeWarp.Nuru.Analyzers\", \"1.0.0\")]");
    sb.AppendLine("internal static class GeneratedRouteInvokers");
    sb.AppendLine("{");

    foreach (DelegateSignature signature in signatures)
    {
      GenerateInvokerMethod(sb, signature);
      sb.AppendLine();
    }

    // Generate the lookup dictionary
    GenerateLookupDictionary(sb, signatures);

    sb.AppendLine("}");

    return sb.ToString();
  }

  private static void GenerateInvokerMethod(System.Text.StringBuilder sb, DelegateSignature signature)
  {
    string methodName = GetInvokerMethodName(signature);
    bool isAsync = signature.IsAsync;

    // Generate XML documentation
    sb.AppendLine("  /// <summary>");
    sb.Append(CultureInfo.InvariantCulture, $"  /// Invoker for signature: {signature.UniqueIdentifier}");
    sb.AppendLine();
    sb.AppendLine("  /// </summary>");

    // Generate method signature
    if (isAsync)
    {
      sb.Append(CultureInfo.InvariantCulture, $"  public static async global::System.Threading.Tasks.Task<object?> {methodName}(");
      sb.AppendLine();
      sb.AppendLine("    global::System.Delegate handler,");
      sb.AppendLine("    object?[] args)");
    }
    else
    {
      sb.Append(CultureInfo.InvariantCulture, $"  public static object? {methodName}(");
      sb.AppendLine();
      sb.AppendLine("    global::System.Delegate handler,");
      sb.AppendLine("    object?[] args)");
    }

    sb.AppendLine("  {");

    // Generate the cast and invocation
    string delegateType = GetDelegateTypeName(signature);
    string castExpression = string.Create(CultureInfo.InvariantCulture, $"(({delegateType})handler)");
    string argsExpression = GetArgumentsExpression(signature);

    if (signature.ReturnType.IsVoid)
    {
      // Action delegate - void return
      sb.Append(CultureInfo.InvariantCulture, $"    {castExpression}({argsExpression});");
      sb.AppendLine();
      sb.AppendLine("    return null;");
    }
    else if (isAsync)
    {
      // Async delegate - Task or Task<T>
      if (signature.ReturnType.TaskResultType is not null)
      {
        // Task<T> - return the result
        sb.Append(CultureInfo.InvariantCulture, $"    return await {castExpression}({argsExpression});");
        sb.AppendLine();
      }
      else
      {
        // Task - no result
        sb.Append(CultureInfo.InvariantCulture, $"    await {castExpression}({argsExpression});");
        sb.AppendLine();
        sb.AppendLine("    return null;");
      }
    }
    else
    {
      // Func delegate - returns a value
      sb.Append(CultureInfo.InvariantCulture, $"    return {castExpression}({argsExpression});");
      sb.AppendLine();
    }

    sb.AppendLine("  }");
  }

  private static void GenerateLookupDictionary(System.Text.StringBuilder sb, List<DelegateSignature> signatures)
  {
    sb.AppendLine("  /// <summary>");
    sb.AppendLine("  /// Lookup dictionary for invokers by signature identifier.");
    sb.AppendLine("  /// </summary>");
    sb.AppendLine("  public static global::System.Collections.Generic.IReadOnlyDictionary<string, global::System.Func<global::System.Delegate, object?[], object?>> SyncInvokers { get; } =");
    sb.AppendLine("    new global::System.Collections.Generic.Dictionary<string, global::System.Func<global::System.Delegate, object?[], object?>>");
    sb.AppendLine("    {");

    foreach (DelegateSignature signature in signatures.Where(s => !s.IsAsync))
    {
      string methodName = GetInvokerMethodName(signature);
      sb.Append(CultureInfo.InvariantCulture, $"      [\"{signature.UniqueIdentifier}\"] = {methodName},");
      sb.AppendLine();
    }

    sb.AppendLine("    };");
    sb.AppendLine();

    sb.AppendLine("  /// <summary>");
    sb.AppendLine("  /// Lookup dictionary for async invokers by signature identifier.");
    sb.AppendLine("  /// </summary>");
    sb.AppendLine("  public static global::System.Collections.Generic.IReadOnlyDictionary<string, global::System.Func<global::System.Delegate, object?[], global::System.Threading.Tasks.Task<object?>>> AsyncInvokers { get; } =");
    sb.AppendLine("    new global::System.Collections.Generic.Dictionary<string, global::System.Func<global::System.Delegate, object?[], global::System.Threading.Tasks.Task<object?>>>");
    sb.AppendLine("    {");

    foreach (DelegateSignature signature in signatures.Where(s => s.IsAsync))
    {
      string methodName = GetInvokerMethodName(signature);
      sb.Append(CultureInfo.InvariantCulture, $"      [\"{signature.UniqueIdentifier}\"] = {methodName},");
      sb.AppendLine();
    }

    sb.AppendLine("    };");
  }

  private static string GetInvokerMethodName(DelegateSignature signature)
  {
    string prefix = signature.IsAsync ? "InvokeAsync" : "Invoke";
    return string.Create(CultureInfo.InvariantCulture, $"{prefix}_{signature.UniqueIdentifier}");
  }

  private static string GetDelegateTypeName(DelegateSignature signature)
  {
    if (signature.Parameters.Length == 0)
    {
      // No parameters
      if (signature.ReturnType.IsVoid)
        return "global::System.Action";

      return string.Create(CultureInfo.InvariantCulture, $"global::System.Func<{signature.ReturnType.FullName}>");
    }

    // Build type parameter list
    System.Text.StringBuilder typeParams = new();
    foreach (DelegateParameterInfo param in signature.Parameters)
    {
      if (typeParams.Length > 0)
        typeParams.Append(", ");

      typeParams.Append(param.Type.FullName);
      // Append ? for nullable reference types only
      // Nullable value types (int?, etc.) already have ? in their FullName from ToDisplayString
      if (param.IsNullable && !param.Type.FullName.EndsWith('?'))
        typeParams.Append('?');
    }

    if (signature.ReturnType.IsVoid)
    {
      return string.Create(CultureInfo.InvariantCulture, $"global::System.Action<{typeParams}>");
    }

    return string.Create(CultureInfo.InvariantCulture, $"global::System.Func<{typeParams}, {signature.ReturnType.FullName}>");
  }

  private static string GetArgumentsExpression(DelegateSignature signature)
  {
    if (signature.Parameters.Length == 0)
      return string.Empty;

    System.Text.StringBuilder args = new();
    for (int i = 0; i < signature.Parameters.Length; i++)
    {
      if (args.Length > 0)
        args.Append(", ");

      DelegateParameterInfo param = signature.Parameters[i];

      // Cast args[i] to the correct type
      // For value types, we need to unbox; for reference types, we cast
      if (param.IsNullable)
      {
        // For nullable types, cast to the nullable type (add ? if not already present)
        string castType = param.Type.FullName.EndsWith('?')
          ? param.Type.FullName
          : param.Type.FullName + "?";
        args.Append(CultureInfo.InvariantCulture, $"({castType})args[{i}]");
      }
      else
      {
        args.Append(CultureInfo.InvariantCulture, $"({param.Type.FullName})args[{i}]!");
      }
    }

    return args.ToString();
  }
}
