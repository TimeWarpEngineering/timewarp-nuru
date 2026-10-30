#!/usr/bin/env -S dotnet --
// ═══════════════════════════════════════════════════════════════════════════════
// DEV CLI - TIMEWARP.NURU DEVELOPMENT TOOL
// ═══════════════════════════════════════════════════════════════════════════════
//
// This is the unified development CLI for TimeWarp.Nuru that provides:
// - CI/CD orchestration commands for GitHub Actions
// - Development workflow automation
// - AOT-compiled binary for fast execution
// - Endpoints for flexible command organization
//
// Usage:
//   As runfile:  dotnet tools/dev-cli/dev.cs <command>
//   As AOT:      ./dev <command>
//
// Architecture:
//   - Uses TimeWarp.Nuru with endpoints for command registration
//   - TimeWarp.Amuru for cross-platform shell and .NET operations
//   - AOT-compatible design for maximum performance
//
// Commands (Phase 1 - CI/CD Orchestration):
//   dev ci                 - Run full CI/CD pipeline (auto-detects mode)
//   dev ci --mode pr       - PR workflow: build -> verify-samples -> test
//   dev ci --mode release  - Release workflow: tag-gate -> check-version -> locate-run -> download-artifact -> verify -> push
//   dev build              - Build all TimeWarp.Nuru projects
//   dev clean              - Clean solution and artifacts
//   dev test               - Run CI test suite
//   dev verify-samples     - Verify sample compilation
//   dev check-version      - Check if version already published
//   dev release            - Cut a release: tag + GitHub Release from source/Directory.Build.props (--dry-run to preview)
//   dev format             - Check code formatting (--fix to auto-fix)
//   dev analyze            - Run Roslynator analysis and fixes
//   dev self-install       - AOT compile and install dev CLI to ./bin
//
// Telemetry (tools/dev-cli/telemetry.md):
//   UseTelemetry service name is the literal "timewarp-nuru-dev".
//   Export follows OTEL_EXPORTER_OTLP_ENDPOINT. Unset exports nothing.
//   TRACEPARENT / TRACESTATE join a parent process's trace.
//
// To bootstrap:
//   dotnet run tools/dev-cli/dev.cs -- self-install
//   direnv allow
//   dev --help
// ═══════════════════════════════════════════════════════════════════════════════

#region Purpose
// Dev CLI command for TimeWarp.Nuru development workflow
#endregion

#region Design
// UseTelemetry is on so a CI job or a ganda walk that shells out to dev
// exports one command span. The OTLP endpoint stays unset here: a non-literal
// OtlpEndpoint is ignored, and an unset OTEL_EXPORTER_OTLP_ENDPOINT exports
// nothing. ServiceName is the literal timewarp-nuru-dev. OTEL_SERVICE_NAME
// overrides it. IncomingTraceContext restores TRACEPARENT before RunAsync
// because TelemetryBehavior parents its span to Activity.Current.
#endregion

NuruApp app = NuruApp.CreateBuilder()
  .WithName("dev")
  .WithDescription("Development CLI for timewarp-nuru")
  .UseTelemetry(options => options.ServiceName = "timewarp-nuru-dev")
  .ConfigureServices(services =>
  {
    services.AddSingleton<IRepoCleanService, RepoCleanService>();
    services.AddSingleton<NuGetVersionService>();
    services.AddSingleton<IRepoConfigService, RepoConfigService>();
    services.AddSingleton<IPackableProjectService, PackableProjectService>();
  })
  .DiscoverEndpoints()
  .Build();

using IDisposable? incomingTrace = IncomingTraceContext.Restore();
return await app.RunAsync(args);
