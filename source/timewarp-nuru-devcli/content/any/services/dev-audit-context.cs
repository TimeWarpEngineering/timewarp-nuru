#region Purpose
// What one `dev audit` run hands each check and the pass cache (kanban task 601): the repo
// root and the route's --force and --verbose options.
#endregion
#region Design
// A record, not loose parameters, so a later option reaches every check without another
// IDevAuditCheck signature change. AuditCommand builds it from the route; an in-process
// caller (a repo's PR workflow running a named subset) builds its own.
#endregion

namespace DevCli;

/// <summary>
/// The inputs of one <c>dev audit</c> run, passed to every <see cref="IDevAuditCheck"/>
/// and to the <see cref="IDevAuditPassCache"/>.
/// </summary>
/// <param name="RepoRoot">The repository root the checks read.</param>
/// <param name="Force">
/// <c>--force</c>: run every check even when the pass cache says these inputs already passed.
/// </param>
/// <param name="Verbose"><c>--verbose</c> / <c>-v</c>: a check may print more detail.</param>
internal sealed record DevAuditContext(string RepoRoot, bool Force = false, bool Verbose = false);
