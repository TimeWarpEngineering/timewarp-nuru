#region Purpose
// Emits pattern matching code for individual routes.
// Generates C# pattern matching expressions for each route definition.
#endregion
#region Design
// UNIFIED MATCHING: All routes use EmitMatch (formerly EmitComplexMatch).
// A dual simple/complex code path existed previously and caused 5+ bugs (#179, #301, #302, #303, #403)
// due to behavioral concerns being added to one path but forgotten in the other.
// BUILT-IN FLAG SKIP GUARD: Routes with no literals (empty pattern) must skip built-in flags
// like --help REGARDLESS of whether they have required parameters. The check is `hasNoLiterals`
// not `minPositionalArgs == 0`. Without this, --help gets consumed as a parameter value.
//
// ALIAS MATCHING: The alias string is a COMPLETE replacement for groupPrefix + pattern literals.
// EmitAliasMatch must NOT re-emit literal segments from route.Segments — they are already
// encoded in the alias parts. Only parameters, catch-all, and end-of-options segments are emitted.
//
// EXACT LENGTH: Routes without optional params or catch-all enforce exact positional arg count.
// Unknown options never reach the positional list (see UNKNOWN OPTIONS), so a route with options
// enforces it too.
//
// UNKNOWN OPTIONS (task 515): An option-shaped token before "--" that no declared option consumed
// (NuruUnknownOption.IsOptionLike: not "-", not a number) is held out of the positional list.
// The route never runs with one. When the route's leading literals match (or, for a route with no
// leading literal, when the positional args fit the route's shape), the token and the route's option
// forms are recorded in __unknownOption / __unknownOptionForms and matching moves on. A later route or
// built-in can still match; if none does, the no-match fallback names the option and exits 1.
// The REPL calls the same ExecuteRouteAsync, so a REPL line fails the same way.
//
// OPTION GUARDS: A missing required option, a missing required value, or an absent unbound
// discriminator flag sets __optionMissing_N instead of skipping at once. The skip runs after the
// unknown-option check, so a close typo of the missing form is recorded and suggested (515 M1).
// A route with no leading literal checks its shape (length and literals) at the same point, before
// the guard skip (515 M2). The shape check also tries the positional args without the token right
// after an unknown option, since that token is likely the option's value ("{file} --outpt out").
// Only a typo of a value-taking option drops it (515 M3); a flag typo leaves the next token positional.
// Option type conversions run after that skip, because a missing value must skip, not error.
// Guards take the skip label of the block they are in; an alias block jumping to route_skip_N
// would jump backward to before itself.
//
// FILE/DIRECTORY INFO: FileInfo and DirectoryInfo constructors throw
// ArgumentException, PathTooLongException, NotSupportedException,
// UnauthorizedAccessException, and SecurityException. Catch Exception so any of
// those stay on the invalid-value exit instead of escaping the route.
//
// TYPED ARRAY CONVERSION: Catch-all parameters and repeated options call
// GetBuiltInTryConversion once per element. A failed TryParse, including
// overflow, writes the invalid-value line and returns 1. DateTimeOffset and
// Version use that same TryParse map as scalar parameters.
//
// SHARED PREFIX HELP: Routes that share leading literals are printed together by
// EmitSharedPrefixHelpChecks. EmitPerRouteHelpCheck skips those routes. A prefix
// with one route keeps the single-route help check beside that route's matcher.
//
// JSON MEMBERS: Option parsing records an argv reject flag and does not return.
// The error is written only after the route literals match, so a different
// command, --help, and --capabilities are not rejected. A required JSON member
// cannot be supplied on the argv path, so that call exits 1 and names --json-args.
#endregion

namespace TimeWarp.Nuru.Generators;

using System.Text;

/// <summary>
/// Emits pattern matching code for a single route.
/// Generates C# list patterns and conditional logic for route matching.
/// </summary>
internal static partial class RouteMatcherEmitter
{
  /// <summary>
  /// Emits the pattern matching code for a route.
  /// </summary>
  /// <param name="sb">The StringBuilder to append to.</param>
  /// <param name="route">The route definition to emit.</param>
  /// <param name="routeIndex">The index of this route (used for unique handler names).</param>
  /// <param name="services">Registered services from ConfigureServices.</param>
  /// <param name="behaviors">Pipeline behaviors to wrap the handler with.</param>
  /// <param name="customConverters">Custom type converters registered via AddTypeConverter.</param>
  /// <param name="loggerFactoryFieldName">Logger factory source for ILogger injection, or null if no logging.</param>
  /// <param name="useRuntimeDI">When true, uses GetServiceProvider().GetRequiredService&lt;T&gt;() instead of static instantiation.</param>
  /// <param name="runtimeDISuffix">Suffix for per-app runtime DI methods (e.g., "_0" for multi-app assemblies).</param>
  /// <param name="httpClientConfigurations">Named HTTP clients registered on the app.</param>
  /// <param name="sharedPrefixHelpRoutes">Routes whose help is emitted by the shared-prefix check.</param>
  /// <param name="forwardRoutes">
  /// True when the app called AddServeRoute(): a matched route that is not local-only is offered to the
  /// route forwarder before its behaviors and handler.
  /// </param>
  /// <param name="verbatimRendererOutput">When true (<c>UseVerbatimRendererOutput()</c>), renderer text is written with no added newline.</param>
  public static void Emit(
    StringBuilder sb,
    RouteDefinition route,
    int routeIndex,
    ImmutableArray<ServiceDefinition> services,
    ImmutableArray<BehaviorDefinition> behaviors = default,
    ImmutableArray<CustomConverterDefinition> customConverters = default,
    string? loggerFactoryFieldName = null,
    bool useRuntimeDI = false,
    string runtimeDISuffix = "",
    ImmutableArray<HttpClientConfiguration> httpClientConfigurations = default,
    HashSet<RouteDefinition>? sharedPrefixHelpRoutes = null,
    bool useDirectInvoke = false,
    bool hasGeneratedMediator = false,
    bool forwardRoutes = false,
    bool verbatimRendererOutput = false)
  {
    // Use empty array if optional parameters not provided
    if (behaviors.IsDefault)
      behaviors = [];
    if (customConverters.IsDefault)
      customConverters = [];

    // Comment showing the route pattern
    sb.AppendLine(
      $"    // Route: {EscapeXmlComment(route.FullPattern)}");

    // Per-route help: args that match this route's literal prefix plus --help.
    // Routes that share that prefix are printed once, ahead of this loop.
    RouteHelpEmitter.EmitPerRouteHelpCheck(sb, route, routeIndex, sharedPrefixHelpRoutes);

    // Use unified matching for all routes
    bool forward = forwardRoutes && !route.IsLocalOnly;
    EmitMatch(sb, route, routeIndex, services, behaviors, customConverters, loggerFactoryFieldName, useRuntimeDI, runtimeDISuffix, httpClientConfigurations, useDirectInvoke, hasGeneratedMediator, forward, verbatimRendererOutput);

    // Emit alias matches
    foreach (string alias in route.Aliases)
    {
      EmitAliasMatch(sb, route, routeIndex, alias, services, behaviors, customConverters, loggerFactoryFieldName, useRuntimeDI, runtimeDISuffix, httpClientConfigurations, useDirectInvoke, hasGeneratedMediator, forward, verbatimRendererOutput);
    }

    sb.AppendLine();
  }

  /// <summary>
  /// Emits route matching code using two-pass approach for all routes.
  /// Uses a two-pass approach:
  /// 1. First pass: Extract options and their values, tracking consumed indices
  /// 2. Second pass: Build positional args array, match literals, extract parameters
  /// This allows options to appear anywhere (interleaved with positional args).
  /// </summary>
  private static void EmitMatch(
    StringBuilder sb,
    RouteDefinition route,
    int routeIndex,
    ImmutableArray<ServiceDefinition> services,
    ImmutableArray<BehaviorDefinition> behaviors,
    ImmutableArray<CustomConverterDefinition> customConverters,
    string? loggerFactoryFieldName,
    bool useRuntimeDI,
    string runtimeDISuffix,
    ImmutableArray<HttpClientConfiguration> httpClientConfigurations,
    bool useDirectInvoke,
    bool hasGeneratedMediator,
    bool forward,
    bool verbatimRendererOutput)
  {
    // Calculate minimum required positional args (positional match segments + required non-catch-all params)
    // PositionalMatchSegments includes group prefix literals, pattern literals, and end-of-options
    int minPositionalArgs = route.PositionalMatchSegments.Count() + route.Parameters.Count(p => !p.IsOptional && !p.IsCatchAll);

    // Initial length check (minimum possible args)
    sb.AppendLine($"    if (routeArgs.Length >= {minPositionalArgs})");
    sb.AppendLine("    {");

    // Issue #179: Routes with no literals (empty pattern like [NuruRoute("")]) can intercept
    // built-in flags like --help. The flag ends up as a positional arg or option value.
    // Skip built-in flags for these routes regardless of required parameter count,
    // UNLESS the route explicitly maps a built-in flag (e.g., Map("--version")).
    bool hasNoLiterals = !route.PositionalMatchSegments.Any();
    bool isBuiltInFlagRoute = route.OriginalPattern is "--help" or "-h" or "--version" or "--capabilities";
    if (hasNoLiterals && !isBuiltInFlagRoute)
    {
      sb.AppendLine("      // Skip built-in flags for routes with no literals (default/options-only)");
      sb.AppendLine($"      if (routeArgs is {BuiltInFlags.PatternMatchExpression})");
      sb.AppendLine($"        goto route_skip_{routeIndex};");
    }

    // Emit the set of all option forms for this route (used to distinguish options from values)
    EmitOptionFormsSet(sb, route, routeIndex);

    // Find end-of-options marker (--)
    sb.AppendLine($"      int __endOfOptions_{routeIndex} = global::System.Array.IndexOf(routeArgs, \"--\");");
    sb.AppendLine($"      if (__endOfOptions_{routeIndex} < 0) __endOfOptions_{routeIndex} = routeArgs.Length;");

    // Track consumed indices (options and their values)
    sb.AppendLine($"      global::System.Collections.Generic.HashSet<int> __consumed_{routeIndex} = [];");

    // First pass: Extract all options (with index tracking)
    string optionConversions = EmitOptionParsingWithIndexTracking(sb, route, routeIndex, customConverters);

    // Build positional args array (excluding consumed indices; -- is included only if route expects it)
    List<string> literalPrefix = RouteHelpEmitter.GetLiteralPrefix(route);
    EmitPositionalArrayConstruction(sb, route, routeIndex, trackUnknownValues: literalPrefix.Count == 0);

    // An unknown option stops this route; record it when the command's leading literals match, or
    // for a route with no leading literal, when the positional args fit the route.
    // This runs before the option-guard skip so a typo of a required option is still suggested.
    if (literalPrefix.Count > 0)
      EmitUnknownOptionPrefixCheck(sb, routeIndex, literalPrefix, $"route_skip_{routeIndex}");
    else
      EmitUnknownOptionShapeCheck(sb, routeIndex, minPositionalArgs, exact: !route.HasOptionalPositionalParams && !route.HasCatchAll, GetPositionalLiteralChecks(route, route.GroupPrefix?.Split(' ', StringSplitOptions.RemoveEmptyEntries) ?? [], includeSegmentLiterals: true), $"route_skip_{routeIndex}");
    EmitOptionGuardSkip(sb, route, routeIndex, $"route_skip_{routeIndex}");
    sb.Append(optionConversions);

    // Check minimum positional args (required parameters must be present)
    sb.AppendLine($"      if (__positionalArgs_{routeIndex}.Length < {minPositionalArgs}) goto route_skip_{routeIndex};");

    // For routes with ONLY literals, required params, and options (no optional params, no catch-all),
    // enforce exact length matching. This prevents empty pattern routes from matching arbitrary input (Issue #179).
    // Unknown options are held out of the positional list, so routes with options enforce it too.
    bool hasOnlyLiteralsAndRequiredParams = !route.HasOptionalPositionalParams && !route.HasCatchAll;
    if (hasOnlyLiteralsAndRequiredParams)
    {
      // Enforce exact length (no extra args allowed)
      sb.AppendLine($"      if (__positionalArgs_{routeIndex}.Length > {minPositionalArgs}) goto route_skip_{routeIndex};");
    }

    // Emit matching and parameter extraction in one pass through ALL segments
    // This keeps positionalIndex correctly aligned for interleaved patterns
    // (e.g., "execute {script} -- {*args}" where param appears before separator)
    int positionalIndex = 0;

    // First, handle group prefix literals
    if (!string.IsNullOrEmpty(route.GroupPrefix))
    {
      foreach (string word in route.GroupPrefix.Split(' ', StringSplitOptions.RemoveEmptyEntries))
      {
        sb.AppendLine(
          $"      if (__positionalArgs_{routeIndex}[{positionalIndex}] != \"{EmitterStringUtils.EscapeForStringLiteral(word)}\") goto route_skip_{routeIndex};");
        positionalIndex++;
      }
    }

    // Then iterate through ALL segments, matching AND extracting at correct positions
    foreach (SegmentDefinition segment in route.Segments)
    {
      switch (segment)
      {
        case LiteralDefinition literal:
          sb.AppendLine(
            $"      if (__positionalArgs_{routeIndex}[{positionalIndex}] != \"{EmitterStringUtils.EscapeForStringLiteral(literal.Value)}\") goto route_skip_{routeIndex};");
          positionalIndex++;
          break;

        case ParameterDefinition param when param.IsCatchAll:
          // Catch-all: slice from current position to end
          string catchAllVar = PositionalCaptureVar(param, routeIndex);
          sb.AppendLine(
            $"      string[] {catchAllVar} = __positionalArgs_{routeIndex}[{positionalIndex}..];");
          // Don't increment - catch-all consumes remaining
          break;

        case ParameterDefinition param when param.IsOptional:
          // Optional param: conditional extraction
          string optVar = PositionalCaptureVar(param, routeIndex);
          sb.AppendLine(
            $"      string? {optVar} = __positionalArgs_{routeIndex}.Length > {positionalIndex} ? __positionalArgs_{routeIndex}[{positionalIndex}] : null;");
          positionalIndex++;
          break;

        case ParameterDefinition param:
          // Required param: direct extraction
          string reqVar = PositionalCaptureVar(param, routeIndex);
          sb.AppendLine(
            $"      string {reqVar} = __positionalArgs_{routeIndex}[{positionalIndex}];");
          positionalIndex++;
          break;

        case EndOfOptionsSeparatorDefinition:
          sb.AppendLine(
            $"      if (__positionalArgs_{routeIndex}[{positionalIndex}] != \"--\") goto route_skip_{routeIndex};");
          positionalIndex++;
          break;

          // OptionDefinition: skip (handled by EmitOptionParsingWithIndexTracking)
      }
    }

    // The route literal matched. JSON argv rejection and a missing required JSON member
    // stop this call here. Doing it earlier would reject every later command.
    EmitJsonMemberAfterMatch(sb, route, routeIndex);

    // Emit type conversions for typed parameters
    EmitTypeConversions(sb, route, routeIndex, customConverters, indent: 6);
    if (forward)
      EmitForward(sb, route, routeIndex);
    InterceptorEmitter.EmitMissingHandlerOnDispatch(sb, route, useRuntimeDI, hasGeneratedMediator, "      ");
    InterceptorEmitter.EmitExclusiveGate(sb, route, "      ", openBlock: true);

    // Emit handler invocation (wrapped with behaviors if any)
    if (behaviors.Length > 0)
    {
      // For endpoints (Command), the command is created by BehaviorEmitter before the pipeline
      bool commandCreatedByBehavior = route.Handler.HandlerKind == HandlerKind.Command;

      BehaviorEmitter.EmitPipelineWrapper(
        sb, route, routeIndex, behaviors, services, indent: 6,
        () => HandlerInvokerEmitter.Emit(sb, route, routeIndex, services, indent: 8, commandAlreadyCreated: commandCreatedByBehavior, loggerFactoryFieldName: loggerFactoryFieldName, useRuntimeDI: useRuntimeDI, runtimeDISuffix: runtimeDISuffix, httpClientConfigurations: httpClientConfigurations, useDirectInvoke: useDirectInvoke, hasGeneratedMediator: hasGeneratedMediator, verbatimRendererOutput: verbatimRendererOutput),
        methodSuffix: runtimeDISuffix);

      sb.AppendLine("      return global::TimeWarp.Nuru.NuruCallContext.CurrentExitCode;");
    }
    else
    {
      HandlerInvokerEmitter.Emit(sb, route, routeIndex, services, indent: 6, loggerFactoryFieldName: loggerFactoryFieldName, useRuntimeDI: useRuntimeDI, runtimeDISuffix: runtimeDISuffix, httpClientConfigurations: httpClientConfigurations, useDirectInvoke: useDirectInvoke, hasGeneratedMediator: hasGeneratedMediator, verbatimRendererOutput: verbatimRendererOutput);
      sb.AppendLine("      return global::TimeWarp.Nuru.NuruCallContext.CurrentExitCode;");
    }

    InterceptorEmitter.EmitExclusiveGateEnd(sb, route, "      ");
    sb.AppendLine("    }");
    sb.AppendLine($"    route_skip_{routeIndex}:;");
  }
}
