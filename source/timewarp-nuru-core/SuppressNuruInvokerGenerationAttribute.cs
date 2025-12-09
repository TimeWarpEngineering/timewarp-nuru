namespace TimeWarp.Nuru;

/// <summary>
/// When applied to an assembly, suppresses the NuruInvokerGenerator source generator
/// from generating the GeneratedRouteInvokers class for that assembly.
/// </summary>
/// <remarks>
/// <para>
/// This attribute is used for library assemblies that contain Map() calls but should not
/// generate their own invokers. Instead, the consuming application's generated invokers
/// will be used at runtime.
/// </para>
/// <para>
/// This prevents CS0436 conflicts when a library assembly (like TimeWarp.Nuru) contains
/// Map() calls and a consuming application also has Map() calls - both would generate
/// a GeneratedRouteInvokers class with the same fully-qualified name.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // In AssemblyInfo.cs or any .cs file in the library
/// [assembly: SuppressNuruInvokerGeneration]
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false)]
public sealed class SuppressNuruInvokerGenerationAttribute : Attribute
{
}
