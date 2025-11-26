namespace TimeWarp.Nuru;

using TimeWarp.Nuru.Parsing;

/// <summary>
/// JSON serialization context for TimeWarp.Nuru with source generation support.
/// For user-defined types, create your own JsonSerializerContext in your application.
/// </summary>
/// <remarks>
/// Exception types use SafeExceptionConverter to avoid TargetSite reflection issues in Native AOT.
/// The source generator creates metadata for exception properties (including TargetSite), but this
/// code is never executed because SafeExceptionConverter is checked first via TryGetTypeInfoForRuntimeCustomConverter.
/// IL2026 warnings are suppressed because the generated property accessors are protected by the converter.
/// </remarks>
// Exception types (handled by SafeExceptionConverter at runtime)
[UnconditionalSuppressMessage("Trimming", "IL2026:RequiresUnreferencedCode",
    Justification = "Exception types use SafeExceptionConverter which handles serialization without accessing TargetSite. " +
                    "The generated metadata that accesses TargetSite is protected by TryGetTypeInfoForRuntimeCustomConverter check and is never executed.")]
[JsonSerializable(typeof(Exception))]
[JsonSerializable(typeof(InvalidOperationException))]
[JsonSerializable(typeof(ArgumentException))]
[JsonSerializable(typeof(ParseException))]
[JsonSerializable(typeof(PatternException))]
// Errors (not exceptions - these are safe for source generation)
[JsonSerializable(typeof(SemanticError))]
[JsonSerializable(typeof(DuplicateParameterNamesError))]
[JsonSerializable(typeof(ConflictingOptionalParametersError))]
[JsonSerializable(typeof(CatchAllNotAtEndError))]
[JsonSerializable(typeof(MixedCatchAllWithOptionalError))]
[JsonSerializable(typeof(DuplicateOptionAliasError))]
[JsonSerializable(typeof(OptionalBeforeRequiredError))]
[JsonSerializable(typeof(InvalidEndOfOptionsSeparatorError))]
[JsonSerializable(typeof(OptionsAfterEndOfOptionsSeparatorError))]
[JsonSerializable(typeof(ParseError))]
[JsonSerializable(typeof(InvalidParameterSyntaxError))]
[JsonSerializable(typeof(UnbalancedBracesError))]
[JsonSerializable(typeof(InvalidOptionFormatError))]
[JsonSerializable(typeof(InvalidTypeConstraintError))]
[JsonSerializable(typeof(InvalidCharacterError))]
[JsonSerializable(typeof(UnexpectedTokenError))]
[JsonSerializable(typeof(NullPatternError))]
// Basic types
[JsonSerializable(typeof(string))]
[JsonSerializable(typeof(int))]
[JsonSerializable(typeof(int?))]
[JsonSerializable(typeof(long))]
[JsonSerializable(typeof(long?))]
[JsonSerializable(typeof(double))]
[JsonSerializable(typeof(double?))]
[JsonSerializable(typeof(float))]
[JsonSerializable(typeof(float?))]
[JsonSerializable(typeof(decimal))]
[JsonSerializable(typeof(decimal?))]
[JsonSerializable(typeof(bool))]
[JsonSerializable(typeof(bool?))]
[JsonSerializable(typeof(DateTime))]
[JsonSerializable(typeof(DateTime?))]
[JsonSerializable(typeof(DateTimeOffset))]
[JsonSerializable(typeof(DateTimeOffset?))]
[JsonSerializable(typeof(Guid))]
[JsonSerializable(typeof(Guid?))]
[JsonSerializable(typeof(TimeSpan))]
[JsonSerializable(typeof(TimeSpan?))]
// Collections
[JsonSerializable(typeof(string[]))]
[JsonSerializable(typeof(int[]))]
[JsonSerializable(typeof(List<string>))]
[JsonSerializable(typeof(List<int>))]
[JsonSerializable(typeof(Dictionary<string, object>))]
[JsonSerializable(typeof(Dictionary<string, string>))]
[JsonSerializable(typeof(Dictionary<string, int>))]
[JsonSerializable(typeof(List<SemanticError>))]
[JsonSerializable(typeof(List<ParseError>))]
[JsonSerializable(typeof(IReadOnlyList<SemanticError>))]
[JsonSerializable(typeof(IReadOnlyList<ParseError>))]
// Object type for dynamic responses
[JsonSerializable(typeof(object))]
[JsonSourceGenerationOptions(
    WriteIndented = true,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    Converters = [typeof(SafeExceptionConverter)])]
public partial class NuruJsonSerializerContext : JsonSerializerContext;
