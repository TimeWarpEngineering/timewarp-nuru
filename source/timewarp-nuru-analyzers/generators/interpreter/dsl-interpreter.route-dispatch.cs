#region Purpose
// Map, endpoint, and route-level dispatch handlers for the DSL interpreter.
#endregion

namespace TimeWarp.Nuru.Generators;

using Microsoft.CodeAnalysis.Text;

public sealed partial class DslInterpreter
{
  /// <summary>
  /// Dispatches DiscoverEndpoints() call to enable endpoint discovery.
  /// </summary>
  private object? DispatchDiscoverEndpoints(InvocationExpressionSyntax invocation, object? receiver)
  {
    if (receiver is not IIrAppBuilder appBuilder)
    {
      throw new InvalidOperationException(
        "DiscoverEndpoints() must be called on an app builder.");
    }

    ArgumentListSyntax? args = invocation.ArgumentList;
    if (args?.Arguments.Count > 0)
    {
      // Extract type arguments from params array (typeof() expressions)
      ImmutableArray<string>.Builder typeNames = ImmutableArray.CreateBuilder<string>();
      foreach (ArgumentSyntax arg in args.Arguments)
      {
        if (arg.Expression is TypeOfExpressionSyntax typeofExpr)
        {
          SymbolInfo symbolInfo = SemanticModel.GetSymbolInfo(typeofExpr.Type);
          if (symbolInfo.Symbol is INamedTypeSymbol typeSymbol)
          {
            string typeName = typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            typeNames.Add(typeName);
          }
        }
      }

      return appBuilder.DiscoverEndpoints(typeNames.ToImmutable());
    }

    // No arguments - discover all endpoints. NURU_A003 points at the method name, not at the
    // start of the builder chain, so a #pragma on the DiscoverEndpoints() line suppresses it.
    LocationInfo? callLocation = invocation.Expression is MemberAccessExpressionSyntax memberAccess
      ? LocationInfo.CreateFrom(Location.Create(
          invocation.SyntaxTree,
          TextSpan.FromBounds(memberAccess.Name.SpanStart, invocation.Span.End)))
      : LocationInfo.CreateFrom(invocation.GetLocation());

    return appBuilder.DiscoverEndpoints(callLocation);
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
    ITypeSymbol? typeSymbol = TypeSyntaxResolver.ResolveType(SemanticModel, typeSyntax, CancellationToken);

    return typeSymbol?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
  }

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

      InvalidIdentifierError e =>
        Diagnostic.Create(DiagnosticDescriptors.InvalidIdentifier, location, e.InvalidIdentifier),

      InvalidModifierCombinationError e =>
        Diagnostic.Create(DiagnosticDescriptors.InvalidModifierCombination, location, e.ParameterName),

      AdjacentParametersError =>
        Diagnostic.Create(DiagnosticDescriptors.AdjacentParameters, location, "{a} {b}", "{a}{b}"),

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
  private object? DispatchWithGroupPrefix(InvocationExpressionSyntax invocation, object? receiver)
  {
    if (!IsDslBuilderMethod(invocation)) return null;

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
  private object? DispatchWithDescription(InvocationExpressionSyntax invocation, object? receiver)
  {
    if (!IsDslBuilderMethod(invocation)) return null;

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
}
