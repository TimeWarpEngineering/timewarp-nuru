// Extracts handler information from lambda expressions and method references.
//
// Handles:
// - Lambda expressions: (string env, bool force) => Deploy(env, force)
// - Method group expressions: HandleDeploy
// - Returns HandlerDefinition with parameters, return type, async info

namespace TimeWarp.Nuru.Generators;

using RoslynParameterSyntax = Microsoft.CodeAnalysis.CSharp.Syntax.ParameterSyntax;

/// <summary>
/// Extracts handler information from lambda expressions and method references.
/// </summary>
internal static class HandlerExtractor
{
  /// <summary>
  /// Extracts a HandlerDefinition from a WithHandler() invocation.
  /// </summary>
  /// <param name="withHandlerInvocation">The .WithHandler(...) invocation expression.</param>
  /// <param name="semanticModel">Semantic model for type resolution.</param>
  /// <param name="cancellationToken">Cancellation token.</param>
  /// <returns>The extracted handler definition, or null if extraction fails.</returns>
  public static HandlerDefinition? Extract
  (
    InvocationExpressionSyntax withHandlerInvocation,
    SemanticModel semanticModel,
    CancellationToken cancellationToken
  )
  {
    ArgumentListSyntax? args = withHandlerInvocation.ArgumentList;
    if (args is null || args.Arguments.Count == 0)
      return null;

    ExpressionSyntax handlerExpression = args.Arguments[0].Expression;

    return handlerExpression switch
    {
      ParenthesizedLambdaExpressionSyntax lambda => ExtractFromLambda(lambda, semanticModel, cancellationToken),
      SimpleLambdaExpressionSyntax simpleLambda => ExtractFromSimpleLambda(simpleLambda, semanticModel, cancellationToken),
      IdentifierNameSyntax methodGroup => ExtractFromMethodGroup(methodGroup, semanticModel, cancellationToken),
      MemberAccessExpressionSyntax memberAccess => ExtractFromMemberAccess(memberAccess, semanticModel, cancellationToken),
      _ => CreateDefaultHandler()
    };
  }

  /// <summary>
  /// Extracts handler information from a parenthesized lambda expression.
  /// </summary>
  private static HandlerDefinition? ExtractFromLambda
  (
    ParenthesizedLambdaExpressionSyntax lambda,
    SemanticModel semanticModel,
    CancellationToken cancellationToken
  )
  {
    ImmutableArray<ParameterBinding>.Builder parameters = ImmutableArray.CreateBuilder<ParameterBinding>();
    bool hasCancellationToken = false;
    bool requiresServiceProvider = false;

    // Capture lambda body source
    string? lambdaBodySource = null;
    bool isExpressionBody = false;

    if (lambda.Body is ExpressionSyntax expr)
    {
      lambdaBodySource = expr.ToFullString().Trim();
      isExpressionBody = true;
    }
    else if (lambda.Body is BlockSyntax block)
    {
      lambdaBodySource = block.ToFullString();
      isExpressionBody = false;
    }

    // Try to get symbol info for accurate type resolution
    SymbolInfo symbolInfo = semanticModel.GetSymbolInfo(lambda, cancellationToken);

    if (symbolInfo.Symbol is IMethodSymbol methodSymbol)
    {
      // Use method symbol for accurate parameter/return type info
      // but also capture the lambda body source
      HandlerDefinition baseDefinition = ExtractFromMethodSymbol(methodSymbol, semanticModel.Compilation);
      return baseDefinition with
      {
        LambdaBodySource = lambdaBodySource,
        IsExpressionBody = isExpressionBody
      };
    }

    // Fallback to syntax-only analysis
    foreach (RoslynParameterSyntax param in lambda.ParameterList.Parameters)
    {
      string paramName = param.Identifier.Text;
      string typeName = NormalizeTypeName(param.Type?.ToString() ?? "object");

      if (IsCancellationTokenType(typeName))
      {
        hasCancellationToken = true;
        parameters.Add(ParameterBinding.ForCancellationToken(paramName));
      }
      else if (IsServiceType(typeName))
      {
        requiresServiceProvider = true;
        parameters.Add(ParameterBinding.FromService(paramName, typeName));
      }
      else
      {
        // Assume it's a route parameter - will be matched later
        parameters.Add(ParameterBinding.FromParameter(
          parameterName: paramName,
          typeName: typeName,
          segmentName: paramName.ToLowerInvariant(),
          isOptional: param.Default is not null,
          defaultValue: param.Default?.Value.ToString(),
          requiresConversion: typeName != "global::System.String"));
      }
    }

    // Determine return type and async status
    bool isAsync = lambda.AsyncKeyword.IsKind(SyntaxKind.AsyncKeyword);
    HandlerReturnType returnType = isAsync ? HandlerReturnType.Task : HandlerReturnType.Void;

    // Try to infer return type from body
    if (lambda.Body is not null)
    {
      returnType = InferReturnType(lambda.Body, isAsync, semanticModel, cancellationToken);
    }

    return new HandlerDefinition(
      HandlerKind: HandlerKind.Delegate,
      FullTypeName: null,
      MethodName: null,
      LambdaBodySource: lambdaBodySource,
      IsExpressionBody: isExpressionBody,
      Parameters: parameters.ToImmutable(),
      ReturnType: returnType,
      IsAsync: isAsync,
      RequiresCancellationToken: hasCancellationToken,
      RequiresServiceProvider: requiresServiceProvider);
  }

  /// <summary>
  /// Extracts handler information from a simple lambda expression (single parameter).
  /// </summary>
  private static HandlerDefinition? ExtractFromSimpleLambda
  (
    SimpleLambdaExpressionSyntax lambda,
    SemanticModel semanticModel,
    CancellationToken cancellationToken
  )
  {
    // Capture lambda body source
    string? lambdaBodySource = null;
    bool isExpressionBody = false;

    if (lambda.Body is ExpressionSyntax expr)
    {
      lambdaBodySource = expr.ToFullString().Trim();
      isExpressionBody = true;
    }
    else if (lambda.Body is BlockSyntax block)
    {
      lambdaBodySource = block.ToFullString();
      isExpressionBody = false;
    }

    SymbolInfo symbolInfo = semanticModel.GetSymbolInfo(lambda, cancellationToken);

    if (symbolInfo.Symbol is IMethodSymbol methodSymbol)
    {
      // Use method symbol for accurate parameter/return type info
      // but also capture the lambda body source
      HandlerDefinition baseDefinition = ExtractFromMethodSymbol(methodSymbol, semanticModel.Compilation);
      return baseDefinition with
      {
        LambdaBodySource = lambdaBodySource,
        IsExpressionBody = isExpressionBody
      };
    }

    // Fallback to syntax-only
    string paramName = lambda.Parameter.Identifier.Text;

    ImmutableArray<ParameterBinding> parameters =
    [
      ParameterBinding.FromParameter(
        parameterName: paramName,
        typeName: "global::System.Object",
        segmentName: paramName.ToLowerInvariant())
    ];

    bool isAsync = lambda.AsyncKeyword.IsKind(SyntaxKind.AsyncKeyword);
    HandlerReturnType returnType = isAsync ? HandlerReturnType.Task : HandlerReturnType.Void;

    // Try to infer return type from body
    if (lambda.Body is not null)
    {
      returnType = InferReturnType(lambda.Body, isAsync, semanticModel, cancellationToken);
    }

    return new HandlerDefinition(
      HandlerKind: HandlerKind.Delegate,
      FullTypeName: null,
      MethodName: null,
      LambdaBodySource: lambdaBodySource,
      IsExpressionBody: isExpressionBody,
      Parameters: parameters,
      ReturnType: returnType,
      IsAsync: isAsync,
      RequiresCancellationToken: false,
      RequiresServiceProvider: false);
  }

  /// <summary>
  /// Extracts handler information from a method group expression.
  /// </summary>
  private static HandlerDefinition? ExtractFromMethodGroup
  (
    IdentifierNameSyntax methodGroup,
    SemanticModel semanticModel,
    CancellationToken cancellationToken
  )
  {
    SymbolInfo symbolInfo = semanticModel.GetSymbolInfo(methodGroup, cancellationToken);

    // For method groups, Symbol may be null but CandidateSymbols contains the method(s)
    if (symbolInfo.Symbol is IMethodSymbol methodSymbol)
    {
      return ExtractFromMethodSymbolAsMethod(methodSymbol, semanticModel.Compilation);
    }

    // Check CandidateSymbols - method groups often have the method here
    if (symbolInfo.CandidateSymbols.Length > 0 &&
        symbolInfo.CandidateSymbols[0] is IMethodSymbol candidateMethod)
    {
      return ExtractFromMethodSymbolAsMethod(candidateMethod, semanticModel.Compilation);
    }

    // If we can't resolve, create a minimal handler as delegate (not method)
    return HandlerDefinition.ForDelegate(
      parameters: [],
      returnType: HandlerReturnType.Void,
      isAsync: false);
  }

  /// <summary>
  /// Extracts handler information from a member access expression (e.g., obj.Method).
  /// </summary>
  private static HandlerDefinition? ExtractFromMemberAccess
  (
    MemberAccessExpressionSyntax memberAccess,
    SemanticModel semanticModel,
    CancellationToken cancellationToken
  )
  {
    SymbolInfo symbolInfo = semanticModel.GetSymbolInfo(memberAccess, cancellationToken);

    if (symbolInfo.Symbol is IMethodSymbol methodSymbol)
    {
      return ExtractFromMethodSymbolAsMethod(methodSymbol, semanticModel.Compilation);
    }

    // Check CandidateSymbols - method groups often have the method here
    if (symbolInfo.CandidateSymbols.Length > 0 &&
        symbolInfo.CandidateSymbols[0] is IMethodSymbol candidateMethod)
    {
      return ExtractFromMethodSymbolAsMethod(candidateMethod, semanticModel.Compilation);
    }

    // Fallback to delegate since we don't have type info
    return HandlerDefinition.ForDelegate(
      parameters: [],
      returnType: HandlerReturnType.Void,
      isAsync: false);
  }

  /// <summary>
  /// Extracts handler information from a resolved method symbol (for delegates).
  /// </summary>
  private static HandlerDefinition ExtractFromMethodSymbol(IMethodSymbol methodSymbol, Compilation? compilation = null)
  {
    ImmutableArray<ParameterBinding>.Builder parameters = ImmutableArray.CreateBuilder<ParameterBinding>();
    bool hasCancellationToken = false;
    bool requiresServiceProvider = false;

    foreach (IParameterSymbol param in methodSymbol.Parameters)
    {
      string typeName = param.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

      if (IsCancellationTokenType(typeName))
      {
        hasCancellationToken = true;
        parameters.Add(ParameterBinding.ForCancellationToken(param.Name));
      }
      else if (IsServiceType(typeName))
      {
        requiresServiceProvider = true;
        parameters.Add(CreateServiceBinding(param, typeName, compilation));
      }
      else
      {
        bool isOptional = param.IsOptional || param.NullableAnnotation == NullableAnnotation.Annotated;
        string? defaultValue = param.HasExplicitDefaultValue ? param.ExplicitDefaultValue?.ToString() : null;

        parameters.Add(ParameterBinding.FromParameter(
          parameterName: param.Name,
          typeName: typeName,
          segmentName: param.Name.ToLowerInvariant(),
          isOptional: isOptional,
          defaultValue: defaultValue,
          requiresConversion: typeName != "global::System.String"));
      }
    }

    HandlerReturnType returnType = GetReturnType(methodSymbol);
    bool isAsync = methodSymbol.IsAsync || returnType.IsTask;

    return new HandlerDefinition(
      HandlerKind: HandlerKind.Delegate,
      FullTypeName: null,
      MethodName: null,
      LambdaBodySource: null,  // Will be set by caller if lambda
      IsExpressionBody: true,
      Parameters: parameters.ToImmutable(),
      ReturnType: returnType,
      IsAsync: isAsync,
      RequiresCancellationToken: hasCancellationToken,
      RequiresServiceProvider: requiresServiceProvider);
  }

  /// <summary>
  /// Extracts handler information from a resolved method symbol (for method references).
  /// </summary>
  private static HandlerDefinition ExtractFromMethodSymbolAsMethod(IMethodSymbol methodSymbol, Compilation? compilation = null)
  {
    ImmutableArray<ParameterBinding>.Builder parameters = ImmutableArray.CreateBuilder<ParameterBinding>();
    bool hasCancellationToken = false;
    bool requiresServiceProvider = false;

    foreach (IParameterSymbol param in methodSymbol.Parameters)
    {
      string typeName = param.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

      if (IsCancellationTokenType(typeName))
      {
        hasCancellationToken = true;
        parameters.Add(ParameterBinding.ForCancellationToken(param.Name));
      }
      else if (IsServiceType(typeName))
      {
        requiresServiceProvider = true;
        parameters.Add(CreateServiceBinding(param, typeName, compilation));
      }
      else
      {
        bool isOptional = param.IsOptional || param.NullableAnnotation == NullableAnnotation.Annotated;
        string? defaultValue = param.HasExplicitDefaultValue ? param.ExplicitDefaultValue?.ToString() : null;

        parameters.Add(ParameterBinding.FromParameter(
          parameterName: param.Name,
          typeName: typeName,
          segmentName: param.Name.ToLowerInvariant(),
          isOptional: isOptional,
          defaultValue: defaultValue,
          requiresConversion: typeName != "global::System.String"));
      }
    }

    HandlerReturnType returnType = GetReturnType(methodSymbol);
    bool isAsync = methodSymbol.IsAsync || returnType.IsTask;

    string fullTypeName = methodSymbol.ContainingType?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
      ?? "global::System.Object";

    return HandlerDefinition.ForMethod(
      fullTypeName: fullTypeName,
      methodName: methodSymbol.Name,
      parameters: parameters.ToImmutable(),
      returnType: returnType,
      isAsync: isAsync,
      requiresCancellationToken: hasCancellationToken,
      requiresServiceProvider: requiresServiceProvider);
  }

  /// <summary>
  /// Gets the return type from a method symbol.
  /// </summary>
  private static HandlerReturnType GetReturnType(IMethodSymbol methodSymbol)
  {
    ITypeSymbol returnType = methodSymbol.ReturnType;
    string fullTypeName = returnType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
    string shortTypeName = returnType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);

    if (returnType.SpecialType == SpecialType.System_Void)
      return HandlerReturnType.Void;

    if (fullTypeName == "global::System.Threading.Tasks.Task")
      return HandlerReturnType.Task;

    if (fullTypeName.StartsWith("global::System.Threading.Tasks.Task<", StringComparison.Ordinal) ||
        fullTypeName.StartsWith("global::System.Threading.Tasks.ValueTask<", StringComparison.Ordinal))
    {
      if (returnType is INamedTypeSymbol namedType && namedType.TypeArguments.Length == 1)
      {
        string innerType = namedType.TypeArguments[0].ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        string innerShort = namedType.TypeArguments[0].ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
        return HandlerReturnType.TaskOf(innerType, innerShort);
      }

      return HandlerReturnType.Task;
    }

    return HandlerReturnType.Of(fullTypeName, shortTypeName);
  }

  /// <summary>
  /// Infers the return type from a lambda body.
  /// </summary>
  private static HandlerReturnType InferReturnType
  (
    CSharpSyntaxNode body,
    bool isAsync,
    SemanticModel semanticModel,
    CancellationToken cancellationToken
  )
  {
    // For expression bodies, try to get the type
    if (body is ExpressionSyntax expression)
    {
      TypeInfo typeInfo = semanticModel.GetTypeInfo(expression, cancellationToken);
      if (typeInfo.Type is not null && typeInfo.Type.SpecialType != SpecialType.System_Void)
      {
        string fullTypeName = typeInfo.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        string shortTypeName = typeInfo.Type.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);

        if (isAsync)
          return HandlerReturnType.TaskOf(fullTypeName, shortTypeName);

        return HandlerReturnType.Of(fullTypeName, shortTypeName);
      }
    }

    // For block bodies, find return statements and infer type from the returned expression
    if (body is BlockSyntax block)
    {
      // Find the first return statement that has an expression
      ReturnStatementSyntax? returnStatement = block
        .DescendantNodes()
        .OfType<ReturnStatementSyntax>()
        .FirstOrDefault(r => r.Expression is not null);

      if (returnStatement?.Expression is not null)
      {
        TypeInfo typeInfo = semanticModel.GetTypeInfo(returnStatement.Expression, cancellationToken);
        if (typeInfo.Type is not null && typeInfo.Type.SpecialType != SpecialType.System_Void)
        {
          string fullTypeName = typeInfo.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
          string shortTypeName = typeInfo.Type.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);

          if (isAsync)
            return HandlerReturnType.TaskOf(fullTypeName, shortTypeName);

          return HandlerReturnType.Of(fullTypeName, shortTypeName);
        }
      }
    }

    return isAsync ? HandlerReturnType.Task : HandlerReturnType.Void;
  }

  /// <summary>
  /// Creates a default handler definition when extraction fails.
  /// </summary>
  private static HandlerDefinition CreateDefaultHandler()
  {
    return HandlerDefinition.ForDelegate(
      parameters: [],
      returnType: HandlerReturnType.Void,
      isAsync: false);
  }

  /// <summary>
  /// Checks if a type name represents CancellationToken.
  /// </summary>
  private static bool IsCancellationTokenType(string typeName)
  {
    return typeName == "global::System.Threading.CancellationToken" ||
           typeName == "CancellationToken" ||
           typeName == "System.Threading.CancellationToken";
  }

  /// <summary>
  /// Checks if a type name appears to be a service (interface or known service types).
  /// Excludes built-in route-bindable types like IPAddress, IList, etc.
  /// </summary>
  private static bool IsServiceType(string typeName)
  {
    // First, check if it's a built-in route-bindable type
    // These should NEVER be treated as services even if they look like interfaces
    if (IsBuiltInRouteBindableType(typeName))
      return false;

    // Common service patterns
    if (typeName.StartsWith("global::Microsoft.Extensions.", StringComparison.Ordinal))
      return true;

    if (typeName.Contains("ILogger", StringComparison.Ordinal))
      return true;

    if (typeName.Contains("IServiceProvider", StringComparison.Ordinal))
      return true;

    // Check if it's an interface (starts with I followed by uppercase letter)
    // This handles user-defined service interfaces like IGreeter, IFormatter, etc.
    string shortName = GetShortTypeName(typeName);
    if (shortName.Length >= 2 && shortName[0] == 'I' && char.IsUpper(shortName[1]))
      return true;

    return false;
  }

  /// <summary>
  /// Checks if a type is a built-in route-bindable type from TypeConversionMap.
  /// These types (like IPAddress, IList) should be bound from route parameters, not injected as services.
  /// </summary>
  private static bool IsBuiltInRouteBindableType(string typeName)
  {
    // Map fully-qualified type names to their built-in status
    // This list must stay in sync with TypeConversionMap.GetClrTypeName()
    return typeName switch
    {
      // Primitive types (unlikely to be confused with services, but included for completeness)
      "global::System.Int32" or "int" => true,
      "global::System.Int64" or "long" => true,
      "global::System.Int16" or "short" => true,
      "global::System.Byte" or "byte" => true,
      "global::System.SByte" or "sbyte" => true,
      "global::System.UInt16" or "ushort" => true,
      "global::System.UInt32" or "uint" => true,
      "global::System.UInt64" or "ulong" => true,
      "global::System.Single" or "float" => true,
      "global::System.Double" or "double" => true,
      "global::System.Decimal" or "decimal" => true,
      "global::System.Boolean" or "bool" => true,
      "global::System.Char" or "char" => true,
      "global::System.String" or "string" => true,

      // System value types
      "global::System.Guid" => true,
      "global::System.DateTime" => true,
      "global::System.DateTimeOffset" => true,
      "global::System.TimeSpan" => true,
      "global::System.DateOnly" => true,
      "global::System.TimeOnly" => true,

      // Reference types that could be confused with interfaces/services
      "global::System.Uri" => true,
      "global::System.Version" => true,
      "global::System.IO.FileInfo" => true,
      "global::System.IO.DirectoryInfo" => true,
      "global::System.Net.IPAddress" => true,  // Key one! Starts with 'I' but is a class

      _ => false
    };
  }

  /// <summary>
  /// Gets the short type name from a fully qualified type name.
  /// </summary>
  private static string GetShortTypeName(string typeName)
  {
    // Remove global:: prefix
    if (typeName.StartsWith("global::", StringComparison.Ordinal))
      typeName = typeName[8..];

    // Get last segment after the final dot
    int lastDot = typeName.LastIndexOf('.');
    return lastDot >= 0 ? typeName[(lastDot + 1)..] : typeName;
  }

  /// <summary>
  /// Normalizes a type name to fully qualified form.
  /// </summary>
  private static string NormalizeTypeName(string typeName)
  {
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

  /// <summary>
  /// Checks if a type is IOptions&lt;T&gt; and extracts the configuration section key and validator type.
  /// </summary>
  /// <param name="typeSymbol">The type symbol to check.</param>
  /// <param name="compilation">The compilation to search for validators.</param>
  /// <param name="configurationKey">The extracted configuration key (from [ConfigurationKey] or convention).</param>
  /// <param name="validatorTypeName">The validator type implementing IValidateOptions&lt;T&gt;, if found.</param>
  /// <returns>True if the type is IOptions&lt;T&gt;, false otherwise.</returns>
  private static bool TryGetOptionsInfo(
    ITypeSymbol typeSymbol,
    Compilation? compilation,
    out string? configurationKey,
    out string? validatorTypeName)
  {
    configurationKey = null;
    validatorTypeName = null;

    // Check if it's IOptions<T>
    if (typeSymbol is not INamedTypeSymbol namedType)
      return false;

    string typeName = namedType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

    // Match IOptions<T> patterns
    if (!typeName.StartsWith("global::Microsoft.Extensions.Options.IOptions<", StringComparison.Ordinal))
      return false;

    // Get the inner type T from IOptions<T>
    if (namedType.TypeArguments.Length != 1)
      return false;

    ITypeSymbol innerType = namedType.TypeArguments[0];

    // Look for [ConfigurationKey("...")] attribute on the inner type
    foreach (AttributeData attribute in innerType.GetAttributes())
    {
      string? attrName = attribute.AttributeClass?.Name;
      if (attrName is "ConfigurationKeyAttribute" or "ConfigurationKey")
      {
        // Get the key from the first constructor argument
        if (attribute.ConstructorArguments.Length > 0 &&
            attribute.ConstructorArguments[0].Value is string key)
        {
          configurationKey = key;
          break;
        }
      }
    }

    // Fall back to convention: strip "Options" suffix from class name
    if (configurationKey is null)
    {
      string innerTypeName = innerType.Name;
      const string optionsSuffix = "Options";
      if (innerTypeName.EndsWith(optionsSuffix, StringComparison.Ordinal) && innerTypeName.Length > optionsSuffix.Length)
      {
        configurationKey = innerTypeName[..^optionsSuffix.Length];
      }
      else
      {
        configurationKey = innerTypeName;
      }
    }

    // Search for IValidateOptions<T> implementations
    if (compilation is not null)
    {
      validatorTypeName = FindValidatorForOptionsType(innerType, compilation);
    }

    return true;
  }

  /// <summary>
  /// Searches the compilation for a type implementing IValidateOptions&lt;T&gt; for the given options type.
  /// </summary>
  /// <param name="optionsType">The options type T.</param>
  /// <param name="compilation">The compilation to search.</param>
  /// <returns>The fully qualified validator type name, or null if not found.</returns>
  /// <exception cref="InvalidOperationException">Thrown if multiple validators are found for the same options type.</exception>
  private static string? FindValidatorForOptionsType(ITypeSymbol optionsType, Compilation compilation)
  {
    List<INamedTypeSymbol> validators = [];

    // Search all types in the compilation
    foreach (INamedTypeSymbol type in GetAllTypes(compilation.GlobalNamespace))
    {
      // Skip abstract types and interfaces
      if (type.IsAbstract || type.TypeKind == TypeKind.Interface)
        continue;

      // Check if it implements IValidateOptions<T>
      foreach (INamedTypeSymbol iface in type.AllInterfaces)
      {
        if (iface.Name == "IValidateOptions" &&
            iface.ContainingNamespace?.ToDisplayString() == "Microsoft.Extensions.Options" &&
            iface.TypeArguments.Length == 1 &&
            SymbolEqualityComparer.Default.Equals(iface.TypeArguments[0], optionsType))
        {
          validators.Add(type);
          break;
        }
      }
    }

    if (validators.Count == 0)
      return null;

    if (validators.Count > 1)
    {
      string optionsTypeName = optionsType.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat);
      string validatorNames = string.Join(", ", validators.Select(v => v.Name));
      throw new InvalidOperationException(
        $"Multiple validators found for {optionsTypeName}: {validatorNames}. " +
        $"Only one IValidateOptions<{optionsTypeName}> implementation is allowed.");
    }

    return validators[0].ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
  }

  /// <summary>
  /// Recursively gets all types in a namespace and its nested namespaces.
  /// </summary>
  private static IEnumerable<INamedTypeSymbol> GetAllTypes(INamespaceSymbol namespaceSymbol)
  {
    foreach (INamedTypeSymbol type in namespaceSymbol.GetTypeMembers())
    {
      yield return type;

      // Include nested types
      foreach (INamedTypeSymbol nestedType in GetNestedTypes(type))
      {
        yield return nestedType;
      }
    }

    foreach (INamespaceSymbol childNamespace in namespaceSymbol.GetNamespaceMembers())
    {
      foreach (INamedTypeSymbol type in GetAllTypes(childNamespace))
      {
        yield return type;
      }
    }
  }

  /// <summary>
  /// Recursively gets all nested types within a type.
  /// </summary>
  private static IEnumerable<INamedTypeSymbol> GetNestedTypes(INamedTypeSymbol type)
  {
    foreach (INamedTypeSymbol nestedType in type.GetTypeMembers())
    {
      yield return nestedType;

      foreach (INamedTypeSymbol deeplyNested in GetNestedTypes(nestedType))
      {
        yield return deeplyNested;
      }
    }
  }

  /// <summary>
  /// Creates a service parameter binding, detecting IOptions&lt;T&gt; and extracting configuration key and validator.
  /// </summary>
  /// <param name="param">The parameter symbol.</param>
  /// <param name="typeName">The fully qualified type name.</param>
  /// <param name="compilation">Optional compilation for validator discovery.</param>
  private static ParameterBinding CreateServiceBinding(IParameterSymbol param, string typeName, Compilation? compilation = null)
  {
    string? configurationKey = null;
    string? validatorTypeName = null;

    // Check if it's IOptions<T> and extract the configuration key and validator
    if (TryGetOptionsInfo(param.Type, compilation, out string? key, out string? validator))
    {
      configurationKey = key;
      validatorTypeName = validator;
    }

    return ParameterBinding.FromService(param.Name, typeName, configurationKey: configurationKey, validatorTypeName: validatorTypeName);
  }
}
