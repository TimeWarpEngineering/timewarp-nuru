# Single File Tests

These are executable C# script files that test specific aspects of TimeWarp.Nuru.
They use the shebang `#!/usr/bin/dotnet --` to run directly as scripts.

## Organization

### Parser/
Tests for the route pattern parser:
- `test-parser-errors.cs` - Tests parser error handling for various patterns
- `test-hanging-patterns.cs` - Tests patterns that previously caused parser hangs
- `test-hanging-patterns-fixed.cs` - Verifies hanging patterns are fixed
- `test-specific-hanging.cs` - Test individual patterns with command line args
- `test-analyzer-patterns.cs` - Tests for all analyzer diagnostic scenarios

### Lexer/
Tests for the lexer/tokenizer:
- `test-lexer-hang.cs` - Tests lexer hanging scenarios
- `test-lexer-only.cs` - Tests lexer tokenization
- `test-parser-single.cs` - Tests single pattern parsing with debug output

### Routing/
Tests for route matching and execution:
- `test-route-matching.cs` - Tests route pattern matching
- `test-all-routes.cs` - Comprehensive route testing

### Features/
Tests for specific features:
- `test-auto-help.cs` - Tests automatic help generation
- `test-desc.cs` - Tests route descriptions
- `test-option-params.cs` - Tests option parameters
- `test-kubectl.cs` - Tests kubectl-style command patterns
- `test-shell-behavior.cs` - Tests shell integration
- `test-default-route` - Tests default route behavior

## Running Tests

All test files are executable:
```bash
./Parser/test-parser-errors.cs
./Lexer/test-lexer-only.cs
```

Or with dotnet:
```bash
dotnet Parser/test-parser-errors.cs
```

Enable debug output:
```bash
NURU_DEBUG=true ./Parser/test-hanging-patterns.cs
```