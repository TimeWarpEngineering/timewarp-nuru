namespace TimeWarp.Nuru;

/// <summary>
/// Handler extraction and body rewriting methods for the delegate command generator.
/// </summary>
public partial class NuruDelegateCommandGenerator
{
  /// <summary>
  /// Extracts handler information from a lambda expression, including rewritten body.
  /// </summary>
  private static HandlerInfo? ExtractHandlerInfo(
    ExpressionSyntax handlerExpression,
    List<ParameterClassification> parameters,
    DelegateSignature signature,
    SemanticModel semanticModel,
    CancellationToken cancellationToken)
  {
    // Only support lambda expressions for now (method groups deferred)
    if (handlerExpression is not LambdaExpressionSyntax lambda)
      return null;

    // Check for closures (captured external variables)
    if (DetectClosures(lambda, parameters, semanticModel, out List<string> capturedVariables))
    {
      // For now, skip handler generation if closures detected
      // In future, we could report diagnostics here
      return null;
    }

    // Detect if lambda is async
    bool isAsync = lambda.Modifiers.Any(SyntaxKind.AsyncKeyword);

    // Get lambda body
    CSharpSyntaxNode? body = lambda.Body;
    if (body is null)
      return null;

    // Build parameter mappings for rewriting
    Dictionary<string, string> routeParamMappings = [];
    Dictionary<string, string> diParamMappings = [];

    foreach (ParameterClassification param in parameters)
    {
      if (param.IsRouteParam)
      {
        // env → request.Env
        routeParamMappings[param.Name] = $"request.{ToPascalCase(param.Name)}";
      }
      else if (param.IsDiParam)
      {
        // logger → Logger (field reference)
        diParamMappings[param.Name] = ToPascalCase(param.Name);
      }
    }

    // Rewrite the lambda body
    ParameterRewriter rewriter = new(routeParamMappings, diParamMappings);
    Microsoft.CodeAnalysis.SyntaxNode rewrittenBody = rewriter.Visit(body);

    // Convert body to string
    string bodyString = RewrittenBodyToString(rewrittenBody, signature.ReturnType, isAsync);

    return new HandlerInfo(
      Parameters: [.. parameters],
      LambdaBody: bodyString,
      IsAsync: isAsync,
      ReturnType: signature.ReturnType);
  }

  /// <summary>
  /// Detects if a lambda captures variables from enclosing scope (closures).
  /// </summary>
  private static bool DetectClosures(
    LambdaExpressionSyntax lambda,
    List<ParameterClassification> parameters,
    SemanticModel semanticModel,
    out List<string> capturedVariables)
  {
    capturedVariables = [];
    HashSet<string> allowedNames = [.. parameters.Select(p => p.Name)];

    // Track local variables declared inside the lambda
    HashSet<string> localVariables = [];
    foreach (VariableDeclaratorSyntax declarator in lambda.Body.DescendantNodes()
      .OfType<VariableDeclaratorSyntax>())
    {
      localVariables.Add(declarator.Identifier.Text);
    }

    // Walk lambda body looking for identifiers
    foreach (IdentifierNameSyntax identifier in lambda.Body.DescendantNodes()
      .OfType<IdentifierNameSyntax>())
    {
      string name = identifier.Identifier.Text;

      // Skip if it's a parameter
      if (allowedNames.Contains(name))
        continue;

      // Skip if it's a local variable declared inside lambda
      if (localVariables.Contains(name))
        continue;

      // Skip if it's part of a member access on the right side (obj.name - we care about 'obj', not 'name')
      if (identifier.Parent is MemberAccessExpressionSyntax memberAccess && memberAccess.Name == identifier)
        continue;

      // Get symbol info
      SymbolInfo symbolInfo = semanticModel.GetSymbolInfo(identifier);
      ISymbol? symbol = symbolInfo.Symbol;

      if (symbol is null)
      {
        // Symbol resolution failed - be conservative and treat as closure
        // This can happen for variables in top-level statements or outer scopes
        // where the semantic model doesn't resolve correctly in source generators
        capturedVariables.Add(name);
        continue;
      }

      // Check what kind of symbol it is
      switch (symbol)
      {
        case ILocalSymbol:
          // Local variable from outer scope - closure!
          capturedVariables.Add(name);
          break;

        case IParameterSymbol param:
          // If not our lambda's parameter, it's from outer method - closure!
          if (!allowedNames.Contains(param.Name))
          {
            capturedVariables.Add(name);
          }

          break;

        case IFieldSymbol field when !field.IsStatic:
          // Instance field access (via implicit 'this') - closure!
          capturedVariables.Add($"this.{name}");
          break;

        case IPropertySymbol prop when !prop.IsStatic:
          // Instance property access (via implicit 'this') - closure!
          capturedVariables.Add($"this.{name}");
          break;

        // Static members are OK
        case IMethodSymbol:
        case IFieldSymbol { IsStatic: true }:
        case IPropertySymbol { IsStatic: true }:
        case INamedTypeSymbol:
        case INamespaceSymbol:
          break;
      }
    }

    return capturedVariables.Count > 0;
  }

  /// <summary>
  /// Converts the rewritten lambda body to a string suitable for the Handle method.
  /// </summary>
  private static string RewrittenBodyToString(
    Microsoft.CodeAnalysis.SyntaxNode rewrittenBody,
    DelegateTypeInfo returnType,
    bool isAsync)
  {
    string bodyText = rewrittenBody.ToFullString().Trim();

    // Handle block body vs expression body
    if (rewrittenBody is BlockSyntax)
    {
      // Block body - strip the outer braces, we'll add our own
      if (bodyText.StartsWith('{') && bodyText.EndsWith('}'))
      {
        bodyText = bodyText[1..^1].Trim();
      }

      // For sync (non-async) block bodies with non-void return,
      // we need to wrap return statements in ValueTask
      if (!isAsync && !returnType.IsVoid)
      {
        bodyText = WrapReturnStatementsInValueTask(bodyText, returnType.FullName);
      }

      return bodyText;
    }
    else
    {
      // Expression body - wrap appropriately
      if (returnType.IsVoid)
      {
        // void return - just execute the expression
        return $"{bodyText};";
      }
      else if (returnType.IsTask)
      {
        if (isAsync)
        {
          // async Task or Task<T> - await and return
          if (returnType.TaskResultType is null)
          {
            // async Task → await, then return Unit
            return $"await {bodyText};";
          }
          else
          {
            // async Task<T> → return await result
            return $"return await {bodyText};";
          }
        }
        else
        {
          // Non-async Task return (rare) - return the task wrapped
          return $"return new global::System.Threading.Tasks.ValueTask<{returnType.TaskResultType?.FullName ?? "global::Mediator.Unit"}>({bodyText});";
        }
      }
      else
      {
        // Sync return value - wrap in ValueTask
        return $"return new global::System.Threading.Tasks.ValueTask<{returnType.FullName}>({bodyText});";
      }
    }
  }

  /// <summary>
  /// Wraps return statements in a block body with ValueTask for sync handlers.
  /// Transforms "return X;" to "return new ValueTask&lt;T&gt;(X);"
  /// </summary>
  private static string WrapReturnStatementsInValueTask(string bodyText, string returnTypeName)
  {
    // Simple regex-based transformation for return statements
    // This handles: return value; → return new ValueTask<T>(value);
    System.Text.StringBuilder result = new();
    string[] lines = bodyText.Split('\n');

    foreach (string line in lines)
    {
      string trimmedLine = line.TrimStart();
      if (trimmedLine.StartsWith("return ", StringComparison.Ordinal) && trimmedLine.EndsWith(';'))
      {
        // Extract the return value (everything between "return " and ";")
        string returnValue = trimmedLine[7..^1].Trim();

        // Preserve leading whitespace
        int leadingSpaces = line.Length - line.TrimStart().Length;
        string indent = line[..leadingSpaces];

        result.Append(indent);
        result.Append(CultureInfo.InvariantCulture, $"return new global::System.Threading.Tasks.ValueTask<{returnTypeName}>({returnValue});");
        result.AppendLine();
      }
      else
      {
        result.AppendLine(line);
      }
    }

    return result.ToString().TrimEnd();
  }
}
