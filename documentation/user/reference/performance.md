# Performance

TimeWarp.Nuru delivers high performance while maintaining rich functionality.

## Key Metrics

### Memory Footprint

| Approach | Memory Allocated | Notes |
|----------|------------------|-------|
| Direct | ~4 KB | Minimal overhead |
| Mediator | Moderate | DI container overhead |
| Mixed | Optimal per command | Best balance |

### Execution Speed (37 Integration Tests)

| Implementation | Test Results | Execution Time | vs JIT Baseline |
|----------------|--------------|----------------|-----------------|
| **Direct (JIT)** | 37/37 ✓ | 2.49s | Baseline |
| **Mediator (JIT)** | 37/37 ✓ | 6.52s | 161% slower |
| **Direct (AOT)** | 37/37 ✓ | **0.30s** 🚀 | 88% faster |
| **Mediator (AOT)** | 37/37 ✓ | **0.42s** 🚀 | 93% faster |

### Binary Size (Native AOT)

| Approach | Binary Size | Startup Time |
|----------|-------------|--------------|
| Direct (AOT) | 3.3 MB | < 1 ms |
| Mediator (AOT) | 4.8 MB | < 1 ms |

## Key Insights

### AOT is Dramatically Faster

Native AOT compilation provides **88-93% faster execution** than JIT:
- Sub-second execution for 37 complex CLI tests
- Instant startup (< 1ms)
- No JIT compilation overhead

### Direct Approach: Maximum Performance

- **Fastest execution**: 0.30s for 37 tests
- **Smallest binary**: 3.3 MB
- **Minimal memory**: ~4 KB allocated
- **Best for**: Performance-critical commands, small utilities

### Mediator Approach: Worth the Overhead

- **Still very fast**: 0.42s for 37 tests (only 40% slower than Direct)
- **Moderate size**: 4.8 MB (+45% over Direct)
- **Benefits**: DI, testability, structure
- **Best for**: Complex commands, enterprise applications

### Mixed Approach: Optimal Balance

- **Size**: Between 3.3-4.8 MB depending on DI usage
- **Speed**: Optimized per command
- **Flexibility**: Use Direct where speed matters, Mediator where structure matters

## Benchmark Details

### Test Suite

The 37 integration tests cover:
- Route pattern parsing
- Parameter binding
- Type conversion
- Optional parameters
- Options (flags)
- Catch-all parameters
- Sub-commands
- Complex scenarios

All tests run against both Direct and Mediator implementations to ensure feature parity.

### Test Environment

- **.NET Version**: 9.0
- **Hardware**: Standard development machine
- **OS**: Linux
- **Compilation**: Release mode, optimizations enabled

## Comparison with Other Frameworks

| Framework | Memory | Startup | Binary (AOT) | Route Patterns |
|-----------|--------|---------|--------------|----------------|
| **Nuru Direct** | ~4 KB | < 1 ms | 3.3 MB | ✅ Web-style |
| **Nuru Mediator** | Moderate | < 1 ms | 4.8 MB | ✅ Web-style |
| Cocona | Moderate | ~50 ms | Not optimized | ❌ Attribute-based |
| CommandLineParser | Low | ~10 ms | ~8 MB | ❌ Fluent API |
| System.CommandLine | Moderate | ~20 ms | ~12 MB | ❌ Fluent API |

*Note: Other framework measurements are approximate and for comparison purposes.*

## Performance Optimization Tips

### Choose the Right Approach

```csharp
// ✅ Direct for hot paths
builder.AddRoute("ping", () => "pong");  // Called frequently

// ✅ Mediator for complex logic
builder.AddRoute<DeployCommand>("deploy {env}");  // Complex, less frequent
```

### Use Native AOT

```xml
<PropertyGroup>
  <PublishAot>true</PublishAot>
  <TrimMode>partial</TrimMode>
</PropertyGroup>
```

Provides **88-93% performance improvement** over JIT.

### Minimize Allocations

```csharp
// ✅ Return value types when possible
.AddRoute("status", () => 0);  // int return

// ❌ Avoid unnecessary allocations
.AddRoute("status", () => new Status());  // Allocates object
```

### Async Only When Needed

```csharp
// ✅ Sync for CPU-bound work
.AddRoute("calc {x:int} {y:int}", (int x, int y) => x + y)

// ✅ Async for I/O-bound work
.AddRoute("fetch {url}", async (string url) => await FetchAsync(url))
```

## Scaling Characteristics

### Command Count vs Performance

| Commands | Direct Overhead | Mediator Overhead |
|----------|-----------------|-------------------|
| 10 | Negligible | Negligible |
| 100 | Negligible | Small |
| 1000 | Small | Moderate |

Route matching is O(n) where n = number of routes, but with early-exit optimization.

### Argument Count vs Performance

| Arguments | Parsing Time |
|-----------|--------------|
| 1-5 | < 1 μs |
| 6-20 | < 5 μs |
| 21-50 | < 20 μs |

Linear scaling with number of arguments.

## Memory Profiling

### Direct Approach Allocations

```
Total: 3,992 bytes

Breakdown:
- Route registration: 1,200 bytes
- Parameter binding: 800 bytes
- Type conversion: 400 bytes
- Execution: 1,592 bytes
```

### Mediator Approach Allocations

```
Total: Moderate (DI container dependent)

Additional overhead:
- DI container: Varies by implementation
- Command instances: Per command execution
- Handler resolution: Per command execution
```

## Real-World Performance

### Typical CLI Invocation

```bash
time ./mytool deploy prod
```

**Direct (AOT):**
```
real    0m0.008s  # 8ms total
user    0m0.004s
sys     0m0.004s
```

**Mediator (AOT):**
```
real    0m0.012s  # 12ms total
user    0m0.006s
sys     0m0.006s
```

Both are effectively instant to users.

### Throughput Testing

```bash
# Run command 1000 times
for i in {1..1000}; do ./mytool ping; done
```

**Direct (AOT):**
- Total: 2.1s
- Per invocation: 2.1ms

**Mediator (AOT):**
- Total: 3.5s
- Per invocation: 3.5ms

## Performance Best Practices

### ✅ DO

- Use Native AOT for production
- Use Direct approach for hot paths
- Use value types for returns when possible
- Profile before optimizing
- Cache expensive computations

### ❌ DON'T

- Micro-optimize prematurely
- Use Mediator for trivial commands
- Allocate unnecessarily in hot paths
- Skip AOT compilation for production

## Related Documentation

- **[Architecture Choices](../guides/architecture-choices.md)** - Choose the right approach
- **[Deployment](../guides/deployment.md)** - AOT compilation guide
- **[Calculator Samples](../../../Samples/Calculator/)** - See implementations to compare
