#!/usr/bin/dotnet --
#:project timewarp-nuru-sample/timewarp-nuru-sample.csproj

// ============================================================================
// Custom Type Converter Example
// ============================================================================
// This sample demonstrates how to create and register custom type converters
// in TimeWarp.Nuru, extending beyond the 15 built-in types.
//
// We'll implement three custom converters:
//   1. EmailAddress - Common pattern for email validation
//   2. HexColor - RGB colors in hex format (#FF5733)
//   3. SemanticVersion - SemVer 2.0 parsing (major.minor.patch)
//
// This shows:
//   - Implementing IRouteTypeConverter
//   - Registering custom converters
//   - Using custom types in route patterns
//   - Validation and error handling
//   - Both simple and complex parsing logic
// ============================================================================

using TimeWarp.Nuru;

// ============================================================================
// Application Setup
// ============================================================================

NuruAppBuilder builder = new();

// Register custom converters
builder.AddTypeConverter(new EmailAddressConverter());
builder.AddTypeConverter(new HexColorConverter());
builder.AddTypeConverter(new SemanticVersionConverter());

// ============================================================================
// Routes Using Custom Types
// ============================================================================

builder.Map("send-email {to:email} {subject}", (EmailAddress to, string subject) =>
{
  Console.WriteLine($"📧 Sending Email:");
  Console.WriteLine($"   To: {to}");
  Console.WriteLine($"   Local part: {to.LocalPart}");
  Console.WriteLine($"   Domain: {to.Domain}");
  Console.WriteLine($"   Subject: {subject}");
  return 0;
});

builder.Map("set-theme {primary:hexcolor} {secondary:hexcolor}", (HexColor primary, HexColor secondary) =>
{
  Console.WriteLine($"🎨 Theme Configuration:");
  Console.WriteLine($"   Primary color: {primary}");
  Console.WriteLine($"     RGB: ({primary.Red}, {primary.Green}, {primary.Blue})");
  Console.WriteLine($"   Secondary color: {secondary}");
  Console.WriteLine($"     RGB: ({secondary.Red}, {secondary.Green}, {secondary.Blue})");
  return 0;
});

builder.Map("release {version:semver}", (SemanticVersion version) =>
{
  Console.WriteLine($"🚀 Creating Release:");
  Console.WriteLine($"   Version: {version}");
  Console.WriteLine($"   Major: {version.Major}");
  Console.WriteLine($"   Minor: {version.Minor}");
  Console.WriteLine($"   Patch: {version.Patch}");
  if (version.Prerelease != null)
    Console.WriteLine($"   Prerelease: {version.Prerelease}");
  if (version.BuildMetadata != null)
    Console.WriteLine($"   Build: {version.BuildMetadata}");
  return 0;
});

builder.Map("notify {recipient:email} {color:hexcolor} {message}", (EmailAddress recipient, HexColor color, string message) =>
{
  Console.WriteLine($"🔔 Notification:");
  Console.WriteLine($"   Recipient: {recipient}");
  Console.WriteLine($"   Color: {color}");
  Console.WriteLine($"   Message: {message}");
  return 0;
});

builder.Map("deploy {version:semver} {env} --notify {email:email?}", (SemanticVersion version, string env, EmailAddress? email) =>
{
  Console.WriteLine($"🚀 Deployment Plan:");
  Console.WriteLine($"   Version: {version}");
  Console.WriteLine($"   Environment: {env}");
  if (email != null)
    Console.WriteLine($"   Notification: {email}");
  return 0;
});

NuruCoreApp app = builder.Build();

// Show usage examples
if (args.Length == 0)
{
  Console.WriteLine("═══════════════════════════════════════════════════════════════");
  Console.WriteLine("   TimeWarp.Nuru - Custom Type Converters Demo");
  Console.WriteLine("═══════════════════════════════════════════════════════════════");
  Console.WriteLine();
  Console.WriteLine("This sample demonstrates three custom type converters:");
  Console.WriteLine();
  Console.WriteLine("1. EmailAddress (constraint: email)");
  Console.WriteLine("   - Validates email format (local@domain)");
  Console.WriteLine("   - Extracts local part and domain");
  Console.WriteLine();
  Console.WriteLine("2. HexColor (constraint: hexcolor)");
  Console.WriteLine("   - Parses RGB colors in hex format");
  Console.WriteLine("   - Accepts #RRGGBB or RRGGBB");
  Console.WriteLine();
  Console.WriteLine("3. SemanticVersion (constraint: semver)");
  Console.WriteLine("   - Parses SemVer 2.0 versions");
  Console.WriteLine("   - Supports major.minor.patch-prerelease+build");
  Console.WriteLine();
  Console.WriteLine("═══════════════════════════════════════════════════════════════");
  Console.WriteLine();
  Console.WriteLine("Try these examples:");
  Console.WriteLine();
  Console.WriteLine("EmailAddress examples:");
  Console.WriteLine("  ./CustomTypeConverterExample.cs send-email user@example.com \"Hello World\"");
  Console.WriteLine("  ./CustomTypeConverterExample.cs send-email admin@company.org \"System Alert\"");
  Console.WriteLine();
  Console.WriteLine("HexColor examples:");
  Console.WriteLine("  ./CustomTypeConverterExample.cs set-theme #FF5733 #3498DB");
  Console.WriteLine("  ./CustomTypeConverterExample.cs set-theme FF5733 C70039");
  Console.WriteLine();
  Console.WriteLine("SemanticVersion examples:");
  Console.WriteLine("  ./CustomTypeConverterExample.cs release 1.0.0");
  Console.WriteLine("  ./CustomTypeConverterExample.cs release 2.1.3-beta");
  Console.WriteLine("  ./CustomTypeConverterExample.cs release 1.0.0-rc.1+20231225");
  Console.WriteLine();
  Console.WriteLine("Combined examples:");
  Console.WriteLine("  ./CustomTypeConverterExample.cs notify admin@example.com #FF0000 \"Critical Alert\"");
  Console.WriteLine("  ./CustomTypeConverterExample.cs deploy 1.2.3 production --notify ops@company.com");
  Console.WriteLine("  ./CustomTypeConverterExample.cs deploy 2.0.0-beta staging");
  Console.WriteLine();
  Console.WriteLine("═══════════════════════════════════════════════════════════════");
  Console.WriteLine();
  Console.WriteLine("Implementation Notes:");
  Console.WriteLine();
  Console.WriteLine("Each converter implements IRouteTypeConverter:");
  Console.WriteLine("  - TargetType: The type being converted to");
  Console.WriteLine("  - ConstraintName: The name used in route patterns");
  Console.WriteLine("  - TryConvert: Parsing logic with validation");
  Console.WriteLine();
  Console.WriteLine("Register converters with:");
  Console.WriteLine("  builder.AddTypeConverter(new YourConverter());");
  Console.WriteLine();
  Console.WriteLine("Then use in route patterns:");
  Console.WriteLine("  builder.Map(\"cmd {param:yourtype}\", (YourType param) => ...)");
  Console.WriteLine();
  Console.WriteLine("═══════════════════════════════════════════════════════════════");
  return 0;
}

return await app.RunAsync(args);

// ============================================================================
// Custom Type: EmailAddress
// ============================================================================
// Simple validation - checks for @ and basic structure

public record EmailAddress(string LocalPart, string Domain)
{
  public override string ToString() => $"{LocalPart}@{Domain}";

  public static bool TryParse(string value, out EmailAddress? result)
  {
    result = null;
    if (string.IsNullOrWhiteSpace(value))
      return false;

    int atIndex = value.IndexOf('@');
    if (atIndex <= 0 || atIndex == value.Length - 1)
      return false;

    // Check for multiple @ symbols
    if (value.IndexOf('@', atIndex + 1) >= 0)
      return false;

    string localPart = value[..atIndex];
    string domain = value[(atIndex + 1)..];

    // Basic validation - domain must have at least one dot
    if (!domain.Contains('.'))
      return false;

    result = new EmailAddress(localPart, domain);
    return true;
  }
}

public class EmailAddressConverter : IRouteTypeConverter
{
  public Type TargetType => typeof(EmailAddress);
  public string ConstraintName => "email";

  public bool TryConvert(string value, out object? result)
  {
    if (EmailAddress.TryParse(value, out EmailAddress? email))
    {
      result = email;
      return true;
    }

    result = null;
    return false;
  }
}

// ============================================================================
// Custom Type: HexColor
// ============================================================================
// RGB color in hex format: #RRGGBB or RRGGBB

public record HexColor(byte Red, byte Green, byte Blue)
{
  public override string ToString() => $"#{Red:X2}{Green:X2}{Blue:X2}";

  public static bool TryParse(string value, out HexColor? result)
  {
    result = null;
    if (string.IsNullOrWhiteSpace(value))
      return false;

    // Remove # if present
    string hex = value.StartsWith('#') ? value[1..] : value;

    // Must be exactly 6 hex digits
    if (hex.Length != 6)
      return false;

    try
    {
      byte r = Convert.ToByte(hex[0..2], 16);
      byte g = Convert.ToByte(hex[2..4], 16);
      byte b = Convert.ToByte(hex[4..6], 16);

      result = new HexColor(r, g, b);
      return true;
    }
    catch (FormatException)
    {
      return false;
    }
  }
}

public class HexColorConverter : IRouteTypeConverter
{
  public Type TargetType => typeof(HexColor);
  public string ConstraintName => "hexcolor";

  public bool TryConvert(string value, out object? result)
  {
    if (HexColor.TryParse(value, out HexColor? color))
    {
      result = color;
      return true;
    }

    result = null;
    return false;
  }
}

// ============================================================================
// Custom Type: SemanticVersion
// ============================================================================
// SemVer 2.0: major.minor.patch with optional prerelease and build metadata

public record SemanticVersion(int Major, int Minor, int Patch, string? Prerelease = null, string? BuildMetadata = null)
{
  public override string ToString()
  {
    string version = $"{Major}.{Minor}.{Patch}";
    if (!string.IsNullOrEmpty(Prerelease))
      version += $"-{Prerelease}";
    if (!string.IsNullOrEmpty(BuildMetadata))
      version += $"+{BuildMetadata}";
    return version;
  }

  public static bool TryParse(string value, out SemanticVersion? result)
  {
    result = null;
    if (string.IsNullOrWhiteSpace(value))
      return false;

    // Split on + for build metadata
    string[] buildParts = value.Split('+', 2);
    string versionPart = buildParts[0];
    string? buildMetadata = buildParts.Length > 1 ? buildParts[1] : null;

    // Split on - for prerelease
    string[] prereleaseParts = versionPart.Split('-', 2);
    string corePart = prereleaseParts[0];
    string? prerelease = prereleaseParts.Length > 1 ? prereleaseParts[1] : null;

    // Parse major.minor.patch
    string[] versionNumbers = corePart.Split('.');
    if (versionNumbers.Length != 3)
      return false;

    if (!int.TryParse(versionNumbers[0], out int major) || major < 0)
      return false;
    if (!int.TryParse(versionNumbers[1], out int minor) || minor < 0)
      return false;
    if (!int.TryParse(versionNumbers[2], out int patch) || patch < 0)
      return false;

    result = new SemanticVersion(major, minor, patch, prerelease, buildMetadata);
    return true;
  }
}

public class SemanticVersionConverter : IRouteTypeConverter
{
  public Type TargetType => typeof(SemanticVersion);
  public string ConstraintName => "semver";

  public bool TryConvert(string value, out object? result)
  {
    if (SemanticVersion.TryParse(value, out SemanticVersion? version))
    {
      result = version;
      return true;
    }

    result = null;
    return false;
  }
}
