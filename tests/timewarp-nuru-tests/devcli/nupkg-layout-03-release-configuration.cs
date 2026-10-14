#!/usr/bin/env -S dotnet --

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.DevCli
{

using System.IO.Compression;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using global::DevCli;

/// <summary>
/// Release-configuration gate (task 503): published betas up to beta.80 shipped
/// Debug assemblies. The helper must read AssemblyConfiguration and Debuggable
/// flags from a packed assembly. Fixtures are emitted in memory so the test
/// does not depend on how the repo was last built.
/// </summary>
[TestTag("DevCli")]
public class NupkgLayoutReleaseConfigurationTests
{
  private const string EntryName = "lib/net10.0/TimeWarp.Nuru.dll";

  [ModuleInitializer]
  internal static void Register() => RegisterTests<NupkgLayoutReleaseConfigurationTests>();

  // Hand-built with MetadataBuilder: the multi-mode test assembly is AOT-compatible,
  // so Reflection.Emit (PersistedAssemblyBuilder) is unavailable.
  private static byte[] EmitAssembly(string configuration, int debuggableFlags)
  {
    MetadataBuilder metadata = new();
    metadata.AddModule(0, metadata.GetOrAddString("ConfigFixture.dll"), metadata.GetOrAddGuid(Guid.NewGuid()), default, default);
    AssemblyDefinitionHandle assemblyHandle = metadata.AddAssembly(
      metadata.GetOrAddString("ConfigFixture"), new Version(1, 0, 0, 0), default, default, 0, AssemblyHashAlgorithm.None);
    AssemblyReferenceHandle runtimeRef = metadata.AddAssemblyReference(
      metadata.GetOrAddString("System.Runtime"), new Version(10, 0, 0, 0), default, default, 0, default);

    metadata.AddTypeDefinition(
      default, default, metadata.GetOrAddString("<Module>"), default,
      MetadataTokens.FieldDefinitionHandle(1), MetadataTokens.MethodDefinitionHandle(1));

    TypeReferenceHandle configType = metadata.AddTypeReference(
      runtimeRef, metadata.GetOrAddString("System.Reflection"), metadata.GetOrAddString("AssemblyConfigurationAttribute"));
    TypeReferenceHandle debuggableType = metadata.AddTypeReference(
      runtimeRef, metadata.GetOrAddString("System.Diagnostics"), metadata.GetOrAddString("DebuggableAttribute"));

    BlobBuilder stringCtorSig = new();
    new BlobEncoder(stringCtorSig).MethodSignature(isInstanceMethod: true)
      .Parameters(1, returnType => returnType.Void(), parameters => parameters.AddParameter().Type().String());
    BlobBuilder intCtorSig = new();
    new BlobEncoder(intCtorSig).MethodSignature(isInstanceMethod: true)
      .Parameters(1, returnType => returnType.Void(), parameters => parameters.AddParameter().Type().Int32());

    MemberReferenceHandle configCtor = metadata.AddMemberReference(configType, metadata.GetOrAddString(".ctor"), metadata.GetOrAddBlob(stringCtorSig));
    MemberReferenceHandle debuggableCtor = metadata.AddMemberReference(debuggableType, metadata.GetOrAddString(".ctor"), metadata.GetOrAddBlob(intCtorSig));

    BlobBuilder configBlob = new();
    configBlob.WriteUInt16(1);
    configBlob.WriteSerializedString(configuration);
    configBlob.WriteUInt16(0);
    metadata.AddCustomAttribute(assemblyHandle, configCtor, metadata.GetOrAddBlob(configBlob));

    BlobBuilder debuggableBlob = new();
    debuggableBlob.WriteUInt16(1);
    debuggableBlob.WriteInt32(debuggableFlags);
    debuggableBlob.WriteUInt16(0);
    metadata.AddCustomAttribute(assemblyHandle, debuggableCtor, metadata.GetOrAddBlob(debuggableBlob));

    ManagedPEBuilder peBuilder = new(
      PEHeaderBuilder.CreateLibraryHeader(), new MetadataRootBuilder(metadata), new BlobBuilder());
    BlobBuilder peBlob = new();
    peBuilder.Serialize(peBlob);
    return peBlob.ToArray();
  }

  private static string MakeNupkg(byte[] assemblyBytes)
  {
    string dir = Directory.CreateTempSubdirectory("nupkg-config-test-").FullName;
    string path = Path.Combine(dir, "test.nupkg");
    using (ZipArchive zip = ZipFile.Open(path, ZipArchiveMode.Create))
    {
      ZipArchiveEntry entry = zip.CreateEntry(EntryName);
      using Stream entryStream = entry.Open();
      entryStream.Write(assemblyBytes);
    }

    return path;
  }

  public static async Task Release_assembly_is_reported_optimized()
  {
    string nupkg = MakeNupkg(EmitAssembly("Release", 0x2));
    try
    {
      AssemblyBuildInfo info = NupkgLayoutCheck.ReadPackedAssemblyBuildInfo(nupkg, EntryName);
      info.Configuration.ShouldBe("Release");
      info.DebuggableFlags.ShouldBe(0x2);
      info.IsOptimized.ShouldBeTrue();
    }
    finally { Directory.Delete(Path.GetDirectoryName(nupkg)!, recursive: true); }

    await Task.CompletedTask;
  }

  public static async Task Debug_assembly_is_reported_unoptimized()
  {
    string nupkg = MakeNupkg(EmitAssembly("Debug", 0x107));
    try
    {
      AssemblyBuildInfo info = NupkgLayoutCheck.ReadPackedAssemblyBuildInfo(nupkg, EntryName);
      info.Configuration.ShouldBe("Debug");
      info.DebuggableFlags.ShouldBe(0x107);
      info.IsOptimized.ShouldBeFalse();
    }
    finally { Directory.Delete(Path.GetDirectoryName(nupkg)!, recursive: true); }

    await Task.CompletedTask;
  }

  public static async Task Missing_entry_throws()
  {
    string nupkg = MakeNupkg(EmitAssembly("Release", 0));
    try
    {
      Should.Throw<FileNotFoundException>(() => NupkgLayoutCheck.ReadPackedAssemblyBuildInfo(nupkg, "lib/net10.0/Other.dll"));
    }
    finally { Directory.Delete(Path.GetDirectoryName(nupkg)!, recursive: true); }

    await Task.CompletedTask;
  }
}

} // namespace TimeWarp.Nuru.Tests.DevCli
