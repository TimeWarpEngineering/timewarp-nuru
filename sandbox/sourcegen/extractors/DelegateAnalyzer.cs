// sandbox/sourcegen/extractors/DelegateAnalyzer.cs
// Analyzes delegate/lambda expressions to extract handler information.
//
// Agent: Amina
// Task: #242-step-3

namespace TimeWarp.Nuru.SourceGen.Extractors;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Immutable;

/// <summary>
/// Analyzes delegate/lambda expressions to extract handler parameter and return type information.
/// </summary>
public static class DelegateAnalyzer
{
  /// <summary>
  /// Result of analyzing a delegate.
  /// </summary>
  public record DelegateAnalysisResult(
    ImmutableArray<DelegateParameter> Parameters,
    string ReturnTypeName,
    bool IsAsync,
    bool HasCancellationToken,
    ImmutableArray<Diagnostic> Diagnostics);

  /// <summary>
  /// Information about a delegate parameter.
  /// </summary>
  public record DelegateParameter(
    string Name,
    string TypeName,
    bool IsOptional,
    string? DefaultValue);

  /// <summary>
  /// Analyzes a lambda expression to extract parameter and return type information.
  /// Requires semantic model for type resolution.
  /// </summary>
  public static DelegateAnalysisResult AnalyzeLambda(
    LambdaExpressionSyntax lambda,
    SemanticModel semanticModel)
  {
    List<DelegateParameter> parameters = [];
    List<Diagnostic> diagnostics = [];
    bool hasCancellationToken = false;

    // Extract parameters
    ImmutableArray<IParameterSymbol>? parameterSymbols = GetLambdaParameters(lambda, semanticModel);

    if (parameterSymbols.HasValue)
    {
      foreach (IParameterSymbol param in parameterSymbols.Value)
      {
        string typeName = param.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

        // Check for CancellationToken
        if (typeName == "global::System.Threading.CancellationToken")
        {
          hasCancellationToken = true;
        }

        bool isOptional = param.IsOptional || param.NullableAnnotation == NullableAnnotation.Annotated;
        string? defaultValue = param.HasExplicitDefaultValue
          ? param.ExplicitDefaultValue?.ToString()
          : null;

        parameters.Add(new DelegateParameter(
          Name: param.Name,
          TypeName: typeName,
          IsOptional: isOptional,
          DefaultValue: defaultValue));
      }
    }

    // Extract return type
    (string returnTypeName, bool isAsync) = GetReturnType(lambda, semanticModel);

    return new DelegateAnalysisResult(
      Parameters: [.. parameters],
      ReturnTypeName: returnTypeName,
      IsAsync: isAsync,
      HasCancellationToken: hasCancellationToken,
      Diagnostics: [.. diagnostics]);
  }

  /// <summary>
  /// Analyzes a lambda expression using only syntax (no semantic model).
  /// Less accurate but works without compilation.
  /// </summary>
  public static DelegateAnalysisResult AnalyzeLambdaSyntaxOnly(LambdaExpressionSyntax lambda)
  {
    List<DelegateParameter> parameters = [];
    bool hasCancellationToken = false;

    // Extract parameters from syntax
    switch (lambda)
    {
      case ParenthesizedLambdaExpressionSyntax parenthesized:
        foreach (ParameterSyntax param in parenthesized.ParameterList.Parameters)
        {
          string typeName = param.Type?.ToString() ?? "object";
          string name = param.Identifier.Text;

          if (typeName.Contains("CancellationToken", StringComparison.Ordinal))
          {
            hasCancellationToken = true;
          }

          bool isOptional = param.Default is not null;
          string? defaultValue = param.Default?.Value.ToString();

          parameters.Add(new DelegateParameter(
            Name: name,
            TypeName: NormalizeTypeName(typeName),
            IsOptional: isOptional,
            DefaultValue: defaultValue));
        }
        break;

      case SimpleLambdaExpressionSyntax simple:
        // Simple lambda has single parameter without type
        parameters.Add(new DelegateParameter(
          Name: simple.Parameter.Identifier.Text,
          TypeName: "global::System.Object", // Unknown without semantic model
          IsOptional: false,
          DefaultValue: null));
        break;
    }

    // Try to determine if async from syntax
    bool isAsync = lambda.AsyncKeyword.IsKind(SyntaxKind.AsyncKeyword);

    // Try to infer return type from body (limited without semantic model)
    string returnTypeName = isAsync ? "global::System.Threading.Tasks.Task" : "void";

    return new DelegateAnalysisResult(
      Parameters: [.. parameters],
      ReturnTypeName: returnTypeName,
      IsAsync: isAsync,
      HasCancellationToken: hasCancellationToken,
      Diagnostics: []);
  }

  /// <summary>
  /// Gets lambda parameters from semantic model.
  /// </summary>
  private static ImmutableArray<IParameterSymbol>? GetLambdaParameters(
    LambdaExpressionSyntax lambda,
    SemanticModel semanticModel)
  {
    ISymbol? symbol = semanticModel.GetSymbolInfo(lambda).Symbol;

    if (symbol is IMethodSymbol methodSymbol)
    {
      return methodSymbol.Parameters;
    }

    return null;
  }

  /// <summary>
  /// Gets the return type of a lambda expression.
  /// </summary>
  private static (string TypeName, bool IsAsync) GetReturnType(
    LambdaExpressionSyntax lambda,
    SemanticModel semanticModel)
  {
    ISymbol? symbol = semanticModel.GetSymbolInfo(lambda).Symbol;

    if (symbol is IMethodSymbol methodSymbol)
    {
      ITypeSymbol returnType = methodSymbol.ReturnType;
      string typeName = returnType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

      bool isAsync = methodSymbol.IsAsync ||
                     typeName.StartsWith("global::System.Threading.Tasks.Task", StringComparison.Ordinal);

      return (typeName, isAsync);
    }

    // Fallback based on async keyword
    bool hasAsyncKeyword = lambda.AsyncKeyword.IsKind(SyntaxKind.AsyncKeyword);
    return (hasAsyncKeyword ? "global::System.Threading.Tasks.Task" : "void", hasAsyncKeyword);
  }

  /// <summary>
  /// Normalizes a type name from syntax to fully qualified form.
  /// </summary>
  private static string NormalizeTypeName(string typeName)
  {
    // Handle common primitive types
    return typeName switch
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
      "void" => "void",
      _ when typeName.StartsWith("global::", StringComparison.Ordinal) => typeName,
      _ => $"global::{typeName}"
    };
  }
}
