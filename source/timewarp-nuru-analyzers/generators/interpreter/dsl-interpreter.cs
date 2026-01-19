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
  private List<Diagnostic> CollectedDiagnostics = null!;

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
    CollectedDiagnostics = [];

    ProcessBlock(block);

    // Finalize all built apps
    return BuiltApps.ConvertAll(app => app.FinalizeModel());
  }

  /// <summary>
  /// Interprets a block of statements to produce an ExtractionResult with diagnostics.
  /// </summary>
  /// <param name="block">The block containing DSL statements.</param>
  /// <returns>Extraction result containing models and any diagnostics.</returns>
  public ExtractionResult InterpretWithDiagnostics(BlockSyntax block)
  {
    ArgumentNullException.ThrowIfNull(block);

    // Fresh state per interpretation
    VariableState = new Dictionary<ISymbol, object?>(SymbolEqualityComparer.Default);
    BuiltApps = [];
    CollectedDiagnostics = [];

    try
    {
      ProcessBlock(block);
    }
    catch (InvalidOperationException ex)
    {
      // Convert exception to diagnostic (fallback for not-yet-converted throws)
      CollectedDiagnostics.Add(CreateDiagnosticFromException(ex, block.GetLocation()));
    }

    // Finalize all built apps
    List<AppModel> models = BuiltApps.ConvertAll(app => app.FinalizeModel());

    // Return first model (or null) with collected diagnostics
    AppModel? model = models.Count > 0 ? models[0] : null;
    return new ExtractionResult(model, [.. CollectedDiagnostics]);
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
    CollectedDiagnostics = [];

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

  /// <summary>
  /// Interprets top-level statements to produce an ExtractionResult with diagnostics.
  /// </summary>
  /// <param name="compilationUnit">The compilation unit containing top-level statements.</param>
  /// <returns>Extraction result containing models and any diagnostics.</returns>
  public ExtractionResult InterpretTopLevelStatementsWithDiagnostics(CompilationUnitSyntax compilationUnit)
  {
    ArgumentNullException.ThrowIfNull(compilationUnit);

    // Fresh state per interpretation
    VariableState = new Dictionary<ISymbol, object?>(SymbolEqualityComparer.Default);
    BuiltApps = [];
    CollectedDiagnostics = [];

    try
    {
      // Process each GlobalStatementSyntax member
      foreach (MemberDeclarationSyntax member in compilationUnit.Members)
      {
        CancellationToken.ThrowIfCancellationRequested();

        if (member is GlobalStatementSyntax globalStatement)
        {
          ProcessStatement(globalStatement.Statement);
        }
      }
    }
    catch (InvalidOperationException ex)
    {
      // Convert exception to diagnostic (fallback for not-yet-converted throws)
      CollectedDiagnostics.Add(CreateDiagnosticFromException(ex, compilationUnit.GetLocation()));
    }

    // Finalize all built apps
    List<AppModel> models = BuiltApps.ConvertAll(app => app.FinalizeModel());

    // Return first model (or null) with collected diagnostics
    AppModel? model = models.Count > 0 ? models[0] : null;
    return new ExtractionResult(model, [.. CollectedDiagnostics]);
  }

  /// <summary>
  /// Extracts an AppModel by tracing back from an entry point call (RunAsync or RunReplAsync) to find the app builder.
  /// Uses semantic model to follow variable declarations - no block walking needed.
  /// </summary>
  /// <param name="invocation">The entry point call site (RunAsync or RunReplAsync).</param>
  /// <param name="isReplCall">True if this is a RunReplAsync call, false for RunAsync.</param>
  /// <returns>Extraction result containing the model and any diagnostics.</returns>
  public ExtractionResult ExtractFromEntryPointCall(InvocationExpressionSyntax invocation, bool isReplCall)
  {
    ArgumentNullException.ThrowIfNull(invocation);

    // Fresh state per interpretation
    VariableState = new Dictionary<ISymbol, object?>(SymbolEqualityComparer.Default);
    BuiltApps = [];
    CollectedDiagnostics = [];

    try
    {
      // Get the receiver expression (the 'app' part of 'app.RunAsync()' or 'app.RunReplAsync()')
      if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess)
        return ExtractionResult.Empty;

      ExpressionSyntax receiver = memberAccess.Expression;

      // Evaluate the receiver - this will trace back through variable declarations
      // using the semantic model to find the builder chain
      object? result = EvaluateExpression(receiver);

      // Get the app builder from the result
      IrAppBuilder? appBuilder = result switch
      {
        BuiltAppMarker marker => (IrAppBuilder)marker.Builder,
        IrAppBuilder builder => builder,
        _ => null
      };

      if (appBuilder is null)
        return ExtractionResult.Empty;

      // Add the intercept site for this entry point call
      InterceptSiteModel? site = InterceptSiteExtractor.Extract(SemanticModel, invocation);
      if (site is not null)
      {
        string methodName = isReplCall ? "RunReplAsync" : "RunAsync";
        appBuilder.AddInterceptSite(methodName, site);
      }

      // Finalize and return the model
      AppModel model = appBuilder.FinalizeModel();
      return new ExtractionResult(model, [.. CollectedDiagnostics]);
    }
    catch (InvalidOperationException ex)
    {
      CollectedDiagnostics.Add(CreateDiagnosticFromException(ex, invocation.GetLocation()));
      return new ExtractionResult(null, [.. CollectedDiagnostics]);
    }
  }

  /// <summary>
  /// Extracts an AppModel by tracing back from a RunAsync invocation to find the app builder.
  /// Uses semantic model to follow variable declarations - no block walking needed.
  /// </summary>
  /// <param name="runAsyncInvocation">The RunAsync() call site.</param>
  /// <returns>Extraction result containing the model and any diagnostics.</returns>
  [Obsolete("Use ExtractFromEntryPointCall instead")]
  public ExtractionResult ExtractFromRunAsyncCall(InvocationExpressionSyntax runAsyncInvocation) =>
    ExtractFromEntryPointCall(runAsyncInvocation, isReplCall: false);

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

      case TryStatementSyntax tryStmt:
        // Process statements inside try block
        ProcessBlock(tryStmt.Block);

        // Also process catch and finally blocks if they contain DSL code
        foreach (CatchClauseSyntax catchClause in tryStmt.Catches)
        {
          ProcessBlock(catchClause.Block);
        }

        if (tryStmt.Finally is not null)
        {
          ProcessBlock(tryStmt.Finally.Block);
        }

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
      AssignmentExpressionSyntax assignment => EvaluateAssignment(assignment),
      _ => null // Literals, etc. - not relevant for DSL interpretation
    };
  }

  /// <summary>
  /// Evaluates an assignment expression (e.g., "App = NuruApp.CreateBuilder([])...Build()").
  /// Stores the value in VariableState if the left side is a field or variable.
  /// </summary>
  private object? EvaluateAssignment(AssignmentExpressionSyntax assignment)
  {
    // Evaluate the right side first
    object? value = EvaluateExpression(assignment.Right);

    // Store in VariableState if left side is an identifier (field or variable)
    ISymbol? leftSymbol = SemanticModel.GetSymbolInfo(assignment.Left).Symbol;
    if (leftSymbol is not null && value is not null)
    {
      VariableState[leftSymbol] = value;
    }

    return value;
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
  /// Resolves an identifier to its value by tracing back to its declaration.
  /// Uses semantic model to find where the variable is declared and evaluates the initializer.
  /// This handles captured variables from outer scopes (like lambdas).
  /// Also handles static/instance fields by searching for assignments in the containing type.
  /// </summary>
  private object? ResolveIdentifier(IdentifierNameSyntax identifier)
  {
    ISymbol? symbol = SemanticModel.GetSymbolInfo(identifier).Symbol;
    if (symbol is null)
      return null;

    // Check cache first (avoid re-evaluating same declaration)
    if (VariableState.TryGetValue(symbol, out object? cached))
      return cached;

    // Use semantic model to find declaration and evaluate initializer
    if (symbol is ILocalSymbol localSymbol)
    {
      SyntaxReference? syntaxRef = localSymbol.DeclaringSyntaxReferences.FirstOrDefault();
      if (syntaxRef?.GetSyntax(CancellationToken) is VariableDeclaratorSyntax declarator
          && declarator.Initializer?.Value is { } initializer)
      {
        object? value = EvaluateExpression(initializer);

        // Cache for future lookups
        VariableState[symbol] = value;
        return value;
      }
    }
    else if (symbol is IFieldSymbol fieldSymbol)
    {
      // Handle static/instance fields by searching for assignments in the containing type
      object? value = FindFieldAssignmentInContainingType(fieldSymbol);
      if (value is not null)
      {
        // Cache for future lookups
        VariableState[symbol] = value;
        return value;
      }
    }

    return null;
  }

  /// <summary>
  /// Finds an assignment to a field within the containing type and evaluates the right-hand side.
  /// This enables tracking of static fields assigned in Setup() methods and accessed elsewhere.
  /// </summary>
  /// <param name="field">The field symbol to find an assignment for.</param>
  /// <returns>The evaluated value of the assignment, or null if not found.</returns>
  private object? FindFieldAssignmentInContainingType(IFieldSymbol field)
  {
    // Get the containing type
    INamedTypeSymbol? containingType = field.ContainingType;
    if (containingType is null)
      return null;

    // Get all syntax references for the type
    foreach (SyntaxReference syntaxRef in containingType.DeclaringSyntaxReferences)
    {
      RoslynSyntaxNode typeSyntax = syntaxRef.GetSyntax(CancellationToken);

      // Find assignments to this field: field = ...
      foreach (AssignmentExpressionSyntax assignment in typeSyntax.DescendantNodes()
        .OfType<AssignmentExpressionSyntax>())
      {
        if (IsAssignmentToField(assignment, field))
        {
          // Evaluate the right-hand side
          object? value = EvaluateExpression(assignment.Right);
          if (value is not null)
            return value;
        }
      }
    }

    return null;
  }

  /// <summary>
  /// Checks if an assignment expression is assigning to the specified field.
  /// </summary>
  /// <param name="assignment">The assignment expression to check.</param>
  /// <param name="field">The field symbol to match.</param>
  /// <returns>True if the assignment is to the specified field.</returns>
  private bool IsAssignmentToField(AssignmentExpressionSyntax assignment, IFieldSymbol field)
  {
    // Get the symbol for the left-hand side of the assignment
    ISymbol? leftSymbol = SemanticModel.GetSymbolInfo(assignment.Left).Symbol;
    if (leftSymbol is null)
      return false;

    // Compare symbols using SymbolEqualityComparer.Default
    return SymbolEqualityComparer.Default.Equals(leftSymbol, field);
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

      "Map" when IsGenericMapCall(invocation) => DispatchMapEndpoint(invocation, receiver),

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

      "UseTelemetry" => DispatchUseTelemetry(receiver),

      "EnableCompletion" => DispatchEnableCompletion(receiver),

      "AddTypeConverter" => DispatchAddTypeConverter(invocation, receiver),

      "WithAlias" => DispatchWithAlias(invocation, receiver),

      "Implements" => DispatchImplements(invocation, receiver),

      "DiscoverEndpoints" => DispatchDiscoverEndpoints(receiver),

      "RunAsync" => DispatchRunAsyncCall(invocation, receiver),

      "RunReplAsync" => DispatchRunReplAsyncCall(invocation, receiver),

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

  /// <summary>
  /// Checks if the invocation is calling a method on a Nuru DSL builder type.
  /// Uses semantic model to resolve the method's containing type.
  /// </summary>
  private bool IsDslBuilderMethod(InvocationExpressionSyntax invocation)
  {
    SymbolInfo symbolInfo = SemanticModel.GetSymbolInfo(invocation, CancellationToken);

    if (symbolInfo.Symbol is not IMethodSymbol methodSymbol)
      return false;

    INamedTypeSymbol? containingType = methodSymbol.ContainingType;
    if (containingType is null)
      return false;

    // Check if containing type is in TimeWarp.Nuru namespace
    string? namespaceName = containingType.ContainingNamespace?.ToDisplayString();
    if (namespaceName is not "TimeWarp.Nuru" and not "TimeWarp.Nuru.Generators")
      return false;

    // Check if type name matches known DSL types (builders and built app)
    string typeName = containingType.Name;
    return typeName is "NuruCoreAppBuilder" or "NuruAppBuilder"
        or "EndpointBuilder" or "GroupBuilder" or "GroupEndpointBuilder"
        or "NestedCompiledRouteBuilder" or "NuruApp" or "NuruCoreApp";
  }

  /// <summary>
  /// Checks if this is a generic Map&lt;T&gt;() call vs Map("pattern").
  /// </summary>
  private static bool IsGenericMapCall(InvocationExpressionSyntax invocation)
  {
    if (invocation.Expression is MemberAccessExpressionSyntax memberAccess)
    {
      return memberAccess.Name is GenericNameSyntax;
    }

    return false;
  }

  /// <summary>
  /// Dispatches DiscoverEndpoints() call to enable endpoint discovery.
  /// </summary>
  private static object? DispatchDiscoverEndpoints(object? receiver)
  {
    if (receiver is not IIrAppBuilder appBuilder)
    {
      throw new InvalidOperationException(
        "DiscoverEndpoints() must be called on an app builder.");
    }

    return appBuilder.DiscoverEndpoints();
  }

  /// <summary>
  /// Dispatches Map&lt;TEndpoint&gt;() call to include a specific endpoint.
  /// </summary>
  private object? DispatchMapEndpoint(InvocationExpressionSyntax invocation, object? receiver)
  {
    if (receiver is not IIrAppBuilder appBuilder)
    {
      throw new InvalidOperationException(
        $"Map<T>() must be called on an app builder. Location: {invocation.GetLocation().GetLineSpan()}");
    }

    string? typeName = ExtractGenericTypeArgument(invocation);
    if (typeName is null)
    {
      throw new InvalidOperationException(
        $"Map<T>() requires a type argument. Location: {invocation.GetLocation().GetLineSpan()}");
    }

    return appBuilder.MapEndpoint(typeName);
  }

  /// <summary>
  /// Extracts the fully qualified type name from a generic method call like Map&lt;T&gt;().
  /// </summary>
  private string? ExtractGenericTypeArgument(InvocationExpressionSyntax invocation)
  {
    if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess)
      return null;

    if (memberAccess.Name is not GenericNameSyntax genericName)
      return null;

    TypeArgumentListSyntax? typeArgs = genericName.TypeArgumentList;
    if (typeArgs.Arguments.Count != 1)
      return null;

    TypeSyntax typeSyntax = typeArgs.Arguments[0];
    ITypeSymbol? typeSymbol = SemanticModel.GetTypeInfo(typeSyntax).Type;

    return typeSymbol?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
  }

  // ═══════════════════════════════════════════════════════════════════════════════
  // DISPATCH METHODS - Using marker interfaces for polymorphic dispatch
  // ═══════════════════════════════════════════════════════════════════════════════

  /// <summary>
  /// Dispatches Map() call to any IIrRouteSource (app or group builder).
  /// </summary>
  private object? DispatchMap(InvocationExpressionSyntax invocation, object? receiver)
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

    // Validate pattern and collect any parse/semantic errors
    Location patternLocation = GetPatternLocation(invocation);
    PatternParseResult parseResult = PatternStringExtractor.ExtractSegmentsWithErrors(pattern);

    if (!parseResult.Success)
    {
      // Map parse errors to diagnostics
      if (parseResult.ParseErrors is not null)
      {
        foreach (ParseError error in parseResult.ParseErrors)
        {
          Diagnostic? diagnostic = MapParseErrorToDiagnostic(error, pattern, patternLocation);
          if (diagnostic is not null)
          {
            CollectedDiagnostics.Add(diagnostic);
          }
        }
      }

      // Map semantic errors to diagnostics
      if (parseResult.SemanticErrors is not null)
      {
        foreach (SemanticError error in parseResult.SemanticErrors)
        {
          Diagnostic? diagnostic = MapSemanticErrorToDiagnostic(error, pattern, patternLocation);
          if (diagnostic is not null)
          {
            CollectedDiagnostics.Add(diagnostic);
          }
        }
      }
    }

    return source.Map(pattern);
  }

  /// <summary>
  /// Gets the location of the pattern string argument from a Map() invocation.
  /// </summary>
  private static Location GetPatternLocation(InvocationExpressionSyntax invocation)
  {
    ArgumentListSyntax? argumentList = invocation.ArgumentList;
    if (argumentList?.Arguments.Count > 0)
    {
      return argumentList.Arguments[0].Expression.GetLocation();
    }

    return invocation.GetLocation();
  }

  /// <summary>
  /// Maps a parse error to a diagnostic.
  /// </summary>
  private static Diagnostic? MapParseErrorToDiagnostic(ParseError error, string pattern, Location location)
  {
    return error switch
    {
      InvalidParameterSyntaxError e =>
        Diagnostic.Create(DiagnosticDescriptors.InvalidParameterSyntax, location, e.InvalidSyntax, e.Suggestion),

      UnbalancedBracesError e =>
        Diagnostic.Create(DiagnosticDescriptors.UnbalancedBraces, location, e.Pattern),

      InvalidOptionFormatError e =>
        Diagnostic.Create(DiagnosticDescriptors.InvalidOptionFormat, location, e.InvalidOption),

      InvalidTypeConstraintError e =>
        Diagnostic.Create(DiagnosticDescriptors.InvalidTypeConstraint, location, e.InvalidType),

      InvalidCharacterError e =>
        Diagnostic.Create(DiagnosticDescriptors.InvalidCharacter, location, e.Character),

      UnexpectedTokenError e =>
        Diagnostic.Create(DiagnosticDescriptors.UnexpectedToken, location, e.Expected, e.Found),

      NullPatternError =>
        Diagnostic.Create(DiagnosticDescriptors.NullPattern, location),

      _ => null
    };
  }

  /// <summary>
  /// Maps a semantic error to a diagnostic.
  /// </summary>
  private static Diagnostic? MapSemanticErrorToDiagnostic(SemanticError error, string pattern, Location location)
  {
    return error switch
    {
      DuplicateParameterNamesError e =>
        Diagnostic.Create(DiagnosticDescriptors.DuplicateParameterNames, location, e.ParameterName),

      ConflictingOptionalParametersError e =>
        Diagnostic.Create(DiagnosticDescriptors.ConflictingOptionalParameters, location, string.Join(", ", e.ConflictingParameters)),

      CatchAllNotAtEndError e =>
        Diagnostic.Create(DiagnosticDescriptors.CatchAllNotAtEnd, location, e.CatchAllParameter, e.FollowingSegment),

      MixedCatchAllWithOptionalError e =>
        Diagnostic.Create(DiagnosticDescriptors.MixedCatchAllWithOptional, location, string.Join(", ", e.OptionalParams), e.CatchAllParam),

      DuplicateOptionAliasError e =>
        Diagnostic.Create(DiagnosticDescriptors.DuplicateOptionAlias, location, e.Alias, string.Join(", ", e.ConflictingOptions)),

      OptionalBeforeRequiredError e =>
        Diagnostic.Create(DiagnosticDescriptors.OptionalBeforeRequired, location, e.OptionalParam, e.RequiredParam),

      InvalidEndOfOptionsSeparatorError e =>
        Diagnostic.Create(DiagnosticDescriptors.InvalidEndOfOptionsSeparator, location, e.Reason),

      OptionsAfterEndOfOptionsSeparatorError e =>
        Diagnostic.Create(DiagnosticDescriptors.OptionsAfterEndOfOptionsSeparator, location, e.Option),

      _ => null
    };
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

    // Validate handler expression and collect diagnostics
    ArgumentListSyntax? args = invocation.ArgumentList;
    if (args?.Arguments.Count > 0)
    {
      ExpressionSyntax handlerExpression = args.Arguments[0].Expression;
      ImmutableArray<Diagnostic> handlerDiagnostics = Validation.HandlerValidator.Validate(
        handlerExpression,
        SemanticModel,
        handlerExpression.GetLocation());

      CollectedDiagnostics.AddRange(handlerDiagnostics);
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
  /// Uses semantic model check to filter out non-DSL methods (e.g., CustomKeyBindingProfile.WithName).
  /// </summary>
  private object? DispatchWithName(InvocationExpressionSyntax invocation, object? receiver)
  {
    // Check if this is actually a DSL builder method call
    // Prevents methods like CustomKeyBindingProfile.WithName() from being incorrectly dispatched
    if (!IsDslBuilderMethod(invocation))
    {
      return null;
    }

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

    // Extract logging configuration from AddLogging() if present
    LoggingConfiguration? loggingConfig = ServiceExtractor.ExtractLoggingConfiguration(
      invocation,
      SemanticModel,
      CancellationToken);

    if (loggingConfig is not null)
    {
      appBuilder.SetLoggingConfiguration(loggingConfig);
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
  /// Dispatches UseTelemetry() call to IIrAppBuilder.
  /// This is a no-op - telemetry is runtime only.
  /// </summary>
  private static object? DispatchUseTelemetry(object? receiver)
  {
    if (receiver is not IIrAppBuilder appBuilder)
    {
      throw new InvalidOperationException("UseTelemetry() must be called on an app builder.");
    }

    return appBuilder.UseTelemetry();
  }

  /// <summary>
  /// Dispatches EnableCompletion() call to IIrAppBuilder.
  /// This is a no-op for the generator - completion is configured at runtime.
  /// </summary>
  private static object? DispatchEnableCompletion(object? receiver)
  {
    if (receiver is not IIrAppBuilder appBuilder)
    {
      throw new InvalidOperationException("EnableCompletion() must be called on an app builder.");
    }

    // Just pass through - completion configuration is runtime-only
    return appBuilder;
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
      // Use semantic model to get fully qualified converter type name
      SymbolInfo symbolInfo = SemanticModel.GetSymbolInfo(objectCreation.Type);
      string converterTypeName;
      string targetTypeName = "";

      if (symbolInfo.Symbol is INamedTypeSymbol namedType)
      {
        // Use fully qualified format to avoid ambiguity with System types
        converterTypeName = namedType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

        if (namedType.IsGenericType && namedType.TypeArguments.Length > 0)
        {
          // Generic converter like EnumTypeConverter<Environment>
          // Extract the first type argument as the target type
          ITypeSymbol targetType = namedType.TypeArguments[0];
          targetTypeName = targetType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        }
      }
      else
      {
        // Fallback to syntax text if semantic model resolution fails
        converterTypeName = objectCreation.Type.ToString();
      }

      CustomConverterDefinition converter = new(
        ConverterTypeName: converterTypeName,
        TargetTypeName: targetTypeName,
        ConstraintAlias: null);

      // DEBUG: Trace converter registration
      CollectedDiagnostics.Add(Diagnostic.Create(
        new DiagnosticDescriptor("NURU_DEBUG_CONV1", "Debug", "AddTypeConverter called: ConverterTypeName={0}, TargetTypeName={1}", "Debug", DiagnosticSeverity.Hidden, true),
        invocation.GetLocation(),
        converterTypeName, targetTypeName));

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
    // Ignore Build() calls on non-Nuru types (e.g., DotNet.Build().Build())
    if (receiver is not IrAppBuilder appBuilder)
    {
      return null;
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

    // Ignore RunAsync() calls on non-Nuru types (e.g., CommandResult.RunAsync())
    if (appBuilder is null)
    {
      return null;
    }

    // Extract and add the intercept site
    InterceptSiteModel? site = InterceptSiteExtractor.Extract(SemanticModel, invocation);
    if (site is not null)
    {
      appBuilder.AddInterceptSite("RunAsync", site);
    }

    return null; // RunAsync returns void/Task
  }

  /// <summary>
  /// Dispatches RunReplAsync() call to a built app.
  /// </summary>
  private object? DispatchRunReplAsyncCall(InvocationExpressionSyntax invocation, object? receiver)
  {
    // Receiver could be:
    // 1. BuiltAppMarker - from chained ".Build().RunReplAsync()"
    // 2. IrAppBuilder - from variable reference "app.RunReplAsync()"
    IrAppBuilder? appBuilder = receiver switch
    {
      BuiltAppMarker marker => (IrAppBuilder)marker.Builder,
      IrAppBuilder builder => builder,
      _ => null
    };

    // Ignore RunReplAsync() calls on non-Nuru types
    if (appBuilder is null)
    {
      return null;
    }

    // Extract and add the intercept site
    InterceptSiteModel? site = InterceptSiteExtractor.Extract(SemanticModel, invocation);
    if (site is not null)
    {
      appBuilder.AddInterceptSite("RunReplAsync", site);
    }

    return null; // RunReplAsync returns void/Task
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
  /// Creates a diagnostic from an exception that occurred during interpretation.
  /// Used as a fallback for not-yet-converted exception throws.
  /// </summary>
  private static Diagnostic CreateDiagnosticFromException(Exception exception, Location location)
  {
    // Use NURU_S999 as a general extraction error diagnostic
    // This is a fallback - ideally all throws should be converted to proper diagnostics
    DiagnosticDescriptor descriptor = new(
      id: "NURU_S999",
      title: "DSL Interpretation Error",
      messageFormat: "{0}",
      category: "TimeWarp.Nuru.Semantic",
      defaultSeverity: DiagnosticSeverity.Error,
      isEnabledByDefault: true,
      description: "An error occurred while interpreting the DSL code.");

    return Diagnostic.Create(descriptor, location, exception.Message);
  }

  /// <summary>
  /// Marker type indicating a built app (for RunAsync detection).
  /// </summary>
  private sealed class BuiltAppMarker(IIrAppBuilder builder)
  {
    public IIrAppBuilder Builder { get; } = builder;
  }
}
