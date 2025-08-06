# TimeWarp.Nuru.Parsing

A source-only NuGet package containing the route pattern parsing implementation for TimeWarp.Nuru.

## What is a Source-Only Package?

This package delivers source code that gets compiled directly into your project rather than distributing a pre-compiled assembly. This approach enables:

- No runtime dependencies
- Full compiler optimizations
- No assembly loading issues in analyzers
- Smaller deployment size

## Usage

This package is automatically included when you reference:
- `TimeWarp.Nuru` - The main CLI framework
- `TimeWarp.Nuru.Analyzers` - Compile-time route pattern validation

You typically won't reference this package directly.

## Contents

The package includes:
- Route pattern lexer for tokenization
- Recursive descent parser for syntax analysis  
- AST (Abstract Syntax Tree) representations
- Route compilation to runtime structures
- Comprehensive error reporting

## License

Licensed under the same terms as TimeWarp.Nuru. See the main repository for details.