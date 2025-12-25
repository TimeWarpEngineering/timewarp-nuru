// Emits handler invocation code based on handler kind.
// Supports delegate, mediator, and method-based handlers.

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
  /// <param name="indent">Number of spaces for indentation.</param>
  public static void Emit(StringBuilder sb, RouteDefinition route, int indent = 6)
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
        EmitDelegateInvocation(sb, route, indentStr);
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
  /// The delegate code is captured at extraction time.
  /// </summary>
  private static void EmitDelegateInvocation(StringBuilder sb, RouteDefinition route, string indent)
  {
    HandlerDefinition handler = route.Handler;
    string args = BuildArgumentList(handler);

    if (handler.IsAsync)
    {
      if (handler.ReturnType.HasValue)
      {
        sb.AppendLine(CultureInfo.InvariantCulture,
          $"{indent}{handler.ReturnType.ShortTypeName.Replace("Task<", "", StringComparison.Ordinal).TrimEnd('>')} result = await Handler({args});");
        EmitResultOutput(sb, indent);
      }
      else
      {
        sb.AppendLine(CultureInfo.InvariantCulture,
          $"{indent}await Handler({args});");
      }
    }
    else
    {
      if (handler.ReturnType.HasValue)
      {
        sb.AppendLine(CultureInfo.InvariantCulture,
          $"{indent}{handler.ReturnType.ShortTypeName} result = Handler({args});");
        EmitResultOutput(sb, indent);
      }
      else
      {
        sb.AppendLine(CultureInfo.InvariantCulture,
          $"{indent}Handler({args});");
      }
    }
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
  /// Emits invocation for a method-based handler.
  /// Resolves the containing type and calls the method.
  /// </summary>
  private static void EmitMethodInvocation(StringBuilder sb, RouteDefinition route, string indent)
  {
    HandlerDefinition handler = route.Handler;
    string typeName = handler.FullTypeName ?? "UnknownHandler";
    string methodName = handler.MethodName ?? "Handle";
    string args = BuildArgumentList(handler);

    // Resolve the handler instance
    sb.AppendLine(CultureInfo.InvariantCulture,
      $"{indent}{typeName} handlerInstance = app.Services.GetRequiredService<{typeName}>();");

    if (handler.IsAsync)
    {
      if (handler.ReturnType.HasValue)
      {
        sb.AppendLine(CultureInfo.InvariantCulture,
          $"{indent}{handler.ReturnType.UnwrappedTypeName ?? "object"} result = await handlerInstance.{methodName}({args});");
        EmitResultOutput(sb, indent);
      }
      else
      {
        sb.AppendLine(CultureInfo.InvariantCulture,
          $"{indent}await handlerInstance.{methodName}({args});");
      }
    }
    else
    {
      if (handler.ReturnType.HasValue)
      {
        sb.AppendLine(CultureInfo.InvariantCulture,
          $"{indent}{handler.ReturnType.ShortTypeName} result = handlerInstance.{methodName}({args});");
        EmitResultOutput(sb, indent);
      }
      else
      {
        sb.AppendLine(CultureInfo.InvariantCulture,
          $"{indent}handlerInstance.{methodName}({args});");
      }
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
