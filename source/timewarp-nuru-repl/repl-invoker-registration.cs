namespace TimeWarp.Nuru;

/// <summary>
/// Registers pre-built invokers for REPL command signatures.
/// This ensures REPL commands work without requiring the user's project
/// to generate invokers for these common signatures.
/// </summary>
[System.CodeDom.Compiler.GeneratedCode("TimeWarp.Nuru.Repl", "1.0.0")]
internal static class ReplInvokerRegistration
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register()
  {
    // Register invoker for () => Task signature (used by ExitAsync, ClearScreenAsync, etc.)
    InvokerRegistry.RegisterAsyncInvoker("_Returns_Task", static (handler, args) =>
    {
      Func<Task> typedHandler = (Func<Task>)handler;
      return typedHandler().ContinueWith(static t => (object?)null);
    });

    // Register invoker for () => void signature (NoParams)
    InvokerRegistry.RegisterSync("NoParams", static (handler, args) =>
    {
      Action typedHandler = (Action)handler;
      typedHandler();
      return null;
    });

    // Register invoker for (NuruCoreAppHolder) => Task<int> signature (used by StartInteractiveModeAsync)
    InvokerRegistry.RegisterAsyncInvoker("NuruCoreAppHolder_Returns_TaskInt", static async (handler, args) =>
    {
      Func<NuruCoreAppHolder, Task<int>> typedHandler = (Func<NuruCoreAppHolder, Task<int>>)handler;
      return await typedHandler((NuruCoreAppHolder)args[0]!).ConfigureAwait(false);
    });
  }
}
