#region Purpose
// Decides whether generated startup should build real configuration sources.
#endregion

#region Design
// HasConfiguration is compilation-scoped. Endpoint Handler types are not nested in a
// builder chain, so a match cannot be attributed to one Build() call. One CLI per
// compilation is the common case; every app in a multi-app compilation shares the flag.
// A match is .AddConfiguration() on NuruAppBuilder, or a handler parameter of type
// IConfiguration, IConfigurationRoot, or IOptions<T>. Handler forms are a WithHandler
// delegate (lambda, anonymous method, or method group) and a nested Handler constructor
// or Handle method. IOptionsSnapshot<T> and IOptionsMonitor<T> are not configuration
// setup signals. An unresolved AddConfiguration() call still matches: dropping sources
// while the call is unbound would hide appsettings and command-line overrides.
#endregion

namespace TimeWarp.Nuru.Generators;

using RoslynSyntaxNode = Microsoft.CodeAnalysis.SyntaxNode;
using ParameterSyntax = Microsoft.CodeAnalysis.CSharp.Syntax.ParameterSyntax;
using SyntaxNode = Microsoft.CodeAnalysis.SyntaxNode;

/// <summary>
/// Locates configuration use that requires the full configuration builder.
/// </summary>
internal static class AddConfigurationLocator
{
  private const string MethodName = "AddConfiguration";
  private const string WithHandlerMethodName = "WithHandler";
  private const string HandlerTypeName = "Handler";
  private const string HandleMethodName = "Handle";
  private const string ConfigurationNamespace = "Microsoft.Extensions.Configuration";
  private const string OptionsNamespace = "Microsoft.Extensions.Options";
  private const string BuilderTypeName = "NuruAppBuilder";

  public static bool IsPotentialMatch(RoslynSyntaxNode node)
  {
    if (node is InvocationExpressionSyntax invocation &&
        invocation.Expression is MemberAccessExpressionSyntax memberAccess)
    {
      string name = memberAccess.Name.Identifier.ValueText;
      return name is MethodName or WithHandlerMethodName;
    }

    if (node is ParameterSyntax parameter)
      return ParameterTypeMightBeConfiguration(parameter.Type);

    return false;
  }

  /// <summary>
  /// Returns true when <paramref name="context"/> is a confirmed configuration use.
  /// </summary>
  public static bool UsesConfiguration
  (
    GeneratorSyntaxContext context,
    CancellationToken cancellationToken
  )
  {
    if (context.Node is InvocationExpressionSyntax invocation &&
        invocation.Expression is MemberAccessExpressionSyntax memberAccess)
    {
      string name = memberAccess.Name.Identifier.ValueText;
      if (name == MethodName)
        return IsAddConfigurationCall(context, invocation, cancellationToken);

      if (name == WithHandlerMethodName)
        return WithHandlerUsesConfiguration(context, invocation, cancellationToken);
    }

    if (context.Node is ParameterSyntax parameter)
      return ParameterUsesConfiguration(context, parameter, cancellationToken);

    return false;
  }

  private static bool IsAddConfigurationCall
  (
    GeneratorSyntaxContext context,
    InvocationExpressionSyntax invocation,
    CancellationToken cancellationToken
  )
  {
    SymbolInfo symbolInfo = context.SemanticModel.GetSymbolInfo(invocation, cancellationToken);
    if (IsNuruAddConfiguration(symbolInfo.Symbol))
      return true;

    foreach (ISymbol candidate in symbolInfo.CandidateSymbols)
    {
      if (IsNuruAddConfiguration(candidate))
        return true;
    }

    return symbolInfo.Symbol is null && symbolInfo.CandidateSymbols.IsEmpty;
  }

  private static bool IsNuruAddConfiguration(ISymbol? symbol)
  {
    if (symbol is not IMethodSymbol method || method.Name != MethodName)
      return false;

    for (INamedTypeSymbol? type = method.ContainingType; type is not null; type = type.BaseType)
    {
      if (type.Name == BuilderTypeName)
        return true;
    }

    return false;
  }

  private static bool WithHandlerUsesConfiguration
  (
    GeneratorSyntaxContext context,
    InvocationExpressionSyntax invocation,
    CancellationToken cancellationToken
  )
  {
    if (invocation.ArgumentList.Arguments.Count == 0)
      return false;

    ExpressionSyntax expression = UnwrapExpression(invocation.ArgumentList.Arguments[0].Expression);
    return ExpressionUsesConfiguration(context.SemanticModel, expression, cancellationToken);
  }

  private static bool ExpressionUsesConfiguration
  (
    SemanticModel semanticModel,
    ExpressionSyntax expression,
    CancellationToken cancellationToken
  )
  {
    if (expression is LambdaExpressionSyntax lambda &&
        LambdaUsesConfiguration(semanticModel, lambda, cancellationToken))
    {
      return true;
    }

    if (expression is AnonymousMethodExpressionSyntax anonymous &&
        AnonymousUsesConfiguration(semanticModel, anonymous, cancellationToken))
    {
      return true;
    }

    SymbolInfo symbolInfo = semanticModel.GetSymbolInfo(expression, cancellationToken);
    if (SymbolUsesConfiguration(symbolInfo.Symbol))
      return true;

    foreach (ISymbol candidate in symbolInfo.CandidateSymbols)
    {
      if (SymbolUsesConfiguration(candidate))
        return true;
    }

    return false;
  }

  private static bool SymbolUsesConfiguration(ISymbol? symbol) =>
    symbol is IMethodSymbol method && ParametersUseConfiguration(method);

  private static bool ParametersUseConfiguration(IMethodSymbol method)
  {
    foreach (IParameterSymbol parameter in method.Parameters)
    {
      if (IsConfigurationParameterType(parameter.Type))
        return true;
    }

    return false;
  }

  private static bool LambdaUsesConfiguration
  (
    SemanticModel semanticModel,
    LambdaExpressionSyntax lambda,
    CancellationToken cancellationToken
  )
  {
    if (lambda is SimpleLambdaExpressionSyntax simple)
      return ParameterSymbolUsesConfiguration(semanticModel, simple.Parameter, cancellationToken);

    if (lambda is ParenthesizedLambdaExpressionSyntax parenthesized)
    {
      foreach (ParameterSyntax parameter in parenthesized.ParameterList.Parameters)
      {
        if (ParameterSymbolUsesConfiguration(semanticModel, parameter, cancellationToken))
          return true;
      }
    }

    return false;
  }

  private static bool AnonymousUsesConfiguration
  (
    SemanticModel semanticModel,
    AnonymousMethodExpressionSyntax anonymous,
    CancellationToken cancellationToken
  )
  {
    if (anonymous.ParameterList is null)
      return false;

    foreach (ParameterSyntax parameter in anonymous.ParameterList.Parameters)
    {
      if (ParameterSymbolUsesConfiguration(semanticModel, parameter, cancellationToken))
        return true;
    }

    return false;
  }

  private static bool ParameterSymbolUsesConfiguration
  (
    SemanticModel semanticModel,
    ParameterSyntax parameter,
    CancellationToken cancellationToken
  )
  {
    if (semanticModel.GetDeclaredSymbol(parameter, cancellationToken) is IParameterSymbol parameterSymbol)
      return IsConfigurationParameterType(parameterSymbol.Type);

    return ParameterTypeMightBeConfiguration(parameter.Type);
  }

  private static bool ParameterUsesConfiguration
  (
    GeneratorSyntaxContext context,
    ParameterSyntax parameter,
    CancellationToken cancellationToken
  )
  {
    if (context.SemanticModel.GetDeclaredSymbol(parameter, cancellationToken) is not IParameterSymbol parameterSymbol)
      return ParameterSyntaxIsEndpointHandlerConfiguration(parameter);

    if (!IsConfigurationParameterType(parameterSymbol.Type))
      return false;

    if (parameterSymbol.ContainingSymbol is not IMethodSymbol method)
      return false;

    bool isConstructor = method.MethodKind == MethodKind.Constructor;
    bool isHandleMethod = method.Name == HandleMethodName;
    if (!isConstructor && !isHandleMethod)
      return false;

    return IsEndpointHandlerType(method.ContainingType, cancellationToken);
  }

  private static bool ParameterSyntaxIsEndpointHandlerConfiguration(ParameterSyntax parameter)
  {
    if (!ParameterTypeMightBeConfiguration(parameter.Type))
      return false;

    if (parameter.Parent is not ParameterListSyntax parameterList)
      return false;

    SyntaxNode? owner = parameterList.Parent;
    if (owner is ConstructorDeclarationSyntax constructor)
    {
      owner = constructor.Parent;
    }
    else if (owner is MethodDeclarationSyntax method)
    {
      if (method.Identifier.ValueText != HandleMethodName)
        return false;

      owner = method.Parent;
    }

    if (owner is not ClassDeclarationSyntax classDeclaration)
      return false;

    if (classDeclaration.Identifier.ValueText != HandlerTypeName)
      return false;

    return DeclaresHandlerInterface(classDeclaration);
  }

  private static bool IsEndpointHandlerType(INamedTypeSymbol type, CancellationToken cancellationToken)
  {
    if (type.Name != HandlerTypeName)
      return false;

    foreach (INamedTypeSymbol iface in type.AllInterfaces)
    {
      if (IsHandlerInterfaceName(iface.Name))
        return true;
    }

    foreach (SyntaxReference syntaxReference in type.DeclaringSyntaxReferences)
    {
      if (syntaxReference.GetSyntax(cancellationToken) is ClassDeclarationSyntax classDeclaration &&
          DeclaresHandlerInterface(classDeclaration))
      {
        return true;
      }
    }

    return false;
  }

  private static bool DeclaresHandlerInterface(ClassDeclarationSyntax classDeclaration)
  {
    if (classDeclaration.BaseList is null)
      return false;

    foreach (BaseTypeSyntax baseType in classDeclaration.BaseList.Types)
    {
      if (IsHandlerInterfaceName(SimpleTypeName(baseType.Type)))
        return true;
    }

    return false;
  }

  private static bool IsHandlerInterfaceName(string? name) =>
    name is "ICommandHandler" or "IQueryHandler" or "IIdempotentCommandHandler";

  private static bool IsConfigurationParameterType(ITypeSymbol type)
  {
    if (type is INamedTypeSymbol named && named.IsGenericType)
    {
      INamedTypeSymbol definition = named.OriginalDefinition;
      if (definition.Arity == 1 &&
          definition.Name == "IOptions" &&
          (IsNamespace(definition, OptionsNamespace) || definition.TypeKind == TypeKind.Error))
      {
        return true;
      }
    }

    if (type.Name is "IConfiguration" or "IConfigurationRoot" &&
        (IsNamespace(type, ConfigurationNamespace) || type.TypeKind == TypeKind.Error))
    {
      return true;
    }

    return false;
  }

  private static bool IsNamespace(INamespaceOrTypeSymbol symbol, string namespaceName) =>
    symbol.ContainingNamespace?.ToDisplayString() == namespaceName;

  private static bool ParameterTypeMightBeConfiguration(TypeSyntax? typeSyntax)
  {
    string? name = SimpleTypeName(typeSyntax);
    return name is "IConfiguration" or "IConfigurationRoot" or "IOptions";
  }

  private static string? SimpleTypeName(TypeSyntax? typeSyntax)
  {
    while (typeSyntax is NullableTypeSyntax nullable)
      typeSyntax = nullable.ElementType;

    return typeSyntax switch
    {
      IdentifierNameSyntax identifier => identifier.Identifier.ValueText,
      GenericNameSyntax generic => generic.Identifier.ValueText,
      QualifiedNameSyntax qualified => SimpleTypeName(qualified.Right),
      AliasQualifiedNameSyntax alias => SimpleTypeName(alias.Name),
      _ => null
    };
  }

  private static ExpressionSyntax UnwrapExpression(ExpressionSyntax expression)
  {
    while (expression is ParenthesizedExpressionSyntax parenthesized)
      expression = parenthesized.Expression;

    return expression;
  }
}
