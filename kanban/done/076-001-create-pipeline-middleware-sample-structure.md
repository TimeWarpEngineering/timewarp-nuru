# Create Pipeline Middleware Sample Structure

## Description

Set up the initial directory structure and sample file scaffold for the Pipeline Middleware sample.

## Parent

076_Add-Pipeline-Middleware-Sample

## Checklist

- [x] Create `samples/pipeline-middleware/` directory
- [x] Create `pipeline-middleware.cs` with basic NuruApp setup
- [x] Add project references and package directives
- [x] Create placeholder commands (will be enhanced in later tasks)
- [x] Verify sample compiles

## Notes

Initial file should include:
- Shebang for .NET 10 runfile
- Project references to TimeWarp.Nuru and TimeWarp.Nuru.Logging
- Package references for System.Diagnostics.DiagnosticSource (for Activity/OpenTelemetry)
- Basic NuruAppBuilder setup with AddDependencyInjection
- At least one simple command to verify pipeline works

## Implementation Notes

- Created `samples/pipeline-middleware/` directory
- Created `pipeline-middleware.cs` with:
  - Project references to TimeWarp.Nuru and TimeWarp.Nuru.Logging
  - Two sample commands: `EchoCommand` and `SlowCommand`
  - Two pipeline behaviors: `LoggingBehavior` and `PerformanceBehavior`
  - Behaviors registered in DI with correct ordering
- System.Diagnostics.DiagnosticSource not needed for basic sample (Stopwatch from System.Diagnostics is built-in)
- Sample compiles successfully
