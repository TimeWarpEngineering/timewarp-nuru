#region Purpose
// Behavior, terminal, telemetry, and type converter dispatch handlers for the DSL interpreter.
#endregion

namespace TimeWarp.Nuru.Generators;

public sealed partial class DslInterpreter
{
  /// <summary>
  /// Dispatches AddBehavior() call to IIrAppBuilder.
  /// Extracts the behavior type, constructor dependencies, nested State class, and filter type.
  /// </summary>
  private object? DispatchAddBehavior(InvocationExpressionSyntax invocation, object? receiver)
  {
    if (receiver is not IIrAppBuilder appBuilder)
    {
      throw new InvalidOperationException(
        $"AddBehavior() must be called on an app builder. Location: {invocation.GetLocation().GetLineSpan()}");
    }

    // Extract the typeof() argument
    ArgumentListSyntax? args = invocation.ArgumentList;
    if (args is null || args.Arguments.Count == 0)
    {
      throw new InvalidOperationException(
        $"AddBehavior() requires a type argument. Location: {invocation.GetLocation().GetLineSpan()}");
    }

    ExpressionSyntax argExpression = args.Arguments[0].Expression;
    if (argExpression is TypeOfExpressionSyntax typeofExpr)
    {
      ITypeSymbol? behaviorType = TypeSyntaxResolver.ResolveType(SemanticModel, typeofExpr.Type, CancellationToken);
      if (behaviorType is INamedTypeSymbol namedType)
      {
        string typeName = namedType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

        // Extract constructor dependencies
        ImmutableArray<ParameterBinding> constructorDeps = ExtractBehaviorConstructorDependencies(namedType);

        // Look for nested State class that inherits from BehaviorContext
        string? stateTypeName = FindNestedStateClass(namedType);

        // Check for INuruBehavior<TFilter> implementation
        string? filterTypeName = ExtractBehaviorFilterType(namedType, invocation);

        BehaviorDefinition behavior = filterTypeName is not null
          ? BehaviorDefinition.ForFilter(
              typeName,
              filterTypeName,
              order: 0,
              constructorDependencies: constructorDeps,
              stateTypeName: stateTypeName)
          : BehaviorDefinition.ForAll(
              typeName,
              order: 0,
              constructorDependencies: constructorDeps,
              stateTypeName: stateTypeName);

        return appBuilder.AddBehavior(behavior);
      }
    }

    // If we can't extract the type, just return the builder unchanged
    return appBuilder;
  }

  /// <summary>
  /// Extracts the filter type from INuruBehavior&lt;TFilter&gt; implementation.
  /// Returns null if the behavior implements INuruBehavior (non-generic) or no behavior interface.
  /// Throws if the behavior implements multiple INuruBehavior&lt;T&gt; interfaces.
  /// </summary>
  private static string? ExtractBehaviorFilterType(INamedTypeSymbol behaviorType, InvocationExpressionSyntax invocation)
  {
    List<string> filterTypes = [];

    foreach (INamedTypeSymbol iface in behaviorType.AllInterfaces)
    {
      // Check for INuruBehavior<TFilter>
      if (iface.IsGenericType &&
          iface.Name == "INuruBehavior" &&
          iface.ContainingNamespace.ToDisplayString() == "TimeWarp.Nuru" &&
          iface.TypeArguments.Length == 1)
      {
        string filterTypeName = iface.TypeArguments[0].ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        filterTypes.Add(filterTypeName);
      }
    }

    // Error if multiple INuruBehavior<T> interfaces
    if (filterTypes.Count > 1)
    {
      throw new InvalidOperationException(
        $"Behavior '{behaviorType.Name}' implements multiple INuruBehavior<T> interfaces ({string.Join(", ", filterTypes)}). " +
        $"A behavior may only implement one filtered interface. Location: {invocation.GetLocation().GetLineSpan()}");
    }

    return filterTypes.Count == 1 ? filterTypes[0] : null;
  }

  /// <summary>
  /// Extracts constructor dependencies from a behavior type.
  /// </summary>
  private static ImmutableArray<ParameterBinding> ExtractBehaviorConstructorDependencies(INamedTypeSymbol behaviorType)
  {
    // Find the primary constructor or first non-implicit constructor
    IMethodSymbol? constructor = behaviorType.Constructors
      .FirstOrDefault(c => !c.IsImplicitlyDeclared && c.DeclaredAccessibility == Accessibility.Public);

    if (constructor is null || constructor.Parameters.Length == 0)
      return [];

    ImmutableArray<ParameterBinding>.Builder deps = ImmutableArray.CreateBuilder<ParameterBinding>();

    foreach (IParameterSymbol param in constructor.Parameters)
    {
      string paramTypeName = param.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
      deps.Add(ParameterBinding.FromService(param.Name, paramTypeName));
    }

    return deps.ToImmutable();
  }

  /// <summary>
  /// Finds a nested State class that inherits from BehaviorContext.
  /// </summary>
  private static string? FindNestedStateClass(INamedTypeSymbol behaviorType)
  {
    // Look for a nested type named "State"
    INamedTypeSymbol? stateType = behaviorType.GetTypeMembers("State").FirstOrDefault();

    if (stateType is null)
      return null;

    // Verify it inherits from BehaviorContext
    INamedTypeSymbol? baseType = stateType.BaseType;
    while (baseType is not null)
    {
      if (baseType.Name == "BehaviorContext" &&
          baseType.ContainingNamespace.ToDisplayString() == "TimeWarp.Nuru")
      {
        return stateType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
      }

      baseType = baseType.BaseType;
    }

    // State class exists but doesn't inherit from BehaviorContext - ignore it
    return null;
  }

  /// <summary>
  /// Dispatches UseTerminal() call to IIrAppBuilder.
  /// This is a no-op - terminal is runtime only.
  /// </summary>
  private static object? DispatchUseTerminal(object? receiver)
  {
    if (receiver is not IIrAppBuilder appBuilder)
    {
      throw new InvalidOperationException("UseTerminal() must be called on an app builder.");
    }

    return appBuilder.UseTerminal();
  }

  /// <summary>
  /// Dispatches UseTelemetry() call to IIrAppBuilder.
  /// Extracts options from the lambda when the Action overload is used.
  /// </summary>
  private object? DispatchUseTelemetry(InvocationExpressionSyntax invocation, object? receiver)
  {
    if (receiver is not IIrAppBuilder appBuilder)
    {
      throw new InvalidOperationException("UseTelemetry() must be called on an app builder.");
    }

    ArgumentListSyntax? args = invocation.ArgumentList;
    if (args?.Arguments.Count > 0)
    {
      TelemetryModel? customOptions = TelemetryOptionsExtractor.Extract(invocation, SemanticModel, CancellationToken);
      if (customOptions is not null)
      {
        return appBuilder.UseTelemetry(customOptions);
      }
    }

    return appBuilder.UseTelemetry();
  }

  /// <summary>
  /// Dispatches UseMicrosoftDependencyInjection() call to IIrAppBuilder.
  /// Enables runtime DI instead of source-generated static instantiation.
  /// </summary>
  private static object? DispatchUseMicrosoftDependencyInjection(object? receiver)
  {
    if (receiver is not IIrAppBuilder appBuilder)
    {
      throw new InvalidOperationException("UseMicrosoftDependencyInjection() must be called on an app builder.");
    }

    return appBuilder.UseMicrosoftDependencyInjection();
  }

  /// <summary>
  /// Dispatches UseDirectInvoke() to IIrAppBuilder.
  /// Nested handlers are invoked directly and the mediator container is not required for those apps.
  /// </summary>
  private static object? DispatchUseDirectInvoke(object? receiver)
  {
    if (receiver is not IIrAppBuilder appBuilder)
      throw new InvalidOperationException("UseDirectInvoke() must be called on an app builder.");

    return appBuilder.UseDirectInvoke();
  }

  /// <summary>
  /// Dispatches UseVerbatimRendererOutput() to IIrAppBuilder.
  /// Renderer text is written with Write, so no newline is added.
  /// </summary>
  private static object? DispatchUseVerbatimRendererOutput(object? receiver)
  {
    if (receiver is not IIrAppBuilder appBuilder)
      throw new InvalidOperationException("UseVerbatimRendererOutput() must be called on an app builder.");

    return appBuilder.UseVerbatimRendererOutput();
  }

  /// <summary>
  /// Dispatches UseProblemRendererForErrors() to IIrAppBuilder.
  /// CLI binding errors go through the app's problem renderer.
  /// </summary>
  private static object? DispatchUseProblemRendererForErrors(object? receiver)
  {
    if (receiver is not IIrAppBuilder appBuilder)
      throw new InvalidOperationException("UseProblemRendererForErrors() must be called on an app builder.");

    return appBuilder.UseProblemRendererForErrors();
  }

  /// <summary>
  /// Dispatches EnableCompletion() call to IIrAppBuilder.
  /// Marks the app as having shell completion enabled.
  /// </summary>
  private static object? DispatchEnableCompletion(object? receiver)
  {
    if (receiver is not IIrAppBuilder appBuilder)
    {
      throw new InvalidOperationException("EnableCompletion() must be called on an app builder.");
    }

    return appBuilder.EnableCompletion();
  }

  /// <summary>
  /// Dispatches AddTypeConverter() call to IIrAppBuilder.
  /// Extracts converter type information for code generation.
  /// Uses semantic model to resolve generic type arguments (e.g., EnumTypeConverter&lt;Environment&gt;).
  /// </summary>
  private object? DispatchAddTypeConverter(InvocationExpressionSyntax invocation, object? receiver)
  {
    if (receiver is not IIrAppBuilder appBuilder)
    {
      throw new InvalidOperationException("AddTypeConverter() must be called on an app builder.");
    }

    // Extract converter info from: AddTypeConverter(new EmailAddressConverter())
    // or: AddTypeConverter(new EnumTypeConverter<Environment>())
    ArgumentSyntax? arg = invocation.ArgumentList.Arguments.FirstOrDefault();
    if (arg?.Expression is ObjectCreationExpressionSyntax objectCreation)
    {
      // Fully qualified name: GetSymbolInfo, then GetTypeInfo; TypeKind.Error is unresolved.
      string converterTypeName;
      string targetTypeName = "";

      INamedTypeSymbol? resolvedType =
        TypeSyntaxResolver.ResolveType(SemanticModel, objectCreation.Type, CancellationToken) as INamedTypeSymbol;

      if (resolvedType is not null)
      {
        // Use fully qualified format to avoid ambiguity with System types
        converterTypeName = resolvedType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

        if (resolvedType.IsGenericType && resolvedType.TypeArguments.Length > 0)
        {
          // Generic converter like EnumTypeConverter<Environment>
          // Extract the first type argument as the target type
          ITypeSymbol targetType = resolvedType.TypeArguments[0];
          targetTypeName = targetType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        }
      }
      else
      {
        // Both GetSymbolInfo and GetTypeInfo failed to resolve a type: this is a genuine
        // generator error, not something to silently emit an unqualified/broken name for.
        CollectedDiagnostics.Add(Diagnostic.Create(
          DiagnosticDescriptors.UnresolvedTypeConverterType,
          objectCreation.Type.GetLocation()));
        return appBuilder;
      }

      CustomConverterDefinition converter = new(
        ConverterTypeName: converterTypeName,
        TargetTypeName: targetTypeName,
        ConstraintAlias: null);

      return appBuilder.AddTypeConverter(converter);
    }

    // If we can't extract the type, log a warning but continue
    // The route will fail to match custom type constraints
    return appBuilder;
  }
}
