// Semantic DSL interpreter that walks blocks statement-by-statement.
//
// Instead of pattern-matching syntax, this interpreter "executes" the DSL
// by dispatching method calls to corresponding IR builder methods.
//
// Phase 1a: Block-based processing with variable tracking.
// - Takes BlockSyntax (a method body) as input
// - Returns IReadOnlyList<AppModel> (supports multiple apps per block)
// - Tracks variable assignments in VariableState dictionary
// - Processes statements one by one
//
// Key design:
// - Uses SemanticModel for accurate type resolution
// - Evaluates expressions recursively with variable resolution
// - Dispatches to IR builders based on method name
// - Uses marker interfaces (IIrRouteSource, IIrAppBuilder, IIrGroupBuilder, IIrRouteBuilder)
//   for polymorphic dispatch without explicit type enumeration
// - Fails fast on unrecognized DSL methods

namespace TimeWarp.Nuru.Generators;

using RoslynSyntaxNode = Microsoft.CodeAnalysis.SyntaxNode;

/// <summary>
/// Interprets DSL code semantically by walking statements and dispatching to IR builders.
/// </summary>
/// <remarks>
/// CA1859 is suppressed because this class uses polymorphic dispatch where methods
/// intentionally return object? to support different builder types in a fluent chain.
/// </remarks>
[System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1859:Use concrete types when possible for improved performance", Justification = "Polymorphic dispatch pattern requires object? return types")]
public sealed class DslInterpreter
{
  private readonly SemanticModel SemanticModel;
  private readonly CancellationToken CancellationToken;

  // Per-interpretation state
  private Dictionary<ISymbol, object?> VariableState = null!;
  private List<IrAppBuilder> BuiltApps = null!;

  /// <summary>
  /// Creates a new DSL interpreter.
  /// </summary>
  /// <param name="semanticModel">Semantic model for type resolution.</param>
  /// <param name="cancellationToken">Cancellation token.</param>
  public DslInterpreter(SemanticModel semanticModel, CancellationToken cancellationToken)
  {
    SemanticModel = semanticModel;
    CancellationToken = cancellationToken;
  }

  /// <summary>
  /// Interprets a block of statements to produce AppModels.
  /// </summary>
  /// <param name="block">The block containing DSL statements.</param>
  /// <returns>List of interpreted AppModels (one per built app).</returns>
  public IReadOnlyList<AppModel> Interpret(BlockSyntax block)
  {
    ArgumentNullException.ThrowIfNull(block);

    // Fresh state per interpretation
    VariableState = new Dictionary<ISymbol, object?>(SymbolEqualityComparer.Default);
    BuiltApps = [];

    ProcessBlock(block);

    // Finalize all built apps
    return BuiltApps.ConvertAll(app => app.FinalizeModel());
  }

  /// <summary>
  /// Interprets top-level statements from a CompilationUnit to produce AppModels.
  /// Top-level statements are GlobalStatementSyntax nodes directly under CompilationUnitSyntax.
  /// </summary>
  /// <param name="compilationUnit">The compilation unit containing top-level statements.</param>
  /// <returns>List of interpreted AppModels (one per built app).</returns>
  public IReadOnlyList<AppModel> InterpretTopLevelStatements(CompilationUnitSyntax compilationUnit)
  {
    ArgumentNullException.ThrowIfNull(compilationUnit);

    // Fresh state per interpretation
    VariableState = new Dictionary<ISymbol, object?>(SymbolEqualityComparer.Default);
    BuiltApps = [];

    // Process each GlobalStatementSyntax member
    foreach (MemberDeclarationSyntax member in compilationUnit.Members)
    {
      CancellationToken.ThrowIfCancellationRequested();

      if (member is GlobalStatementSyntax globalStatement)
      {
        ProcessStatement(globalStatement.Statement);
      }
    }

    // Finalize all built apps
    return BuiltApps.ConvertAll(app => app.FinalizeModel());
  }

  // ═══════════════════════════════════════════════════════════════════════════════
  // BLOCK AND STATEMENT PROCESSING
  // ═══════════════════════════════════════════════════════════════════════════════

  /// <summary>
  /// Processes all statements in a block.
  /// </summary>
  private void ProcessBlock(BlockSyntax block)
  {
    foreach (StatementSyntax statement in block.Statements)
    {
      CancellationToken.ThrowIfCancellationRequested();
      ProcessStatement(statement);
    }
  }

  /// <summary>
  /// Processes a single statement based on its type.
  /// </summary>
  private void ProcessStatement(StatementSyntax statement)
  {
    switch (statement)
    {
      case LocalDeclarationStatementSyntax localDecl:
        ProcessLocalDeclaration(localDecl);
        break;

      case ExpressionStatementSyntax exprStmt:
        ProcessExpressionStatement(exprStmt);
        break;

      case ReturnStatementSyntax returnStmt:
        ProcessReturnStatement(returnStmt);
        break;

      // Ignore other statement types (if, etc.)
      default:
        break;
    }
  }

  /// <summary>
  /// Processes a return statement (e.g., "return await app.RunAsync(...)").
  /// Bug #298: This was missing, causing "return await" pattern to not be intercepted.
  /// </summary>
  private void ProcessReturnStatement(ReturnStatementSyntax returnStmt)
  {
    if (returnStmt.Expression is not null)
    {
      // Evaluate the return expression for its side effects (e.g., RunAsync interception)
      EvaluateExpression(returnStmt.Expression);
    }
  }

  /// <summary>
  /// Processes a local variable declaration (e.g., "var app = NuruApp.CreateBuilder(...)...").
  /// </summary>
  private void ProcessLocalDeclaration(LocalDeclarationStatementSyntax localDecl)
  {
    foreach (VariableDeclaratorSyntax declarator in localDecl.Declaration.Variables)
    {
      if (declarator.Initializer?.Value is null)
        continue;

      // Get the symbol for this variable
      ISymbol? symbol = SemanticModel.GetDeclaredSymbol(declarator);
      if (symbol is null)
        continue;

      // Evaluate the initializer expression
      object? value = EvaluateExpression(declarator.Initializer.Value);

      // Store in variable state
      VariableState[symbol] = value;

      // If it's an app builder (or built app marker), set the variable name
      IrAppBuilder? appBuilder = value switch
      {
        IrAppBuilder builder => builder,
        BuiltAppMarker marker => (IrAppBuilder)marker.Builder,
        _ => null
      };

      appBuilder?.SetVariableName(declarator.Identifier.Text);
    }
  }

  /// <summary>
  /// Processes an expression statement (e.g., "await app.RunAsync(...)").
  /// </summary>
  private void ProcessExpressionStatement(ExpressionStatementSyntax exprStmt)
  {
    // Evaluate the expression for its side effects
    EvaluateExpression(exprStmt.Expression);
  }

  // ═══════════════════════════════════════════════════════════════════════════════
  // EXPRESSION EVALUATION
  // ═══════════════════════════════════════════════════════════════════════════════

  /// <summary>
  /// Evaluates an expression, returning its IR representation.
  /// </summary>
  private object? EvaluateExpression(ExpressionSyntax expression)
  {
    return expression switch
    {
      InvocationExpressionSyntax invocation => EvaluateInvocation(invocation),
      MemberAccessExpressionSyntax memberAccess => EvaluateMemberAccess(memberAccess),
      IdentifierNameSyntax identifier => ResolveIdentifier(identifier),
      AwaitExpressionSyntax awaitExpr => EvaluateExpression(awaitExpr.Expression),
      _ => null // Literals, etc. - not relevant for DSL interpretation
    };
  }

  /// <summary>
  /// Evaluates an invocation expression.
  /// </summary>
  private object? EvaluateInvocation(InvocationExpressionSyntax invocation)
  {
    // First, evaluate what we're calling the method on
    object? receiver = invocation.Expression switch
    {
      MemberAccessExpressionSyntax memberAccess => EvaluateExpression(memberAccess.Expression),
      _ => null // Static call
    };

    // Get method name
    string? methodName = GetMethodName(invocation);
    if (methodName is null)
      return null;

    // Dispatch based on method name
    return DispatchMethodCall(invocation, receiver, methodName);
  }

  /// <summary>
  /// Evaluates a member access expression (for variable resolution).
  /// </summary>
  private object? EvaluateMemberAccess(MemberAccessExpressionSyntax memberAccess)
  {
    // For member access, we need to evaluate the receiver first
    // This is used for things like "app.RunAsync(...)" where we need to resolve "app"
    return EvaluateExpression(memberAccess.Expression);
  }

  /// <summary>
  /// Resolves an identifier to its value from VariableState.
  /// </summary>
  private object? ResolveIdentifier(IdentifierNameSyntax identifier)
  {
    ISymbol? symbol = SemanticModel.GetSymbolInfo(identifier).Symbol;
    if (symbol is null)
      return null;

    return VariableState.TryGetValue(symbol, out object? value) ? value : null;
  }

  // ═══════════════════════════════════════════════════════════════════════════════
  // METHOD DISPATCH
  // ═══════════════════════════════════════════════════════════════════════════════

  /// <summary>
  /// Dispatches a method call to the appropriate IR builder method.
  /// </summary>
  private object? DispatchMethodCall(
    InvocationExpressionSyntax invocation,
    object? receiver,
    string methodName)
  {
    return methodName switch
    {
      "CreateBuilder" => CreateNewAppBuilder(),

      "Map" => DispatchMap(invocation, receiver),

      "WithGroupPrefix" => DispatchWithGroupPrefix(invocation, receiver),

      "WithHandler" => DispatchWithHandler(invocation, receiver),

      "WithDescription" => DispatchWithDescription(invocation, receiver),

      "AsQuery" => DispatchAsQuery(receiver),

      "AsCommand" => DispatchAsCommand(receiver),

      "AsIdempotentCommand" => DispatchAsIdempotentCommand(receiver),

      "Done" => DispatchDone(receiver),

      "Build" => DispatchBuild(receiver),

      "WithName" => DispatchWithName(invocation, receiver),

      "WithAiPrompt" => DispatchWithAiPrompt(invocation, receiver),

      "AddHelp" => DispatchAddHelp(invocation, receiver),

      "AddRepl" => DispatchAddRepl(invocation, receiver),

      "AddConfiguration" => DispatchAddConfiguration(receiver),

      "AddCheckUpdatesRoute" => DispatchAddCheckUpdatesRoute(receiver),

      "ConfigureServices" => DispatchConfigureServices(invocation, receiver),

      "AddBehavior" => DispatchAddBehavior(invocation, receiver),

      "UseTerminal" => DispatchUseTerminal(receiver),

      "AddTypeConverter" => DispatchAddTypeConverter(invocation, receiver),

      "WithAlias" => DispatchWithAlias(invocation, receiver),

      "Implements" => DispatchImplements(invocation, receiver),

      "RunAsync" => DispatchRunAsyncCall(invocation, receiver),

      _ => HandleNonDslMethod(invocation, receiver, methodName)
    };
  }

  /// <summary>
  /// Creates a new IR app builder.
  /// </summary>
  private static IrAppBuilder CreateNewAppBuilder() => new();

  /// <summary>
  /// Handles method calls that are not part of the DSL.
  /// For builder receivers, throws an error (unknown DSL method).
  /// For non-builder receivers (Console.WriteLine, etc.), returns null (ignore).
  /// </summary>
  private static object? HandleNonDslMethod(
    InvocationExpressionSyntax invocation,
    object? receiver,
    string methodName)
  {
    // If receiver is a builder type, fail fast - unknown DSL method
    if (receiver is IIrRouteSource or IIrRouteBuilder or IIrGroupBuilder or IIrAppBuilder)
    {
      throw new InvalidOperationException(
        $"Unrecognized DSL method: {methodName}. " +
        $"Location: {invocation.GetLocation().GetLineSpan()}");
    }

    // Non-DSL method call (Console.WriteLine, etc.) - ignore
    return null;
  }

  // ═══════════════════════════════════════════════════════════════════════════════
  // DISPATCH METHODS - Using marker interfaces for polymorphic dispatch
  // ═══════════════════════════════════════════════════════════════════════════════

  /// <summary>
  /// Dispatches Map() call to any IIrRouteSource (app or group builder).
  /// </summary>
  private static object? DispatchMap(InvocationExpressionSyntax invocation, object? receiver)
  {
    if (receiver is not IIrRouteSource source)
    {
      throw new InvalidOperationException(
        $"Map() must be called on an app or group builder. Location: {invocation.GetLocation().GetLineSpan()}");
    }

    string? pattern = ExtractStringArgument(invocation);
    if (pattern is null)
    {
      throw new InvalidOperationException(
        $"Map() requires a pattern string. Location: {invocation.GetLocation().GetLineSpan()}");
    }

    return source.Map(pattern);
  }

  /// <summary>
  /// Dispatches WithGroupPrefix() call to any IIrRouteSource (app or group builder).
  /// </summary>
  private static object? DispatchWithGroupPrefix(InvocationExpressionSyntax invocation, object? receiver)
  {
    if (receiver is not IIrRouteSource source)
    {
      throw new InvalidOperationException(
        $"WithGroupPrefix() must be called on an app or group builder. Location: {invocation.GetLocation().GetLineSpan()}");
    }

    string? prefix = ExtractStringArgument(invocation);
    if (prefix is null)
    {
      throw new InvalidOperationException(
        $"WithGroupPrefix() requires a prefix string. Location: {invocation.GetLocation().GetLineSpan()}");
    }

    return source.WithGroupPrefix(prefix);
  }

  /// <summary>
  /// Dispatches WithHandler() call to IIrRouteBuilder.
  /// </summary>
  private object? DispatchWithHandler(InvocationExpressionSyntax invocation, object? receiver)
  {
    if (receiver is not IIrRouteBuilder routeBuilder)
    {
      throw new InvalidOperationException(
        $"WithHandler() must be called on a route builder. Location: {invocation.GetLocation().GetLineSpan()}");
    }

    HandlerDefinition? handler = HandlerExtractor.Extract(invocation, SemanticModel, CancellationToken);
    if (handler is null)
    {
      throw new InvalidOperationException(
        $"Could not extract handler from WithHandler(). Location: {invocation.GetLocation().GetLineSpan()}");
    }

    return routeBuilder.WithHandler(handler);
  }

  /// <summary>
  /// Dispatches WithDescription() call to IIrRouteBuilder or IIrAppBuilder.
  /// </summary>
  private static object? DispatchWithDescription(InvocationExpressionSyntax invocation, object? receiver)
  {
    string? description = ExtractStringArgument(invocation);

    return receiver switch
    {
      IIrRouteBuilder routeBuilder => routeBuilder.WithDescription(description ?? ""),
      IIrAppBuilder appBuilder => appBuilder.WithDescription(description ?? ""),
      _ => throw new InvalidOperationException(
        $"WithDescription() must be called on an app or route builder. Location: {invocation.GetLocation().GetLineSpan()}")
    };
  }

  /// <summary>
  /// Dispatches WithName() call to IIrAppBuilder.
  /// </summary>
  private static object? DispatchWithName(InvocationExpressionSyntax invocation, object? receiver)
  {
    if (receiver is not IIrAppBuilder appBuilder)
    {
      throw new InvalidOperationException(
        $"WithName() must be called on an app builder. Location: {invocation.GetLocation().GetLineSpan()}");
    }

    string? name = ExtractStringArgument(invocation);
    return appBuilder.WithName(name ?? "");
  }

  /// <summary>
  /// Dispatches WithAiPrompt() call to IIrAppBuilder.
  /// </summary>
  private static object? DispatchWithAiPrompt(InvocationExpressionSyntax invocation, object? receiver)
  {
    if (receiver is not IIrAppBuilder appBuilder)
    {
      throw new InvalidOperationException(
        $"WithAiPrompt() must be called on an app builder. Location: {invocation.GetLocation().GetLineSpan()}");
    }

    string? aiPrompt = ExtractStringArgument(invocation);
    return appBuilder.WithAiPrompt(aiPrompt ?? "");
  }

  /// <summary>
  /// Dispatches AddHelp() call to IIrAppBuilder.
  /// For now, we just enable help with defaults. Options extraction is Phase 5+.
  /// </summary>
  private static object? DispatchAddHelp(InvocationExpressionSyntax invocation, object? receiver)
  {
    if (receiver is not IIrAppBuilder appBuilder)
    {
      throw new InvalidOperationException(
        $"AddHelp() must be called on an app builder. Location: {invocation.GetLocation().GetLineSpan()}");
    }

    // For now, just enable help with defaults
    // TODO: Phase 5+ - extract options from lambda if present
    return appBuilder.AddHelp();
  }

  /// <summary>
  /// Dispatches AddRepl() call to IIrAppBuilder.
  /// For now, we just enable REPL with defaults. Options extraction is Phase 5+.
  /// </summary>
  private static object? DispatchAddRepl(InvocationExpressionSyntax invocation, object? receiver)
  {
    if (receiver is not IIrAppBuilder appBuilder)
    {
      throw new InvalidOperationException(
        $"AddRepl() must be called on an app builder. Location: {invocation.GetLocation().GetLineSpan()}");
    }

    // For now, just enable REPL with defaults
    // TODO: Phase 5+ - extract options from lambda if present
    return appBuilder.AddRepl();
  }

  /// <summary>
  /// Dispatches AddConfiguration() call to IIrAppBuilder.
  /// </summary>
  private static object? DispatchAddConfiguration(object? receiver)
  {
    if (receiver is not IIrAppBuilder appBuilder)
    {
      throw new InvalidOperationException("AddConfiguration() must be called on an app builder.");
    }

    return appBuilder.AddConfiguration();
  }

  /// <summary>
  /// Dispatches AddCheckUpdatesRoute() call to IIrAppBuilder.
  /// </summary>
  private static object? DispatchAddCheckUpdatesRoute(object? receiver)
  {
    if (receiver is not IIrAppBuilder appBuilder)
    {
      throw new InvalidOperationException("AddCheckUpdatesRoute() must be called on an app builder.");
    }

    return appBuilder.AddCheckUpdatesRoute();
  }

  /// <summary>
  /// Dispatches ConfigureServices() call to IIrAppBuilder.
  /// Extracts service registrations from the lambda and adds them to the IR model.
  /// </summary>
  private object? DispatchConfigureServices(InvocationExpressionSyntax invocation, object? receiver)
  {
    if (receiver is not IIrAppBuilder appBuilder)
    {
      throw new InvalidOperationException(
        $"ConfigureServices() must be called on an app builder. Location: {invocation.GetLocation().GetLineSpan()}");
    }

    // Extract service registrations from the ConfigureServices lambda
    ImmutableArray<ServiceDefinition> services = ServiceExtractor.Extract(
      invocation,
      SemanticModel,
      CancellationToken);

    // Add each service to the IR model
    foreach (ServiceDefinition service in services)
    {
      appBuilder.AddService(service);
    }

    return appBuilder;
  }

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
      TypeInfo typeInfo = SemanticModel.GetTypeInfo(typeofExpr.Type);
      ITypeSymbol? behaviorType = typeInfo.Type;
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
  /// Dispatches AddTypeConverter() call to IIrAppBuilder.
  /// Extracts converter type information for code generation.
  /// </summary>
  private static object? DispatchAddTypeConverter(InvocationExpressionSyntax invocation, object? receiver)
  {
    if (receiver is not IIrAppBuilder appBuilder)
    {
      throw new InvalidOperationException("AddTypeConverter() must be called on an app builder.");
    }

    // Extract converter info from: AddTypeConverter(new EmailAddressConverter())
    ArgumentSyntax? arg = invocation.ArgumentList.Arguments.FirstOrDefault();
    if (arg?.Expression is ObjectCreationExpressionSyntax objectCreation)
    {
      // Get the converter type name
      string converterTypeName = objectCreation.Type.ToString();

      // For now, we'll extract the target type and alias at emit time
      // by analyzing the converter class. Store just the converter type name.
      // The emitter will generate: new ConverterTypeName() and call TryConvert.
      CustomConverterDefinition converter = new(
        ConverterTypeName: converterTypeName,
        TargetTypeName: "", // Will be resolved from handler parameter type
        ConstraintAlias: null); // Not extractable at syntax level

      return appBuilder.AddTypeConverter(converter);
    }

    // If we can't extract the type, log a warning but continue
    // The route will fail to match custom type constraints
    return appBuilder;
  }

  /// <summary>
  /// Dispatches WithAlias() call to IIrRouteBuilder.
  /// </summary>
  private static object? DispatchWithAlias(InvocationExpressionSyntax invocation, object? receiver)
  {
    if (receiver is not IIrRouteBuilder routeBuilder)
    {
      throw new InvalidOperationException(
        $"WithAlias() must be called on a route builder. Location: {invocation.GetLocation().GetLineSpan()}");
    }

    string? alias = ExtractStringArgument(invocation);
    if (alias is null)
    {
      throw new InvalidOperationException(
        $"WithAlias() requires an alias string. Location: {invocation.GetLocation().GetLineSpan()}");
    }

    return routeBuilder.WithAlias(alias);
  }

  /// <summary>
  /// Dispatches Implements&lt;T&gt;() call to IIrRouteBuilder.
  /// Extracts the interface type and property assignments from the expression.
  /// </summary>
  private object? DispatchImplements(InvocationExpressionSyntax invocation, object? receiver)
  {
    if (receiver is not IIrRouteBuilder routeBuilder)
    {
      throw new InvalidOperationException(
        $"Implements<T>() must be called on a route builder. Location: {invocation.GetLocation().GetLineSpan()}");
    }

    InterfaceImplementationDefinition? implementation = ImplementsExtractor.Extract(invocation, SemanticModel);
    if (implementation is null)
    {
      throw new InvalidOperationException(
        $"Could not extract interface implementation from Implements<T>(). Location: {invocation.GetLocation().GetLineSpan()}");
    }

    return routeBuilder.AddImplementation(implementation);
  }

  /// <summary>
  /// Dispatches AsQuery() call to IIrRouteBuilder.
  /// </summary>
  private static object? DispatchAsQuery(object? receiver)
  {
    if (receiver is not IIrRouteBuilder routeBuilder)
    {
      throw new InvalidOperationException("AsQuery() must be called on a route builder.");
    }

    return routeBuilder.AsQuery();
  }

  /// <summary>
  /// Dispatches AsCommand() call to IIrRouteBuilder.
  /// </summary>
  private static object? DispatchAsCommand(object? receiver)
  {
    if (receiver is not IIrRouteBuilder routeBuilder)
    {
      throw new InvalidOperationException("AsCommand() must be called on a route builder.");
    }

    return routeBuilder.AsCommand();
  }

  /// <summary>
  /// Dispatches AsIdempotentCommand() call to IIrRouteBuilder.
  /// </summary>
  private static object? DispatchAsIdempotentCommand(object? receiver)
  {
    if (receiver is not IIrRouteBuilder routeBuilder)
    {
      throw new InvalidOperationException("AsIdempotentCommand() must be called on a route builder.");
    }

    return routeBuilder.AsIdempotentCommand();
  }

  /// <summary>
  /// Dispatches Done() call to IIrRouteBuilder or IIrGroupBuilder.
  /// </summary>
  private static object? DispatchDone(object? receiver)
  {
    return receiver switch
    {
      IIrRouteBuilder routeBuilder => routeBuilder.Done(),
      IIrGroupBuilder groupBuilder => groupBuilder.Done(),
      _ => throw new InvalidOperationException("Done() must be called on a route or group builder.")
    };
  }

  /// <summary>
  /// Dispatches Build() call to IIrAppBuilder.
  /// </summary>
  private object? DispatchBuild(object? receiver)
  {
    if (receiver is not IrAppBuilder appBuilder)
    {
      throw new InvalidOperationException("Build() must be called on an app builder.");
    }

    // Mark as built
    appBuilder.Build();

    // Track this built app for finalization
    BuiltApps.Add(appBuilder);

    // Return a marker for RunAsync detection
    return new BuiltAppMarker(appBuilder);
  }

  /// <summary>
  /// Dispatches RunAsync() call to a built app.
  /// </summary>
  private object? DispatchRunAsyncCall(InvocationExpressionSyntax invocation, object? receiver)
  {
    // Receiver could be:
    // 1. BuiltAppMarker - from chained ".Build().RunAsync()"
    // 2. IrAppBuilder - from variable reference "app.RunAsync()"
    IrAppBuilder? appBuilder = receiver switch
    {
      BuiltAppMarker marker => (IrAppBuilder)marker.Builder,
      IrAppBuilder builder => builder,
      _ => null
    };

    if (appBuilder is null)
    {
      throw new InvalidOperationException(
        $"RunAsync() must be called on a built app. Location: {invocation.GetLocation().GetLineSpan()}");
    }

    // Extract and add the intercept site
    InterceptSiteModel? site = InterceptSiteExtractor.Extract(SemanticModel, invocation);
    if (site is not null)
    {
      appBuilder.AddInterceptSite(site);
    }

    return null; // RunAsync returns void/Task
  }

  // ═══════════════════════════════════════════════════════════════════════════════
  // HELPER METHODS
  // ═══════════════════════════════════════════════════════════════════════════════

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
  /// Extracts the first string argument from a method invocation.
  /// </summary>
  private static string? ExtractStringArgument(InvocationExpressionSyntax invocation)
  {
    ArgumentListSyntax? args = invocation.ArgumentList;
    if (args is null || args.Arguments.Count == 0)
      return null;

    ExpressionSyntax argExpression = args.Arguments[0].Expression;

    return argExpression switch
    {
      LiteralExpressionSyntax literal when literal.IsKind(SyntaxKind.StringLiteralExpression)
        => literal.Token.ValueText,
      _ => null
    };
  }

  /// <summary>
  /// Determines if a type is a DSL builder type.
  /// Used for semantic type checking when handling unknown method calls.
  /// </summary>
  private static bool IsBuilderType(ITypeSymbol? type)
  {
    if (type is null) return false;

    string typeName = type.Name;
    return typeName is "NuruCoreAppBuilder" or "NuruAppBuilder"
        or "EndpointBuilder" or "GroupBuilder" or "GroupEndpointBuilder"
        or "NestedCompiledRouteBuilder";
  }

  /// <summary>
  /// Marker type indicating a built app (for RunAsync detection).
  /// </summary>
  private sealed class BuiltAppMarker(IIrAppBuilder builder)
  {
    public IIrAppBuilder Builder { get; } = builder;
  }
}
