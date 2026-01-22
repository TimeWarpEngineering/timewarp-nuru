# Add DI Diagnostics for Unsupported Patterns

## Parent

Epic #391: Full DI Support - Source-Gen and Runtime Options

## Description

Add compile-time diagnostics that clearly report when source-gen DI cannot handle a registration pattern. Guide users toward either fixing the pattern or opting into runtime DI.

**Key principle:** When `UseMicrosoftDependencyInjection = false`, validate registrations and report actionable errors. When `true`, skip validation (runtime DI handles everything).

## Requirements

### New Diagnostics

| Code | Severity | When | Message |
|------|----------|------|---------|
| NURU050 | Error | Handler requires unregistered service | "Handler requires service '{0}' but it is not registered in ConfigureServices" |
| NURU051 | Error | Service has constructor dependencies | "Service '{0}' has constructor dependencies. Use .UseMicrosoftDependencyInjection() or register dependencies" |
| NURU052 | Warning | Extension method call detected | "Cannot analyze registrations inside '{0}()'. Registrations may not be visible to source-gen DI" |
| NURU053 | Error | Factory delegate registration | "Service '{0}' uses factory delegate. Use .UseMicrosoftDependencyInjection() for factory support" |
| NURU054 | Error | Internal type not accessible | "Cannot instantiate internal type '{0}'. Use .UseMicrosoftDependencyInjection() or expose public type" |

### Implementation Plan

#### Step 1: Create Diagnostic Descriptors

**File:** `source/timewarp-nuru-analyzers/diagnostics/diagnostic-descriptors.service.cs` (new)

- [x] Add partial class with `ServiceCategory = "Service.Validation"`
- [x] Define NURU050-054 following pattern in `diagnostic-descriptors.handler.cs`
- [x] Include actionable guidance (mention `.UseMicrosoftDependencyInjection()` escape hatch)

#### Step 2: Extend ServiceDefinition Model

**File:** `source/timewarp-nuru-analyzers/generators/models/service-definition.cs`

- [x] Add `ImmutableArray<string> ConstructorDependencyTypes` (for NURU051)
- [x] Add `bool IsFactoryRegistration` (for NURU053)
- [x] Add `bool IsInternalType` (for NURU054)
- [x] Add `Location? RegistrationLocation` (for error reporting)
- [x] Update factory methods with defaults

#### Step 3: Create ExtensionMethodCall Record

**File:** `source/timewarp-nuru-analyzers/generators/models/service-extraction-result.cs` (new)

- [x] Create `record ExtensionMethodCall(string MethodName, Location Location)`
- [x] Create `record ServiceExtractionResult(Services, ExtensionMethods)`

#### Step 4: Enhance ServiceExtractor

**File:** `source/timewarp-nuru-analyzers/generators/extractors/service-extractor.cs`

- [x] Change return type to `ServiceExtractionResult(Services, ExtensionMethods)`
- [x] Detect factory delegates (lambdas in arguments) → set `IsFactoryRegistration`
- [x] Extract constructor parameters from implementation type → populate `ConstructorDependencyTypes`
- [x] Check type accessibility → set `IsInternalType`
- [x] Capture `invocation.GetLocation()` → store in `RegistrationLocation`
- [x] Track non-standard method calls as `ExtensionMethodCall`

#### Step 5: Create ServiceValidator

**File:** `source/timewarp-nuru-analyzers/validation/service-validator.cs` (new)

- [x] Create `ServiceValidator.Validate(AppModel)`
- [x] Skip ALL validation when `UseMicrosoftDependencyInjection = true`
- [x] Build registered service set (include built-ins: ITerminal, IConfiguration, NuruApp, CancellationToken)
- [x] NURU050: Check handler service requirements
- [x] NURU051: Check service constructor dependencies
- [x] NURU053: Check factory registrations
- [x] NURU054: Check type accessibility
- [x] Create `ValidateExtensionMethods()` for NURU052 warnings

#### Step 6: Integrate in ModelValidator

**File:** `source/timewarp-nuru-analyzers/validation/model-validator.cs`

- [x] Add `extensionMethods` parameter to `Validate()`
- [x] Call `ServiceValidator.Validate()` after overlap validation
- [x] Call `ServiceValidator.ValidateExtensionMethods()` for NURU052 warnings

#### Step 7: Update Generator Pipeline

**Files:**
- `source/timewarp-nuru-analyzers/generators/nuru-generator.cs`
- `source/timewarp-nuru-analyzers/generators/interpreter/dsl-interpreter.cs`
- `source/timewarp-nuru-analyzers/generators/ir-builders/ir-app-builder.cs`
- `source/timewarp-nuru-analyzers/generators/ir-builders/abstractions/iir-app-builder.cs`
- `source/timewarp-nuru-analyzers/generators/models/app-model.cs`

- [x] Add `ExtensionMethods` property to `AppModel`
- [x] Add `AddExtensionMethodCall()` to `IIrAppBuilder` interface
- [x] Implement in `IrAppBuilder`
- [x] Update DSL interpreter to use `ServiceExtractionResult` and add extension methods
- [x] Pass `ExtensionMethods` to `ModelValidator.Validate()`

### Files Summary

| File | Action |
|------|--------|
| `diagnostics/diagnostic-descriptors.service.cs` | Created |
| `generators/models/service-definition.cs` | Extended |
| `generators/models/service-extraction-result.cs` | Created |
| `generators/models/app-model.cs` | Extended |
| `generators/extractors/service-extractor.cs` | Enhanced |
| `generators/interpreter/dsl-interpreter.cs` | Updated |
| `generators/ir-builders/ir-app-builder.cs` | Updated |
| `generators/ir-builders/abstractions/iir-app-builder.cs` | Updated |
| `validation/service-validator.cs` | Created |
| `validation/model-validator.cs` | Integrated |
| `generators/nuru-generator.cs` | Wired up |

### Verification

- [x] Build: `dotnet build timewarp-nuru.slnx -c Release`
- [ ] Test NURU050: Handler with unregistered service → error
- [ ] Test NURU051: Service with constructor deps not registered → error
- [ ] Test NURU052: Extension method like `AddLogging()` → warning
- [ ] Test NURU053: Factory delegate registration → error
- [ ] Test NURU054: Internal implementation type → error
- [ ] Test skip behavior: All above with `UseMicrosoftDependencyInjection()` → no diagnostics
- [x] Run CI tests: `dotnet run tests/ci-tests/run-ci-tests.cs`

## Notes

- This is Phase 2 of Epic #391
- Diagnostics should be actionable - tell users HOW to fix
- Include "Use .UseMicrosoftDependencyInjection()" as escape hatch in all messages
- Phase 3 will reduce NURU051 errors by supporting constructor dependencies
- Phase 4 will reduce NURU052 warnings by analyzing extension methods
