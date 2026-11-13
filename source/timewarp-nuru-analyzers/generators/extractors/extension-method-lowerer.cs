#region Purpose
// Lower pure IServiceCollection AddX scripts (in-project syntax or decompiled referenced bodies) into ServiceDefinitions.
#endregion

#region Design
// Follow DeclaringSyntaxReferences when the method is in this compilation — no decompiler.
// Referenced assemblies go through ILSpy on the lib/ implementation. Fail closed: any statement
// that is not a lowerable collection call (or a same-assembly helper that is also only that)
// aborts the whole user-facing AddX. Never partial-lower. Type arguments from the call site
// substitute into generic AddX<T> bodies. TryAdd* consults the accumulated model so user
// lines and inlined library calls share one order.
#endregion

namespace TimeWarp.Nuru.Generators;

/// <summary>
/// Lowers opaque <c>AddX</c> extension methods into <see cref="ServiceDefinition"/> entries.
/// </summary>
internal static partial class ExtensionMethodLowerer
{
  /// <summary>
  /// Attempts to lower a user-facing <c>AddX</c> invocation.
  /// On failure the caller must report NURU052 and must not keep any partial services.
  /// </summary>
  internal static bool TryLower
  (
    InvocationExpressionSyntax invocation,
    SemanticModel semanticModel,
    IReadOnlyList<ServiceDefinition> alreadyRegistered,
    Location userFacingLocation,
    CancellationToken cancellationToken,
    out ImmutableArray<ServiceDefinition> services
  )
  {
    services = [];
    ArgumentNullException.ThrowIfNull(invocation);
    ArgumentNullException.ThrowIfNull(semanticModel);
    ArgumentNullException.ThrowIfNull(alreadyRegistered);

    IMethodSymbol? methodSymbol = GetMethodSymbol(invocation, semanticModel, cancellationToken);
    if (methodSymbol is null)
      return false;

    IMethodSymbol unreduced = methodSymbol.ReducedFrom ?? methodSymbol;
    if (!IsVoidOrServiceCollection(unreduced.ReturnType))
      return false;

    List<ServiceDefinition> accumulator = [.. alreadyRegistered];
    int start = accumulator.Count;
    HashSet<IMethodSymbol> visiting = new(SymbolEqualityComparer.Default);

    ImmutableDictionary<string, ITypeSymbol> emptyTypeArgs = [];
    if (!TryLowerMethod(
      unreduced,
      methodSymbol,
      semanticModel.Compilation,
      emptyTypeArgs,
      accumulator,
      visiting,
      userFacingLocation,
      cancellationToken))
    {
      return false;
    }

    services = [.. accumulator.Skip(start)];
    return true;
  }

  private static bool TryLowerMethod
  (
    IMethodSymbol unreduced,
    IMethodSymbol constructed,
    Compilation compilation,
    ImmutableDictionary<string, ITypeSymbol> parentTypeArgs,
    List<ServiceDefinition> accumulator,
    HashSet<IMethodSymbol> visiting,
    Location userFacingLocation,
    CancellationToken cancellationToken
  )
  {
    IMethodSymbol identity = unreduced.OriginalDefinition;
    if (!visiting.Add(identity))
      return false;

    try
    {
      ImmutableDictionary<string, ITypeSymbol> typeArgs = MapTypeArguments(identity, constructed, parentTypeArgs);

      SyntaxReference? syntaxReference = identity.DeclaringSyntaxReferences.FirstOrDefault();
      if (syntaxReference is not null)
      {
        return TryLowerFromSyntax(
          syntaxReference,
          identity,
          compilation,
          typeArgs,
          accumulator,
          visiting,
          userFacingLocation,
          cancellationToken);
      }

      return TryLowerFromDecompile(
        identity,
        compilation,
        typeArgs,
        accumulator,
        visiting,
        userFacingLocation,
        cancellationToken);
    }
    finally
    {
      visiting.Remove(identity);
    }
  }

  private static bool TryLowerFromSyntax
  (
    SyntaxReference syntaxReference,
    IMethodSymbol identity,
    Compilation compilation,
    ImmutableDictionary<string, ITypeSymbol> typeArgs,
    List<ServiceDefinition> accumulator,
    HashSet<IMethodSymbol> visiting,
    Location userFacingLocation,
    CancellationToken cancellationToken
  )
  {
    Microsoft.CodeAnalysis.SyntaxNode methodSyntax = syntaxReference.GetSyntax(cancellationToken);
    CSharpSyntaxNode? body = methodSyntax switch
    {
      MethodDeclarationSyntax methodDeclaration => (CSharpSyntaxNode?)methodDeclaration.Body ?? methodDeclaration.ExpressionBody?.Expression,
      LocalFunctionStatementSyntax localFunction => (CSharpSyntaxNode?)localFunction.Body ?? localFunction.ExpressionBody?.Expression,
      _ => null
    };

    if (body is null)
      return false;

    SemanticModel semanticModel = compilation.GetSemanticModel(methodSyntax.SyntaxTree);
    string? collectionParameterName = GetCollectionParameterName(identity);
    return TryLowerBody(
      body,
      semanticModel,
      compilation,
      identity,
      typeArgs,
      collectionParameterName,
      accumulator,
      visiting,
      userFacingLocation,
      cancellationToken);
  }

  private static bool TryLowerFromDecompile
  (
    IMethodSymbol identity,
    Compilation compilation,
    ImmutableDictionary<string, ITypeSymbol> typeArgs,
    List<ServiceDefinition> accumulator,
    HashSet<IMethodSymbol> visiting,
    Location userFacingLocation,
    CancellationToken cancellationToken
  )
  {
    ReferencedMethodDecompiler.CachedDecompile decompiled = ReferencedMethodDecompiler.Decompile(identity, compilation);
    if (!decompiled.Success || string.IsNullOrWhiteSpace(decompiled.Source))
      return false;

    string containingNamespace = identity.ContainingNamespace.IsGlobalNamespace
      ? string.Empty
      : identity.ContainingNamespace.ToDisplayString();

    string wrapper = WrapDecompiledMethod(decompiled.Source, containingNamespace);
    SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(wrapper, cancellationToken: cancellationToken);
    CSharpCompilation dummyCompilation = CSharpCompilation.Create(
      assemblyName: "NuruDecompiledAddX",
      syntaxTrees: [syntaxTree],
      references: compilation.References,
      options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

    SemanticModel dummyModel = dummyCompilation.GetSemanticModel(syntaxTree);
    MethodDeclarationSyntax? methodDeclaration = syntaxTree.GetRoot(cancellationToken)
      .DescendantNodes()
      .OfType<MethodDeclarationSyntax>()
      .FirstOrDefault();

    if (methodDeclaration is null)
      return false;

    CSharpSyntaxNode? body = (CSharpSyntaxNode?)methodDeclaration.Body ?? methodDeclaration.ExpressionBody?.Expression;
    if (body is null)
      return false;

    string? collectionParameterName = methodDeclaration.ParameterList.Parameters
      .Select(static p => p.Identifier.Text)
      .FirstOrDefault();

    if (string.IsNullOrEmpty(collectionParameterName))
      collectionParameterName = GetCollectionParameterName(identity);

    return TryLowerBody(
      body,
      dummyModel,
      compilation,
      identity,
      typeArgs,
      collectionParameterName,
      accumulator,
      visiting,
      userFacingLocation,
      cancellationToken);
  }

  private static string WrapDecompiledMethod(string decompiledSource, string containingNamespace)
  {
    string usingNamespace = string.IsNullOrEmpty(containingNamespace)
      ? string.Empty
      : $"using {containingNamespace};\n";

    return
      "#nullable disable\n"
      + "using System;\n"
      + "using Microsoft.Extensions.DependencyInjection;\n"
      + "using Microsoft.Extensions.DependencyInjection.Extensions;\n"
      + usingNamespace
      + "static class __NuruDecompiledHolder\n"
      + "{\n"
      + decompiledSource + "\n"
      + "}";
  }

  private static bool TryLowerBody
  (
    CSharpSyntaxNode body,
    SemanticModel semanticModel,
    Compilation compilation,
    IMethodSymbol identity,
    ImmutableDictionary<string, ITypeSymbol> typeArgs,
    string? collectionParameterName,
    List<ServiceDefinition> accumulator,
    HashSet<IMethodSymbol> visiting,
    Location userFacingLocation,
    CancellationToken cancellationToken
  )
  {
    if (body is ExpressionSyntax expression)
      return TryLowerExpression(expression, semanticModel, compilation, identity, typeArgs, collectionParameterName, accumulator, visiting, userFacingLocation, cancellationToken);

    if (body is not BlockSyntax block)
      return false;

    foreach (StatementSyntax statement in block.Statements)
    {
      cancellationToken.ThrowIfCancellationRequested();

      switch (statement)
      {
        case EmptyStatementSyntax:
          continue;
        case ReturnStatementSyntax returnStatement:
          if (returnStatement.Expression is null)
            continue;
          if (!TryLowerExpression(returnStatement.Expression, semanticModel, compilation, identity, typeArgs, collectionParameterName, accumulator, visiting, userFacingLocation, cancellationToken))
            return false;
          break;
        case ExpressionStatementSyntax expressionStatement:
          if (!TryLowerExpression(expressionStatement.Expression, semanticModel, compilation, identity, typeArgs, collectionParameterName, accumulator, visiting, userFacingLocation, cancellationToken))
            return false;
          break;
        default:
          return false;
      }
    }

    return true;
  }

  private static bool TryLowerExpression
  (
    ExpressionSyntax expression,
    SemanticModel semanticModel,
    Compilation compilation,
    IMethodSymbol identity,
    ImmutableDictionary<string, ITypeSymbol> typeArgs,
    string? collectionParameterName,
    List<ServiceDefinition> accumulator,
    HashSet<IMethodSymbol> visiting,
    Location userFacingLocation,
    CancellationToken cancellationToken
  )
  {
    if (expression is IdentifierNameSyntax identifier)
      return collectionParameterName is not null && identifier.Identifier.Text == collectionParameterName;

    if (expression is not InvocationExpressionSyntax)
      return false;

    List<InvocationExpressionSyntax> chain = FlattenChain(expression);
    if (chain.Count == 0)
      return false;

    if (!ReceiverIsCollectionParameter(chain[0], collectionParameterName))
      return false;

    foreach (InvocationExpressionSyntax invocation in chain)
    {
      if (!TryLowerInvocation(invocation, semanticModel, compilation, identity, typeArgs, collectionParameterName, accumulator, visiting, userFacingLocation, cancellationToken))
        return false;
    }

    return true;
  }

  private static bool TryLowerInvocation
  (
    InvocationExpressionSyntax invocation,
    SemanticModel semanticModel,
    Compilation compilation,
    IMethodSymbol enclosingMethod,
    ImmutableDictionary<string, ITypeSymbol> typeArgs,
    string? collectionParameterName,
    List<ServiceDefinition> accumulator,
    HashSet<IMethodSymbol> visiting,
    Location userFacingLocation,
    CancellationToken cancellationToken
  )
  {
    string? methodName = ServiceExtractor.GetInvocationMethodName(invocation);
    if (methodName is null)
      return false;

    if (ServiceRegistrationMethods.IsSpecialCased(methodName))
      return false;

    if (ServiceRegistrationMethods.TryGetLifetime(methodName, out ServiceLifetime lifetime, out bool isTryAdd))
    {
      return TryAddLifetimeRegistration(
        invocation,
        semanticModel,
        compilation,
        enclosingMethod,
        typeArgs,
        lifetime,
        isTryAdd,
        collectionParameterName,
        accumulator,
        userFacingLocation,
        cancellationToken);
    }

    IMethodSymbol? helper = GetMethodSymbol(invocation, semanticModel, cancellationToken);
    if (helper is null)
      return false;

    IMethodSymbol unreducedHelper = helper.ReducedFrom ?? helper;
    if (!SymbolEqualityComparer.Default.Equals(unreducedHelper.ContainingAssembly, enclosingMethod.ContainingAssembly))
      return false;

    if (!IsVoidOrServiceCollection(unreducedHelper.ReturnType))
      return false;

    return TryLowerMethod(
      unreducedHelper,
      helper,
      compilation,
      typeArgs,
      accumulator,
      visiting,
      userFacingLocation,
      cancellationToken);
  }
}
