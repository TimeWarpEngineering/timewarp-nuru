namespace TimeWarp.Nuru;

/// <summary>
/// Registers pre-built invokers for core library signatures.
/// This ensures routes registered by the library work without requiring
/// the user's project to generate invokers for these signatures.
/// </summary>
[System.CodeDom.Compiler.GeneratedCode("TimeWarp.Nuru.Core", "1.0.0")]
internal static class CoreInvokerRegistration
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register()
  {
    // Register invoker for () => string signature (used by help routes)
    InvokerRegistry.RegisterSync("_Returns_String", static (handler, args) =>
    {
      Func<string> typedHandler = (Func<string>)handler;
      return typedHandler();
    });

    // Register invoker for (SessionContext) => string signature (used by context-aware help routes)
    InvokerRegistry.RegisterSync("SessionContext_Returns_String", static (handler, args) =>
    {
      Func<SessionContext, string> typedHandler = (Func<SessionContext, string>)handler;
      return typedHandler((SessionContext)args[0]!);
    });
  }
}
