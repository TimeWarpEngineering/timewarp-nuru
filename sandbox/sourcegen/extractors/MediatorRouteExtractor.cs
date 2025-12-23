// sandbox/sourcegen/extractors/MediatorRouteExtractor.cs
// Extracts route definitions from Map<TRequest>("pattern") fluent chains.
//
// Handles Source 4: Map<TRequest>("pattern").WithDescription("...").AsQuery()
//
// Agent: Amina
// Task: #242-step-3

namespace TimeWarp.Nuru.SourceGen.Extractors;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Immutable;

/// <summary>
/// Extracts route definitions from Map&lt;TRequest&gt;("pattern") fluent chains.
/// </summary>
public static class MediatorRouteExtractor
{
  /// <summary>
  /// Result of extracting from a mediator Map chain.
  /// </summary>
  public record MediatorRouteResult(
    string? Pattern,
    string? Description,
    string? MessageType,
    string? RequestTypeName,
    ImmutableArray<string> Aliases,
    ImmutableArray<Diagnostic> Diagnostics);

  /// <summary>
  /// Extracts route information from a Map&lt;T&gt;() fluent chain.
  /// </summary>
  /// <param name="mapInvocation">The Map&lt;T&gt;(...) invocation expression.</param>
  /// <returns>Extracted route information.</returns>
  public static MediatorRouteResult ExtractFromMediatorMapChain(InvocationExpressionSyntax mapInvocation)
  {
    string? pattern = null;
    string? description = null;
    string? messageType = null;
    string? requestTypeName = null;
    List<string> aliases = [];
    List<Diagnostic> diagnostics = [];

    // Extract pattern from Map<T>("...") argument
    pattern = ExtractPatternFromMap(mapInvocation);

    // Extract type argument from Map<T>
    requestTypeName = ExtractTypeArgument(mapInvocation);

    // Walk the fluent chain to find other calls
    SyntaxNode? current = mapInvocation.Parent;

    while (current is not null)
    {
      if (current is InvocationExpressionSyntax invocation)
      {
        string? methodName = GetMethodName(invocation);

        switch (methodName)
        {
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

    return new MediatorRouteResult(
      Pattern: pattern,
      Description: description,
      MessageType: messageType ?? "Unspecified",
      RequestTypeName: requestTypeName,
      Aliases: [.. aliases],
      Diagnostics: [.. diagnostics]);
  }

  /// <summary>
  /// Checks if a Map invocation is a generic Map&lt;T&gt; call.
  /// </summary>
  public static bool IsGenericMapCall(InvocationExpressionSyntax mapInvocation)
  {
    // Check for generic type arguments on the method
    return mapInvocation.Expression switch
    {
      MemberAccessExpressionSyntax memberAccess =>
        memberAccess.Name is GenericNameSyntax,
      GenericNameSyntax => true,
      _ => false
    };
  }

  /// <summary>
  /// Extracts the pattern string from Map&lt;T&gt;("pattern").
  /// </summary>
  private static string? ExtractPatternFromMap(InvocationExpressionSyntax mapInvocation)
  {
    ArgumentListSyntax? args = mapInvocation.ArgumentList;
    if (args is null || args.Arguments.Count == 0)
    {
      return null;
    }

    // For Map<T>("pattern"), pattern is the first argument
    ArgumentSyntax firstArg = args.Arguments[0];
    return ExtractStringLiteral(firstArg.Expression);
  }

  /// <summary>
  /// Extracts the type argument T from Map&lt;T&gt;().
  /// </summary>
  private static string? ExtractTypeArgument(InvocationExpressionSyntax mapInvocation)
  {
    GenericNameSyntax? genericName = mapInvocation.Expression switch
    {
      MemberAccessExpressionSyntax memberAccess when memberAccess.Name is GenericNameSyntax generic => generic,
      GenericNameSyntax generic => generic,
      _ => null
    };

    if (genericName is null || genericName.TypeArgumentList.Arguments.Count == 0)
    {
      return null;
    }

    TypeSyntax typeArg = genericName.TypeArgumentList.Arguments[0];
    return NormalizeTypeName(typeArg.ToString());
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
      MemberAccessExpressionSyntax memberAccess => memberAccess.Name switch
      {
        GenericNameSyntax generic => generic.Identifier.Text,
        IdentifierNameSyntax identifier => identifier.Identifier.Text,
        _ => null
      },
      IdentifierNameSyntax identifier => identifier.Identifier.Text,
      GenericNameSyntax generic => generic.Identifier.Text,
      _ => null
    };
  }

  /// <summary>
  /// Normalizes a type name to fully qualified form.
  /// </summary>
  private static string NormalizeTypeName(string typeName)
  {
    // If already qualified, return as-is
    if (typeName.StartsWith("global::", StringComparison.Ordinal))
    {
      return typeName;
    }

    // Handle common primitive types
    return typeName.ToLowerInvariant() switch
    {
      "int" => "global::System.Int32",
      "long" => "global::System.Int64",
      "short" => "global::System.Int16",
      "byte" => "global::System.Byte",
      "float" => "global::System.Single",
      "double" => "global::System.Double",
      "decimal" => "global::System.Decimal",
      "bool" => "global::System.Boolean",
      "char" => "global::System.Char",
      "string" => "global::System.String",
      "object" => "global::System.Object",
      _ => $"global::{typeName}"
    };
  }
}
