#region Purpose
// Package layout gate (kanban 461): verify a produced .nupkg actually contains
// its declared critical payload entries before CI uploads/publishes it.
// Exists because pack can silently produce hollow packages (evaluation-time
// wildcard globs — nuget.org TimeWarp.Nuru 3.0.0-beta.72 shipped without
// build/net10.0, breaking consumers with MSB4062 on clean restores). Tests
// exercise project references, never the packed nupkg — this gate is the only
// thing that looks inside the package.
//
// NuruRequiredPackageEntries (kanban 462) is the static required-payload list
// used by `dev workflow`. It must stay in sync with the explicit Pack="true"
// None items under build/ and analyzers/ in timewarp-nuru.csproj — the
// nupkg-layout-02-csproj-gate-parity test diffs those two sets. Do NOT parse
// the csproj on the release path; keep this array hand-maintained and
// fail-closed (461).
#endregion
#region Design
// Pure-ish: opens the zip read-only, compares entry names ordinally against a
// caller-supplied required list, returns the missing subset. No Terminal, no
// process execution — callers decide how loudly to fail (CI: abort the run).
#endregion

namespace DevCli;

using System.IO.Compression;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Text.RegularExpressions;

/// <summary>
/// Build configuration facts read from an assembly's metadata.
/// </summary>
/// <param name="Configuration">AssemblyConfigurationAttribute value, or empty when absent.</param>
/// <param name="DebuggableFlags">DebuggableAttribute DebuggingModes flags, or 0 when absent.</param>
/// <param name="IsOptimized">True when the DisableOptimizations flag (0x100) is not set.</param>
public sealed record AssemblyBuildInfo(string Configuration, int DebuggableFlags, bool IsOptimized);

public static class NupkgLayoutCheck
{
  private const int DisableOptimizationsFlag = 0x100;

  private static readonly Regex TfmSegmentPattern = new(
    @"^net\d+(\.\d+)*$",
    RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

  /// <summary>
  /// Critical payload the TimeWarp.Nuru nupkg must contain (kanban 461 / 462).
  /// Mirrors explicit Pack includes under build/ and analyzers/ in
  /// timewarp-nuru.csproj, plus the packed lib assembly. Update this array
  /// when those Pack items change — nupkg-layout-02-csproj-gate-parity enforces
  /// build/ + analyzers/ parity with the csproj.
  /// </summary>
  public static readonly string[] NuruRequiredPackageEntries =
  [
    "build/TimeWarp.Nuru.targets",
    "build/TimeWarp.Nuru.props",
    "buildTransitive/TimeWarp.Nuru.props",
    "build/net10.0/TimeWarp.Nuru.Build.dll",
    "build/net10.0/TimeWarp.Nuru.Analyzers.dll",
    "build/net10.0/ICSharpCode.Decompiler.dll",
    "build/net10.0/Microsoft.Build.Framework.dll",
    "build/net10.0/Microsoft.Build.Utilities.Core.dll",
    "build/net10.0/Microsoft.CodeAnalysis.dll",
    "build/net10.0/Microsoft.CodeAnalysis.CSharp.dll",
    "build/net10.0/Microsoft.NET.StringTools.dll",
    "build/net10.0/System.Configuration.ConfigurationManager.dll",
    "build/net10.0/System.Diagnostics.EventLog.dll",
    "build/net10.0/System.Security.Cryptography.ProtectedData.dll",
    "analyzers/dotnet/cs/TimeWarp.Nuru.Analyzers.dll",
    "analyzers/dotnet/cs/Microsoft.Extensions.Logging.Abstractions.dll",
    "analyzers/dotnet/cs/ICSharpCode.Decompiler.dll",
    "lib/net10.0/TimeWarp.Nuru.dll"
  ];

  /// <summary>
  /// Returns the required entries NOT present in the package (empty = pass).
  /// Entry comparison is ordinal against forward-slash zip paths.
  /// </summary>
  public static IReadOnlyList<string> FindMissing(string nupkgPath, IReadOnlyList<string> requiredEntries)
  {
    ArgumentException.ThrowIfNullOrWhiteSpace(nupkgPath);
    ArgumentNullException.ThrowIfNull(requiredEntries);

    using ZipArchive zip = ZipFile.OpenRead(nupkgPath);
    HashSet<string> entries = new(StringComparer.Ordinal);
    foreach (ZipArchiveEntry entry in zip.Entries)
    {
      entries.Add(entry.FullName);
    }

    List<string> missing = [];
    foreach (string required in requiredEntries)
    {
      if (!entries.Contains(required))
      {
        missing.Add(required);
      }
    }

    return missing;
  }

  /// <summary>
  /// Reads the AssemblyConfiguration and Debuggable build facts of an assembly
  /// packed inside a nupkg (task 503). Used by the gate that refuses to ship a
  /// Debug build: the entry is read into memory and inspected with
  /// System.Reflection.Metadata, so nothing is loaded into the process.
  /// </summary>
  public static AssemblyBuildInfo ReadPackedAssemblyBuildInfo(string nupkgPath, string entryName)
  {
    ArgumentException.ThrowIfNullOrWhiteSpace(nupkgPath);
    ArgumentException.ThrowIfNullOrWhiteSpace(entryName);

    using ZipArchive zip = ZipFile.OpenRead(nupkgPath);
    ZipArchiveEntry entry = zip.GetEntry(entryName)
      ?? throw new FileNotFoundException($"Entry '{entryName}' not found in {nupkgPath}.");

    using MemoryStream buffer = new();
    using (Stream entryStream = entry.Open())
    {
      entryStream.CopyTo(buffer);
    }

    buffer.Position = 0;
    return ReadAssemblyBuildInfo(buffer);
  }

  /// <summary>
  /// Reads build configuration facts from a PE assembly stream.
  /// </summary>
  public static AssemblyBuildInfo ReadAssemblyBuildInfo(Stream assemblyStream)
  {
    ArgumentNullException.ThrowIfNull(assemblyStream);

    using PEReader peReader = new(assemblyStream, PEStreamOptions.LeaveOpen);
    MetadataReader reader = peReader.GetMetadataReader();
    string configuration = string.Empty;
    int debuggableFlags = 0;

    foreach (CustomAttributeHandle handle in reader.GetAssemblyDefinition().GetCustomAttributes())
    {
      CustomAttribute attribute = reader.GetCustomAttribute(handle);
      string? typeName = GetAttributeTypeName(reader, attribute);
      if (typeName is null)
      {
        continue;
      }

      BlobReader blob = reader.GetBlobReader(attribute.Value);
      if (typeName == "System.Reflection.AssemblyConfigurationAttribute")
      {
        blob.ReadUInt16(); // prolog
        configuration = blob.ReadSerializedString() ?? string.Empty;
      }
      else if (typeName == "System.Diagnostics.DebuggableAttribute")
      {
        blob.ReadUInt16(); // prolog
        if (blob.RemainingBytes >= 6)
        {
          // DebuggableAttribute(DebuggingModes) -- Int32 flags.
          debuggableFlags = blob.ReadInt32();
        }
        else
        {
          // Legacy DebuggableAttribute(bool isJITTrackingEnabled, bool isJITOptimizerDisabled).
          bool tracking = blob.ReadBoolean();
          bool optimizerDisabled = blob.ReadBoolean();
          debuggableFlags = (tracking ? 1 : 0) | (optimizerDisabled ? DisableOptimizationsFlag : 0);
        }
      }
    }

    return new AssemblyBuildInfo(configuration, debuggableFlags, (debuggableFlags & DisableOptimizationsFlag) == 0);
  }

  private static string? GetAttributeTypeName(MetadataReader reader, CustomAttribute attribute)
  {
    EntityHandle parent;
    if (attribute.Constructor.Kind == HandleKind.MemberReference)
    {
      parent = reader.GetMemberReference((MemberReferenceHandle)attribute.Constructor).Parent;
    }
    else if (attribute.Constructor.Kind == HandleKind.MethodDefinition)
    {
      parent = reader.GetMethodDefinition((MethodDefinitionHandle)attribute.Constructor).GetDeclaringType();
    }
    else
    {
      return null;
    }

    if (parent.Kind == HandleKind.TypeReference)
    {
      TypeReference typeRef = reader.GetTypeReference((TypeReferenceHandle)parent);
      return $"{reader.GetString(typeRef.Namespace)}.{reader.GetString(typeRef.Name)}";
    }

    if (parent.Kind == HandleKind.TypeDefinition)
    {
      TypeDefinition typeDef = reader.GetTypeDefinition((TypeDefinitionHandle)parent);
      return $"{reader.GetString(typeDef.Namespace)}.{reader.GetString(typeDef.Name)}";
    }

    return null;
  }

  /// <summary>
  /// Resolves the published nupkg entry path for a Pack Include + PackagePath
  /// pair (kanban 462). PackagePath that already names a file is used as-is;
  /// directory PackagePaths append the Include file name. TFM segments like
  /// net10.0 are treated as directories, not file names.
  /// </summary>
  public static string ResolvePublishedEntry(string include, string packagePath)
  {
    ArgumentException.ThrowIfNullOrWhiteSpace(include);
    ArgumentException.ThrowIfNullOrWhiteSpace(packagePath);

    string normalizedPath = packagePath.Replace('\\', '/').TrimEnd('/');
    string fileName = Path.GetFileName(include.Replace('\\', '/'));
    if (string.IsNullOrEmpty(fileName))
    {
      throw new ArgumentException("Include must end with a file name.", nameof(include));
    }

    string lastSegment = normalizedPath.Split('/').Last();
    if (PackagePathNamesFile(lastSegment))
    {
      return normalizedPath;
    }

    return $"{normalizedPath}/{fileName}";
  }

  private static bool PackagePathNamesFile(string lastSegment) =>
    lastSegment.Contains('.', StringComparison.Ordinal)
    && !TfmSegmentPattern.IsMatch(lastSegment);
}
