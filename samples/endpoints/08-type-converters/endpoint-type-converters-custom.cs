#!/usr/bin/dotnet --
#:project ../../../source/timewarp-nuru/timewarp-nuru.csproj

// ═══════════════════════════════════════════════════════════════════════════════
// ENDPOINT DSL - CUSTOM TYPE CONVERTERS ⭐ RECOMMENDED
// ═══════════════════════════════════════════════════════════════════════════════
//
// This sample demonstrates creating custom IRouteTypeConverter implementations
// for your own types. Shows EmailAddress, HexColor, and SemanticVersion converters.
//
// DSL: Endpoint with custom type parameters
//
// PATTERN:
//   1. Create class implementing IRouteTypeConverter<T>
//   2. Implement TryConvert with validation
//   3. Register with [RouteTypeConverter(typeof(YourConverter))] on the type
// ═══════════════════════════════════════════════════════════════════════════════

using System.Globalization;
using System.Text.RegularExpressions;
using TimeWarp.Nuru;
using static System.Console;

NuruApp app = NuruApp.CreateBuilder()
  .DiscoverEndpoints()
  .Build();

return await app.RunAsync(args);

// =============================================================================
// CUSTOM TYPE: EMAIL ADDRESS
// =============================================================================

[RouteTypeConverter(typeof(EmailAddressConverter))]
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

public class EmailAddressConverter : IRouteTypeConverter<EmailAddress>
{
  public Type TargetType => typeof(EmailAddress);
  public string ConstraintAlias => "email";

  public bool TryConvert(string value, out EmailAddress result)
  {
    if (EmailAddress.IsValid(value))
    {
      result = new EmailAddress(value);
      return true;
    }

    result = default;
    return false;
  }
}

// =============================================================================
// CUSTOM TYPE: HEX COLOR
// =============================================================================

[RouteTypeConverter(typeof(HexColorConverter))]
public readonly record struct HexColor
{
  public string Value { get; }
  public byte R => Convert.ToByte(Value.Substring(1, 2), 16);
  public byte G => Convert.ToByte(Value.Substring(3, 2), 16);
  public byte B => Convert.ToByte(Value.Substring(5, 2), 16);

  public HexColor(string value)
  {
    if (!IsValid(value))
      throw new ArgumentException($"Invalid hex color: {value}");
    Value = value.ToLowerInvariant();
  }

  public static bool IsValid(string color)
  {
    if (string.IsNullOrWhiteSpace(color)) return false;
    return Regex.IsMatch(color, @"^#[0-9A-Fa-f]{6}$");
  }

  public override string ToString() => Value;
}

public class HexColorConverter : IRouteTypeConverter<HexColor>
{
  public Type TargetType => typeof(HexColor);
  public string ConstraintAlias => "color";

  public bool TryConvert(string value, out HexColor result)
  {
    if (HexColor.IsValid(value))
    {
      result = new HexColor(value);
      return true;
    }

    result = default;
    return false;
  }
}

// =============================================================================
// CUSTOM TYPE: SEMANTIC VERSION
// =============================================================================

[RouteTypeConverter(typeof(SemanticVersionConverter))]
public readonly record struct SemanticVersion : IComparable<SemanticVersion>
{
  public int Major { get; }
  public int Minor { get; }
  public int Patch { get; }
  public string? Prerelease { get; }
  public string? Build { get; }

  public SemanticVersion(int major, int minor, int patch, string? prerelease = null, string? build = null)
  {
    Major = major;
    Minor = minor;
    Patch = patch;
    Prerelease = prerelease;
    Build = build;
  }

  public SemanticVersion(string version)
  {
    Match match = Regex.Match(version, @"^(\d+)\.(\d+)\.(\d+)(?:-([\w.]+))?(?:\+([\w.]+))?$");
    if (!match.Success)
      throw new ArgumentException($"Invalid semantic version: {version}");

    Major = int.Parse(match.Groups[1].Value);
    Minor = int.Parse(match.Groups[2].Value);
    Patch = int.Parse(match.Groups[3].Value);
    Prerelease = match.Groups[4].Success ? match.Groups[4].Value : null;
    Build = match.Groups[5].Success ? match.Groups[5].Value : null;
  }

  public int CompareTo(SemanticVersion other)
  {
    int result = Major.CompareTo(other.Major);
    if (result != 0) return result;

    result = Minor.CompareTo(other.Minor);
    if (result != 0) return result;

    result = Patch.CompareTo(other.Patch);
    if (result != 0) return result;

    // Prerelease versions have lower precedence
    if (Prerelease == null && other.Prerelease != null) return 1;
    if (Prerelease != null && other.Prerelease == null) return -1;

    return string.Compare(Prerelease, other.Prerelease, StringComparison.Ordinal);
  }

  public override string ToString()
  {
    string result = $"{Major}.{Minor}.{Patch}";
    if (Prerelease != null) result += $"-{Prerelease}";
    if (Build != null) result += $"+{Build}";
    return result;
  }
}

public class SemanticVersionConverter : IRouteTypeConverter<SemanticVersion>
{
  public Type TargetType => typeof(SemanticVersion);
  public string ConstraintAlias => "semver";

  public bool TryConvert(string value, out SemanticVersion result)
  {
    try
    {
      result = new SemanticVersion(value);
      return true;
    }
    catch
    {
      result = default;
      return false;
    }
  }
}

// =============================================================================
// ENDPOINT DEFINITIONS
// =============================================================================

[NuruRoute("email", Description = "Send email to address")]
public sealed class EmailCommand : ICommand<Unit>
{
  [Parameter] public EmailAddress Address { get; set; }
  [Parameter] public string Message { get; set; } = "";

  public sealed class Handler : ICommandHandler<EmailCommand, Unit>
  {
    public ValueTask<Unit> Handle(EmailCommand c, CancellationToken ct)
    {
      WriteLine($"Sending email to: {c.Address}");
      WriteLine($"  Domain: {c.Address.Domain}");
      WriteLine($"  Message: {c.Message}");
      WriteLine("✓ Email sent (simulated)");
      return default;
    }
  }
}

[NuruRoute("color", Description = "Set theme color")]
public sealed class ColorCommand : ICommand<Unit>
{
  [Parameter] public HexColor Primary { get; set; }
  [Parameter(IsOptional = true)] public HexColor? Secondary { get; set; }

  public sealed class Handler : ICommandHandler<ColorCommand, Unit>
  {
    public ValueTask<Unit> Handle(ColorCommand c, CancellationToken ct)
    {
      WriteLine("Theme colors:");
      WriteLine($"  Primary: {c.Primary}");
      WriteLine($"    RGB: ({c.Primary.R}, {c.Primary.G}, {c.Primary.B})");

      if (c.Secondary.HasValue)
      {
        WriteLine($"  Secondary: {c.Secondary.Value}");
        WriteLine($"    RGB: ({c.Secondary.Value.R}, {c.Secondary.Value.G}, {c.Secondary.Value.B})");
      }

      return default;
    }
  }
}

[NuruRoute("version", Description = "Check version compatibility")]
public sealed class VersionCommand : ICommand<Unit>
{
  [Parameter] public SemanticVersion Current { get; set; }
  [Parameter] public SemanticVersion Required { get; set; }

  public sealed class Handler : ICommandHandler<VersionCommand, Unit>
  {
    public ValueTask<Unit> Handle(VersionCommand c, CancellationToken ct)
    {
      WriteLine($"Current: {c.Current}");
      WriteLine($"Required: {c.Required}");

      int comparison = c.Current.CompareTo(c.Required);

      if (comparison >= 0)
      {
        WriteLine("✓ Version requirement satisfied".Green());
      }
      else
      {
        WriteLine("✗ Version too old - update required".Red());
      }

      return default;
    }
  }
}

[NuruRoute("validate", Description = "Validate custom types")]
public sealed class ValidateTypesCommand : ICommand<Unit>
{
  [Parameter(IsCatchAll = true)] public string[] Values { get; set; } = [];

  public sealed class Handler : ICommandHandler<ValidateTypesCommand, Unit>
  {
    public ValueTask<Unit> Handle(ValidateTypesCommand c, CancellationToken ct)
    {
      WriteLine("Validating values...\n");

      foreach (string value in c.Values)
      {
        bool isEmail = EmailAddress.IsValid(value);
        bool isColor = HexColor.IsValid(value);

        string type = (isEmail, isColor) switch
        {
          (true, _) => "email",
          (_, true) => "color",
          _ => "unknown"
        };

        WriteLine($"  {value,-30} -> {type}");
      }

      return default;
    }
  }
}
