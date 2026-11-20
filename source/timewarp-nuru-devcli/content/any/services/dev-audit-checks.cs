#region Purpose
// The registry `dev audit` reads its checks from (kanban task 600).
#endregion
#region Design
// A partial method, not DI or reflection. DevCli is a source-only package, so this
// class compiles into the consuming dev CLI's own assembly. A repo with checks adds,
// in its tools/dev-cli:
//
//   internal static partial class DevAuditChecks
//   {
//     static partial void AddRepoChecks(List<IDevAuditCheck> checks) => checks.Add(new MyCheck());
//   }
//
// A repo without checks writes nothing and the call compiles away. This is fixed at
// compile time, so it works under AOT and trimming. Nuru's source-generated DI does not
// resolve IEnumerable<T> parameters, which ruled out a DI multi-registration.
#endregion

namespace DevCli;

/// <summary>
/// The repo's <see cref="IDevAuditCheck"/> list, in the order the repo adds them.
/// </summary>
internal static partial class DevAuditChecks
{
  /// <summary>Returns every check the repo registered in <c>AddRepoChecks</c>.</summary>
  public static IReadOnlyList<IDevAuditCheck> All()
  {
    List<IDevAuditCheck> checks = [];
    AddRepoChecks(checks);
    return checks;
  }

  /// <summary>Implemented by the repo's own tools/dev-cli to add its checks in run order.</summary>
  static partial void AddRepoChecks(List<IDevAuditCheck> checks);
}
