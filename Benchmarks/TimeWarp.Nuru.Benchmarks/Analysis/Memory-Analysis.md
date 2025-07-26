# TimeWarp.Nuru Memory Analysis

## Current Memory Usage: 18,576 B (2nd best)
**Comparison**: System.CommandLine uses 11,360 B (38% less)

## Memory Allocation Breakdown

Based on code analysis, the 18,576 bytes are allocated across these components:

### 1. Dependency Injection Container (~8-10 KB)
```csharp
ServiceProvider serviceProvider = ServiceCollection.BuildServiceProvider();
```

**Allocations**:
- ServiceProvider internal structures
- Service descriptors and caches
- Scoped service tracking
- Type activation caches

### 2. Route Parsing Data Structures (~3-4 KB)
```csharp
var segments = new List<RouteSegment>();
var requiredOptions = new List<string>();
var optionSegments = new List<OptionSegment>();
var parameters = new Dictionary<string, RouteParameter>();
```

**Per Route Allocations**:
- Multiple List<T> instances with default capacity
- Dictionary with string keys
- RouteSegment object instances
- String allocations from Split operations

### 3. Command Resolution (~2-3 KB)
```csharp
var extractedValues = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
var remainingArgs = args.Skip(consumedArgs).ToList();
```

**Per Resolution Attempt**:
- New Dictionary for each endpoint match attempt
- LINQ operations creating intermediate collections
- String allocations for extracted values

### 4. String Operations (~1-2 KB)
```csharp
string[] parts = routePattern.Split(' ', StringSplitOptions.RemoveEmptyEntries);
extractedValues[param.Name] = string.Join(" ", args.Skip(i));
```

**String Allocations**:
- Split creates new string array
- Join creates new string
- Substring operations in parsing

### 5. Regex Compilation (~1-2 KB)
```csharp
private static readonly Regex ParameterRegex = new(@"\{(\*)?([^}:]+)(:([^}]+))?\}", RegexOptions.Compiled);
```

**Regex Allocations**:
- Compiled regex state machine
- Internal regex structures
- Match object allocations

## Comparison with System.CommandLine (11,360 B)

System.CommandLine achieves lower allocations through:

1. **No DI Container**: Direct instantiation saves ~8 KB
2. **Pooled Objects**: Reuses parsing structures
3. **Span-based Parsing**: Avoids string allocations
4. **Pre-compiled Syntax**: No runtime regex compilation

## Memory Optimization Strategies

### 1. Eliminate DI Container Allocations (Save: 8-10 KB)
```csharp
// Direct instantiation path
public static class NuruDirect
{
    private static readonly TypeConverterRegistry TypeConverters = new();
    private static readonly EndpointCollection Endpoints = new();
    
    public static Task<int> RunAsync(string[] args)
    {
        var resolver = new RouteBasedCommandResolver(Endpoints, TypeConverters);
        // Direct execution without DI
    }
}
```

### 2. Use ArrayPool for Temporary Collections (Save: 2-3 KB)
```csharp
// Rent arrays instead of allocating
var segments = ArrayPool<RouteSegment>.Shared.Rent(10);
try
{
    // Use segments
}
finally
{
    ArrayPool<RouteSegment>.Shared.Return(segments);
}
```

### 3. Reuse Dictionary Instances (Save: 1-2 KB)
```csharp
public class RouteBasedCommandResolver
{
    // Reuse this dictionary, clearing between uses
    private readonly Dictionary<string, string> _extractedValues = 
        new(StringComparer.OrdinalIgnoreCase);
    
    private bool MatchRoute(IReadOnlyList<string> args)
    {
        _extractedValues.Clear();
        // Use _extractedValues
    }
}
```

### 4. Span-based String Operations (Save: 1-2 KB)
```csharp
// Avoid string.Split allocations
public static void ParseArgs(ReadOnlySpan<char> input)
{
    int start = 0;
    for (int i = 0; i < input.Length; i++)
    {
        if (input[i] == ' ')
        {
            ProcessSegment(input.Slice(start, i - start));
            start = i + 1;
        }
    }
}
```

### 5. String Interning for Common Values (Save: 0.5-1 KB)
```csharp
// Intern commonly used strings
private static class CommonStrings
{
    public static readonly string Dash = string.Intern("-");
    public static readonly string DoubleDash = string.Intern("--");
    public static readonly string True = string.Intern("true");
    public static readonly string False = string.Intern("false");
}
```

## Memory-Efficient Design Patterns

### 1. Struct-based Route Segments
```csharp
// Use structs instead of classes for small objects
public readonly struct RouteSegment
{
    public readonly SegmentType Type;
    public readonly string Value;
    
    public RouteSegment(SegmentType type, string value)
    {
        Type = type;
        Value = value;
    }
}
```

### 2. Stackalloc for Small Buffers
```csharp
// Use stack allocation for temporary buffers
Span<char> buffer = stackalloc char[256];
```

### 3. ValueTask for Async Operations
```csharp
// Avoid Task allocation for synchronous paths
public ValueTask<int> RunAsync(string[] args)
{
    if (IsSynchronousCommand(args))
        return new ValueTask<int>(ExecuteSync(args));
    
    return new ValueTask<int>(ExecuteAsync(args));
}
```

## Estimated Memory Usage After Optimizations

| Optimization | Memory Saved | New Total |
|--------------|--------------|-----------|
| Current | - | 18,576 B |
| Remove DI | -9,000 B | 9,576 B ✅ |
| Object pooling | -2,000 B | 7,576 B |
| Span parsing | -1,500 B | 6,076 B |
| String interning | -500 B | 5,576 B |

**Realistic Target**: 8-10 KB (achievable by removing DI overhead)

## Priority Recommendations

1. **High Priority**: Create DI-free execution path
2. **Medium Priority**: Implement object pooling for dictionaries
3. **Low Priority**: Convert to span-based parsing
4. **Low Priority**: Use struct-based segments

## Trade-offs to Consider

While we can reduce allocations further, consider:

1. **Code Complexity**: Span-based parsing is harder to maintain
2. **Performance Impact**: Object pooling adds overhead for simple cases
3. **Usability**: DI provides valuable features for complex applications

**Recommendation**: Focus on removing DI overhead for the benchmark scenario while keeping it available for real applications that need it.

## Detailed Memory Allocation Breakdown (2025-07-27)

Through incremental testing, we've identified exactly where TimeWarp.Nuru's 18,464 B allocations occur:

### Test Results

| Test Scenario | Allocations | Description |
|---------------|-------------|-------------|
| Return immediately | 0 B | Baseline - no framework code runs |
| Create AppBuilder only | 712 B | AppBuilder + ServiceCollection + EndpointCollection |
| Add route + Build() | 7,984 B | DI container creation (7,272 B) |
| DI + GetRequiredService<NuruCli> | 13,928 B | Resolving NuruCli + RouteBasedCommandResolver (5,944 B) |
| Full execution (with route) | 18,464 B | Actually running the CLI (4,536 B) |

### Key Findings

1. **DI Container is NOT the largest consumer** (contrary to initial estimate)
   - DI container build: 7,272 B (39%)
   - NuruCli resolution: 5,944 B (32%)
   - CLI execution: 4,536 B (25%)
   - AppBuilder setup: 712 B (4%)

2. **Surprising allocations during "no-op" execution**
   - Even with no routes defined, just calling RunAsync allocates ~6KB
   - This includes: array concatenation, ResolverResult, error messages, console buffers

3. **We're only 2,568 B away from beating System.CommandLine**
   - Current: 18,464 B (with full route parsing/execution)
   - Without CLI execution: 13,928 B (still more than System.CommandLine's 11,360 B)
   - System.CommandLine: 11,360 B
   - Gap: 2,568 B

4. **Proper DI pattern increases allocations**
   - After fixing DI anti-pattern (injecting services directly instead of ServiceProvider)
   - Memory increased from 14,200 B to 15,296 B (+1,096 B)
   - This is due to eager resolution of CommandExecutor and ITypeConverterRegistry
   - The lazy GetRequiredService pattern was saving memory in our benchmark scenario

5. **System.CommandLine comparison is not apples-to-apples**
   - System.CommandLine benchmark: NO dependency injection
   - TimeWarp.Nuru benchmark: Full DI container included
   - This accounts for most of the allocation difference

### Memory Allocation Sources

#### 1. AppBuilder Construction (712 B)
```csharp
new AppBuilder()
  - ServiceCollection instance
  - EndpointCollection instance
  - Default service registrations
```

#### 2. DI Container Build (7,272 B)
```csharp
ServiceCollection.BuildServiceProvider()
  - ServiceProvider internal structures
  - Service descriptors and caches
  - Scoped service tracking
  - Type activation caches
```

#### 3. NuruCli Resolution (5,944 B)
```csharp
ServiceProvider.GetRequiredService<NuruCli>()
  - NuruCli instance
  - RouteBasedCommandResolver instance
  - Getting EndpointCollection from DI
  - Getting ITypeConverterRegistry from DI
```

#### 4. CLI Execution (4,536 B)
```csharp
cli.RunAsync(args)
  - new[] { "test" }.Concat(args).ToArray()
  - Dictionary<string, string> for each endpoint match attempt
  - ResolverResult with error message
  - Console.Error.WriteLineAsync buffers
  - ArraySegment for remaining args
```

## Planned Optimizations

### DirectAppBuilder - No-DI Path (Planned: 2025-07-27)

Based on our analysis, we plan to implement a lightweight alternative that skips DI entirely:

**Design**:
```csharp
public class DirectAppBuilder
{
    private readonly EndpointCollection endpoints = new();
    private readonly TypeConverterRegistry typeConverters = new();
    
    public void AddRoute(string pattern, Delegate handler) { ... }
    public Task<int> RunAsync(string[] args) { ... }
}
```

**Expected savings**:
- Skip ServiceCollection: -712 B
- Skip ServiceProvider build: -7,272 B
- Skip NuruCli DI resolution: -1,096 B
- **Total expected savings: ~9KB**

**Trade-offs**:
- No Mediator support (requires DI)
- No service injection into handlers
- Manual wiring only
- But perfect for simple CLI tools that don't need DI

This would give users two paths:
- **AppBuilder**: Full features with DI (~15.3KB)
- **DirectAppBuilder**: Lightweight, no DI (~6-7KB expected)

## Implemented Optimizations

### 1. ArraySegment for Argument Slicing (Implemented: 2025-07-26)

**Change**: Replaced `args.Skip(consumedArgs).ToList()` with `ArraySegment<string>`

**Location**: `RouteBasedCommandResolver.cs:53`

**Before**:
```csharp
var remainingArgs = args.Skip(consumedArgs).ToList();
```

**After**:
```csharp
var remainingArgs = new ArraySegment<string>(args, consumedArgs, args.Length - consumedArgs);
```

**Results**:
- Memory saved: 112 bytes (18,576 B → 18,464 B)
- Percentage improvement: 0.6%
- Performance impact: Improved from 42.747 ms to 35.720 ms (16.4% faster)
- Ranking improvement: Moved from 7th to 5th place (now ahead of McMaster and CommandLineParser)

**Notes**: 
- Small memory saving but significant performance improvement
- Eliminated LINQ allocation overhead
- Changed method signatures from `IReadOnlyList<string>` to `string[]` for efficiency

### 2. String Interning for Common Values (Implemented: 2025-07-26)

**Change**: Created `CommonStrings` class with interned common CLI strings

**Files Changed**:
- Created `CommonStrings.cs` with interned strings
- Updated `RouteBasedCommandResolver.cs` to use `CommonStrings.SingleDash` 
- Updated `RoutePatternParser.cs` to use `CommonStrings.DoubleDash` and `CommonStrings.SingleDash`
- Updated `OptionSegment.cs` to use interned dash strings
- Updated `BoolTypeConverter.cs` to use interned boolean value strings

**Implementation**:
```csharp
internal static class CommonStrings
{
  public static readonly string SingleDash = string.Intern("-");
  public static readonly string DoubleDash = string.Intern("--");
  public static readonly string True = string.Intern("true");
  public static readonly string False = string.Intern("false");
  // ... more common strings
}
```

**Results**:
- Memory saved: 0 bytes in cold-start benchmark
- Memory benefit appears in repeated executions when same strings are reused
- Ensures common strings like "-", "--", "true", "false" share memory across the application

**Notes**:
- String interning reduces memory when strings are created multiple times
- No benefit in single-execution benchmarks
- Most valuable in long-running applications or repeated command parsing