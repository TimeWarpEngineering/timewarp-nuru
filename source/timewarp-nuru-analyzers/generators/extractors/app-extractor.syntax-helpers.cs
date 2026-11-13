#region Purpose
// Syntax tree lookup helpers and user using-directive extraction (AppExtractor partial).
#endregion

namespace TimeWarp.Nuru.Generators;

using RoslynSyntaxNode = Microsoft.CodeAnalysis.SyntaxNode;

internal static partial class AppExtractor
{
  /// <summary>
  /// Finds the containing block (method body) for a syntax node.
  /// Walks up the syntax tree until a BlockSyntax is found.
  /// </summary>
  private static BlockSyntax? FindContainingBlock(RoslynSyntaxNode node)
  {
    RoslynSyntaxNode? current = node.Parent;
    while (current is not null)
    {
      if (current is BlockSyntax block)
      {
        // Only return blocks that are method bodies, not nested blocks (try, using, if, etc.)
        if (block.Parent is MethodDeclarationSyntax or
            LocalFunctionStatementSyntax or
            AccessorDeclarationSyntax or
            ConstructorDeclarationSyntax or
            DestructorDeclarationSyntax or
            OperatorDeclarationSyntax or
            ConversionOperatorDeclarationSyntax or
            AnonymousMethodExpressionSyntax or
            ParenthesizedLambdaExpressionSyntax or
            SimpleLambdaExpressionSyntax)
        {
          return block;
        }
        // Keep walking up for nested blocks
      }

      current = current.Parent;
    }

    return null;
  }

  /// <summary>
  /// True when <paramref name="node"/> is inside a top-level statement.
  /// </summary>
  private static bool IsInTopLevelStatement(RoslynSyntaxNode node) =>
    node.FirstAncestorOrSelf<GlobalStatementSyntax>() is not null;

  /// <summary>
  /// Finds the containing CompilationUnit for a syntax node.
  /// Used for top-level statement support where there is no BlockSyntax.
  /// </summary>
  private static CompilationUnitSyntax? FindCompilationUnit(RoslynSyntaxNode node)
  {
    RoslynSyntaxNode? current = node.Parent;
    while (current is not null)
    {
      if (current is CompilationUnitSyntax compilationUnit)
        return compilationUnit;

      current = current.Parent;
    }

    return null;
  }

  /// <summary>
  /// Extracts user's using directives from a CompilationUnit and converts them to global form.
  /// Filters out usings that are already included in the generated code's default set.
  /// </summary>
  /// <param name="compilationUnit">The compilation unit containing using directives.</param>
  /// <returns>User's using directives in global form.</returns>
  private static ImmutableArray<string> ExtractUserUsings(CompilationUnitSyntax compilationUnit)
  {
    ImmutableArray<string>.Builder usings = ImmutableArray.CreateBuilder<string>();

    foreach (UsingDirectiveSyntax usingDirective in compilationUnit.Usings)
    {
      // Skip alias usings (using Foo = Bar;)
      if (usingDirective.Alias is not null)
        continue;

      string? namespaceName = usingDirective.Name?.ToString();
      if (string.IsNullOrEmpty(namespaceName))
        continue;

      // Skip usings already in the default set
      if (DefaultUsings.Contains(namespaceName))
        continue;

      // Convert to global form for safety in generated code
      string globalUsing = usingDirective.StaticKeyword != default
        ? $"using static global::{namespaceName};"
        : $"using global::{namespaceName};";

      usings.Add(globalUsing);
    }

    return usings.ToImmutable();
  }
}
