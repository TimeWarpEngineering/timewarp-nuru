// Extracts route information from fluent Map() chains.
//
// Handles Source 1: Map("pattern").WithHandler(delegate).WithDescription("...").AsQuery()

namespace TimeWarp.Nuru.SourceGen;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Immutable;

/// <summary>
/// Extracts route definition data from fluent Map() chains.
/// </summary>
internal static class FluentChainExtractor
{
  /// <summary>
  /// Result of extracting from a fluent chain.
  /// </summary>
  public record FluentChainResult(
    string? Pattern,
    string? Description,
    string? MessageType,
    ImmutableArray<string> Aliases,
    LambdaExpressionSyntax? HandlerLambda,
    ImmutableArray<Diagnostic> Diagnostics);

  /// <summary>
  /// Extracts route information from a Map() fluent chain.
  /// </summary>
  /// <param name="mapInvocation">The Map(...) invocation expression.</param>
  /// <returns>Extracted route information.</returns>
  public static FluentChainResult ExtractFromMapChain(InvocationExpressionSyntax mapInvocation)
  {
    string? pattern = null;
    string? description = null;
    string? messageType = null;
    List<string> aliases = [];
    LambdaExpressionSyntax? handlerLambda = null;
    List<Diagnostic> diagnostics = [];

    // Extract pattern from Map("...") argument
    pattern = ExtractPatternFromMap(mapInvocation);

    // Walk the fluent chain to find other calls
    SyntaxNode? current = mapInvocation.Parent;

    while (current is not null)
    {
      if (current is InvocationExpressionSyntax invocation)
      {
        string? methodName = GetMethodName(invocation);

        switch (methodName)
        {
          case "WithHandler":
            handlerLambda = ExtractHandlerLambda(invocation);
            break;

          case "WithDescription":
            description = ExtractStringArgument(invocation);
            break;

          case "WithAlias":
            string? alias = ExtractStringArgument(invocation);
            if (alias is not null)
            {
              aliases.Add(alias);
            }

            break;

          case "AsQuery":
            messageType = "Query";
            break;

          case "AsCommand":
            messageType = "Command";
            break;

          case "AsIdempotentCommand":
            messageType = "IdempotentCommand";
            break;

          case "Done":
            // End of chain
            break;
        }
      }

      // Move up the syntax tree
      current = current.Parent;
    }

    return new FluentChainResult(
      Pattern: pattern,
      Description: description,
      MessageType: messageType ?? "Unspecified",
      Aliases: [.. aliases],
      HandlerLambda: handlerLambda,
      Diagnostics: [.. diagnostics]);
  }

  /// <summary>
  /// Extracts the pattern string from Map("pattern") or Map&lt;T&gt;("pattern").
  /// </summary>
  private static string? ExtractPatternFromMap(InvocationExpressionSyntax mapInvocation)
  {
    ArgumentListSyntax? args = mapInvocation.ArgumentList;
    if (args is null || args.Arguments.Count == 0)
    {
      return null;
    }

    // For Map("pattern"), it's the first argument
    // For Map<T>("pattern"), it's also the first argument
    ArgumentSyntax firstArg = args.Arguments[0];

    return ExtractStringLiteral(firstArg.Expression);
  }

  /// <summary>
  /// Extracts the lambda expression from WithHandler(lambda).
  /// </summary>
  private static LambdaExpressionSyntax? ExtractHandlerLambda(InvocationExpressionSyntax invocation)
  {
    ArgumentListSyntax? args = invocation.ArgumentList;
    if (args is null || args.Arguments.Count == 0)
    {
      return null;
    }

    ExpressionSyntax expr = args.Arguments[0].Expression;

    return expr switch
    {
      ParenthesizedLambdaExpressionSyntax lambda => lambda,
      SimpleLambdaExpressionSyntax simpleLambda => simpleLambda,
      _ => null
    };
  }

  /// <summary>
  /// Extracts a string argument from a method invocation.
  /// </summary>
  private static string? ExtractStringArgument(InvocationExpressionSyntax invocation)
  {
    ArgumentListSyntax? args = invocation.ArgumentList;
    if (args is null || args.Arguments.Count == 0)
    {
      return null;
    }

    return ExtractStringLiteral(args.Arguments[0].Expression);
  }

  /// <summary>
  /// Extracts the string value from a string literal expression.
  /// </summary>
  private static string? ExtractStringLiteral(ExpressionSyntax expression)
  {
    return expression switch
    {
      LiteralExpressionSyntax literal when literal.IsKind(SyntaxKind.StringLiteralExpression)
        => literal.Token.ValueText,

      InterpolatedStringExpressionSyntax
        => null, // Can't handle interpolated strings at compile time

      _ => null
    };
  }

  /// <summary>
  /// Gets the method name from an invocation expression.
  /// </summary>
  private static string? GetMethodName(InvocationExpressionSyntax invocation)
  {
    return invocation.Expression switch
    {
      MemberAccessExpressionSyntax memberAccess => memberAccess.Name.Identifier.Text,
      IdentifierNameSyntax identifier => identifier.Identifier.Text,
      _ => null
    };
  }
}
