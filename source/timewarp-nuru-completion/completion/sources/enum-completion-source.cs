namespace TimeWarp.Nuru;

/// <summary>
/// Provides completions for enum values.
/// Automatically suggests all enum values for a given enum type.
/// </summary>
/// <typeparam name="TEnum">The enum type to provide completions for.</typeparam>
public sealed class EnumCompletionSource<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TEnum> : ICompletionSource
  where TEnum : struct, Enum
{
  private readonly bool IncludeCaseInsensitive;

  /// <summary>
  /// Initializes a new instance of the <see cref="EnumCompletionSource{TEnum}"/> class.
  /// </summary>
  /// <param name="includeCaseInsensitive">
  /// Whether to include lowercase versions of enum values for case-insensitive matching.
  /// Default is false (only suggest exact enum names).
  /// </param>
  public EnumCompletionSource(bool includeCaseInsensitive = false)
  {
    IncludeCaseInsensitive = includeCaseInsensitive;
  }

  /// <summary>
  /// Gets completions for all enum values.
  /// </summary>
  public IEnumerable<CompletionCandidate> GetCompletions(CompletionContext context)
  {
    ArgumentNullException.ThrowIfNull(context);

    List<CompletionCandidate> candidates = [];

    // Get all enum values
    TEnum[] values = Enum.GetValues<TEnum>();

    foreach (TEnum value in values)
    {
      string name = value.ToString();

      // Add the exact enum name
      candidates.Add(new CompletionCandidate(
        Value: name,
        Description: GetEnumDescription(value),
        Type: CompletionType.Parameter
      ));

      // Optionally add lowercase version
      if (IncludeCaseInsensitive && !name.Equals(name.ToLowerInvariant(), StringComparison.Ordinal))
      {
        candidates.Add(new CompletionCandidate(
          Value: name.ToLowerInvariant(),
          Description: GetEnumDescription(value),
          Type: CompletionType.Parameter
        ));
      }
    }

    return candidates.OrderBy(c => c.Value, StringComparer.Ordinal);
  }

  private static string? GetEnumDescription(TEnum value)
  {
    // Try to get description from System.ComponentModel.DescriptionAttribute
    System.Reflection.MemberInfo[] memberInfo = typeof(TEnum).GetMember(value.ToString());
    if (memberInfo.Length > 0)
    {
      object[] attributes = memberInfo[0].GetCustomAttributes(
        typeof(System.ComponentModel.DescriptionAttribute),
        inherit: false
      );

      if (attributes.Length > 0 &&
          attributes[0] is System.ComponentModel.DescriptionAttribute descAttr)
      {
        return descAttr.Description;
      }
    }

    // Fallback: use the numeric value
    return $"Value: {Convert.ToInt32(value, System.Globalization.CultureInfo.InvariantCulture)}";
  }
}
