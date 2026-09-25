#region Purpose
// Single acceptance list of built-in route type-constraint names.
#endregion

#region Design
// InvalidTypeConstraintError and NURU_P004 both print this list. A name added here is
// accepted by the parser and shown in both error texts, so the two cannot drift.
// Matching is ordinal and exact: "uri" and "Uri" are both accepted; "URI" is not.
#endregion

namespace TimeWarp.Nuru;

/// <summary>
/// Built-in type-constraint names the parser accepts, in display order.
/// </summary>
internal static class BuiltInTypeNames
{
  internal static IReadOnlyList<string> Names { get; } =
  [
    "string",
    "int",
    "byte",
    "sbyte",
    "short",
    "ushort",
    "uint",
    "ulong",
    "long",
    "float",
    "double",
    "decimal",
    "bool",
    "char",
    "DateTime",
    "Guid",
    "TimeSpan",
    "uri",
    "Uri",
    "fileinfo",
    "FileInfo",
    "directoryinfo",
    "DirectoryInfo",
    "ipaddress",
    "IPAddress",
    "dateonly",
    "DateOnly",
    "timeonly",
    "TimeOnly"
  ];

  private static readonly HashSet<string> NameSet = new(Names, StringComparer.Ordinal);

  internal static bool Contains(string type) => NameSet.Contains(type);

  internal static string SupportedList { get; } = string.Join(", ", Names);
}
