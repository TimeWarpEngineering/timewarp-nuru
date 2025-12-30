// ═══════════════════════════════════════════════════════════════════════════════
// ATTRIBUTED ROUTES - AUTO-REGISTRATION EXAMPLE
// ═══════════════════════════════════════════════════════════════════════════════
//
// This sample demonstrates attributed routes with auto-registration:
// - No Map() calls needed - routes are discovered via [NuruRoute] attribute
// - Parameters and options defined via [Parameter] and [Option] attributes
// - Grouped routes via [NuruRouteGroup] on base classes
// - Route aliases via [NuruRouteAlias]
//
// HOW IT WORKS:
//   The NuruAttributedRouteGenerator source generator scans for classes with
//   [NuruRoute] and generates:
//   1. CompiledRouteBuilder calls for each route
//   2. ModuleInitializer code to register routes with NuruRouteRegistry
//   3. Route pattern strings for help display
//
// At app.Build() time, routes from NuruRouteRegistry are added to the
// endpoint collection automatically.
//
// COMMANDS (see commands/ folder):
//   - GreetRequest        - Simple parameter example
//   - DeployRequest       - Parameters and options example
//   - DefaultRequest      - Empty pattern (default route) example
//   - GoodbyeRequest      - Route aliases example
//   - DockerRequestBase   - Route group base class
//   - DockerRunRequest    - Grouped route example
//   - DockerBuildRequest  - Grouped route example
//   - ExecRequest         - Catch-all parameter example
// ═══════════════════════════════════════════════════════════════════════════════

using TimeWarp.Nuru;

// No Map() calls! Routes are auto-registered via [NuruRoute] attributes
// No external packages needed - TimeWarp.Nuru provides ICommand<T>, IQuery<T>, handlers, and Unit
NuruCoreApp app = NuruApp.CreateBuilder(args)
  .Build();

return await app.RunAsync(args);
