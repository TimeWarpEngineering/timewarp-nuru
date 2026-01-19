# RunReplAsync Interception and Build-Based Extraction

## Original Issue
`RunReplAsync()` was not being intercepted by the source generator.

## Resolution

### 1. Added RunReplAsync Interception
- Created `RunReplAsyncLocator` to detect RunReplAsync calls
- Generalized `InterceptSitesByMethod` dictionary to support N entry points
- Updated `InterceptorEmitter` to emit interceptors for RunReplAsync

### 2. Fixed Build-Based Extraction
The original extraction approach was flawed:
- Found each entry point (RunAsync/RunReplAsync) → extracted full AppModel
- Deduplicated by routes (WRONG - different apps with same routes got merged!)

New approach:
- Find each Build() call → extract AppModel with all entry points
- Deduplicate by `BuildLocation` (source location of Build() call)
- Each Build() = one unique app

### Files Modified
- `models/app-model.cs` - Added `BuildLocation`, `InterceptSitesByMethod`
- `ir-builders/ir-app-builder.cs` - `AddInterceptSite(methodName, site)`
- `extractors/app-extractor.cs` - New `ExtractFromBuildCall()` method
- `nuru-generator.cs` - Build()-based pipeline, BuildLocation deduplication
- `emitters/interceptor-emitter.cs` - Emit for RunReplAsync
- `locators/run-repl-async-locator.cs` - New locator

### Test Results
- Before: 706/937 passing
- After: 782/937 passing (+76 tests)

## Closed
Inline RunReplAsync interception now works correctly.
