# Add Typed Array Support for Catch-All Parameters

## Description

Extend the catch-all parameter feature to support typed arrays beyond just string arrays. Currently, catch-all parameters (`{*args}`) only work with `string[]`, requiring handlers to manually parse and convert values. This enhancement would allow specifying a type constraint on catch-all parameters, enabling automatic conversion by the framework.

## Current Limitation

```csharp
// Currently only string[] is supported
app.AddRoute("sum {*numbers}", (string[] numbers) => {
    // Handler must do conversion
    var intNumbers = numbers.Select(int.Parse).ToArray();
    var total = intNumbers.Sum();
});
```

## Proposed Enhancement

```csharp
// With typed array support
app.AddRoute("sum {*numbers:int}", (int[] numbers) => {
    // Framework handles conversion
    var total = numbers.Sum();
});

// String arrays still work (explicit or default)
app.AddRoute("echo {*args:string}", (string[] args) => { });
app.AddRoute("echo {*args}", (string[] args) => { }); // Default is string
```

## Supported Array Types

- `{*values:string}` → `string[]` (default if no type specified)
- `{*values:int}` → `int[]`
- `{*values:double}` → `double[]`
- `{*values:bool}` → `bool[]`
- `{*values:DateTime}` → `DateTime[]`
- `{*values:Guid}` → `Guid[]`
- `{*values:long}` → `long[]`
- `{*values:float}` → `float[]`
- `{*values:decimal}` → `decimal[]`
- Enum types: `{*values:LogLevel}` → `LogLevel[]`

## Implementation Changes

### 1. Update DelegateParameterBinder

In `DelegateParameterBinder.cs`, enhance the array handling:

```csharp
if (param.ParameterType.IsArray)
{
    Type elementType = param.ParameterType.GetElementType()!;
    if (elementType == typeof(string))
    {
        args[i] = stringValue.Split(' ', StringSplitOptions.RemoveEmptyEntries);
    }
    else
    {
        // New: Convert each element to the target type
        var stringValues = stringValue.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var typedArray = Array.CreateInstance(elementType, stringValues.Length);
        
        for (int j = 0; j < stringValues.Length; j++)
        {
            if (typeConverterRegistry.TryConvert(stringValues[j], elementType, out var converted))
            {
                typedArray.SetValue(converted, j);
            }
            else
            {
                throw new InvalidOperationException(
                    $"Cannot convert '{stringValues[j]}' to {elementType} for array parameter '{param.Name}'");
            }
        }
        args[i] = typedArray;
    }
}
```

### 2. Update Route Parsing

Ensure the parser correctly handles type constraints on catch-all parameters:
- `{*args}` → default to string array
- `{*args:type}` → use specified type for array elements

### 3. Error Handling

Decide on error handling strategy:
- **Fail fast**: Throw exception on first conversion failure
- **Best effort**: Skip invalid values and continue
- **Configurable**: Let developer choose the strategy

## Examples

```csharp
// Mathematics
app.AddRoute("sum {*numbers:int}", (int[] numbers) => 
    Console.WriteLine($"Sum: {numbers.Sum()}"));

app.AddRoute("average {*values:double}", (double[] values) => 
    Console.WriteLine($"Average: {values.Average()}"));

// Date handling
app.AddRoute("schedule {*dates:DateTime}", (DateTime[] dates) => {
    foreach (var date in dates.OrderBy(d => d))
        Console.WriteLine(date.ToString("yyyy-MM-dd"));
});

// Mixed with other parameters
app.AddRoute("stats {dataset} {*values:double}", (string dataset, double[] values) => {
    Console.WriteLine($"{dataset}: Min={values.Min()}, Max={values.Max()}");
});

// Developers can still use string[] for custom parsing
app.AddRoute("custom {*args:string}", (string[] args) => {
    // Full control over parsing and error handling
});
```

## Benefits

1. **Cleaner code** - No boilerplate conversion in handlers
2. **Type safety** - Compile-time checking of handler signatures
3. **Consistency** - Same type conversion logic as single parameters
4. **Flexibility** - Developers can still use string[] when needed
5. **Better error messages** - Framework can provide consistent conversion errors

## Testing

- Unit tests for each supported array type
- Error cases (invalid conversions)
- Mixed valid/invalid values
- Empty arrays
- Integration with existing catch-all functionality

## Success Criteria

- All built-in types support array conversion
- Existing string[] catch-all continues to work
- Clear error messages for conversion failures
- Performance comparable to manual conversion
- Documentation updated with examples