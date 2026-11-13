#region Purpose
// Semantic DSL interpreter that walks blocks statement-by-statement.
#endregion
#region Design
// This interpreter "executes" the DSL by dispatching method calls to corresponding IR builder methods.
// Phase 1a: Block-based processing: takes a BlockSyntax (method body), returns IReadOnlyList<AppModel>
// (multiple apps per block), tracks variable assignments in VariableState, processes statements one by one.
// Key design:
// - Uses SemanticModel for accurate type resolution
// - Evaluates expressions recursively with variable resolution
// - Dispatches to IR builders based on method name
// - Uses marker interfaces (IIrRouteSource, IIrAppBuilder, IIrGroupBuilder, IIrRouteBuilder)
//   for polymorphic dispatch without explicit type enumeration
// - Fails fast on unrecognized DSL methods
#endregion

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
public sealed partial class DslInterpreter
{
  private readonly SemanticModel SemanticModel;
  private readonly CancellationToken CancellationToken;

  // Per-interpretation state
  private Dictionary<ISymbol, object?> VariableState = null!;
  private List<IrAppBuilder> BuiltApps = null!;
  private Dictionary<InvocationExpressionSyntax, IrAppBuilder> BuildCalls = null!;
  private List<Diagnostic> CollectedDiagnostics = null!;

  // Entry point calls already bound to an app, and the apps each variable held in source order (task 547)
  private HashSet<InvocationExpressionSyntax> BoundEntryPointCalls = null!;
  private Dictionary<ISymbol, List<AppAssignment>> AppAssignments = null!;

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
    BuildCalls = [];
    CollectedDiagnostics = [];
    BoundEntryPointCalls = [];
    AppAssignments = new Dictionary<ISymbol, List<AppAssignment>>(SymbolEqualityComparer.Default);

    try
    {
      ProcessBlock(block);
    }
    catch (Exception ex) when (ex is not OperationCanceledException)
    {
      // swallow — the diagnostic path is the one that reports to the user
    }

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
    BuildCalls = [];
    CollectedDiagnostics = [];
    BoundEntryPointCalls = [];
    AppAssignments = new Dictionary<ISymbol, List<AppAssignment>>(SymbolEqualityComparer.Default);

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
    catch (Exception ex) when (ex is not OperationCanceledException)
    {
      // swallow — the diagnostic path is the one that reports to the user
    }

    // Finalize all built apps
    return BuiltApps.ConvertAll(app => app.FinalizeModel());
  }

  /// <summary>
  /// Interprets the code around one Build() call and returns the app that call builds (task 508-008).
  /// </summary>
  /// <remarks>
  /// <para>
  /// <paramref name="scope"/> is the method body (a <see cref="BlockSyntax"/>), the file's top-level
  /// statements (a <see cref="CompilationUnitSyntax"/>), or null for an expression-bodied member or
  /// an initializer. The whole scope is walked so that variables, fragmented builder calls and
  /// RunAsync() sites are seen, and then the app built by <paramref name="buildInvocation"/> is
  /// returned. A scope can build several apps, so the first app is not necessarily this one.
  /// </para>
  /// <para>
  /// When the walk does not reach the Build() call (an expression lambda, a method argument, a
  /// member with no block), the call itself is evaluated with the state the walk produced.
  /// </para>
  /// </remarks>
  /// <param name="scope">The block, compilation unit, or null.</param>
  /// <param name="buildInvocation">The Build() call whose app is returned.</param>
  /// <returns>Extraction result containing the app built by <paramref name="buildInvocation"/> and any diagnostics.</returns>
  public ExtractionResult InterpretBuildCall(RoslynSyntaxNode? scope, InvocationExpressionSyntax buildInvocation)
  {
    ArgumentNullException.ThrowIfNull(buildInvocation);

    // Fresh state per interpretation
    VariableState = new Dictionary<ISymbol, object?>(SymbolEqualityComparer.Default);
    BuiltApps = [];
    BuildCalls = [];
    CollectedDiagnostics = [];
    BoundEntryPointCalls = [];
    AppAssignments = new Dictionary<ISymbol, List<AppAssignment>>(SymbolEqualityComparer.Default);

    try
    {
      switch (scope)
      {
        case BlockSyntax block:
          ProcessBlock(block);
          break;

        case CompilationUnitSyntax compilationUnit:
          foreach (MemberDeclarationSyntax member in compilationUnit.Members)
          {
            CancellationToken.ThrowIfCancellationRequested();

            if (member is GlobalStatementSyntax globalStatement)
            {
              ProcessStatement(globalStatement.Statement);
            }
          }

          break;
      }

      if (FindBuiltApp(buildInvocation) is null)
        EvaluateExpression(buildInvocation);

      RegisterEntryPointCalls(scope ?? EnclosingMember(buildInvocation));
    }
    catch (Exception ex) when (ex is not OperationCanceledException)
    {
      // Convert exception to diagnostic (fallback for not-yet-converted throws)
      CollectedDiagnostics.Add(CreateDiagnosticFromException(ex, (scope ?? buildInvocation).GetLocation()));
    }

    AppModel? model = FindBuiltApp(buildInvocation)?.FinalizeModel();
    return new ExtractionResult(model, [.. CollectedDiagnostics]);
  }

  /// <summary>Default of AddServeRoute(idleTimeoutSeconds), mirrored from NuruDaemonOptions.</summary>
  private const int DefaultServeIdleTimeoutSeconds = 600;

  /// <summary>
  /// One assignment of an app (or of a non-app value, as null) to a variable, at the end of the
  /// assignment in its syntax tree.
  /// </summary>
  private sealed record AppAssignment(SyntaxTree Tree, int Position, IrAppBuilder? App);

  /// <summary>
  /// Marker type indicating a built app (for RunAsync detection).
  /// </summary>
  private sealed class BuiltAppMarker(IIrAppBuilder builder)
  {
    public IIrAppBuilder Builder { get; } = builder;
  }
}
