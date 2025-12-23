// sandbox/sourcegen/extractors/FluentRouteBuilderExtractor.cs
// Extracts segment definitions from fluent route builder lambda expressions.
//
// Handles Source 2: Map(r => r.WithLiteral(...).WithParameter(...).WithOption(...))
//
// Agent: Amina
// Task: #242-step-3

namespace TimeWarp.Nuru.SourceGen.Extractors;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Immutable;

/// <summary>
/// Extracts segment definitions from fluent route builder expressions.
/// </summary>
public static class FluentRouteBuilderExtractor
{
  /// <summary>
  /// Result of extracting from a fluent route builder lambda.
  /// </summary>
  public record FluentRouteBuilderResult(
    ImmutableArray<SegmentDefinition> Segments,
    ImmutableArray<Diagnostic> Diagnostics);

  /// <summary>
  /// Extracts segment definitions from a route builder lambda.
  /// </summary>
  /// <param name="routeBuilderLambda">Lambda like: r => r.WithLiteral("add").WithParameter("x").Done()</param>
  /// <returns>Extracted segment definitions.</returns>
  public static FluentRouteBuilderResult ExtractFromRouteBuilderLambda(LambdaExpressionSyntax routeBuilderLambda)
  {
    List<SegmentDefinition> segments = [];
    List<Diagnostic> diagnostics = [];
    int position = 0;

    // Get the body of the lambda - should be an invocation chain
    ExpressionSyntax? body = routeBuilderLambda switch
    {
      SimpleLambdaExpressionSyntax simple => simple.ExpressionBody,
      ParenthesizedLambdaExpressionSyntax paren => paren.ExpressionBody,
      _ => null
    };

    if (body is null)
    {
      return new FluentRouteBuilderResult([], []);
    }

    // Collect all invocations in the chain (in reverse order as we walk up)
    List<InvocationExpressionSyntax> invocations = [];
    CollectInvocationChain(body, invocations);

    // Process invocations in order (they were collected in reverse)
    invocations.Reverse();

    foreach (InvocationExpressionSyntax invocation in invocations)
    {
      string? methodName = GetMethodName(invocation);

      switch (methodName)
      {
        case "WithLiteral":
          SegmentDefinition? literal = ExtractLiteral(invocation, position);
          if (literal is not null)
          {
            segments.Add(literal);
            position++;
          }
          break;

        case "WithParameter":
          SegmentDefinition? param = ExtractParameter(invocation, position);
          if (param is not null)
          {
            segments.Add(param);
            position++;
          }
          break;

        case "WithOption":
          SegmentDefinition? option = ExtractOption(invocation, position);
          if (option is not null)
          {
            segments.Add(option);
            position++;
          }
          break;

        case "WithFlag":
          SegmentDefinition? flag = ExtractFlag(invocation, position);
          if (flag is not null)
          {
            segments.Add(flag);
            position++;
          }
          break;

        case "Done":
          // End of chain, nothing to extract
          break;
      }
    }

    return new FluentRouteBuilderResult([.. segments], [.. diagnostics]);
  }

  /// <summary>
  /// Recursively collects invocations from a fluent chain.
  /// </summary>
  private static void CollectInvocationChain(ExpressionSyntax expression, List<InvocationExpressionSyntax> invocations)
  {
    if (expression is InvocationExpressionSyntax invocation)
    {
      invocations.Add(invocation);

      // Check if this invocation is on another invocation (fluent chain)
      if (invocation.Expression is MemberAccessExpressionSyntax memberAccess)
      {
        CollectInvocationChain(memberAccess.Expression, invocations);
      }
    }
  }

  /// <summary>
  /// Extracts a literal segment from WithLiteral("value").
  /// </summary>
  private static LiteralDefinition? ExtractLiteral(InvocationExpressionSyntax invocation, int position)
  {
    string? value = GetFirstStringArgument(invocation);
    if (value is null)
    {
      return null;
    }

    return new LiteralDefinition(Position: position, Value: value);
  }

  /// <summary>
  /// Extracts a parameter segment from WithParameter("name") or WithParameter("name", "type").
  /// </summary>
  private static ParameterDefinition? ExtractParameter(InvocationExpressionSyntax invocation, int position)
  {
    ArgumentListSyntax? args = invocation.ArgumentList;
    if (args is null || args.Arguments.Count == 0)
    {
      return null;
    }

    string? name = GetStringLiteral(args.Arguments[0].Expression);
    if (name is null)
    {
      return null;
    }

    // Check for type constraint (second argument)
    string? typeConstraint = null;
    bool isOptional = false;
    bool isCatchAll = false;

    if (args.Arguments.Count >= 2)
    {
      typeConstraint = GetStringLiteral(args.Arguments[1].Expression);
    }

    // Check for named arguments
    foreach (ArgumentSyntax arg in args.Arguments)
    {
      if (arg.NameColon is not null)
      {
        string argName = arg.NameColon.Name.Identifier.Text;
        switch (argName)
        {
          case "type":
            typeConstraint = GetStringLiteral(arg.Expression);
            break;
          case "optional":
            isOptional = GetBoolLiteral(arg.Expression) ?? false;
            break;
          case "catchAll":
            isCatchAll = GetBoolLiteral(arg.Expression) ?? false;
            break;
        }
      }
    }

    string resolvedClrType = ResolveClrType(typeConstraint);

    return new ParameterDefinition(
      Position: position,
      Name: name,
      TypeConstraint: typeConstraint,
      Description: null,
      IsOptional: isOptional,
      IsCatchAll: isCatchAll,
      ResolvedClrTypeName: resolvedClrType,
      DefaultValue: null);
  }

  /// <summary>
  /// Extracts an option segment from WithOption("long", "short") or WithOption("long", "short", "type").
  /// </summary>
  private static OptionDefinition? ExtractOption(InvocationExpressionSyntax invocation, int position)
  {
    ArgumentListSyntax? args = invocation.ArgumentList;
    if (args is null || args.Arguments.Count == 0)
    {
      return null;
    }

    string? longForm = GetStringLiteral(args.Arguments[0].Expression);
    if (longForm is null)
    {
      return null;
    }

    string? shortForm = null;
    string? typeConstraint = null;
    string? parameterName = null;
    bool isOptional = true; // Options are optional by default
    bool isRepeated = false;

    // Second argument could be short form or type
    if (args.Arguments.Count >= 2)
    {
      string? secondArg = GetStringLiteral(args.Arguments[1].Expression);
      if (secondArg is not null && secondArg.Length == 1)
      {
        shortForm = secondArg;
      }
      else
      {
        typeConstraint = secondArg;
        parameterName = longForm; // Use long form as parameter name
      }
    }

    // Third argument is type if second was short form
    if (args.Arguments.Count >= 3 && shortForm is not null)
    {
      typeConstraint = GetStringLiteral(args.Arguments[2].Expression);
    }

    // Check for named arguments
    foreach (ArgumentSyntax arg in args.Arguments)
    {
      if (arg.NameColon is not null)
      {
        string argName = arg.NameColon.Name.Identifier.Text;
        switch (argName)
        {
          case "shortForm":
            shortForm = GetStringLiteral(arg.Expression);
            break;
          case "type":
            typeConstraint = GetStringLiteral(arg.Expression);
            break;
          case "parameterName":
            parameterName = GetStringLiteral(arg.Expression);
            break;
          case "optional":
            isOptional = GetBoolLiteral(arg.Expression) ?? true;
            break;
          case "repeated":
            isRepeated = GetBoolLiteral(arg.Expression) ?? false;
            break;
        }
      }
    }

    // If we have a type constraint, this option expects a value
    bool expectsValue = typeConstraint is not null;
    if (expectsValue && parameterName is null)
    {
      parameterName = longForm;
    }

    string resolvedClrType = expectsValue
      ? ResolveClrType(typeConstraint)
      : "global::System.Boolean";

    return new OptionDefinition(
      Position: position,
      LongForm: longForm,
      ShortForm: shortForm,
      ParameterName: parameterName,
      TypeConstraint: typeConstraint,
      Description: null,
      ExpectsValue: expectsValue,
      IsOptional: isOptional,
      IsRepeated: isRepeated,
      ParameterIsOptional: isOptional,
      ResolvedClrTypeName: resolvedClrType);
  }

  /// <summary>
  /// Extracts a flag segment from WithFlag("long", "short").
  /// </summary>
  private static OptionDefinition? ExtractFlag(InvocationExpressionSyntax invocation, int position)
  {
    ArgumentListSyntax? args = invocation.ArgumentList;
    if (args is null || args.Arguments.Count == 0)
    {
      return null;
    }

    string? longForm = GetStringLiteral(args.Arguments[0].Expression);
    if (longForm is null)
    {
      return null;
    }

    string? shortForm = null;
    if (args.Arguments.Count >= 2)
    {
      shortForm = GetStringLiteral(args.Arguments[1].Expression);
    }

    return new OptionDefinition(
      Position: position,
      LongForm: longForm,
      ShortForm: shortForm,
      ParameterName: longForm,
      TypeConstraint: null,
      Description: null,
      ExpectsValue: false,
      IsOptional: true,
      IsRepeated: false,
      ParameterIsOptional: true,
      ResolvedClrTypeName: "global::System.Boolean");
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

  /// <summary>
  /// Gets the first string argument from an invocation.
  /// </summary>
  private static string? GetFirstStringArgument(InvocationExpressionSyntax invocation)
  {
    ArgumentListSyntax? args = invocation.ArgumentList;
    if (args is null || args.Arguments.Count == 0)
    {
      return null;
    }

    return GetStringLiteral(args.Arguments[0].Expression);
  }

  /// <summary>
  /// Extracts a string literal value from an expression.
  /// </summary>
  private static string? GetStringLiteral(ExpressionSyntax expression)
  {
    return expression switch
    {
      LiteralExpressionSyntax literal when literal.IsKind(SyntaxKind.StringLiteralExpression)
        => literal.Token.ValueText,
      _ => null
    };
  }

  /// <summary>
  /// Extracts a boolean literal value from an expression.
  /// </summary>
  private static bool? GetBoolLiteral(ExpressionSyntax expression)
  {
    return expression switch
    {
      LiteralExpressionSyntax literal when literal.IsKind(SyntaxKind.TrueLiteralExpression) => true,
      LiteralExpressionSyntax literal when literal.IsKind(SyntaxKind.FalseLiteralExpression) => false,
      _ => null
    };
  }

  /// <summary>
  /// Resolves a type constraint to a fully-qualified CLR type name.
  /// </summary>
  private static string ResolveClrType(string? typeConstraint)
  {
    return typeConstraint switch
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
      "string" or null => "global::System.String",
      "Guid" or "guid" => "global::System.Guid",
      "DateTime" or "datetime" => "global::System.DateTime",
      "DateTimeOffset" => "global::System.DateTimeOffset",
      "TimeSpan" or "timespan" => "global::System.TimeSpan",
      "Uri" or "uri" => "global::System.Uri",
      _ when typeConstraint?.StartsWith("global::", StringComparison.Ordinal) == true => typeConstraint,
      _ => $"global::{typeConstraint}"
    };
  }
}
