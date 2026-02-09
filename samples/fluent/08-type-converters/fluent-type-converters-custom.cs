#!/usr/bin/dotnet --
#:project ../../../source/timewarp-nuru/timewarp-nuru.csproj

// ============================================================================
// FLUENT DSL - Custom Type Converter Example
// ============================================================================
// This sample demonstrates how to create and register custom type converters
// using Fluent DSL.
//
// DSL: Fluent API with .AddTypeConverter()
//
// We'll implement three custom converters:
//   1. EmailAddress - Common pattern for email validation
//   2. HexColor - RGB colors in hex format (#FF5733)
//   3. SemanticVersion - SemVer 2.0 parsing (major.minor.patch)
// ============================================================================

using TimeWarp.Nuru;

NuruAppBuilder builder = NuruApp.CreateBuilder();

// Register custom converters
builder.AddTypeConverter(new EmailAddressConverter());
builder.AddTypeConverter(new HexColorConverter());
builder.AddTypeConverter(new SemanticVersionConverter());

// ============================================================================
// Routes Using Custom Types
// ============================================================================

builder.Map("send-email {to:EmailAddress} {subject}")
  .WithHandler((EmailAddress to, string subject) =>
  {
    Console.WriteLine($"📧 Sending Email:");
    Console.WriteLine($"   To: {to}");
    Console.WriteLine($"   Local part: {to.LocalPart}");
    Console.WriteLine($"   Domain: {to.Domain}");
    Console.WriteLine($"   Subject: {subject}");
  })
  .AsCommand().Done();

builder.Map("set-theme {primary:HexColor} {secondary:HexColor}")
  .WithHandler((HexColor primary, HexColor secondary) =>
  {
    Console.WriteLine($"🎨 Theme Configuration:");
    Console.WriteLine($"   Primary color: {primary}");
    Console.WriteLine($"     RGB: ({primary.Red}, {primary.Green}, {primary.Blue})");
    Console.WriteLine($"   Secondary color: {secondary}");
    Console.WriteLine($"     RGB: ({secondary.Red}, {secondary.Green}, {secondary.Blue})");
  })
  .AsIdempotentCommand().Done();

builder.Map("release {version:SemanticVersion}")
  .WithHandler((SemanticVersion version) =>
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
  })
  .AsCommand().Done();

builder.Map("notify {recipient:EmailAddress} {color:HexColor} {message}")
  .WithHandler((EmailAddress recipient, HexColor color, string message) =>
  {
    Console.WriteLine($"🔔 Notification:");
    Console.WriteLine($"   Recipient: {recipient}");
    Console.WriteLine($"   Color: {color}");
    Console.WriteLine($"   Message: {message}");
  })
  .AsCommand().Done();

builder.Map("deploy {version:SemanticVersion} {env} --notify {email:EmailAddress?}")
  .WithHandler((SemanticVersion version, string env, EmailAddress? email) =>
  {
    Console.WriteLine($"🚀 Deployment Plan:");
    Console.WriteLine($"   Version: {version}");
    Console.WriteLine($"   Environment: {env}");
    if (email != null)
      Console.WriteLine($"   Notification: {email}");
  })
  .AsCommand().Done();

NuruApp app = builder.Build();

// Show usage examples
if (args.Length == 0)
{
  Console.WriteLine("═══════════════════════════════════════════════════════════════");
  Console.WriteLine("   TimeWarp.Nuru - Custom Type Converters Demo (Fluent DSL)");
  Console.WriteLine("═══════════════════════════════════════════════════════════════");
  Console.WriteLine();
  Console.WriteLine("This sample demonstrates three custom type converters:");
  Console.WriteLine();
  Console.WriteLine("1. EmailAddress (type constraint: EmailAddress)");
  Console.WriteLine("   ./fluent-type-converters-custom.cs send-email user@example.com \"Hello World\"");
  Console.WriteLine();
  Console.WriteLine("2. HexColor (type constraint: HexColor)");
  Console.WriteLine("   ./fluent-type-converters-custom.cs set-theme #FF5733 #3498DB");
  Console.WriteLine();
  Console.WriteLine("3. SemanticVersion (type constraint: SemanticVersion)");
  Console.WriteLine("   ./fluent-type-converters-custom.cs release 1.0.0");
  Console.WriteLine("   ./fluent-type-converters-custom.cs release 2.1.3-beta");
  Console.WriteLine();
  Console.WriteLine("═══════════════════════════════════════════════════════════════");
}

return await app.RunAsync(args);

// ============================================================================
// Custom Type: EmailAddress
// ============================================================================

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

    if (value.IndexOf('@', atIndex + 1) >= 0)
      return false;

    string localPart = value[..atIndex];
    string domain = value[(atIndex + 1)..];

    if (!domain.Contains('.'))
      return false;

    result = new EmailAddress(localPart, domain);
    return true;
  }
}

public class EmailAddressConverter : IRouteTypeConverter
{
  public Type TargetType => typeof(EmailAddress);
  public string? ConstraintAlias => "email";

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

public record HexColor(byte Red, byte Green, byte Blue)
{
  public override string ToString() => $"#{Red:X2}{Green:X2}{Blue:X2}";

  public static bool TryParse(string value, out HexColor? result)
  {
    result = null;
    if (string.IsNullOrWhiteSpace(value))
      return false;

    string hex = value.StartsWith('#') ? value[1..] : value;

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
  public string? ConstraintAlias => null;

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

    string[] buildParts = value.Split('+', 2);
    string versionPart = buildParts[0];
    string? buildMetadata = buildParts.Length > 1 ? buildParts[1] : null;

    string[] prereleaseParts = versionPart.Split('-', 2);
    string corePart = prereleaseParts[0];
    string? prerelease = prereleaseParts.Length > 1 ? prereleaseParts[1] : null;

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
  public string? ConstraintAlias => "semver";

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
