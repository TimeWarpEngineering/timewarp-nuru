namespace TimeWarp.Nuru;

/// <summary>
/// Diagnostic descriptors for entry point calls the generator cannot intercept.
/// </summary>
internal static partial class DiagnosticDescriptors
{
  internal const string InterceptionCategory = "Interception";

  public static readonly DiagnosticDescriptor RunAsyncNotIntercepted = new(
      id: "NURU_I001",
      title: "RunAsync() call is not intercepted",
      messageFormat: "This RunAsync() call is not intercepted and throws at runtime. The generator could not trace '{0}' to a NuruApp built in this compilation.",
      category: InterceptionCategory,
      defaultSeverity: DiagnosticSeverity.Warning,
      isEnabledByDefault: true,
      description: "The source generator replaces each NuruApp.RunAsync() call with generated code. It finds the app a call runs by tracing the receiver back to a Build() call in the same method, top-level statements, expression-bodied member, or a field of the same type. When the receiver comes from somewhere it cannot follow, such as a method parameter, the return value of another method, or a lambda or local function that captures a local assigned more than one app, no interceptor is emitted and the call throws 'RunAsync was not intercepted' at runtime. Call RunAsync() on a local, field, or Build() chain the generator can see.");
}
