#region Purpose
// Enum whose names match Roslynk's multi_query tool enum, for a --json-args binding probe.
#endregion

namespace RoslynkNuru.Binding;

/// <summary>
/// A stand-in for <c>Morris.Roslynk.Features.MultiQuery.MultiQueryOp</c>.
/// </summary>
public enum ProbeOp
{
  get_symbol,
  find_references,
  get_diagnostics
}
