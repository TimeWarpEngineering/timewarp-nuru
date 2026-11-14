#region Purpose
// Assembly attribute that stops the invoker generator from emitting GeneratedRouteInvokers for library
// assemblies.
#endregion

#region Design
// Opt-out for a library assembly that contains Map() calls but must not emit its own generated
// invokers: a consuming app's generated code serves them at runtime. Without it, both assemblies
// would define a type with the same fully qualified name (CS0436). The assembly attribute is
// public so other library assemblies can use the same opt-out.
#endregion

#region Open Questions
// Q1 (2026-11-14, claude): Nothing under timewarp-nuru-analyzers reads this attribute, and no
//     generator named NuruInvokerGenerator exists in source. The only use is
//     [assembly: SuppressNuruInvokerGeneration] in global-usings.cs. Is it a leftover from the
//     retired invoker generator that can be deleted, or does a current generator still honor it?
#endregion

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
