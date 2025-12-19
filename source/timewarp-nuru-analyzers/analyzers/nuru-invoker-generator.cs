namespace TimeWarp.Nuru;

/// <summary>
/// Source generator that creates typed invoker methods for route handlers.
/// Eliminates the need for DynamicInvoke and enables AOT compatibility.
/// </summary>
[Generator]
public class NuruInvokerGenerator : IIncrementalGenerator
{
  private const string SuppressAttributeName = "TimeWarp.Nuru.SuppressNuruInvokerGenerationAttribute";

  public void Initialize(IncrementalGeneratorInitializationContext context)
  {
    // Step 1: Check for [assembly: SuppressNuruInvokerGeneration] attribute
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

    // Step 2: Find all Map invocations with their delegate signatures
    IncrementalValuesProvider<RouteWithSignature?> routeSignatures = context.SyntaxProvider
      .CreateSyntaxProvider(
        predicate: static (node, _) => IsMapInvocation(node),
        transform: static (ctx, ct) => GetRouteWithSignature(ctx, ct))
      .Where(static info => info?.Signature is not null);

    // Step 3: Collect all unique signatures
    IncrementalValueProvider<ImmutableArray<RouteWithSignature?>> collectedSignatures =
      routeSignatures.Collect();

    // Step 4: Combine signatures with suppress flag
    IncrementalValueProvider<(ImmutableArray<RouteWithSignature?> Routes, bool Suppress)> combined =
      collectedSignatures.Combine(hasSuppressAttribute);

    // Step 5: Generate source code (unless suppressed)
    context.RegisterSourceOutput(combined, static (ctx, data) =>
    {
      // Skip generation if assembly has [SuppressNuruInvokerGeneration]
      if (data.Suppress)
        return;

      ImmutableArray<RouteWithSignature?> routes = data.Routes;

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

  /// <summary>
  /// Detects WithHandler() invocations in the new fluent API pattern:
  /// app.Map("pattern").WithHandler(handler).Done()
  /// </summary>
  private static bool IsMapInvocation(Microsoft.CodeAnalysis.SyntaxNode node)
  {
    if (node is not InvocationExpressionSyntax invocation)
      return false;

    if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess)
      return false;

    // Look for WithHandler() calls - this is where the delegate is specified
    return memberAccess.Name.Identifier.Text == "WithHandler";
  }

  /// <summary>
  /// Extracts route information from a WithHandler() invocation in the fluent API pattern:
  /// app.Map("pattern").WithHandler(handler).Done()
  /// </summary>
  private static RouteWithSignature? GetRouteWithSignature(
    GeneratorSyntaxContext context,
    CancellationToken cancellationToken)
  {
    if (context.Node is not InvocationExpressionSyntax withHandlerInvocation)
      return null;

    ArgumentListSyntax? argumentList = withHandlerInvocation.ArgumentList;
    if (argumentList is null || argumentList.Arguments.Count < 1)
      return null;

    // Extract the handler from WithHandler(handler) - it's the first argument
    ArgumentSyntax handlerArgument = argumentList.Arguments[0];

    // Walk back the fluent chain to find Map(pattern)
    string? pattern = FindPatternFromFluentChain(withHandlerInvocation);
    Location location = withHandlerInvocation.GetLocation();

    // If we couldn't find a pattern, use empty string (equivalent to default route)
    pattern ??= string.Empty;

    // Extract delegate signature from the handler argument
    DelegateSignature? signature = ExtractSignatureFromHandler(
      handlerArgument.Expression,
      context.SemanticModel,
      cancellationToken);

    return new RouteWithSignature(pattern, location, signature);
  }

  /// <summary>
  /// Walks back the fluent chain from WithHandler() to find the Map(pattern) call.
  /// Example chain: app.Map("deploy {env}").WithHandler(handler).AsCommand().Done()
  /// We start at WithHandler and walk back to find Map.
  /// </summary>
  private static string? FindPatternFromFluentChain(InvocationExpressionSyntax withHandlerInvocation)
  {
    // WithHandler is called on something like: app.Map("pattern")
    // The syntax tree looks like:
    // InvocationExpression (WithHandler)
    //   - MemberAccessExpression (.WithHandler)
    //     - InvocationExpression (Map("pattern"))
    //       - MemberAccessExpression (.Map)
    //       - ArgumentList ("pattern")

    if (withHandlerInvocation.Expression is not MemberAccessExpressionSyntax memberAccess)
      return null;

    // The expression being accessed is the result of Map("pattern")
    if (memberAccess.Expression is not InvocationExpressionSyntax mapInvocation)
      return null;

    // Verify this is a Map call
    if (mapInvocation.Expression is not MemberAccessExpressionSyntax mapMemberAccess)
      return null;

    if (mapMemberAccess.Name.Identifier.Text != "Map")
      return null;

    // Get the pattern from Map's arguments
    ArgumentListSyntax? mapArgs = mapInvocation.ArgumentList;
    if (mapArgs is null || mapArgs.Arguments.Count < 1)
      return null;

    // First argument should be the pattern
    ArgumentSyntax patternArg = mapArgs.Arguments[0];
    if (patternArg.Expression is LiteralExpressionSyntax literal &&
        literal.IsKind(SyntaxKind.StringLiteralExpression))
    {
      return literal.Token.ValueText;
    }

    return null;
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
    sb.AppendLine();

    // Generate the module initializer to register invokers
    GenerateModuleInitializer(sb);

    return sb.ToString();
  }

  private static void GenerateModuleInitializer(System.Text.StringBuilder sb)
  {
    sb.AppendLine("/// <summary>");
    sb.AppendLine("/// Module initializer that registers generated invokers with the InvokerRegistry.");
    sb.AppendLine("/// </summary>");
    sb.AppendLine("[global::System.CodeDom.Compiler.GeneratedCode(\"TimeWarp.Nuru.Analyzers\", \"1.0.0\")]");
    sb.AppendLine("internal static class GeneratedInvokerRegistration");
    sb.AppendLine("{");
    sb.AppendLine("  [global::System.Runtime.CompilerServices.ModuleInitializer]");
    sb.AppendLine("  internal static void Register()");
    sb.AppendLine("  {");
    sb.AppendLine("    global::TimeWarp.Nuru.InvokerRegistry.RegisterSyncBatch(GeneratedRouteInvokers.SyncInvokers);");
    sb.AppendLine("    global::TimeWarp.Nuru.InvokerRegistry.RegisterAsyncInvokerBatch(GeneratedRouteInvokers.AsyncInvokers);");
    sb.AppendLine("  }");
    sb.AppendLine("}");
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
