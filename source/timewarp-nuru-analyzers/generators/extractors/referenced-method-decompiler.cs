#region Purpose
// Decompile referenced AddX method bodies from the implementation DLL (lib/), cached by (MVID, token).
#endregion

#region Design
// Roslyn compilations bind against NuGet ref/ stubs whose bodies are throw-null. A "successful"
// decompile of that is garbage, so we resolve lib/ before ILSpy runs. No real body, metadata-only,
// or throw-null IL → treat as cannot-decompile (NURU052). Cache is process-wide for agent rebuild loops.
// File I/O is required to open PE images of compilation references; that is the point of this type.
// lib/ fallback ranks TFMs by closeness to the compile stub (then compilation TFM), not path order.
#endregion

#pragma warning disable RS1035 // Analyzer may not use banned APIs — PE open / lib-vs-ref resolution
#pragma warning disable RS1036 // Analyzers should not do file I/O — same reason
#pragma warning disable CA1031 // Fail-closed decompile must not throw into the generator host

namespace TimeWarp.Nuru.Generators;

using System.Collections.Concurrent;
using System.Globalization;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.CSharp;
using ICSharpCode.Decompiler.Metadata;

/// <summary>
/// Decompiles a referenced method's implementation body for registration-script lowering.
/// </summary>
internal static class ReferencedMethodDecompiler
{
  private static readonly ConcurrentDictionary<(Guid Mvid, int Token), CachedDecompile> Cache = new();
  private static readonly object DecompileGate = new();

  /// <summary>
  /// Result of attempting to decompile a method definition.
  /// </summary>
  /// <param name="Success">True when a real C# body was produced.</param>
  /// <param name="Source">Decompiled method source, including signature, when <paramref name="Success"/> is true.</param>
  internal sealed record CachedDecompile(bool Success, string? Source);

  /// <summary>
  /// Decompiles <paramref name="methodSymbol"/> from the implementation assembly.
  /// </summary>
  internal static CachedDecompile Decompile(IMethodSymbol methodSymbol, Compilation compilation)
  {
    ArgumentNullException.ThrowIfNull(methodSymbol);
    ArgumentNullException.ThrowIfNull(compilation);

    IMethodSymbol definition = (methodSymbol.ReducedFrom ?? methodSymbol).OriginalDefinition;
    int token = definition.MetadataToken;
    if (token == 0)
      return new CachedDecompile(false, null);

    string? implementationPath = ResolveImplementationAssemblyPath(compilation, definition.ContainingAssembly, token);
    if (implementationPath is null)
      return new CachedDecompile(false, null);

    Guid? mvid = TryReadMvid(implementationPath);
    if (mvid is null)
      return new CachedDecompile(false, null);

    (Guid Mvid, int Token) cacheKey = (mvid.Value, token);
    if (Cache.TryGetValue(cacheKey, out CachedDecompile? cached))
      return cached;

    CachedDecompile produced = DecompileUncached(implementationPath, token, compilation);
    return Cache.GetOrAdd(cacheKey, produced);
  }

  private static CachedDecompile DecompileUncached(string implementationPath, int token, Compilation compilation)
  {
    if (HasNoRealBody(implementationPath, token))
      return new CachedDecompile(false, null);

    lock (DecompileGate)
    {
      try
      {
        UniversalAssemblyResolver resolver = new(
          implementationPath,
          throwOnError: false,
          targetFramework: null);

        foreach (PortableExecutableReference peReference in compilation.References.OfType<PortableExecutableReference>())
        {
          if (string.IsNullOrEmpty(peReference.FilePath))
            continue;

          string? directory = Path.GetDirectoryName(peReference.FilePath);
          if (directory is not null)
            resolver.AddSearchDirectory(directory);
        }

        DecompilerSettings decompilerSettings = new()
        {
          ThrowOnAssemblyResolveErrors = false,
          UsingDeclarations = false,
          ShowXmlDocumentation = false,
          UseDebugSymbols = false
        };

        CSharpDecompiler decompiler = new(implementationPath, resolver, decompilerSettings);
        EntityHandle entityHandle = MetadataTokens.EntityHandle(token);
        string source = decompiler.DecompileAsString(entityHandle);
        if (string.IsNullOrWhiteSpace(source) || LooksLikeStubBody(source))
          return new CachedDecompile(false, null);

        return new CachedDecompile(true, source);
      }
      catch (Exception)
      {
        return new CachedDecompile(false, null);
      }
    }
  }

  /// <summary>
  /// Prefers the NuGet <c>lib/</c> implementation over a <c>ref/</c> compile stub.
  /// </summary>
  internal static string? ResolveImplementationAssemblyPath(Compilation compilation, IAssemblySymbol assemblySymbol, int methodToken)
  {
    MetadataReference? metadataReference = compilation.GetMetadataReference(assemblySymbol);
    if (metadataReference is not PortableExecutableReference peReference || string.IsNullOrEmpty(peReference.FilePath))
      return null;

    string path = peReference.FilePath;
    if (IsReferenceAssemblyPath(path))
      return FindLibSibling(path, compilation, methodToken);

    if (HasReferenceAssemblyAttribute(path))
      return FindLibSibling(path, compilation, methodToken);

    return path;
  }

  private static bool IsReferenceAssemblyPath(string path)
  {
    string normalized = path.Replace('\\', '/');
    return normalized.Contains("/ref/", StringComparison.OrdinalIgnoreCase);
  }

  private static string? FindLibSibling(string referencePath, Compilation compilation, int methodToken)
  {
    string? tfmDirectory = Path.GetDirectoryName(referencePath);
    string? refDirectory = tfmDirectory is null ? null : Path.GetDirectoryName(tfmDirectory);
    string? packageRoot = refDirectory is null ? null : Path.GetDirectoryName(refDirectory);
    if (tfmDirectory is null || refDirectory is null || packageRoot is null)
      return null;

    if (!string.Equals(Path.GetFileName(refDirectory), "ref", StringComparison.OrdinalIgnoreCase))
      return null;

    string fileName = Path.GetFileName(referencePath);
    string libRoot = Path.Combine(packageRoot, "lib");
    if (!Directory.Exists(libRoot))
      return null;

    string stubTfm = Path.GetFileName(tfmDirectory);
    string sameTfm = Path.Combine(libRoot, stubTfm, fileName);
    if (File.Exists(sameTfm) && !HasNoRealBody(sameTfm, methodToken))
      return sameTfm;

    string[] matches = Directory.GetFiles(libRoot, fileName, SearchOption.AllDirectories);
    if (matches.Length == 0)
      return null;

    string? compilationTfm = GetCompilationTargetFrameworkMoniker(compilation);
    string targetTfm = TryParseTfm(stubTfm) is not null
      ? stubTfm
      : compilationTfm ?? stubTfm;

    foreach (string candidate in matches.OrderBy(path => RankLibCandidate(path, targetTfm)))
    {
      if (string.Equals(candidate, sameTfm, StringComparison.OrdinalIgnoreCase))
        continue;

      if (!HasNoRealBody(candidate, methodToken))
        return candidate;
    }

    return null;
  }

  private static int RankLibCandidate(string libraryPath, string targetTfm)
  {
    string? folder = Path.GetDirectoryName(libraryPath);
    string candidateTfm = folder is null ? string.Empty : Path.GetFileName(folder);
    ParsedTfm? target = TryParseTfm(targetTfm);
    ParsedTfm? candidate = TryParseTfm(candidateTfm);
    if (target is null || candidate is null)
      return int.MaxValue;

    int targetRank = FrameworkRank(target.Value);
    int candidateRank = FrameworkRank(candidate.Value);
    if (candidateRank <= targetRank)
      return targetRank - candidateRank;

    // Newer than the compile stub: last resort, still nearest-higher rather than path order.
    return 10_000 + (candidateRank - targetRank);
  }

  private static int FrameworkRank(ParsedTfm tfm)
  {
    return tfm.Family switch
    {
      "netstandard" => (tfm.Major * 100) + tfm.Minor,
      "netcoreapp" => 1000 + (tfm.Major * 100) + tfm.Minor,
      "net" => 2000 + (tfm.Major * 100) + tfm.Minor,
      "netfx" => 500 + (tfm.Major * 100) + tfm.Minor,
      _ => 0
    };
  }

  private static ParsedTfm? TryParseTfm(string folderName)
  {
    if (string.IsNullOrEmpty(folderName))
      return null;

    string name = folderName;
    int dash = name.IndexOf('-', StringComparison.Ordinal);
    if (dash >= 0)
      name = name[..dash];

    if (name.StartsWith("netstandard", StringComparison.OrdinalIgnoreCase))
      return ParseDottedVersion("netstandard", name["netstandard".Length..]);

    if (name.StartsWith("netcoreapp", StringComparison.OrdinalIgnoreCase))
      return ParseDottedVersion("netcoreapp", name["netcoreapp".Length..]);

    if (name.StartsWith("net", StringComparison.OrdinalIgnoreCase) && name.Length > 3 && char.IsDigit(name[3]))
    {
      string rest = name[3..];
      if (rest.Contains('.', StringComparison.Ordinal))
        return ParseDottedVersion("net", rest);

      return ParseNetFrameworkVersion(rest);
    }

    return null;
  }

  private static ParsedTfm? ParseDottedVersion(string family, string versionText)
  {
    string[] parts = versionText.Split('.');
    if (parts.Length < 1
        || !int.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out int major))
    {
      return null;
    }

    int minor = 0;
    if (parts.Length > 1)
      _ = int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out minor);

    return new ParsedTfm(family, major, minor);
  }

  private static ParsedTfm? ParseNetFrameworkVersion(string compact)
  {
    // net462 → 4.6.2 (major=4, minor=62 packed as 6*10+2 for ranking)
    if (compact.Length < 2
        || !int.TryParse(compact, NumberStyles.Integer, CultureInfo.InvariantCulture, out int packed))
    {
      return null;
    }

    int major = packed / 100;
    int minor = packed % 100;
    if (major == 0 && compact.Length >= 2)
    {
      major = packed / 10;
      minor = packed % 10;
    }

    return new ParsedTfm("netfx", major, minor);
  }

  private static string? GetCompilationTargetFrameworkMoniker(Compilation compilation)
  {
    foreach (AttributeData attribute in compilation.Assembly.GetAttributes())
    {
      if (attribute.AttributeClass?.Name != "TargetFrameworkAttribute")
        continue;

      if (attribute.ConstructorArguments.Length == 0)
        continue;

      if (attribute.ConstructorArguments[0].Value is not string frameworkName)
        continue;

      return FrameworkDisplayNameToTfm(frameworkName);
    }

    return null;
  }

  private static string? FrameworkDisplayNameToTfm(string frameworkName)
  {
    const string NetCorePrefix = ".NETCoreApp,Version=v";
    const string NetStandardPrefix = ".NETStandard,Version=v";
    const string NetFrameworkPrefix = ".NETFramework,Version=v";

    if (frameworkName.StartsWith(NetCorePrefix, StringComparison.OrdinalIgnoreCase))
    {
      string version = frameworkName[NetCorePrefix.Length..];
      string parseText = version.Contains('.', StringComparison.Ordinal) ? version : version + ".0";
      if (Version.TryParse(parseText, out Version? parsed) && parsed.Major >= 5)
        return "net" + version;

      return "netcoreapp" + version;
    }

    if (frameworkName.StartsWith(NetStandardPrefix, StringComparison.OrdinalIgnoreCase))
      return "netstandard" + frameworkName[NetStandardPrefix.Length..];

    if (frameworkName.StartsWith(NetFrameworkPrefix, StringComparison.OrdinalIgnoreCase))
      return "net" + frameworkName[NetFrameworkPrefix.Length..].Replace(".", "", StringComparison.Ordinal);

    return null;
  }

  private readonly record struct ParsedTfm(string Family, int Major, int Minor);

  private static bool HasReferenceAssemblyAttribute(string path)
  {
    try
    {
      using FileStream fileStream = new(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
      using PEReader peReader = new(fileStream, PEStreamOptions.PrefetchMetadata);
      if (!peReader.HasMetadata)
        return false;

      MetadataReader metadataReader = peReader.GetMetadataReader();
      foreach (CustomAttributeHandle customAttributeHandle in metadataReader.CustomAttributes)
      {
        CustomAttribute customAttribute = metadataReader.GetCustomAttribute(customAttributeHandle);
        if (customAttribute.Constructor.Kind is not (HandleKind.MemberReference or HandleKind.MethodDefinition))
          continue;

        string name = GetAttributeTypeName(metadataReader, customAttribute.Constructor);
        if (string.Equals(name, "ReferenceAssemblyAttribute", StringComparison.Ordinal))
          return true;
      }

      return false;
    }
    catch (Exception)
    {
      return false;
    }
  }

  private static string GetAttributeTypeName(MetadataReader metadataReader, EntityHandle constructor)
  {
    EntityHandle typeHandle = constructor.Kind switch
    {
      HandleKind.MethodDefinition => metadataReader.GetMethodDefinition((MethodDefinitionHandle)constructor).GetDeclaringType(),
      HandleKind.MemberReference => metadataReader.GetMemberReference((MemberReferenceHandle)constructor).Parent,
      _ => default
    };

    if (typeHandle.Kind == HandleKind.TypeReference)
    {
      TypeReference typeReference = metadataReader.GetTypeReference((TypeReferenceHandle)typeHandle);
      return metadataReader.GetString(typeReference.Name);
    }

    if (typeHandle.Kind == HandleKind.TypeDefinition)
    {
      TypeDefinition typeDefinition = metadataReader.GetTypeDefinition((TypeDefinitionHandle)typeHandle);
      return metadataReader.GetString(typeDefinition.Name);
    }

    return string.Empty;
  }

  private static Guid? TryReadMvid(string path)
  {
    try
    {
      using FileStream fileStream = new(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
      using PEReader peReader = new(fileStream, PEStreamOptions.PrefetchMetadata);
      if (!peReader.HasMetadata)
        return null;

      MetadataReader metadataReader = peReader.GetMetadataReader();
      return metadataReader.GetGuid(metadataReader.GetModuleDefinition().Mvid);
    }
    catch (Exception)
    {
      return null;
    }
  }

  private static bool HasNoRealBody(string path, int token)
  {
    try
    {
      using FileStream fileStream = new(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
      using PEReader peReader = new(fileStream, PEStreamOptions.PrefetchEntireImage);
      if (!peReader.HasMetadata)
        return true;

      MetadataReader metadataReader = peReader.GetMetadataReader();
      EntityHandle entityHandle = MetadataTokens.EntityHandle(token);
      if (entityHandle.Kind != HandleKind.MethodDefinition)
        return true;

      MethodDefinition methodDefinition = metadataReader.GetMethodDefinition((MethodDefinitionHandle)entityHandle);
      int relativeVirtualAddress = methodDefinition.RelativeVirtualAddress;
      if (relativeVirtualAddress == 0)
        return true;

      MethodBodyBlock methodBodyBlock = peReader.GetMethodBody(relativeVirtualAddress);
      BlobReader ilReader = methodBodyBlock.GetILReader();
      int length = ilReader.RemainingBytes;
      if (length == 0)
        return true;

      // ret
      if (length == 1 && ilReader.ReadByte() == 0x2A)
        return true;

      // ldnull; throw  (typical ref-assembly stub)
      if (length == 2)
      {
        byte first = ilReader.ReadByte();
        byte second = ilReader.ReadByte();
        if (first == 0x14 && second == 0x7A)
          return true;
      }

      return false;
    }
    catch (Exception)
    {
      return true;
    }
  }

  private static bool LooksLikeStubBody(string source)
  {
    string trimmed = source.Replace("\r", "", StringComparison.Ordinal);
    return trimmed.Contains("throw null;", StringComparison.Ordinal)
        || trimmed.Contains("throw new System.NotSupportedException", StringComparison.Ordinal)
        || trimmed.Contains("throw new global::System.NotSupportedException", StringComparison.Ordinal);
  }
}
