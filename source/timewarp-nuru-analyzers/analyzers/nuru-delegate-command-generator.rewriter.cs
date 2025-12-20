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
