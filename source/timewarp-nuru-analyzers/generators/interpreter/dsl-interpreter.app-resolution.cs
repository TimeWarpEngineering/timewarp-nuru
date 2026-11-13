#region Purpose
// Built-app lookup and variable assignment tracking for the DSL interpreter.
#endregion

namespace TimeWarp.Nuru.Generators;

using Microsoft.CodeAnalysis.Text;
using RoslynSyntaxNode = Microsoft.CodeAnalysis.SyntaxNode;

public sealed partial class DslInterpreter
{
  /// <summary>
  /// The app built by <paramref name="buildInvocation"/> during this interpretation, if any.
  /// </summary>
  private IrAppBuilder? FindBuiltApp(InvocationExpressionSyntax buildInvocation) =>
    BuildCalls.TryGetValue(buildInvocation, out IrAppBuilder? app) ? app : null;

  /// <summary>
  /// The member that holds <paramref name="node"/> when it is not in a method body or top-level
  /// statements (an expression-bodied member or an initializer), else the node itself.
  /// </summary>
  private static RoslynSyntaxNode EnclosingMember(RoslynSyntaxNode node) =>
    (RoslynSyntaxNode?)node.FirstAncestorOrSelf<MemberDeclarationSyntax>() ?? node;

  /// <summary>
  /// Registers every RunAsync() and RunReplAsync() invocation in <paramref name="region"/> whose
  /// receiver is an app this interpretation built (task 547).
  /// </summary>
  /// <remarks>
  /// <para>
  /// The statement walk reaches an entry point call only through the expression shapes it
  /// evaluates, so calls such as <c>(await app.RunAsync(args)).ShouldBe(0)</c>,
  /// <c>Check(await app.RunAsync(args))</c>, a ternary branch or <c>app?.RunAsync(args)</c> were
  /// never intercepted and threw at runtime. This pass finds the calls by method symbol wherever
  /// they sit. A receiver resolves only through state the walk already produced
  /// (<see cref="AppAssignments"/>, <see cref="VariableState"/> and <see cref="BuildCalls"/>), so no
  /// builder chain is evaluated a second time.
  /// </para>
  /// <para>
  /// A call the walk already bound is skipped, so one call site never lands on two apps. A variable
  /// assigned more than one app resolves to the app it held at that point in source order, unless
  /// the call sits in a lambda or local function that captures it (see <see cref="AppAt"/>).
  /// </para>
  /// </remarks>
  private void RegisterEntryPointCalls(RoslynSyntaxNode region)
  {
    foreach (InvocationExpressionSyntax invocation in region.DescendantNodesAndSelf().OfType<InvocationExpressionSyntax>())
    {
      CancellationToken.ThrowIfCancellationRequested();

      if (BoundEntryPointCalls.Contains(invocation))
        continue;

      if (!RunAsyncLocator.TryGetMemberCall(invocation, out SimpleNameSyntax name, out ExpressionSyntax receiver))
        continue;

      string methodName = name.Identifier.ValueText;
      if (methodName is not ("RunAsync" or "RunReplAsync"))
        continue;

      if (SemanticModel.GetSymbolInfo(invocation, CancellationToken).Symbol is not IMethodSymbol { ContainingType.Name: "NuruApp" })
        continue;

      if (ResolveBuiltApp(receiver, invocation) is not IrAppBuilder appBuilder)
        continue;

      InterceptSiteModel? site = InterceptSiteExtractor.Extract(SemanticModel, invocation);
      if (site is null)
        continue;

      appBuilder.AddInterceptSite(methodName, site);
      BoundEntryPointCalls.Add(invocation);
    }
  }

  /// <summary>
  /// The app an entry point call's receiver refers to at <paramref name="call"/>, from the walk's
  /// state only.
  /// </summary>
  private IrAppBuilder? ResolveBuiltApp(ExpressionSyntax receiver, InvocationExpressionSyntax call)
  {
    switch (receiver)
    {
      case ParenthesizedExpressionSyntax parenthesized:
        return ResolveBuiltApp(parenthesized.Expression, call);

      case PostfixUnaryExpressionSyntax postfix when postfix.IsKind(SyntaxKind.SuppressNullableWarningExpression):
        return ResolveBuiltApp(postfix.Operand, call);

      // ...Build().RunAsync(): the Build() call the walk dispatched
      case InvocationExpressionSyntax invocation:
        return FindBuiltApp(invocation);

      case IdentifierNameSyntax or MemberAccessExpressionSyntax:
        ISymbol? symbol = SemanticModel.GetSymbolInfo(receiver, CancellationToken).Symbol;
        if (symbol is null)
          return null;

        if (AppAssignments.TryGetValue(symbol, out List<AppAssignment>? assignments))
          return AppAt(symbol, assignments, call);

        // A field or captured local the walk resolved through its declaration: one value
        return VariableState.TryGetValue(symbol, out object? value) ? AsAppBuilder(value) : null;

      default:
        return null;
    }
  }

  /// <summary>
  /// The app a variable held at <paramref name="call"/>: the last assignment before it in source
  /// order. With no assignment before it (a captured variable, another file), the variable's app
  /// only when every write of it gave it the same one.
  /// </summary>
  /// <remarks>
  /// A lambda, anonymous method or local function usually runs after the code that follows it, so
  /// its source position says nothing about which app a captured variable holds when it runs. Only
  /// an assignment inside the call's innermost closure counts as before the call; an assignment
  /// outside it falls to the every-write rule, and a captured variable assigned two apps is left for
  /// NURU_I001 (review round 2 M4). So is a variable with a write this interpretation did not record
  /// (review round 3 M5): the walk does not enter closures, and a <c>Build()</c> inside a closure is
  /// its own interpretation that never sees the enclosing method's assignments.
  /// </remarks>
  private IrAppBuilder? AppAt(ISymbol symbol, List<AppAssignment> assignments, InvocationExpressionSyntax call)
  {
    RoslynSyntaxNode? closure = call.Ancestors().FirstOrDefault(IsClosure);
    TextSpan scope = closure?.Span ?? call.SyntaxTree.GetRoot().FullSpan;
    int position = call.SpanStart;

    AppAssignment? last = null;
    foreach (AppAssignment assignment in assignments)
    {
      if (assignment.Tree == call.SyntaxTree
        && assignment.Position >= scope.Start
        && assignment.Position <= position
        && (last is null || assignment.Position > last.Position))
      {
        last = assignment;
      }
    }

    if (last is not null)
      return last.App;

    IrAppBuilder? only = assignments[0].App;
    if (!assignments.TrueForAll(assignment => ReferenceEquals(assignment.App, only)))
      return null;

    return HasUnrecordedWrite(symbol, assignments, call.SyntaxTree.GetRoot(CancellationToken)) ? null : only;
  }

  /// <summary>
  /// True when <paramref name="root"/> writes <paramref name="symbol"/> anywhere (a declaration,
  /// an assignment, a deconstruction or a <c>ref</c>/<c>out</c> argument) that this interpretation
  /// did not record. A <c>null</c> or <c>default</c> write never holds an app and is ignored.
  /// </summary>
  private bool HasUnrecordedWrite(ISymbol symbol, List<AppAssignment> assignments, RoslynSyntaxNode root)
  {
    foreach (RoslynSyntaxNode node in root.DescendantNodes())
    {
      CancellationToken.ThrowIfCancellationRequested();

      switch (node)
      {
        case VariableDeclaratorSyntax declarator when declarator.Identifier.ValueText == symbol.Name:
          if (declarator.Initializer is null || IsNullLiteral(declarator.Initializer.Value) || IsRecorded(assignments, declarator))
            continue;

          if (SymbolEqualityComparer.Default.Equals(SemanticModel.GetDeclaredSymbol(declarator, CancellationToken), symbol))
            return true;

          break;

        case IdentifierNameSyntax identifier when identifier.Identifier.ValueText == symbol.Name:
          RoslynSyntaxNode? write = WriteOf(identifier);
          if (write is null || IsRecorded(assignments, write))
            continue;

          if (write is AssignmentExpressionSyntax { Right: ExpressionSyntax right } && IsNullLiteral(right))
            continue;

          if (SymbolEqualityComparer.Default.Equals(SemanticModel.GetSymbolInfo(identifier, CancellationToken).Symbol, symbol))
            return true;

          break;
      }
    }

    return false;
  }

  /// <summary>
  /// The assignment or argument that writes <paramref name="identifier"/>, or null when it is only read.
  /// </summary>
  private static RoslynSyntaxNode? WriteOf(IdentifierNameSyntax identifier)
  {
    RoslynSyntaxNode target = identifier;
    if (target.Parent is MemberAccessExpressionSyntax memberAccess && memberAccess.Name == target)
      target = memberAccess;

    while (true)
    {
      switch (target.Parent)
      {
        case ParenthesizedExpressionSyntax parenthesized:
          target = parenthesized;
          continue;

        // (app, other) = ...: the tuple is the assignment's left side
        case ArgumentSyntax { Parent: TupleExpressionSyntax tuple }:
          target = tuple;
          continue;

        case AssignmentExpressionSyntax assignment when assignment.Left == target:
          return assignment;

        case ArgumentSyntax argument when !argument.RefKindKeyword.IsKind(SyntaxKind.None) && !argument.RefKindKeyword.IsKind(SyntaxKind.InKeyword):
          return argument;

        default:
          return null;
      }
    }
  }

  private static bool IsRecorded(List<AppAssignment> assignments, RoslynSyntaxNode write) =>
    assignments.Exists(recorded => recorded.Tree == write.SyntaxTree && recorded.Position == write.Span.End);

  /// <summary>
  /// True for <c>null</c> or <c>default</c>, also wrapped in parentheses, <c>!</c> or a cast
  /// (<c>null!</c>, <c>(null)</c>, <c>(NuruApp?)null</c>; review round 4 M6).
  /// </summary>
  private static bool IsNullLiteral(ExpressionSyntax expression) => expression switch
  {
    ParenthesizedExpressionSyntax parenthesized => IsNullLiteral(parenthesized.Expression),
    PostfixUnaryExpressionSyntax postfix when postfix.IsKind(SyntaxKind.SuppressNullableWarningExpression) => IsNullLiteral(postfix.Operand),
    CastExpressionSyntax cast => IsNullLiteral(cast.Expression),
    DefaultExpressionSyntax => true,
    _ => expression.IsKind(SyntaxKind.NullLiteralExpression) || expression.IsKind(SyntaxKind.DefaultLiteralExpression)
  };

  /// <summary>
  /// Records that <paramref name="symbol"/> was assigned <paramref name="value"/> at the end of
  /// <paramref name="node"/>. Only variables that hold an app at some point are tracked; a later
  /// non-app assignment is recorded as null so a call after it is not bound (task 547).
  /// </summary>
  private void RecordAssignment(ISymbol symbol, RoslynSyntaxNode node, object? value)
  {
    IrAppBuilder? app = AsAppBuilder(value);
    if (!AppAssignments.TryGetValue(symbol, out List<AppAssignment>? assignments))
    {
      if (app is null)
        return;

      assignments = [];
      AppAssignments[symbol] = assignments;
    }

    assignments.Add(new AppAssignment(node.SyntaxTree, node.Span.End, app));
  }

  private static bool IsClosure(RoslynSyntaxNode node) =>
    node is AnonymousFunctionExpressionSyntax or LocalFunctionStatementSyntax;

  private static IrAppBuilder? AsAppBuilder(object? value) => value switch
  {
    BuiltAppMarker marker => (IrAppBuilder)marker.Builder,
    IrAppBuilder builder => builder,
    _ => null
  };
}
