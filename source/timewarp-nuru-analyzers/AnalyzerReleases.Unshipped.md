; Unshipped analyzer release
; https://github.com/dotnet/roslyn-analyzers/blob/main/src/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md

### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|-------
NURU_DEBUG | Debug | Hidden | Development route detection diagnostic
NURU_P001 | RoutePattern.Syntax | Error | Invalid parameter syntax
NURU_P002 | RoutePattern.Syntax | Error | Unbalanced braces in route pattern
NURU_P003 | RoutePattern.Syntax | Error | Invalid option format
NURU_P004 | RoutePattern.Syntax | Error | Invalid type constraint
NURU_P005 | RoutePattern.Syntax | Error | Invalid character in route pattern
NURU_P006 | RoutePattern.Syntax | Error | Unexpected token in route pattern
NURU_P007 | RoutePattern.Syntax | Error | Null route pattern
NURU_S001 | RoutePattern.Semantic | Error | Duplicate parameter names in route
NURU_S002 | RoutePattern.Semantic | Error | Conflicting optional parameters
NURU_S003 | RoutePattern.Semantic | Error | Catch-all parameter not at end of route
NURU_S004 | RoutePattern.Semantic | Error | Mixed catch-all with optional parameters
NURU_S005 | RoutePattern.Semantic | Error | Option with duplicate alias
NURU_S006 | RoutePattern.Semantic | Error | Optional parameter before required parameter
NURU_S007 | RoutePattern.Semantic | Error | Invalid end-of-options separator
NURU_S008 | RoutePattern.Semantic | Error | Options after end-of-options separator
NURU_D001 | Dependencies | Error | Mediator packages required for Map&lt;TCommand&gt;