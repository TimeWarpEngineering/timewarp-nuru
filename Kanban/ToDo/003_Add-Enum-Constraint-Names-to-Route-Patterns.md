# Add Enum Constraint Names to Route Patterns

## Description

Enable custom type names (especially enums) as constraints in route patterns, allowing syntax like `{member:Member}` where Member is an enum type. This would provide a more intuitive API and better alignment with other web frameworks.

## Requirements

- Update route pattern parser to recognize custom type names as constraints
- Auto-register enum converters when enum constraints are detected in routes
- Support both built-in types and custom enum types
- Maintain backward compatibility with existing constraint syntax

## Implementation Plan

### Phase 1: Update Route Pattern Parser
- Modify the route parser to accept custom type names as constraints
- Add validation to ensure the type exists and is valid
- Handle case-insensitive type name matching

### Phase 2: Auto-Registration of Enum Converters
- When parsing a route with enum constraint (e.g., `{level:LogLevel}`)
- Check if the type is an enum using reflection
- Automatically create and register an `EnumTypeConverter<T>` instance
- Cache the converter for reuse

### Phase 3: Enhanced Error Messages
- Provide helpful error messages when enum parsing fails
- Show valid enum values in error output
- Improve constraint validation messages

## Example Usage

```csharp
// Before (current approach)
.AddRoute("log {message} --level {level}", 
    (string message, LogLevel level) => ...)

// After (with enum constraints)
.AddRoute("log {message} --level {level:LogLevel}", 
    (string message, LogLevel level) => ...)

// Would also work with any enum
.AddRoute("deploy {env:Environment} --priority {priority:Priority}",
    (Environment env, Priority priority) => ...)
```

## Benefits

- More explicit route definitions
- Better self-documentation in route patterns
- Compile-time type safety hints
- Consistent with ASP.NET Core routing patterns
- Enables future expansion to other custom types

## Technical Considerations

- Need to handle type resolution at route registration time
- Consider performance impact of reflection-based type lookup
- Ensure proper error handling for non-existent types
- May need to support assembly-qualified type names for ambiguous cases

## Dependencies

- Requires the custom type converter infrastructure (already implemented)
- Built on top of existing `EnumTypeConverter<T>` class
- Uses `TypeConverterRegistry` for registration

## Notes

This is an optional enhancement that builds on the enum support already added. The current system works fine with automatic enum conversion based on parameter types, but this would provide a more explicit and self-documenting API.