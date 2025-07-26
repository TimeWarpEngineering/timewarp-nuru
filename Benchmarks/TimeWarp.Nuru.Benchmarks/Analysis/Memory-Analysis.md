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