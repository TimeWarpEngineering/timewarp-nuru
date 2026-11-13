#region Purpose
// Entry-point extraction and statement processing for the DSL interpreter.
#endregion

namespace TimeWarp.Nuru.Generators;

public sealed partial class DslInterpreter
{
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
    BuildCalls = [];
    CollectedDiagnostics = [];
    BoundEntryPointCalls = [];
    AppAssignments = new Dictionary<ISymbol, List<AppAssignment>>(SymbolEqualityComparer.Default);

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
    catch (Exception ex) when (ex is not OperationCanceledException)
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

      // Nested bodies (task 508-008). Both branches of an if are walked: the generator sees every
      // app the code can build. Local functions are their own scope and are not walked here.
      case BlockSyntax nestedBlock:
        ProcessBlock(nestedBlock);
        break;

      case IfStatementSyntax ifStmt:
        ProcessStatement(ifStmt.Statement);
        if (ifStmt.Else is not null)
          ProcessStatement(ifStmt.Else.Statement);
        break;

      case CommonForEachStatementSyntax forEachStmt:
        ProcessStatement(forEachStmt.Statement);
        break;

      case ForStatementSyntax forStmt:
        ProcessStatement(forStmt.Statement);
        break;

      case WhileStatementSyntax whileStmt:
        ProcessStatement(whileStmt.Statement);
        break;

      case DoStatementSyntax doStmt:
        ProcessStatement(doStmt.Statement);
        break;

      case UsingStatementSyntax usingStmt:
        if (usingStmt.Declaration is not null)
          ProcessVariableDeclaration(usingStmt.Declaration);
        ProcessStatement(usingStmt.Statement);
        break;

      case LockStatementSyntax lockStmt:
        ProcessStatement(lockStmt.Statement);
        break;

      case FixedStatementSyntax fixedStmt:
        ProcessStatement(fixedStmt.Statement);
        break;

      case CheckedStatementSyntax checkedStmt:
        ProcessBlock(checkedStmt.Block);
        break;

      case UnsafeStatementSyntax unsafeStmt:
        ProcessBlock(unsafeStmt.Block);
        break;

      case LabeledStatementSyntax labeledStmt:
        ProcessStatement(labeledStmt.Statement);
        break;

      case SwitchStatementSyntax switchStmt:
        foreach (SwitchSectionSyntax section in switchStmt.Sections)
        {
          foreach (StatementSyntax sectionStatement in section.Statements)
            ProcessStatement(sectionStatement);
        }

        break;

      // Ignore other statement types (local functions, etc.)
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
  private void ProcessLocalDeclaration(LocalDeclarationStatementSyntax localDecl) =>
    ProcessVariableDeclaration(localDecl.Declaration);

  /// <summary>
  /// Processes the variables of a local declaration or a using statement's declaration.
  /// </summary>
  private void ProcessVariableDeclaration(VariableDeclarationSyntax declaration)
  {
    foreach (VariableDeclaratorSyntax declarator in declaration.Variables)
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
      RecordAssignment(symbol, declarator, value);

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

  // EXPRESSION EVALUATION
  // ═══════════════════════════════════════════════════════════════════════════════
}
