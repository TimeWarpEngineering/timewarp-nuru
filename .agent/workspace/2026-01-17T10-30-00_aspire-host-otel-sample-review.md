# Review: Aspire Host with OpenTelemetry Sample

**Date:** 2026-01-17T10-30-00  
**Reviewer:** Claude Code Analysis  
**Scope:** `samples/_aspire-host-otel/` documentation and code files

---

## Executive Summary

This sample effectively demonstrates Nuru CLI/REPL integration with Aspire's OpenTelemetry dashboard. The implementation is generally sound, showcasing file-based apps, Aspire C# app registration, and telemetry pipeline configuration. However, several documentation inconsistencies and minor code issues should be addressed to ensure the sample accurately represents the current codebase architecture.

---

## Scope

- **Files Reviewed:**
  - `overview.md` - Documentation (233 lines)
  - `apphost.cs` - Aspire host runfile (33 lines)
  - `nuru-client.cs` - Nuru REPL client runfile (176 lines)
  - `Properties/launchSettings.json` - Shared launch profiles (27 lines)

- **Related Source Files Consulted:**
  - `source/timewarp-nuru/telemetry/telemetry-behavior.cs`
  - `source/timewarp-nuru/telemetry/nuru-telemetry-extensions.cs`
  - `source/timewarp-nuru/nuru-app-builder.cs`
  - `source/timewarp-nuru/nuru-app.cs`

---

## Methodology

1. Read all sample files and related source files
2. Verified code patterns against current Nuru architecture
3. Cross-referenced documentation with actual implementation
4. Identified inconsistencies and potential improvements

---

## Findings

### 1. Documentation Path Inconsistency

**Location:** `overview.md:10-11`

```markdown
cd samples/aspire-host-otel
```

The documentation references `samples/aspire-host-otel` but the actual directory is `samples/_aspire-host-otel/` (with underscore prefix, indicating a special sample).

**Impact:** Users following the documentation literally will fail to find the directory.

**Recommendation:** Update all paths in `overview.md` to use `samples/_aspire-host-otel/`.

---

### 2. Reference to Non-Existent Telemetry Project

**Location:** `overview.md:232`

```markdown
- [TimeWarp.Nuru.Telemetry](../../Source/TimeWarp.Nuru.Telemetry/) - Telemetry package
```

The path `../../Source/TimeWarp.Nuru.Telemetry/` does not exist. The telemetry code is actually located at `source/timewarp-nuru/telemetry/` within the main `timewarp-nuru` project, not a separate project.

**Impact:** Documentation link is broken.

**Recommendation:** Either:
- Remove this reference since it's not a separate package
- Correct the link to point to the telemetry source folder

---

### 3. IHostApplicationBuilder Integration Documentation Gap

**Location:** `overview.md:30-40`

The documentation states:

```csharp
// New in Nuru 3.0: NuruAppBuilder implements IHostApplicationBuilder
builder.AddNuruClientDefaults();  // Uses builder.Logging, builder.Services, etc.
```

However, `AddNuruClientDefaults()` method does not exist in the codebase. The telemetry is actually configured via `NuruApp.CreateBuilder()` with internal telemetry auto-wiring.

**Impact:** Documentation describes a method that doesn't exist.

**Recommendation:** Either:
- Add the `AddNuruClientDefaults()` extension method to match documentation
- Update documentation to reflect actual telemetry configuration approach

**Current Implementation (correct approach):**
```csharp
NuruCoreApp app = NuruApp.CreateBuilder(args)
  .ConfigureServices(ConfigureServices)  // Registers TelemetryBehavior
  // Telemetry is auto-configured by NuruApp
```

---

### 4. TelemetryBehavior Generic Type Usage

**Location:** `nuru-client.cs:54-57`

```csharp
services.AddMediator(options =>
{
  options.PipelineBehaviors = [typeof(TelemetryBehavior<,>)];
});
```

This is correct usage and properly demonstrates AOT-compatible behavior registration.

**Positive finding:** The sample correctly uses the generic `TelemetryBehavior<,>` pattern rather than the older `INuruBehavior` pattern found in other samples.

---

### 5. Aspire C# App Warning Suppression

**Location:** `apphost.cs:17`

```csharp
#pragma warning disable ASPIRECSHARPAPPS001
```

The warning suppression is appropriate for sample code. However, a comment explaining *why* this is suppressed would be helpful for learning purposes.

**Recommendation:** Add comment:

```csharp
#pragma warning disable ASPIRECSHARPAPPS001
// Suppressed: File-based apps are intentional for this demo
```

---

### 6. OTLP Endpoint Port Mismatch

**Location:** `launchSettings.json` and `overview.md`

The documentation states OTLP receiver is on port 19034:
- `overview.md:50`: "OTLP receiver (port 19034)"
- `launchSettings.json:12`: `"ASPIRE_DASHBOARD_OTLP_ENDPOINT_URL": "http://localhost:19034"`

However, standard Aspire Dashboard OTLP endpoints are:
- gRPC: `4317`
- HTTP: `4318`

**Impact:** The sample uses correct Aspire-provided OTLP endpoint configuration. Users should understand this is auto-injected by Aspire when running via `AddCSharpApp()`.

**Positive finding:** When running via AppHost, Aspire automatically injects `OTEL_EXPORTER_OTLP_ENDPOINT` pointing to the dashboard's OTLP receiver. The explicit configuration in `launchSettings.json` is for standalone mode.

---

### 7. Dual Output Pattern is Well-Implemented

**Location:** `nuru-client.cs` (throughout command handlers)

The sample correctly demonstrates the dual output pattern:

```csharp
// Console.WriteLine for user feedback (visible in terminal)
Console.WriteLine($"Hello, {request.Name}!");

// ILogger for telemetry (flows to Aspire Dashboard via OTLP)
logger.LogInformation("Greeting {Name} at {Timestamp}", request.Name, DateTime.UtcNow);
```

**Positive finding:** Clear separation of user-facing output and telemetry data.

---

### 8. NuruAppBuilder Creates Telemetry Automatically

**Location:** `nuru-client.cs:23-25`

```csharp
NuruCoreApp app = NuruApp.CreateBuilder(args)
  .ConfigureServices(ConfigureServices)
```

The `NuruAppBuilder` class (see `source/timewarp-nuru/nuru-app-builder.cs:7`) implements `IHostApplicationBuilder` and provides access to `Logging`, `Metrics`, and `Configuration` properties.

**Positive finding:** The architecture correctly supports Aspire-style extension methods as documented.

---

### 9. REPL Welcome Message References Non-Existent Command

**Location:** `nuru-client.cs:36`

```csharp
"  config           - Show telemetry configuration\n" +
```

The `config` command is mapped at line 44:
```csharp
.Map<ConfigCommand>("config").WithDescription("Show telemetry configuration")
```

**Correct:** The command exists and is properly documented.

---

### 10. Resource Name Inconsistency in Telemetry

**Location:** `launchSettings.json:22`

```json
"OTEL_SERVICE_NAME": "nuru-repl-client"
```

But the documentation at `overview.md:103` references:
- `nuruclient` (Aspire-launched)
- `nuru-repl-client` (interactive)

**Impact:** Minor inconsistency in naming. The actual resource names depend on how Aspire launches the app and the `OTEL_SERVICE_NAME` environment variable.

---

## Recommendations

### High Priority

1. **Fix path references** in `overview.md` to use `samples/_aspire-host-otel/`

2. **Fix or remove broken link** to `TimeWarp.Nuru.Telemetry` package

3. **Clarify IHostApplicationBuilder documentation** - Either implement `AddNuruClientDefaults()` or update the documentation to match actual behavior

### Medium Priority

4. Add explanatory comment for `ASPIRECSHARPAPPS001` warning suppression

5. Consider adding a section explaining how Aspire auto-injects `OTEL_EXPORTER_OTLP_ENDPOINT` when using `AddCSharpApp()`

6. Add troubleshooting section for common issues (e.g., "Dashboard not showing telemetry")

### Low Priority

7. Standardize service naming in documentation vs. launchSettings.json

8. Consider adding a diagram showing the actual OTLP endpoint flow

---

## References

- **Source Files:**
  - [`samples/_aspire-host-otel/overview.md`](overview.md)
  - [`samples/_aspire-host-otel/apphost.cs`](apphost.cs)
  - [`samples/_aspire-host-otel/nuru-client.cs`](nuru-client.cs)
  - [`source/timewarp-nuru/telemetry/telemetry-behavior.cs`](../../source/timewarp-nuru/telemetry/telemetry-behavior.cs)
  - [`source/timewarp-nuru/telemetry/nuru-telemetry-extensions.cs`](../../source/timewarp-nuru/telemetry/nuru-telemetry-extensions.cs)
  - [`source/timewarp-nuru/nuru-app-builder.cs`](../../source/timewarp-nuru/nuru-app-builder.cs)

- **Related Samples:**
  - `samples/_aspire-telemetry/` - Similar telemetry sample with different patterns
  - `samples/07-pipeline-middleware/` - TelemetryBehavior with INuruBehavior pattern
