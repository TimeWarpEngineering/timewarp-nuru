namespace TimeWarp.Nuru;

/// <summary>
/// Parameter rewriter for the delegate command generator.
/// </summary>
public partial class NuruDelegateCommandGenerator
{
  /// <summary>
  /// Rewrites parameter references in lambda body:
  /// - Route params: env → request.Env
  /// - DI params: logger → Logger
  /// - Static using members: WriteLine → global::System.Console.WriteLine
  /// </summary>
  private sealed class ParameterRewriter
  {
    private readonly Dictionary<string, string> RouteParamMappings;
    private readonly Dictionary<string, string> DiParamMappings;
    private readonly SemanticModel SemanticModel;

    public ParameterRewriter(
      Dictionary<string, string> routeParamMappings,
      Dictionary<string, string> diParamMappings,
      SemanticModel semanticModel)
    {
      RouteParamMappings = routeParamMappings;
      DiParamMappings = diParamMappings;
      SemanticModel = semanticModel;
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

      // Check if this identifier resolves to a static member from a static using
      // (e.g., WriteLine from 'using static System.Console;')
      SymbolInfo symbolInfo = SemanticModel.GetSymbolInfo(node);
      ISymbol? symbol = symbolInfo.Symbol;

      // Static method: WriteLine → global::System.Console.WriteLine
      if (symbol is IMethodSymbol { IsStatic: true } method && method.ContainingType is not null)
      {
        string fullyQualified = GetFullyQualifiedMemberAccess(method);
        return SyntaxFactory.ParseExpression(fullyQualified)
          .WithTriviaFrom(node);
      }

      // Static property: PI → global::System.Math.PI
      if (symbol is IPropertySymbol { IsStatic: true } prop && prop.ContainingType is not null)
      {
        string fullyQualified = GetFullyQualifiedMemberAccess(prop);
        return SyntaxFactory.ParseExpression(fullyQualified)
          .WithTriviaFrom(node);
      }

      // Static field: Empty → global::System.String.Empty
      if (symbol is IFieldSymbol { IsStatic: true } field && field.ContainingType is not null)
      {
        string fullyQualified = GetFullyQualifiedMemberAccess(field);
        return SyntaxFactory.ParseExpression(fullyQualified)
          .WithTriviaFrom(node);
      }

      return node;
    }

    /// <summary>
    /// Gets the fully qualified member access string for a static member.
    /// e.g., WriteLine → global::System.Console.WriteLine
    /// </summary>
    private static string GetFullyQualifiedMemberAccess(ISymbol symbol)
    {
      string containingType = symbol.ContainingType!.ToDisplayString(
        new SymbolDisplayFormat(
          globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Included,
          typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces));

      return $"{containingType}.{symbol.Name}";
    }
  }
}
