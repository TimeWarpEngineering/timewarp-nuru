# Proposed Optimizations for TimeWarp.Nuru.Direct

After analyzing the current implementation (7,096 B), here are proposed optimizations to further reduce memory allocations:

## 1. Dictionary Reuse in RouteBasedCommandResolver

**Location**: `RouteBasedCommandResolver.cs:47`

**Current Issue**: Creates a new Dictionary for every endpoint match attempt
```csharp
var extractedValues = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
```

**Proposed Solution**: Add a reusable dictionary field
```csharp
private readonly Dictionary<string, string> ReusableExtractedValues = new(StringComparer.OrdinalIgnoreCase);

// In MatchRoute method:
ReusableExtractedValues.Clear();
// ... use ReusableExtractedValues for matching
// Return a copy only when match succeeds:
return (endpoint, new Dictionary<string, string>(ReusableExtractedValues, StringComparer.OrdinalIgnoreCase));
```

**Expected Savings**: ~500-1000 B (depends on number of endpoints tried)

**Trade-offs**: 
- Slightly more complex code
- Need to ensure thread safety if used concurrently

## 2. Eliminate string.Join Allocation

**Location**: `RouteBasedCommandResolver.cs:81`

**Current Issue**: Creates new string for catch-all parameters
```csharp
extractedValues[param.Name] = string.Join(CommonStrings.Space, args.Skip(i));
```

**Proposed Solution**: Use a StringBuilder or pre-calculate the string length
```csharp
// Option A: StringBuilder
var sb = new StringBuilder();
for (int j = i; j < args.Length; j++)
{
    if (j > i) sb.Append(' ');
    sb.Append(args[j]);
}
extractedValues[param.Name] = sb.ToString();

// Option B: Pre-calculate length
int totalLength = args.Length - i - 1; // spaces
for (int j = i; j < args.Length; j++)
    totalLength += args[j].Length;

var result = string.Create(totalLength, (args, i), (span, state) =>
{
    // Fill span with joined string
});
```

**Expected Savings**: ~200-500 B

**Trade-offs**: More complex code for Option B

## 3. Optimize DirectApp Error Handling

**Location**: `DirectAppBuilder.cs:82-85, 97-100`

**Current Issue**: Async Console.Error.WriteLineAsync allocates
```csharp
await Console.Error.WriteLineAsync(
    result.ErrorMessage ?? "No matching command found."
).ConfigureAwait(false);
```

**Proposed Solution**: Use synchronous writes for error paths
```csharp
Console.Error.WriteLine(result.ErrorMessage ?? "No matching command found.");
```

**Expected Savings**: ~100-200 B

**Trade-offs**: 
- Blocking I/O on error path (usually acceptable)
- Less consistent async pattern

## 4. Optimize Parameter Binding

**Location**: `DirectAppBuilder.cs:148-149`

**Current Issue**: Always allocates parameter array even for no-arg delegates
```csharp
ParameterInfo[] parameters = method.GetParameters();
object?[] args = new object?[parameters.Length];
```

**Proposed Solution**: Special case for common scenarios
```csharp
ParameterInfo[] parameters = method.GetParameters();
if (parameters.Length == 0)
{
    del.DynamicInvoke(Array.Empty<object>());
    return;
}

object?[] args = new object?[parameters.Length];
```

**Expected Savings**: ~50-100 B for no-arg commands

**Trade-offs**: Additional code path

## 5. Cache MethodInfo.GetParameters() Results

**Location**: `DirectAppBuilder.cs:148`

**Current Issue**: GetParameters() may allocate internally

**Proposed Solution**: Cache parameter info during route registration
```csharp
// In RouteEndpoint, add:
public ParameterInfo[]? CachedParameters { get; set; }

// During registration:
endpoint.CachedParameters = handler.Method.GetParameters();

// During execution:
ParameterInfo[] parameters = endpoint.CachedParameters ?? method.GetParameters();
```

**Expected Savings**: ~100-300 B

**Trade-offs**: 
- Slightly more memory usage for endpoint storage
- More complex endpoint creation

## 6. Optimize ShowAvailableCommands

**Location**: `DirectAppBuilder.cs:197-201`

**Current Issue**: Multiple Console.WriteLine calls

**Proposed Solution**: Build single string
```csharp
var sb = new StringBuilder("\nAvailable commands:\n");
foreach (RouteEndpoint endpoint in Endpoints)
{
    sb.AppendLine($"  {endpoint.RoutePattern}  {endpoint.Description ?? ""}");
}
Console.Write(sb.ToString());
```

**Expected Savings**: Minimal, only on error path

**Trade-offs**: Allocates StringBuilder but reduces console calls

## Summary

Total potential savings: 1,000 - 2,500 B

This could bring DirectApp down to approximately 4,500 - 6,000 B, which would be:
- 47-60% less than System.CommandLine (11,360 B)
- Among the most memory-efficient CLI frameworks ever measured

## Recommended Priority

1. **High Priority**: Dictionary reuse (#1) - biggest impact, relatively simple
2. **Medium Priority**: String.Join optimization (#2) - noticeable impact
3. **Low Priority**: Others - diminishing returns, added complexity

## Questions to Consider

1. Is thread safety important for DirectApp?
2. Should we optimize for the common case (single endpoint) or general case?
3. Is the added complexity worth the memory savings?
4. Should we create a "DirectAppUltraLight" for absolute minimum allocations?