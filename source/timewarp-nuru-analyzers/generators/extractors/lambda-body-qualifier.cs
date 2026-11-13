#region Purpose
// Produces the source text of a delegate handler's body with every name that resolved through the
// caller's scope written fully qualified, so the body still compiles once the generator inlines it
// into TimeWarp.Nuru.Generated (task 553).
#endregion

#region Design
// The generator inlines a lambda body as a local function inside GeneratedInterceptor, which lives in
// TimeWarp.Nuru.Generated. The caller's namespace, usings inside its namespace block, and its
// enclosing class are not in scope there. Copying the body as syntax text therefore failed CS0103,
// CS0246, or CS1061 whenever the body named such a type or member.
//
// The semantic model decides what each name means at the original site:
// - a name that binds to a type or namespace becomes its global:: form;
// - an unqualified static member (enclosing class or `using static`) gets its containing type;
// - a reduced extension call `x.M(a)` becomes the static call `global::N.T.M(x, a)`.
// Locals, parameters, instance members, type parameters, and the right side of a member access
// stay as written. Text is never parsed for meaning; only the replacement name is built as text.
//
// Members the generated class cannot access (a private member of the enclosing class) still fail;
// qualifying cannot change accessibility.
#endregion

namespace TimeWarp.Nuru.Generators;

using RoslynSyntaxNode = Microsoft.CodeAnalysis.SyntaxNode;

/// <summary>
/// Rewrites an inlined handler body so every name bound in the caller's scope is fully qualified.
/// </summary>
internal static class LambdaBodyQualifier
{
  /// <summary>
  /// Returns the body's source text with caller-scoped names fully qualified.
  /// </summary>
  /// <param name="body">The lambda's expression or block body, from the original syntax tree.</param>
  /// <param name="semanticModel">The semantic model for the body's syntax tree.</param>
  /// <param name="cancellationToken">Cancellation token.</param>
  /// <returns>The rewritten body text, including the original trivia.</returns>
  public static string Qualify(RoslynSyntaxNode body, SemanticModel semanticModel, CancellationToken cancellationToken)
  {
    ArgumentNullException.ThrowIfNull(body);
    ArgumentNullException.ThrowIfNull(semanticModel);

    // The interpreter can follow a builder into another file, and a lowered or decompiled body
    // may come from a tree outside the compilation; bind against the body's own tree when it can.
    if (body.SyntaxTree != semanticModel.SyntaxTree)
    {
      if (!semanticModel.Compilation.ContainsSyntaxTree(body.SyntaxTree))
        return body.ToFullString();

      semanticModel = semanticModel.Compilation.GetSemanticModel(body.SyntaxTree);
    }

    Dictionary<RoslynSyntaxNode, Func<RoslynSyntaxNode, RoslynSyntaxNode?>> replacements = [];

    foreach (RoslynSyntaxNode node in body.DescendantNodesAndSelf())
    {
      cancellationToken.ThrowIfCancellationRequested();

      switch (node)
      {
        case SimpleNameSyntax name:
          string? prefix = GetQualifyingPrefix(name, semanticModel, cancellationToken, out bool replaceWhole);
          if (prefix is not null)
            replacements[name] = rewritten => QualifyName((SimpleNameSyntax)rewritten, prefix, replaceWhole);
          break;

        case InvocationExpressionSyntax invocation:
          string? extensionType = GetExtensionContainingType(invocation, semanticModel, cancellationToken);
          if (extensionType is not null)
            replacements[invocation] = rewritten => ToStaticExtensionCall((InvocationExpressionSyntax)rewritten, extensionType);
          break;
      }
    }

    if (replacements.Count == 0)
      return body.ToFullString();

    WrapQualifiedInterpolations(body, replacements);

    RoslynSyntaxNode result = body.ReplaceNodes(
      replacements.Keys,
      (original, rewritten) => replacements[original](rewritten) ?? rewritten);

    return result.ToFullString();
  }

  /// <summary>
  /// Parenthesizes each interpolation hole that gains a qualified name. Inside a hole a top-level
  /// colon starts the format string, so <c>{global::N.T.M()}</c> would not parse; the
  /// parenthesized <c>{(global::N.T.M())}</c> does.
  /// </summary>
  private static void WrapQualifiedInterpolations(RoslynSyntaxNode body, Dictionary<RoslynSyntaxNode, Func<RoslynSyntaxNode, RoslynSyntaxNode?>> replacements)
  {
    InterpolationSyntax[] holes =
    [
      .. body.DescendantNodes()
        .OfType<InterpolationSyntax>()
        .Where(hole => hole.Expression is not ParenthesizedExpressionSyntax
          && hole.Expression.DescendantNodesAndSelf().Any(replacements.ContainsKey))
    ];

    foreach (InterpolationSyntax hole in holes)
    {
      replacements[hole] = rewritten =>
      {
        InterpolationSyntax interpolation = (InterpolationSyntax)rewritten;
        return interpolation.WithExpression(
          SyntaxFactory.ParenthesizedExpression(interpolation.Expression.WithoutTrivia())
            .WithTriviaFrom(interpolation.Expression));
      };
    }
  }

  /// <summary>
  /// Gets the text that qualifies a simple name, or null when the name stays as written.
  /// </summary>
  /// <param name="name">The simple name.</param>
  /// <param name="semanticModel">The semantic model.</param>
  /// <param name="cancellationToken">Cancellation token.</param>
  /// <param name="replaceWhole">
  /// True when the returned text is the whole qualified name (a non-generic type or a namespace);
  /// false when it is a prefix to put before the name as written.
  /// </param>
  private static string? GetQualifyingPrefix(SimpleNameSyntax name, SemanticModel semanticModel, CancellationToken cancellationToken, out bool replaceWhole)
  {
    replaceWhole = false;

    if (!IsUnqualifiedPosition(name))
      return null;

    // `var` binds to the inferred type, which may have no name (an anonymous type); keep it.
    if (name is IdentifierNameSyntax { IsVar: true })
      return null;

    ISymbol? symbol = GetSymbol(name, semanticModel, cancellationToken);

    switch (symbol)
    {
      case INamespaceSymbol ns when !ns.IsGlobalNamespace:
        replaceWhole = true;
        return ns.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

      case INamedTypeSymbol type when type.TypeKind is not TypeKind.Error and not TypeKind.Dynamic:
        if (name is GenericNameSyntax)
          return GetContainerText(type);

        replaceWhole = true;
        return type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

      case IFieldSymbol or IPropertySymbol or IEventSymbol or IMethodSymbol
        when symbol.IsStatic && symbol.ContainingSymbol is INamedTypeSymbol containingType && IsMember(symbol):
        return containingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

      default:
        return null;
    }
  }

  /// <summary>
  /// Gets whether a simple name stands on its own, so it resolves through the enclosing scope.
  /// The right side of a member access or qualified name, an alias-qualified name, and argument or
  /// initializer labels resolve through their left side or target instead.
  /// </summary>
  private static bool IsUnqualifiedPosition(SimpleNameSyntax name) =>
    name.Parent switch
    {
      MemberAccessExpressionSyntax memberAccess => memberAccess.Expression == name,
      QualifiedNameSyntax qualified => qualified.Left == name,
      MemberBindingExpressionSyntax => false,
      AliasQualifiedNameSyntax => false,
      NameColonSyntax => false,
      NameEqualsSyntax => false,
      AssignmentExpressionSyntax { Parent: InitializerExpressionSyntax } assignment => assignment.Left != name,
      _ => true
    };

  /// <summary>
  /// Gets the bound symbol, or the single overload group's first candidate when binding was
  /// ambiguous only between overloads of one member.
  /// </summary>
  private static ISymbol? GetSymbol(RoslynSyntaxNode node, SemanticModel semanticModel, CancellationToken cancellationToken)
  {
    SymbolInfo info = semanticModel.GetSymbolInfo(node, cancellationToken);
    if (info.Symbol is not null)
      return info.Symbol;

    ImmutableArray<ISymbol> candidates = info.CandidateSymbols;
    if (candidates.IsEmpty)
      return null;

    ISymbol first = candidates[0];
    return candidates.All(c => c.Kind == first.Kind
        && c.IsStatic == first.IsStatic
        && SymbolEqualityComparer.Default.Equals(c.ContainingSymbol, first.ContainingSymbol))
      ? first
      : null;
  }

  /// <summary>
  /// Gets whether the symbol is a member of a type (not a local function or lambda).
  /// </summary>
  private static bool IsMember(ISymbol symbol) =>
    symbol is not IMethodSymbol method || method.MethodKind == MethodKind.Ordinary;

  /// <summary>
  /// Gets the fully qualified text of what contains a type: its containing type, else its namespace
  /// (the bare <c>global::</c> alias, which needs no dot after it, for the global namespace).
  /// </summary>
  private static string GetContainerText(INamedTypeSymbol type)
  {
    if (type.ContainingType is not null)
      return type.ContainingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

    return type.ContainingNamespace is null or { IsGlobalNamespace: true }
      ? "global::"
      : type.ContainingNamespace.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
  }

  /// <summary>
  /// Builds the qualified replacement for a simple name, keeping its trivia.
  /// </summary>
  private static TypeSyntax QualifyName(SimpleNameSyntax rewritten, string prefix, bool replaceWhole)
  {
    string separator = prefix.EndsWith("::", StringComparison.Ordinal) ? "" : ".";
    string text = replaceWhole
      ? prefix
      : prefix + separator + rewritten.WithoutTrivia().ToFullString();

    // ParseTypeName, not ParseName: an alias can name a tuple, array, or keyword type.
    return SyntaxFactory.ParseTypeName(text).WithTriviaFrom(rewritten);
  }

  /// <summary>
  /// Gets the fully qualified type that declares the reduced extension method an invocation calls,
  /// or null when the invocation is not a plain <c>receiver.Method(...)</c> extension call that the
  /// static form can express.
  /// </summary>
  private static string? GetExtensionContainingType(InvocationExpressionSyntax invocation, SemanticModel semanticModel, CancellationToken cancellationToken)
  {
    if (invocation.Expression is not MemberAccessExpressionSyntax { RawKind: (int)SyntaxKind.SimpleMemberAccessExpression } memberAccess)
      return null;

    if (GetSymbol(invocation, semanticModel, cancellationToken) is not IMethodSymbol { MethodKind: MethodKind.ReducedExtension } method)
      return null;

    IMethodSymbol? reducedFrom = method.ReducedFrom;
    if (reducedFrom?.Parameters.IsEmpty != false || reducedFrom.Parameters[0].RefKind != RefKind.None)
      return null;

    // Explicit type arguments on the reduced call omit the receiver's type parameter; the static
    // form would need all of them, so leave that rare shape as written.
    if (memberAccess.Name is GenericNameSyntax && reducedFrom.TypeParameters.Length != method.TypeParameters.Length)
      return null;

    return method.ContainingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
  }

  /// <summary>
  /// Turns <c>receiver.Method(args)</c> into <c>global::N.T.Method(receiver, args)</c>, keeping trivia.
  /// </summary>
  private static ExpressionSyntax? ToStaticExtensionCall(InvocationExpressionSyntax rewritten, string containingType)
  {
    if (rewritten.Expression is not MemberAccessExpressionSyntax memberAccess)
      return null;

    string receiver = memberAccess.Expression.WithoutTrivia().ToFullString();
    SeparatedSyntaxList<ArgumentSyntax> arguments = rewritten.ArgumentList.Arguments;
    string rest = arguments.Count == 0 ? "" : ", " + arguments.ToFullString();

    string text = $"{containingType}.{memberAccess.Name.WithoutTrivia().ToFullString()}({receiver}{rest})";

    return SyntaxFactory.ParseExpression(text).WithTriviaFrom(rewritten);
  }
}
