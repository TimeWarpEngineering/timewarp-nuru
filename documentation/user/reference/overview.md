# Reference

Technical reference materials for TimeWarp.Nuru.

## Available References

### [Performance](performance.md)
Performance characteristics and benchmarks:
- Memory footprint (Direct vs Mediator)
- Execution time (JIT vs AOT)
- Real-world integration test results
- Optimization strategies
- Performance comparison with other frameworks

### [Supported Types](supported-types.md)
Complete list of supported parameter types:
- Built-in type converters
- Type syntax in route patterns
- Custom type converter implementation
- Nullable type handling
- Array and collection support

### [NuruAppOptions](nuru-app-options.md)
Configuration options for `NuruApp.CreateBuilder()`:
- REPL customization (prompt, history, key bindings)
- Telemetry configuration (tracing, metrics, logging)
- Shell completion sources
- Help output filtering
- Built-in route control

## Quick Reference

### Performance Numbers

| Implementation | Memory | Execution (37 tests) | Binary Size |
|----------------|--------|---------------------|-------------|
| Direct (JIT) | ~4KB | 2.49s | N/A |
| Mediator (JIT) | Moderate | 6.52s | N/A |
| Direct (AOT) | ~4KB | 0.30s | 3.3 MB |
| Mediator (AOT) | Moderate | 0.42s | 4.8 MB |

See [Performance](performance.md) for detailed benchmarks.

### Supported Types

Common types supported out of the box:

```csharp
{param:string}    // Default if no type specified
{count:int}       // Int32
{factor:double}   // Double
{enabled:bool}    // Boolean
{date:DateTime}   // DateTime
{id:Guid}         // Guid
{value:long}      // Int64
{price:decimal}   // Decimal
{duration:TimeSpan} // TimeSpan
```

See [Supported Types](supported-types.md) for complete list and custom types.

## Related Documentation

- **[Features](../features/)** - Feature documentation
- **[Guides](../guides/)** - Implementation guides
- **[Developer Documentation](../../developer/)** - Internal architecture
