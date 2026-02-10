using System.Text.RegularExpressions;
using TimeWarp.Nuru;

public readonly record struct EmailAddress
{
  public string Value { get; }
  public string Domain => Value.Split('@').LastOrDefault() ?? "";

  public EmailAddress(string value)
  {
    if (!IsValid(value))
      throw new ArgumentException($"Invalid email address: {value}");
    Value = value;
  }

  public static bool IsValid(string email)
  {
    if (string.IsNullOrWhiteSpace(email)) return false;
    return Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$");
  }

  public override string ToString() => Value;
}

public class EmailAddressConverter : IRouteTypeConverter
{
  public Type TargetType => typeof(EmailAddress);
  public string? ConstraintAlias => "email";

  public bool TryConvert(string value, out object? result)
  {
    if (EmailAddress.IsValid(value))
    {
      result = new EmailAddress(value);
      return true;
    }

    result = null;
    return false;
  }
}
