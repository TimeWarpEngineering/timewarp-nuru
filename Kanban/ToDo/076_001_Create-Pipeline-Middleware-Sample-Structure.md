# Create Pipeline Middleware Sample Structure

## Description

Set up the initial directory structure and sample file scaffold for the Pipeline Middleware sample.

## Parent

076_Add-Pipeline-Middleware-Sample

## Checklist

- [ ] Create `Samples/PipelineMiddleware/` directory
- [ ] Create `pipeline-middleware.cs` with basic NuruApp setup
- [ ] Add project references and package directives
- [ ] Create placeholder commands (will be enhanced in later tasks)
- [ ] Verify sample compiles

## Notes

Initial file should include:
- Shebang for .NET 10 runfile
- Project references to TimeWarp.Nuru and TimeWarp.Nuru.Logging
- Package references for System.Diagnostics.DiagnosticSource (for Activity/OpenTelemetry)
- Basic NuruAppBuilder setup with AddDependencyInjection
- At least one simple command to verify pipeline works
