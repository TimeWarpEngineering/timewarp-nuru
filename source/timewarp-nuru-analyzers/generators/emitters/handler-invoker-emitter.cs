// Emits handler invocation code based on handler kind.
// Supports delegate, command, and method-based handlers.
// For delegate handlers, transforms lambda bodies into local functions.
// For command handlers, instantiates nested Handler class and calls Handle().

namespace TimeWarp.Nuru.Generators;

using System.Text;

/// <summary>
/// Emits code to invoke a route's handler.
/// Handles different handler kinds: delegate, command, and method.
/// </summary>
internal static class HandlerInvokerEmitter
{
  /// <summary>
  /// Emits handler invocation code for a route.
  /// </summary>
  /// <param name="sb">The StringBuilder to append to.</param>
  /// <param name="route">The route containing the handler to invoke.</param>
  /// <param name="routeIndex">The index of this route (used for unique local function names).</param>
  /// <param name="services">Registered services from ConfigureServices.</param>
  /// <param name="indent">Number of spaces for indentation.</param>
  public static void Emit(StringBuilder sb, RouteDefinition route, int routeIndex, ImmutableArray<ServiceDefinition> services, int indent = 6)
  {
    string indentStr = new(' ', indent);
    HandlerDefinition handler = route.Handler;

    // First, resolve any required services
    if (handler.RequiresServiceProvider)
    {
      ServiceResolverEmitter.Emit(sb, handler, services, indent);
    }

    // Emit handler invocation based on kind
    switch (handler.HandlerKind)
    {
      case HandlerKind.Delegate:
        EmitDelegateInvocation(sb, route, routeIndex, indentStr);
        break;

      case HandlerKind.Command:
        EmitCommandInvocation(sb, route, services, indentStr);
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
      string resultTypeName = handler.ReturnType.UnwrappedTypeName ?? handler.ReturnType.FullTypeName;
      sb.AppendLine(CultureInfo.InvariantCulture,
        $"{indent}{GetUnwrappedReturnTypeName(handler)} result = {awaitKeyword}{handlerName}();");

      EmitResultOutput(sb, indent, resultTypeName);
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
      string resultTypeName = handler.ReturnType.UnwrappedTypeName ?? handler.ReturnType.FullTypeName;
      sb.AppendLine(CultureInfo.InvariantCulture,
        $"{indent}{GetUnwrappedReturnTypeName(handler)} result = {awaitKeyword}{handlerName}();");

      EmitResultOutput(sb, indent, resultTypeName);
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
  /// Emits invocation for a command-based handler with nested Handler class.
  /// Creates the command object, resolves handler dependencies, and invokes Handle().
  /// </summary>
  private static void EmitCommandInvocation(StringBuilder sb, RouteDefinition route, ImmutableArray<ServiceDefinition> services, string indent)
  {
    HandlerDefinition handler = route.Handler;
    string commandTypeName = handler.FullTypeName ?? "UnknownCommand";
    string handlerTypeName = handler.NestedHandlerTypeName ?? $"{commandTypeName}.Handler";

    // 1. Create the command object with property initializers
    sb.AppendLine(CultureInfo.InvariantCulture, $"{indent}// Create command instance with bound properties");
    sb.AppendLine(CultureInfo.InvariantCulture, $"{indent}{commandTypeName} __command = new()");
    sb.AppendLine(CultureInfo.InvariantCulture, $"{indent}{{");

    foreach (ParameterBinding param in handler.RouteParameters)
    {
      string propName = ToPascalCase(param.ParameterName);
      sb.AppendLine(CultureInfo.InvariantCulture,
        $"{indent}  {propName} = {param.ParameterName},");
    }

    sb.AppendLine(CultureInfo.InvariantCulture, $"{indent}}};");
    sb.AppendLine();

    // 2. Resolve handler constructor dependencies
    if (handler.ConstructorDependencies.Length > 0)
    {
      sb.AppendLine(CultureInfo.InvariantCulture, $"{indent}// Resolve handler constructor dependencies");
      foreach (ParameterBinding dep in handler.ConstructorDependencies)
      {
        string resolution = ResolveServiceExpression(dep.ParameterTypeName, services);
        sb.AppendLine(CultureInfo.InvariantCulture,
          $"{indent}{dep.ParameterTypeName} __{dep.ParameterName} = {resolution};");
      }

      sb.AppendLine();
    }

    // 3. Create handler instance
    sb.AppendLine(CultureInfo.InvariantCulture, $"{indent}// Create handler and invoke");
    string constructorArgs = string.Join(", ", handler.ConstructorDependencies.Select(d => $"__{d.ParameterName}"));
    sb.AppendLine(CultureInfo.InvariantCulture,
      $"{indent}{handlerTypeName} __handler = new({constructorArgs});");

    // 4. Invoke Handle method
    if (handler.ReturnType.HasValue)
    {
      string resultTypeName = handler.ReturnType.UnwrappedTypeName ?? handler.ReturnType.FullTypeName;
      sb.AppendLine(CultureInfo.InvariantCulture,
        $"{indent}{resultTypeName} result = await __handler.Handle(__command, cancellationToken);");
      EmitResultOutput(sb, indent, resultTypeName);
    }
    else
    {
      sb.AppendLine(CultureInfo.InvariantCulture,
        $"{indent}await __handler.Handle(__command, cancellationToken);");
    }
  }

  /// <summary>
  /// Resolves a service type to its instantiation expression.
  /// Uses the same static resolution approach as delegate service injection.
  /// </summary>
  private static string ResolveServiceExpression(string? serviceTypeName, ImmutableArray<ServiceDefinition> services)
  {
    // Well-known services get direct resolution
    return serviceTypeName switch
    {
      "global::TimeWarp.Terminal.ITerminal" => "app.Terminal",
      "global::Microsoft.Extensions.Configuration.IConfiguration" => "configuration",
      "global::System.Threading.CancellationToken" => "cancellationToken",
      _ => ResolveRegisteredService(serviceTypeName, services)
    };
  }

  /// <summary>
  /// Resolves a registered service from ConfigureServices.
  /// </summary>
  private static string ResolveRegisteredService(string? serviceTypeName, ImmutableArray<ServiceDefinition> services)
  {
    if (string.IsNullOrEmpty(serviceTypeName))
      return "default!";

    // Find matching service registration
    ServiceDefinition? service = services.FirstOrDefault(s => s.ServiceTypeName == serviceTypeName);
    if (service is not null)
    {
      // Return instantiation based on registration lifetime
      // Singletons use Lazy<T> pattern for thread-safety
      // Transient/Scoped create new instances
      return service.Lifetime == ServiceLifetime.Singleton
        ? $"__{ToVariableName(service.ShortServiceTypeName)}.Value"
        : $"new {service.ImplementationTypeName}()";
    }

    // Fallback: try direct instantiation
    return $"new {serviceTypeName}()";
  }

  /// <summary>
  /// Converts a type name to a variable name (camelCase, removes leading I for interfaces).
  /// </summary>
  private static string ToVariableName(string typeName)
  {
    // Remove I prefix from interfaces
    if (typeName.Length > 1 && typeName[0] == 'I' && char.IsUpper(typeName[1]))
      typeName = typeName[1..];

    // Convert to camelCase
    return char.ToLowerInvariant(typeName[0]) + typeName[1..];
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
      string resultTypeName = handler.ReturnType.UnwrappedTypeName ?? handler.ReturnType.FullTypeName;
      string resultType = handler.IsAsync
        ? GetUnwrappedReturnTypeName(handler)
        : handler.ReturnType.ShortTypeName;

      sb.AppendLine(CultureInfo.InvariantCulture,
        $"{indent}{resultType} result = {awaitKeyword}{typeName}.{methodName}({args});");
      EmitResultOutput(sb, indent, resultTypeName);
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
  /// <param name="sb">The StringBuilder to append to.</param>
  /// <param name="indent">The indentation string.</param>
  /// <param name="resultTypeName">Optional result type name to check if null check is needed.</param>
  private static void EmitResultOutput(StringBuilder sb, string indent, string? resultTypeName = null)
  {
    // Value types can never be null, so skip the null check for them
    bool isValueType = IsValueType(resultTypeName);

    if (isValueType)
    {
      // Value types - always have a value, just output directly
      sb.AppendLine(CultureInfo.InvariantCulture, $"{indent}app.Terminal.WriteLine(result.ToString());");
    }
    else
    {
      // Reference types - check for null before outputting
      sb.AppendLine(CultureInfo.InvariantCulture, $"{indent}if (result is not null)");
      sb.AppendLine(CultureInfo.InvariantCulture, $"{indent}{{");
      sb.AppendLine(CultureInfo.InvariantCulture, $"{indent}  app.Terminal.WriteLine(result.ToString());");
      sb.AppendLine(CultureInfo.InvariantCulture, $"{indent}}}");
    }
  }

  /// <summary>
  /// Determines if a type name represents a value type.
  /// </summary>
  private static bool IsValueType(string? typeName)
  {
    if (string.IsNullOrEmpty(typeName))
      return false;

    // Common value types
    return typeName switch
    {
      "global::System.Int32" or "int" => true,
      "global::System.Int64" or "long" => true,
      "global::System.Int16" or "short" => true,
      "global::System.Byte" or "byte" => true,
      "global::System.SByte" or "sbyte" => true,
      "global::System.UInt32" or "uint" => true,
      "global::System.UInt64" or "ulong" => true,
      "global::System.UInt16" or "ushort" => true,
      "global::System.Single" or "float" or "Float" => true,
      "global::System.Double" or "double" or "Double" => true,
      "global::System.Decimal" or "decimal" or "Decimal" => true,
      "global::System.Boolean" or "bool" or "Boolean" => true,
      "global::System.Char" or "char" => true,
      "global::System.DateTime" or "DateTime" => true,
      "global::System.DateTimeOffset" or "DateTimeOffset" => true,
      "global::System.TimeSpan" or "TimeSpan" => true,
      "global::System.Guid" or "Guid" => true,
      "global::TimeWarp.Nuru.Unit" or "Unit" => true,
      _ => false
    };
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
