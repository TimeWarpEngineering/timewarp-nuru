#region Purpose
// The registry `dev audit` reads its checks and its optional pass cache from (kanban
// tasks 600 and 601), plus the named-subset selection an in-process caller uses.
#endregion
#region Design
// Partial methods, not DI or reflection. DevCli is a source-only package, so this
// class compiles into the consuming dev CLI's own assembly. A repo with checks adds,
// in its tools/dev-cli:
//
//   internal static partial class DevAuditChecks
//   {
//     static partial void AddRepoChecks(List<IDevAuditCheck> checks) => checks.Add(new MyCheck());
//     static partial void ConfigurePassCache(ref IDevAuditPassCache? passCache) => passCache = DevAuditTreeStamp.Create();
//   }
//
// A repo without checks writes nothing and the calls compile away. A repo that skips
// ConfigurePassCache gets no pass cache: every run runs every check. This is fixed at
// compile time, so it works under AOT and trimming. Nuru's source-generated DI does not
// resolve IEnumerable<T> parameters, which ruled out a DI multi-registration.
//
// Named(...) picks checks by Name for a run that does not go through the route (ganda's PR
// workflow runs only its deps check, task 601). It keeps registry order, and an unknown
// name throws: a typo there is the caller's bug, and silently running fewer checks would
// hide it.
#endregion

namespace DevCli;

/// <summary>
/// The repo's <see cref="IDevAuditCheck"/> list, in the order the repo adds them, and its
/// optional <see cref="IDevAuditPassCache"/>.
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

  /// <summary>
  /// Returns the registered checks whose <see cref="IDevAuditCheck.Name"/> is in
  /// <paramref name="names"/>, in registry order. Run them with
  /// <see cref="DevAuditRunner"/> and no pass cache.
  /// </summary>
  /// <exception cref="ArgumentException">A name matches no registered check.</exception>
  public static IReadOnlyList<IDevAuditCheck> Named(params IEnumerable<string> names) => Select(All(), names);

  /// <summary>
  /// The checks in <paramref name="checks"/> named in <paramref name="names"/>, in the order of
  /// <paramref name="checks"/>. Names compare ordinally.
  /// </summary>
  /// <exception cref="ArgumentException">A name matches none of <paramref name="checks"/>.</exception>
  public static IReadOnlyList<IDevAuditCheck> Select(IReadOnlyList<IDevAuditCheck> checks, IEnumerable<string> names)
  {
    ArgumentNullException.ThrowIfNull(checks);
    ArgumentNullException.ThrowIfNull(names);

    HashSet<string> wanted = new(names, StringComparer.Ordinal);
    HashSet<string> known = new(checks.Select(check => check.Name), StringComparer.Ordinal);
    List<string> unknown = [.. wanted.Where(name => !known.Contains(name)).Order(StringComparer.Ordinal)];
    if (unknown.Count > 0)
    {
      string registered = known.Count == 0 ? "none" : string.Join(", ", known.Order(StringComparer.Ordinal));
      throw new ArgumentException(
        $"dev audit has no check named {string.Join(", ", unknown)}; registered: {registered}",
        nameof(names));
    }

    return [.. checks.Where(check => wanted.Contains(check.Name))];
  }

  /// <summary>
  /// Returns the pass cache the repo set in <c>ConfigurePassCache</c>, or <see langword="null"/>
  /// when it set none.
  /// </summary>
  public static IDevAuditPassCache? PassCache()
  {
    IDevAuditPassCache? passCache = null;
    ConfigurePassCache(ref passCache);
    return passCache;
  }

  /// <summary>Implemented by the repo's own tools/dev-cli to add its checks in run order.</summary>
  static partial void AddRepoChecks(List<IDevAuditCheck> checks);

  /// <summary>
  /// Implemented by the repo's own tools/dev-cli to turn on a pass cache, usually
  /// <c>passCache = DevAuditTreeStamp.Create();</c>.
  /// </summary>
  static partial void ConfigurePassCache(ref IDevAuditPassCache? passCache);
}
