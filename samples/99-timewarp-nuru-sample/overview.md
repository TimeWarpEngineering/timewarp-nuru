# TimeWarp.Nuru Sample

## Status

This sample compiles and runs but its purpose is unclear.

## What It Currently Demonstrates

- Default route (`""`) - welcome message
- Simple query (`status`)
- String parameter (`echo {message}`)
- Command (`proxy {command}`)
- Typed parameters (`add {value1:double} {value2:double}`)
- `ConfigureServices` (empty - doesn't actually demonstrate anything)

## Issues

**This is redundant.** The following samples already cover these concepts better:
- `01-hello-world/` - Basic hello world
- `02-calculator/` - Parameters, types, math operations
- `04-syntax-examples/` - Comprehensive route syntax patterns

This sample doesn't demonstrate anything unique that isn't already covered elsewhere.

## Future Options

1. **Delete** - Remove if no unique value
2. **Kitchen Sink App** - Evolve into comprehensive reference app demonstrating all features together
3. **Template** - Convert to a `dotnet new` template for new Nuru CLI projects
4. **Integration Test** - Use as a real-world integration test target

## Decision

Deferred until all other samples are migrated. Renamed to `99-*` to indicate it works but needs review.
