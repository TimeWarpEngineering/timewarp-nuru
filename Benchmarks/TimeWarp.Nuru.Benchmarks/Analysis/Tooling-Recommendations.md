# Performance Tooling Recommendations for TimeWarp.Nuru

## 1. Performance Profiling Tools

### BenchmarkDotNet (Already in use)
**Purpose**: Micro-benchmarking framework
**Current Usage**: ✅ Measuring cold-start performance

**Enhancements**:
```csharp
[MemoryDiagnoser]
[SimpleJob(RunStrategy.ColdStart, iterationCount: 100)]
[EventPipeProfiler(EventPipeProfile.CpuSampling)] // CPU profiling
[DisassemblyDiagnoser(maxDepth: 3)] // Assembly analysis
public class CliFrameworkBenchmark
{
    // Add warmup vs cold comparison
    [Benchmark]
    [BenchmarkCategory("Cold")]
    public void ColdStart() => /* ... */;
    
    [Benchmark]
    [BenchmarkCategory("Warm")]
    public void WarmStart() => /* ... */;
}
```

### dotnet-trace
**Purpose**: CPU profiling and event tracing
**Installation**: `dotnet tool install --global dotnet-trace`

**Usage**:
```bash
# Profile the benchmark
dotnet trace collect --profile cpu-sampling --process-id $(pidof dotnet)

# Analyze specific areas
dotnet trace collect --providers Microsoft-Windows-DotNETRuntime:0x1F000080018:5
```

**What to look for**:
- Time spent in ServiceProvider.GetService
- Route parsing overhead
- Reflection calls

### PerfView
**Purpose**: ETW-based performance analysis
**Download**: https://github.com/Microsoft/perfview/releases

**Key Features**:
- CPU sampling with call stacks
- Allocation profiling
- JIT compilation analysis

**Usage for TimeWarp.Nuru**:
```cmd
PerfView /GCCollectOnly collect benchmark.etl
PerfView /StartupTime benchmark.etl
```

## 2. Memory Profiling Tools

### dotnet-gcdump
**Purpose**: Analyze GC heap allocations
**Installation**: `dotnet tool install --global dotnet-gcdump`

```bash
# Capture heap snapshot
dotnet gcdump collect -p $(pidof dotnet) -o nuru.gcdump

# Analyze with PerfView
PerfView nuru.gcdump
```

### dotMemory Unit
**Purpose**: Memory testing in benchmarks
**NuGet**: JetBrains.dotMemory.Unit

```csharp
[Test]
public void RouteMatching_ShouldNotAllocate()
{
    dotMemory.Check(memory =>
    {
        resolver.Resolve(args);
    }, memory =>
    {
        Assert.That(memory.GetObjects(where => where.Type.Is<Dictionary<string, string>>())
            .ObjectsCount, Is.EqualTo(1));
    });
}
```

### Visual Studio Diagnostic Tools
**Purpose**: Integrated profiling
**Access**: Debug > Performance Profiler

**Relevant profilers**:
- .NET Object Allocation Tracking
- CPU Usage
- .NET Async

## 3. Specialized Analysis Tools

### ILSpy / dnSpy
**Purpose**: Analyze generated IL code
**Use Case**: Compare TimeWarp.Nuru IL with ConsoleAppFramework

```bash
# Export assembly
dotnet publish -c Release
# Open in ILSpy to analyze:
# - Inlining opportunities
# - Boxing/unboxing
# - Virtual call overhead
```

### BenchmarkDotNet Disassembly
```csharp
[DisassemblyDiagnoser(printSource: true)]
public class Benchmark
{
    // Shows x64 assembly for hot paths
}
```

### Custom ETW Providers
```csharp
[EventSource(Name = "TimeWarp-Nuru")]
public sealed class NuruEventSource : EventSource
{
    [Event(1, Level = EventLevel.Informational)]
    public void RouteMatchStart(string pattern) => WriteEvent(1, pattern);
    
    [Event(2, Level = EventLevel.Informational)]
    public void RouteMatchEnd(int durationMicros) => WriteEvent(2, durationMicros);
}
```

## 4. Continuous Performance Monitoring

### GitHub Actions Benchmark
```yaml
name: Benchmark
on: [push, pull_request]

jobs:
  benchmark:
    runs-on: ubuntu-latest
    steps:
    - uses: actions/checkout@v3
    - uses: actions/setup-dotnet@v3
    
    - name: Run benchmarks
      run: dotnet run -c Release --project Benchmarks/TimeWarp.Nuru.Benchmarks -- --exporters json
    
    - name: Store benchmark result
      uses: benchmark-action/github-action-benchmark@v1
      with:
        tool: 'benchmarkdotnet'
        output-file-path: BenchmarkDotNet.Artifacts/results/*.json
        alert-threshold: '110%'
        comment-on-alert: true
```

### Performance Regression Tests
```csharp
[Test]
[Timeout(100)] // Fail if takes > 100ms
public async Task SimpleCommand_ShouldExecuteQuickly()
{
    var sw = Stopwatch.StartNew();
    await app.RunAsync(new[] { "--str", "test", "-i", "42", "-b" });
    
    Assert.That(sw.ElapsedMilliseconds, Is.LessThan(35));
}
```

## 5. Quick Profiling Commands

### Find Performance Hotspots
```bash
# CPU profile for 30 seconds
dotnet trace collect -p $(pidof dotnet) --duration 00:00:30

# Convert to speedscope format
dotnet trace convert trace.nettrace --format speedscope

# View in browser
open https://speedscope.app
```

### Memory Allocation Sources
```bash
# Allocation profile
dotnet run -c Release -- \
  --profiler ETW \
  --filter "*TimeWarp.Nuru*" \
  --counters dotnet.runtime
```

### JIT Analysis
```bash
# See which methods are jitted vs interpreted
COMPlus_JitDisasm="Execute*" dotnet run
```

## 6. Recommended Analysis Workflow

1. **Start with BenchmarkDotNet diagnostics**
   ```csharp
   [MemoryDiagnoser]
   [InliningDiagnoser]
   [TailCallDiagnoser]
   ```

2. **Profile with dotnet-trace**
   - Identify hot methods
   - Check for unexpected allocations

3. **Deep dive with PerfView**
   - Analyze call trees
   - Find GC pressure points

4. **Verify optimizations with disassembly**
   - Ensure critical paths are inlined
   - Check for boxing

5. **Set up CI monitoring**
   - Track performance over time
   - Alert on regressions

## 7. Framework-Specific Tools

### Source Generator Analyzer
Since ConsoleAppFramework uses source generators:

```xml
<PackageReference Include="Microsoft.CodeAnalysis.Analyzers" Version="3.3.4">
  <PrivateAssets>all</PrivateAssets>
</PackageReference>
```

Analyze generated code:
```bash
dotnet build -bl
# Open msbuild.binlog in MSBuild Structured Log Viewer
# Navigate to generated files
```

### Tiered Compilation Analysis
```bash
# Disable tiered compilation to see true cold start
COMPlus_TieredCompilation=0 dotnet run

# Compare with tiered compilation
COMPlus_TieredCompilation=1 dotnet run
```

## Summary

**For immediate insights**, use:
1. `dotnet trace` for CPU profiling
2. `dotnet-gcdump` for memory analysis
3. BenchmarkDotNet with `[DisassemblyDiagnoser]`

**For ongoing monitoring**, implement:
1. GitHub Actions benchmarking
2. Performance regression tests
3. Custom ETW events for critical paths

The key is to measure, optimize, and continuously monitor to ensure TimeWarp.Nuru reaches and maintains its performance goals.