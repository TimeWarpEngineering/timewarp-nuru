// ═══════════════════════════════════════════════════════════════════════════════
// ENDPOINTS - DISCOVERY EXAMPLE
// ═══════════════════════════════════════════════════════════════════════════════
//
// This sample demonstrates endpoints with discovery:
// - Use DiscoverEndpoints() to enable discovery of [NuruRoute] classes
// - Parameters and options defined via [Parameter] and [Option] attributes
// - Grouped routes via [NuruRouteGroup] on base classes
// - Route aliases via [NuruRouteAlias]
//
// HOW IT WORKS:
//   The source generator scans for classes with [NuruRoute] and generates:
//   1. CompiledRouteBuilder calls for each endpoint
//   2. ModuleInitializer code to register invokers with InvokerRegistry
//   3. Route pattern strings for help display
//
// When DiscoverEndpoints() is called, endpoints are added to the collection at build time.
//
// ENDPOINTS (see messages/ folder):
//   - GreetRequest        - Simple parameter example
//   - DeployRequest       - Parameters and options example
//   - DefaultRequest      - Empty pattern (default route) example
//   - GoodbyeRequest      - Route aliases example
//   - DockerRequestBase   - Route group base class
//   - DockerRunRequest    - Grouped endpoint example
//   - DockerBuildRequest  - Grouped endpoint example
//   - ExecRequest         - Catch-all parameter example
// ═══════════════════════════════════════════════════════════════════════════════

using TimeWarp.Nuru;

// DiscoverEndpoints() enables automatic discovery of [NuruRoute] classes
// No external packages needed - TimeWarp.Nuru provides ICommand<T>, IQuery<T>, handlers, and Unit
NuruApp app = NuruApp.CreateBuilder()
  .DiscoverEndpoints()
  .Build();

return await app.RunAsync(args);
