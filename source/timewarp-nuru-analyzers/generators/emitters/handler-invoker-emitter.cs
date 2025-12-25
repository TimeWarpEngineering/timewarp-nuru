// Emits handler invocation code based on handler kind.
// Supports delegate, mediator, and method-based handlers.
// For delegate handlers, transforms lambda bodies into local functions.

namespace TimeWarp.Nuru.Generators;

using System.Text;

/// <summary>
/// Emits code to invoke a route's handler.
/// Handles different handler kinds: delegate, mediator, and method.
/// </summary>
internal static class HandlerInvokerEmitter
{
  /// <summary>
  /// Emits handler invocation code for a route.
  /// </summary>
  /// <param name="sb">The StringBuilder to append to.</param>
  /// <param name="route">The route containing the handler to invoke.</param>
  /// <param name="routeIndex">The index of this route (used for unique local function names).</param>
  /// <param name="indent">Number of spaces for indentation.</param>
  public static void Emit(StringBuilder sb, RouteDefinition route, int routeIndex, int indent = 6)
  {
    string indentStr = new(' ', indent);
    HandlerDefinition handler = route.Handler;

    // First, resolve any required services
    if (handler.RequiresServiceProvider)
    {
      ServiceResolverEmitter.Emit(sb, handler, indent);
    }

    // Emit handler invocation based on kind
    switch (handler.HandlerKind)
    {
      case HandlerKind.Delegate:
        EmitDelegateInvocation(sb, route, routeIndex, indentStr);
        break;

      case HandlerKind.Mediator:
        EmitMediatorInvocation(sb, route, indentStr);
        break;

      case HandlerKind.Method:
        EmitMethodInvocation(sb, route, indentStr);
        break;
    }
  }

  /// <summary>
  /// Emits invocation for a delegate-based handler.
  /// Transforms the lambda into a local function and invokes it.
  /// </summary>
  private static void EmitDelegateInvocation(StringBuilder sb, RouteDefinition route, int routeIndex, string indent)
  {
    HandlerDefinition handler = route.Handler;

    // If no lambda body was captured, emit an error comment
    if (string.IsNullOrEmpty(handler.LambdaBodySource))
    {
      sb.AppendLine(CultureInfo.InvariantCulture,
        $"{indent}// ERROR: Lambda body was not captured for this handler");
      sb.AppendLine(CultureInfo.InvariantCulture,
        $"{indent}throw new System.NotSupportedException(\"Handler code was not captured at compile time.\");");
      return;
    }

    string handlerName = $"__handler_{routeIndex}";

    if (handler.IsExpressionBody)
    {
      // Expression body: () => "pong" or (string name) => $"Hello {name}"
      EmitExpressionBodyHandler(sb, handler, handlerName, indent);
    }
    else
    {
      // Block body: () => { DoWork(); return "done"; }
      EmitBlockBodyHandler(sb, handler, handlerName, indent);
    }
  }

  /// <summary>
  /// Emits a local function for an expression-body lambda.
  /// </summary>
  private static void EmitExpressionBodyHandler(StringBuilder sb, HandlerDefinition handler, string handlerName, string indent)
  {
    string returnTypeName = GetReturnTypeName(handler);
    string asyncModifier = handler.IsAsync ? "async " : "";
    string awaitKeyword = handler.IsAsync ? "await " : "";

    if (handler.ReturnType.HasValue)
    {
      // Expression with return value
      // Generate: ReturnType __handler_N() => expression;
      sb.AppendLine(CultureInfo.InvariantCulture,
        $"{indent}{asyncModifier}{returnTypeName} {handlerName}() => {awaitKeyword}{handler.LambdaBodySource};");

      // Invoke and capture result
      sb.AppendLine(CultureInfo.InvariantCulture,
        $"{indent}{GetUnwrappedReturnTypeName(handler)} result = {awaitKeyword}{handlerName}();");

      EmitResultOutput(sb, indent);
    }
    else
    {
      // Void expression (side-effect only)
      // Generate: void __handler_N() => expression;
      if (handler.IsAsync)
      {
        sb.AppendLine(CultureInfo.InvariantCulture,
          $"{indent}async Task {handlerName}() => await {handler.LambdaBodySource};");
        sb.AppendLine(CultureInfo.InvariantCulture,
          $"{indent}await {handlerName}();");
      }
      else
      {
        sb.AppendLine(CultureInfo.InvariantCulture,
          $"{indent}void {handlerName}() => {handler.LambdaBodySource};");
        sb.AppendLine(CultureInfo.InvariantCulture,
          $"{indent}{handlerName}();");
      }
    }
  }

  /// <summary>
  /// Emits a local function for a block-body lambda.
  /// </summary>
  private static void EmitBlockBodyHandler(StringBuilder sb, HandlerDefinition handler, string handlerName, string indent)
  {
    string returnTypeName = GetReturnTypeName(handler);
    string asyncModifier = handler.IsAsync ? "async " : "";
    string awaitKeyword = handler.IsAsync ? "await " : "";

    // Emit the local function with the block body
    // The block already includes { and }, so we just need to add the signature
    sb.AppendLine(CultureInfo.InvariantCulture,
      $"{indent}{asyncModifier}{returnTypeName} {handlerName}()");
    sb.AppendLine(CultureInfo.InvariantCulture,
      $"{indent}{handler.LambdaBodySource}");

    if (handler.ReturnType.HasValue)
    {
      // Invoke and capture result
      sb.AppendLine(CultureInfo.InvariantCulture,
        $"{indent}{GetUnwrappedReturnTypeName(handler)} result = {awaitKeyword}{handlerName}();");

      EmitResultOutput(sb, indent);
    }
    else
    {
      // Void - just invoke
      sb.AppendLine(CultureInfo.InvariantCulture,
        $"{indent}{awaitKeyword}{handlerName}();");
    }
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
      // For Task<T>, use T
      // Extract short name from fully qualified unwrapped type
      string unwrapped = handler.ReturnType.UnwrappedTypeName;
      int lastDot = unwrapped.LastIndexOf('.');
      return lastDot >= 0 ? unwrapped[(lastDot + 1)..] : unwrapped;
    }

    return handler.ReturnType.ShortTypeName;
  }

  /// <summary>
  /// Emits invocation for a mediator-based handler.
  /// Creates the request object and sends it via the mediator.
  /// </summary>
  private static void EmitMediatorInvocation(StringBuilder sb, RouteDefinition route, string indent)
  {
    HandlerDefinition handler = route.Handler;
    string typeName = handler.FullTypeName ?? "UnknownRequest";

    // Create the request object with property initializers
    sb.AppendLine(CultureInfo.InvariantCulture,
      $"{indent}{typeName} request = new()");
    sb.AppendLine(CultureInfo.InvariantCulture, $"{indent}{{");

    foreach (ParameterBinding param in handler.RouteParameters)
    {
      string propName = ToPascalCase(param.ParameterName);
      sb.AppendLine(CultureInfo.InvariantCulture,
        $"{indent}  {propName} = {param.ParameterName},");
    }

    sb.AppendLine(CultureInfo.InvariantCulture, $"{indent}}};");

    // Send via mediator
    if (handler.ReturnType.HasValue)
    {
      sb.AppendLine(CultureInfo.InvariantCulture,
        $"{indent}{handler.ReturnType.UnwrappedTypeName ?? "object"} result = await app.Mediator.Send(request, cancellationToken);");
      EmitResultOutput(sb, indent);
    }
    else
    {
      sb.AppendLine(CultureInfo.InvariantCulture, $"{indent}await app.Mediator.Send(request, cancellationToken);");
    }
  }

  /// <summary>
  /// Emits invocation for a method-based handler (method group).
  /// Calls the static method directly.
  /// </summary>
  private static void EmitMethodInvocation(StringBuilder sb, RouteDefinition route, string indent)
  {
    HandlerDefinition handler = route.Handler;
    string typeName = handler.FullTypeName ?? "UnknownHandler";
    string methodName = handler.MethodName ?? "Handle";
    string args = BuildArgumentList(handler);
    string awaitKeyword = handler.IsAsync ? "await " : "";

    if (handler.ReturnType.HasValue)
    {
      string resultType = handler.IsAsync
        ? GetUnwrappedReturnTypeName(handler)
        : handler.ReturnType.ShortTypeName;

      sb.AppendLine(CultureInfo.InvariantCulture,
        $"{indent}{resultType} result = {awaitKeyword}{typeName}.{methodName}({args});");
      EmitResultOutput(sb, indent);
    }
    else
    {
      sb.AppendLine(CultureInfo.InvariantCulture,
        $"{indent}{awaitKeyword}{typeName}.{methodName}({args});");
    }
  }

  /// <summary>
  /// Emits code to output the handler result to the terminal.
  /// </summary>
  private static void EmitResultOutput(StringBuilder sb, string indent)
  {
    sb.AppendLine(CultureInfo.InvariantCulture, $"{indent}if (result is not null)");
    sb.AppendLine(CultureInfo.InvariantCulture, $"{indent}{{");
    sb.AppendLine(CultureInfo.InvariantCulture, $"{indent}  app.Terminal.WriteLine(result.ToString());");
    sb.AppendLine(CultureInfo.InvariantCulture, $"{indent}}}");
  }

  /// <summary>
  /// Builds the argument list for handler invocation.
  /// </summary>
  private static string BuildArgumentList(HandlerDefinition handler)
  {
    List<string> args = [];

    foreach (ParameterBinding param in handler.Parameters)
    {
      switch (param.Source)
      {
        case BindingSource.Parameter:
        case BindingSource.Option:
        case BindingSource.Flag:
        case BindingSource.CatchAll:
          // Route parameters use their captured variable name
          args.Add(param.ParameterName);
          break;

        case BindingSource.Service:
          // Service parameters use the resolved service variable
          args.Add(param.ParameterName);
          break;

        case BindingSource.CancellationToken:
          args.Add("cancellationToken");
          break;
      }
    }

    return string.Join(", ", args);
  }

  /// <summary>
  /// Converts a string to PascalCase.
  /// </summary>
  private static string ToPascalCase(string value)
  {
    if (string.IsNullOrEmpty(value))
      return value;

    return char.ToUpperInvariant(value[0]) + value[1..];
  }
}
