; Unshipped analyzer release
; https://github.com/dotnet/roslyn-analyzers/blob/main/src/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md

### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|-------
NURU_DEBUG | Debug | Warning | Temporary route detection diagnostic
NURU001 | RoutePattern | Error | Invalid parameter syntax
NURU002 | RoutePattern | Error | Unbalanced braces in route pattern
NURU003 | RoutePattern | Error | Invalid option format
NURU004 | RoutePattern | Error | Invalid type constraint
NURU005 | RoutePattern | Error | Catch-all parameter not at end of route
NURU006 | RoutePattern | Error | Duplicate parameter names in route
NURU007 | RoutePattern | Error | Conflicting optional parameters
NURU008 | RoutePattern | Error | Mixed catch-all with optional parameters
NURU009 | RoutePattern | Error | Option with duplicate alias