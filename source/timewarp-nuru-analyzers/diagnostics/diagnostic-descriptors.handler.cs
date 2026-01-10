namespace TimeWarp.Nuru;

/// <summary>
/// Diagnostic descriptors for handler validation errors.
/// These are reported by NuruHandlerAnalyzer when unsupported handler types are detected.
/// </summary>
internal static partial class DiagnosticDescriptors
{
  internal const string HandlerCategory = "Handler.Validation";

  /// <summary>
  /// NURU_H001: Instance method handlers are not supported.
  /// Instance methods capture 'this' which cannot be resolved at compile-time.
  /// </summary>
  public static readonly DiagnosticDescriptor InstanceMethodNotSupported = new(
    id: "NURU_H001",
    title: "Instance method handler not supported",
    messageFormat: "Instance method '{0}' cannot be used as handler. Use a lambda, static method, or local function instead.",
    category: HandlerCategory,
    defaultSeverity: DiagnosticSeverity.Error,
    isEnabledByDefault: true,
    description: "Instance methods capture 'this' which cannot be resolved at compile-time. Use a lambda expression, static method, or local function instead.");

  /// <summary>
  /// NURU_H002: Closure detected in lambda handler.
  /// Captured variables cannot be injected at runtime.
  /// </summary>
  public static readonly DiagnosticDescriptor ClosureNotAllowed = new(
    id: "NURU_H002",
    title: "Closure detected in handler",
    messageFormat: "Handler lambda captures external variable(s): {0}. Handler generation will be skipped. Use DI-injected parameters or suppress this warning for test code.",
    category: HandlerCategory,
    defaultSeverity: DiagnosticSeverity.Warning,
    isEnabledByDefault: true,
    description: "Lambda handlers cannot capture variables from the enclosing scope. The handler will not be generated for this route. Pass dependencies as handler parameters and they will be injected. For test code that intentionally uses closures, suppress with #pragma warning disable NURU_H002.");

  /// <summary>
  /// NURU_H003: Unsupported handler expression type.
  /// Only lambdas, static methods, and local functions are supported.
  /// </summary>
  public static readonly DiagnosticDescriptor UnsupportedHandlerExpression = new(
    id: "NURU_H003",
    title: "Unsupported handler expression",
    messageFormat: "Handler expression of type '{0}' is not supported. Use a lambda, static method, or local function.",
    category: HandlerCategory,
    defaultSeverity: DiagnosticSeverity.Error,
    isEnabledByDefault: true,
    description: "Only lambda expressions, static methods, and local functions are supported as handlers.");

  /// <summary>
  /// NURU_H004: Private method handler not accessible.
  /// Private methods cannot be called from generated code.
  /// </summary>
  public static readonly DiagnosticDescriptor PrivateMethodNotAccessible = new(
    id: "NURU_H004",
    title: "Private method handler not accessible",
    messageFormat: "Private method '{0}' cannot be used as handler. Handler generation will be skipped. Make the method internal or public, or use a lambda expression.",
    category: HandlerCategory,
    defaultSeverity: DiagnosticSeverity.Warning,
    isEnabledByDefault: true,
    description: "Private methods cannot be called from generated handlers. The route will use the delegate invoker instead. To generate a handler, make the method internal or public.");

  /// <summary>
  /// NURU_H005: Handler parameter name doesn't match route segment.
  /// Parameter names must match for generated code to compile correctly.
  /// </summary>
  public static readonly DiagnosticDescriptor ParameterNameMismatch = new(
    id: "NURU_H005",
    title: "Handler parameter name doesn't match route segment",
    messageFormat: "Handler parameter '{0}' doesn't match any route segment; available segments: {1}",
    category: HandlerCategory,
    defaultSeverity: DiagnosticSeverity.Error,
    isEnabledByDefault: true,
    description: "Handler parameter names must match route segment names for the generated code to compile correctly. The lambda body is inlined in generated code and uses these variable names directly.");
}
