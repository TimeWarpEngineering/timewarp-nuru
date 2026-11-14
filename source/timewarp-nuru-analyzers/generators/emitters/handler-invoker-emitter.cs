#region Purpose
// Emits handler invocation code based on handler kind (delegate, command, and method-based handlers).
// Delegate lambda bodies become local functions; command handlers instantiate the nested Handler
// class and call Handle().
#endregion

namespace TimeWarp.Nuru.Generators;

using System.Text;

/// <summary>Determines how handler results should be output to the terminal.</summary>
internal enum OutputStrategy
{
  /// <summary>No output (Unit, void, null).</summary>
  None,
  /// <summary>Raw ToString() output (primitives, strings).</summary>
  Raw,
  /// <summary>ISO 8601 formatted output (DateTime, DateTimeOffset, DateOnly, TimeOnly).</summary>
  Iso8601,
  /// <summary>Culture-invariant formatted output (TimeSpan).</summary>
  Invariant,
  /// <summary>JSON serialized output (complex objects, collections).</summary>
  Json
}

/// <summary>
/// Emits code to invoke a route's handler.
/// Handles different handler kinds: delegate, command, and method.
/// </summary>
internal static partial class HandlerInvokerEmitter
{
  /// <summary>
  /// Emits handler invocation code for a route.
  /// </summary>
  /// <param name="sb">The StringBuilder to append to.</param>
  /// <param name="route">The route containing the handler to invoke.</param>
  /// <param name="routeIndex">The index of this route (used for unique local function names).</param>
  /// <param name="services">Registered services from ConfigureServices.</param>
  /// <param name="indent">Number of spaces for indentation.</param>
  /// <param name="commandAlreadyCreated">If true, skip command creation (already done by behavior emitter).</param>
  /// <param name="loggerFactoryFieldName">Logger factory source (static field name or "app.LoggerFactory"), or null if no logging.</param>
  /// <param name="useRuntimeDI">When true, uses GetServiceProvider().GetRequiredService&lt;T&gt;() instead of static instantiation.</param>
  /// <param name="runtimeDISuffix">Suffix for per-app runtime DI methods (e.g., "_0" for multi-app assemblies).</param>
  /// <param name="captureResultInto">
  /// When set, the handler result is assigned to this <c>object?</c> expression instead of being written
  /// to the terminal. <c>NuruApp.InvokeAsync</c> uses it; <see cref="EmitRender"/> writes the text later.
  /// </param>
  /// <param name="verbatimRendererOutput">When true (<c>UseVerbatimRendererOutput()</c>), renderer text is written with no added newline.</param>
  public static void Emit(StringBuilder sb, RouteDefinition route, int routeIndex, ImmutableArray<ServiceDefinition> services, int indent = 6, bool commandAlreadyCreated = false, string? loggerFactoryFieldName = null, bool useRuntimeDI = false, string runtimeDISuffix = "", ImmutableArray<HttpClientConfiguration> httpClientConfigurations = default, bool useDirectInvoke = false, bool hasGeneratedMediator = false, bool jsonArgsPath = false, string? captureResultInto = null, bool verbatimRendererOutput = false)
  {
    string indentStr = new(' ', indent);
    HandlerDefinition handler = route.Handler;

    // First, resolve any required services
    if (handler.RequiresServiceProvider)
    {
      ServiceResolverEmitter.Emit(sb, handler, services, indent, useRuntimeDI, runtimeDISuffix, loggerFactoryFieldName, httpClientConfigurations);
    }

    // Emit handler invocation based on kind
    switch (handler.HandlerKind)
    {
      case HandlerKind.Delegate:
        EmitDelegateInvocation(sb, route, routeIndex, indentStr, captureResultInto);
        break;

      case HandlerKind.Command:
        EmitCommandInvocation(sb, route, routeIndex, services, indentStr, commandAlreadyCreated, loggerFactoryFieldName, useRuntimeDI, runtimeDISuffix, httpClientConfigurations, useDirectInvoke, hasGeneratedMediator, jsonArgsPath, captureResultInto, verbatimRendererOutput);
        break;

      case HandlerKind.Method:
        EmitMethodInvocation(sb, route, indentStr, captureResultInto);
        break;
    }
  }

  /// <summary>
  /// Emits invocation for a delegate-based handler.
  /// Transforms the lambda into a local function and invokes it.
  /// </summary>
  private static void EmitDelegateInvocation(StringBuilder sb, RouteDefinition route, int routeIndex, string indent, string? captureResultInto)
  {
    HandlerDefinition handler = route.Handler;

    // If no lambda body was captured, emit an error comment
    if (string.IsNullOrEmpty(handler.LambdaBodySource))
    {
      sb.AppendLine(
        $"{indent}// ERROR: Lambda body was not captured for this handler");
      sb.AppendLine(
        $"{indent}throw new System.NotSupportedException(\"Handler code was not captured at compile time.\");");
      return;
    }

    string handlerName = $"__handler_{routeIndex}";

    if (handler.IsExpressionBody)
    {
      // Expression body: () => "pong" or (string name) => $"Hello {name}"
      EmitExpressionBodyHandler(sb, route, routeIndex, handler, handlerName, indent, captureResultInto);
    }
    else
    {
      // Block body: () => { DoWork(); return "done"; }
      EmitBlockBodyHandler(sb, route, routeIndex, handler, handlerName, indent, captureResultInto);
    }
  }

  /// <summary>
  /// Emits a local function for an expression-body lambda.
  /// </summary>
  private static void EmitExpressionBodyHandler(StringBuilder sb, RouteDefinition route, int routeIndex, HandlerDefinition handler, string handlerName, string indent, string? captureResultInto)
  {
    string returnTypeName = GetReturnTypeName(handler);
    string asyncModifier = handler.IsAsync ? "async " : "";
    string awaitKeyword = handler.IsAsync ? "await " : "";

    // An async lambda's expression body already awaits whatever it needs (e.g. `async s => await s.Send(q)`),
    // so it is emitted verbatim. Only a non-async Task-returning body (e.g. `() => Task.FromResult(1)`) is awaited.
    string bodyAwaitKeyword = handler.IsAsync && !handler.HasAsyncModifier ? "await " : "";

    // Build parameter list for the local function (preserves handler parameter names)
    string paramList = BuildParameterList(handler);
    string argList = BuildArgumentListFromRoute(route, routeIndex, handler);

    if (handler.ReturnType.HasValue)
    {
      // Expression with return value
      // Generate: ReturnType __handler_N(Type param1, ...) => expression;
      sb.AppendLine(
        $"{indent}{asyncModifier}{returnTypeName} {handlerName}({paramList}) => {bodyAwaitKeyword}{handler.LambdaBodySource};");

      // Invoke and capture result
      string resultTypeName = handler.ReturnType.UnwrappedTypeName ?? handler.ReturnType.FullTypeName;
      sb.AppendLine(
        $"{indent}{GetUnwrappedReturnTypeName(handler)} result = {awaitKeyword}{handlerName}({argList});");

      EmitResultOrCapture(sb, indent, resultTypeName, handler.ReturnType.ValueKind, captureResultInto);
    }
    else
    {
      // Void expression (side-effect only)
      // Generate: void __handler_N(Type param1, ...) => expression;
      if (handler.IsAsync)
      {
        sb.AppendLine(
          $"{indent}async Task {handlerName}({paramList}) => {bodyAwaitKeyword}{handler.LambdaBodySource};");
        sb.AppendLine(
          $"{indent}await {handlerName}({argList});");
      }
      else
      {
        sb.AppendLine(
          $"{indent}void {handlerName}({paramList}) => {handler.LambdaBodySource};");
        sb.AppendLine(
          $"{indent}{handlerName}({argList});");
      }
    }
  }

  /// <summary>
  /// Emits a local function for a block-body lambda.
  /// </summary>
  private static void EmitBlockBodyHandler(StringBuilder sb, RouteDefinition route, int routeIndex, HandlerDefinition handler, string handlerName, string indent, string? captureResultInto)
  {
    string returnTypeName = GetReturnTypeName(handler);
    string asyncModifier = handler.IsAsync ? "async " : "";
    string awaitKeyword = handler.IsAsync ? "await " : "";

    // Build parameter list for the local function (preserves handler parameter names)
    string paramList = BuildParameterList(handler);
    string argList = BuildArgumentListFromRoute(route, routeIndex, handler);

    // Emit the local function signature
    sb.AppendLine(
      $"{indent}{asyncModifier}{returnTypeName} {handlerName}({paramList})");

    // Re-indent the block body to match target indentation
    string reindentedBody = ReindentBlockBody(handler.LambdaBodySource ?? "{}", indent);
    sb.Append(reindentedBody);

    if (handler.ReturnType.HasValue)
    {
      // Invoke and capture result
      string resultTypeName = handler.ReturnType.UnwrappedTypeName ?? handler.ReturnType.FullTypeName;
      sb.AppendLine(
        $"{indent}{GetUnwrappedReturnTypeName(handler)} result = {awaitKeyword}{handlerName}({argList});");

      EmitResultOrCapture(sb, indent, resultTypeName, handler.ReturnType.ValueKind, captureResultInto);
    }
    else
    {
      // Void - just invoke
      sb.AppendLine(
        $"{indent}{awaitKeyword}{handlerName}({argList});");
    }
  }

  /// <summary>
  /// Re-indents a block body to match the target indentation level.
  /// Strips the original indentation and applies new indentation to each line.
  /// </summary>
  private static string ReindentBlockBody(string blockBody, string targetIndent)
  {
    if (string.IsNullOrEmpty(blockBody))
      return $"{targetIndent}{{}}\n";

    string[] lines = blockBody.Split('\n');
    if (lines.Length == 0)
      return $"{targetIndent}{{}}\n";

    // Find the minimum indentation (excluding empty lines)
    int minIndent = int.MaxValue;
    foreach (string line in lines)
    {
      if (string.IsNullOrWhiteSpace(line))
        continue;

      int lineIndent = 0;
      foreach (char c in line)
      {
        if (c == ' ')
          lineIndent++;
        else if (c == '\t')
          lineIndent += 2; // Treat tab as 2 spaces
        else
          break;
      }

      if (lineIndent < minIndent)
        minIndent = lineIndent;
    }

    if (minIndent == int.MaxValue)
      minIndent = 0;

    // Re-indent each line
    StringBuilder result = new();
    foreach (string line in lines)
    {
      if (string.IsNullOrWhiteSpace(line))
      {
        result.AppendLine();
        continue;
      }

      // Strip original indentation and apply target indentation
      string trimmedLine = line.Length > minIndent ? line[minIndent..] : line.TrimStart();
      result.AppendLine($"{targetIndent}{trimmedLine}");
    }

    return result.ToString();
  }

  /// <summary>
  /// Gets the return type name for the local function signature.
  /// </summary>
  private static string GetReturnTypeName(HandlerDefinition handler)
  {
    if (handler.ReturnType.IsVoid && !handler.IsAsync)
      return "void";

    if (handler.IsAsync)
    {
      if (handler.ReturnType.HasValue)
        return handler.ReturnType.ShortTypeName; // Task<T>
      return "Task";
    }

    return handler.ReturnType.ShortTypeName;
  }

  /// <summary>
  /// Gets the unwrapped return type name (T from Task&lt;T&gt;, or the type itself if not async).
  /// </summary>
  private static string GetUnwrappedReturnTypeName(HandlerDefinition handler)
  {
    if (handler.IsAsync && handler.ReturnType.UnwrappedTypeName is not null)
    {
      string unwrapped = handler.ReturnType.UnwrappedTypeName;

      // Preserve fully qualified name for Unit to avoid ambiguity
      // when other packages (e.g. MediatR) also define a Unit type.
      if (unwrapped == "global::TimeWarp.Mediator.Unit")
        return unwrapped;

      // For Task<T>, extract short name from fully qualified unwrapped type
      int lastDot = unwrapped.LastIndexOf('.', StringComparison.Ordinal);
      return lastDot >= 0 ? unwrapped[(lastDot + 1)..] : unwrapped;
    }

    return handler.ReturnType.ShortTypeName;
  }

  private const string ProblemDetailsTypeName = "global::TimeWarp.Foundation.Types.SharedProblemDetails";

  private const string ProblemRendererServiceTypeName = $"global::TimeWarp.Contracts.IResponseRenderer<{ProblemDetailsTypeName}>";
}
