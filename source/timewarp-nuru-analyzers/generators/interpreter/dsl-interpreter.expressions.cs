#region Purpose
// Expression evaluation and dispatch core for the DSL interpreter.
#endregion

#region Design
// Dispatch is by method name, so a name the DSL shares with other APIs must also be checked
// semantically. CreateBuilder is the entry point: only TimeWarp.Nuru.NuruApp.CreateBuilder starts
// an IR app. WebApplication.CreateBuilder and other CreateBuilder calls in the same method body are
// not Nuru, so their chains (builder.Services.AddMcpServer()...) and Build() are ignored instead of
// failing with NURU_S999 (task 531).
#endregion

namespace TimeWarp.Nuru.Generators;

using RoslynSyntaxNode = Microsoft.CodeAnalysis.SyntaxNode;

public sealed partial class DslInterpreter
{
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
      ParenthesizedExpressionSyntax parenthesized => EvaluateExpression(parenthesized.Expression),
      PostfixUnaryExpressionSyntax postfix when postfix.IsKind(SyntaxKind.SuppressNullableWarningExpression)
        => EvaluateExpression(postfix.Operand),
      AssignmentExpressionSyntax assignment => EvaluateAssignment(assignment),
      _ => null // Literals, etc. - not relevant for DSL interpretation
    };
  }

  /// <summary>
  /// Evaluates an assignment expression (e.g., "App = NuruApp.CreateBuilder()...Build()").
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

    if (leftSymbol is not null)
      RecordAssignment(leftSymbol, assignment, value);

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

    // Check cache first (avoid re-evaluating same declaration).
    // This doubles as the cycle guard: the null sentinel written below makes a
    // re-entrant resolution of the same symbol return here instead of recursing.
    if (VariableState.TryGetValue(symbol, out object? cached))
      return cached;

    // Pre-cache a null sentinel BEFORE evaluating the initializer. Self-referential
    // or mutually-referential declarations (e.g. the ordinary mid-typing state
    // `var x = x;`) would otherwise recurse forever and kill the analyzer host
    // with an uncatchable StackOverflowException.
    VariableState[symbol] = null;

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
      "CreateBuilder" when IsNuruAppCreateBuilder(invocation) => CreateNewAppBuilder(),

      "Map" when IsGenericMapCall(invocation) => DispatchMapEndpoint(invocation, receiver),

      "Map" => DispatchMap(invocation, receiver),

      "WithGroupPrefix" => DispatchWithGroupPrefix(invocation, receiver),

      "WithHandler" => DispatchWithHandler(invocation, receiver),

      "WithDescription" => DispatchWithDescription(invocation, receiver),

      "AsQuery" => DispatchAsQuery(receiver),

      "AsCommand" => DispatchAsCommand(receiver),

      "AsIdempotentCommand" => DispatchAsIdempotentCommand(receiver),

      "LocalOnly" => DispatchLocalOnly(receiver),

      "Exclusive" => DispatchExclusive(receiver),

      "Done" => DispatchDone(invocation, receiver),

      "Build" => DispatchBuild(invocation, receiver),

      "WithName" => DispatchWithName(invocation, receiver),

      "WithAiPrompt" => DispatchWithAiPrompt(invocation, receiver),

      "ConfigureHelp" => DispatchConfigureHelp(invocation, receiver),

      "AddRepl" => DispatchAddRepl(invocation, receiver),

      "AddConfiguration" => DispatchAddConfiguration(receiver),

      "AddCheckUpdatesRoute" => DispatchAddCheckUpdatesRoute(receiver),

      "AddMcpRoute" => DispatchAddMcpRoute(invocation, receiver),

      "AddServeRoute" => DispatchAddServeRoute(invocation, receiver),

      "ConfigureServices" => DispatchConfigureServices(invocation, receiver),

      "AddBehavior" => DispatchAddBehavior(invocation, receiver),

      "UseTerminal" => DispatchUseTerminal(receiver),

      "UseTelemetry" => DispatchUseTelemetry(invocation, receiver),

      "UseMicrosoftDependencyInjection" => DispatchUseMicrosoftDependencyInjection(receiver),

      "UseDirectInvoke" => DispatchUseDirectInvoke(receiver),

      "UseVerbatimRendererOutput" => DispatchUseVerbatimRendererOutput(receiver),

      "UseProblemRendererForErrors" => DispatchUseProblemRendererForErrors(receiver),

      "EnableCompletion" => DispatchEnableCompletion(receiver),

      "AddTypeConverter" => DispatchAddTypeConverter(invocation, receiver),

      "WithAlias" => DispatchWithAlias(invocation, receiver),

      "WithExample" => DispatchWithExample(invocation, receiver),

      "Implements" => DispatchImplements(invocation, receiver),

      "DiscoverEndpoints" => DispatchDiscoverEndpoints(invocation, receiver),

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
  /// Checks that a <c>CreateBuilder</c> call is <c>NuruApp.CreateBuilder</c>, not another API's
  /// <c>CreateBuilder</c> (for example <c>WebApplication.CreateBuilder</c>) in the same method body.
  /// When the call does not bind, falls back to the syntax <c>NuruApp.CreateBuilder(...)</c>.
  /// </summary>
  private bool IsNuruAppCreateBuilder(InvocationExpressionSyntax invocation)
  {
    SymbolInfo symbolInfo = SemanticModel.GetSymbolInfo(invocation, CancellationToken);
    IMethodSymbol? methodSymbol = symbolInfo.Symbol as IMethodSymbol
      ?? symbolInfo.CandidateSymbols.OfType<IMethodSymbol>().FirstOrDefault();

    if (methodSymbol is null)
      return CreateBuilderLocator.IsPotentialMatch(invocation);

    INamedTypeSymbol? containingType = methodSymbol.ContainingType;
    return containingType is { Name: "NuruApp" }
      && containingType.ContainingNamespace?.ToDisplayString() == "TimeWarp.Nuru";
  }

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
    return typeName is "NuruAppBuilder"
        or "EndpointBuilder" or "GroupBuilder" or "GroupEndpointBuilder"
        or "NestedCompiledRouteBuilder" or "NuruApp";
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
}
