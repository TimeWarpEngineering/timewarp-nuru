# TimeWarp org audit — repos, deviations, and the enforcement architecture

Audited 2026-08-07 against the live GitHub API: all **65 non-fork, non-archived**
repos in TimeWarpEngineering (of 144 total; forks and archived excluded). Raw data
collected per repo: version SSOT location and value, workflow triggers, NuGet push
mechanism, DevCli usage, `.timewarp/dev.jsonc`, branch protection, latest tag.

## Org-level facts that constrain the design

1. **The org is on the GitHub Free plan** (API: `plan.name: "free"`). On Free,
   branch protection is **not enforceable on private repos** — 46 of 144 repos are
   private, including publishers (ganda, health, [redacted-private-repo]).
   Upgrading to Team (~$4/user/mo) would enable it; nothing below requires that.
2. **Zero repos in the org have required status checks.** Only 4 have any branch
   protection at all (nuru, architecture, state, terminal: PR required; mediator:
   protection object exists, no PR requirement). All CI everywhere is advisory.
3. **File-sync as a consistency mechanism was already tried and abandoned** — the
   `sync-configurable-files` workflow exists `.disabled` in timewarp-state and
   timewarp-quickbooks. Copying files N times produced N divergent copies. Any new
   convention that relies on copying files into every repo will fail the same way.

## The enforcement architecture (the decision)

Consistency must be **structural, not copied**. Three layers:

**Layer 1 — one reusable workflow.** `TimeWarpEngineering/.github` (public) hosts a
`workflow_call` reusable workflow (`timewarp-ci.yml`). Every repo's own workflow
file becomes a fixed ~10-line caller. Private repos CAN call reusable workflows
from public repos on the Free plan. Changing CI behavior org-wide = one edit in one
repo, effective everywhere instantly (reference `@master` — confirmed by operator
2026-08-08 over version pinning). This is what kills the "address it every day"
problem: there is nothing per-repo left to drift except the tiny caller, which
Layer 3 audits.

**Layer 2 — DevCli owns all pipeline logic; the release gate is the enforcement
point.** The reusable workflow only does checkout / setup-dotnet / OIDC login /
`dev workflow`. Mode matrix, gates, pack, push, promotion live in
TimeWarp.Nuru.DevCli content (NuGet-distributed, already consumed by 14 repos).
Because Free-plan private repos cannot have GitHub-enforced merge gates, the gate
that matters runs **inside release mode**, identically on every repo, any plan:

- tag == `v{<Version>}` from `source/Directory.Build.props`;
- tag commit is an ancestor of master;
- the commit's `ci` check-run concluded success (queried via the workflow's own
  `GITHUB_TOKEN` — works on private repos, no plan dependency);
- version not already fully published (partial → resume, per 458-005).

Branch protection with required checks then becomes optional defense-in-depth on
public repos (and on private ones if the org ever moves to Team) — useful, never
load-bearing.

**Layer 3 — drift detection is automated, not manual.** A `dev audit-convention`
check (or `ganda repo audit` extension) validates the repo against the convention
(props SSOT present, caller workflow matches the canonical shape, `v`-prefixed
tags, no rogue release workflows, OIDC not secret keys) and runs in every CI merge
build — a deviating repo fails its own CI. Plus one scheduled org sweep (in the
`.github` repo) that regenerates this deviation table so it never has to be
hand-built again.

**Answer to the paid-plan question:** you do not need to pay to be correct. Team
only adds GitHub-side merge blocking on private repos; publish-time enforcement
(Layer 2) is already plan-independent. Upgrade if you want red PRs physically
unmergeable on private repos; the convention works identically either way.
Decided 2026-08-08 (operator): **stay on Free**.

## Publisher deviation matrix (21 repos with a NuGet publish path)

Convention reference: `convention.md`. "Nuru-shape" = source props SSOT + thin
workflow → `dev workflow` + release-published trigger + OIDC.

| Repo | Vis | Version (props) | Deviations from convention |
|------|-----|-----------------|----------------------------|
| timewarp-nuru | pub | 3.0.0-beta.71 | Reference repo. Own findings F1–F9 (dispatch→release incoherence, untested publish, no tag assertion, triplicated package lists, partial-publish deadlock, docs). PR protection, no required checks. |
| timewarp-amuru | pub | 1.0.0 | Nuru-shape. No `.timewarp/dev.jsonc`; no protection; inherits DevCli F1/F2/F5. |
| timewarp-builder | pub | 1.0.0 | Nuru-shape. jsonc: nuget-search + hardcoded packages (F5); no protection. |
| timewarp-flexbox | pub | 1.0.0 | Same as builder. |
| timewarp-jaribu | pub | 1.0.0-beta.15 | Nuru-shape; no jsonc; no protection |
| timewarp-terminal | pub | 1.0.0 | Nuru-shape; jsonc+pkgs (F5); **tag `1.0.0` missing `v` prefix**; PR protection, no checks. |
| timewarp-components | pub | 1.0.0-beta.2 | Nuru-shape; **tag missing `v` prefix**; no jsonc; no protection. |
| timewarp-heroicons | pub | 2.0.19+2.0.18 | Nuru-shape; version embeds build metadata (`+2.0.18`) — allowed by NuGet (metadata stripped) but nonstandard; **zero git tags** → release-published path apparently never exercised; no jsonc. |
| timewarp-simple-icons | pub | 16.27.1 | Nuru-shape; **zero git tags** (same concern as heroicons); no jsonc. |
| timewarp-source-generators | pub | 1.0.0-beta.10 | Nuru-shape; jsonc+pkgs (F5); no protection |
| timewarp-options-validation | pub | 1.0.0-beta.5 | Nuru-shape; no jsonc; no protection |
| timewarp-architecture | pub | 2.0.0-beta.14 | Nuru-shape **but `checkVersionStrategy: git-tag` → F4 inversion: its next release-published run will abort (tag==props reads as "already released") or pass a mismatched tag**. Documented dual-version coupling with its templates tree (manual sync). Real burned-version incident (task 456). PR protection, no checks. |
| timewarp-ganda | PRI | 1.0.0-beta.23 | Nuru-shape; props one ahead of latest tag (beta.22) — pending release or burn; no jsonc; **private ⇒ no protection possible on Free**. |
| timewarp-health | PRI | 1.0.0-beta.1 | Nuru-shape; no tags yet; private ⇒ no protection possible. |
| [redacted-private-repo] | PRI | 1.0.0-beta.1 | Nuru-shape; no jsonc; no tags yet; private ⇒ no protection possible. |
| timewarp-state | pub | via `$(TimeWarpStateVersion)` variable | **Most-deviant active publisher.** Trigger is `release: types [created]` — **fires on draft releases** (`published` does not); dispatch publishes unconditionally; auth via `PUBLISH_TO_NUGET_ORG` secret key, not OIDC; version SSOT behind a variable indirection + `PackageVersion`; no DevCli; disabled sync-configurable-files workflow still in tree. |
| timewarp-mediator | pub | 13.0.0 (root props) | Legacy MediatR-fork tooling: 7 workflows, secret-key auth, root-level props, no DevCli. |
| timewarp-fixie | pub | 3.1.0+9.0.300 (root props) | Root props; build-metadata version; 3 workflows; no DevCli. |
| timewarp-multiavatar | pub | 1.0.0-beta.13 (root props) | **Publishes on ANY tag push (`tags: ['*']`) and on dispatch — no release event, no gate of any kind.** Root props; no DevCli. |
| timewarp-quickbooks | pub | 1.0.0-beta.3 (root props) | Legacy ci-build/release-build pair; dispatch-driven publish; no DevCli; disabled sync + claude-review workflows in tree. |
| timewarp-build-tasks | pub | **not found in props** | Publishes on release-published but version SSOT could not be located in root or source props (likely csproj) — needs identification; no DevCli; no dispatch. Note: referenced repo-wide by other repos' builds. |

Universal deviations (every repo, including Nuru): no required status checks
anywhere; no tag↔props assertion anywhere; nothing publishes tested bytes
(rebuild-without-retest wherever DevCli release mode is used — findings F1/F2/F5
are **org-wide** because 14 repos share the DevCli pattern; fixing DevCli + the
reusable workflow fixes all of them at once — that is the system working as
intended).

## Trusted publishing roster (as of 2026-08-07)

Policy model (MS docs): one policy per (owner account, repo, workflow file
[, environment]); covers ALL packages owned by the account — per repo, not per
package. No public API to enumerate policies; the automatable check is a
`nuget/login` probe (OIDC exchange succeeds iff an active policy matches) —
see task 458-009.

- **Update 2026-08-08 (operator):** TP policies now exist for **all repos that
  actually publish NuGet packages**. Classification correction: **health and
  [redacted-private-repo] do NOT currently publish** — their OIDC
  workflows predate real publishing; N/A for TP until a first publish (create
  the policy just before it; private-repo policies lapse after 7 days without
  a publish). Ganda: stop-publishing decided (458-010) — its policy should be
  deleted, not maintained.
- Policies alone are inert for the 5 secret-key repos (state, build-tasks,
  mediator, fixie, multiavatar, quickbooks) until their workflows migrate to
  `nuget/login` — that migration remains the open work, followed by
  long-lived key revocation.
- **Cleanup once flipped:** revoke long-lived keys on nuget.org, delete
  `NUGET_API_KEY` / `PUBLISH_TO_NUGET_ORG` GitHub secrets.
- **Orphaned packages, no active repo:** TimeWarp.Cli (0.6.0-rc9),
  TimeWarp.AspNetCore.Blazor.Templates — deprecate or adopt; no TP needed.
- Security note: the 1-hour temp key can push ANY owner-account package;
  containment is the shared workflow + DevCli gate, not policy scoping.

## Versioned-but-not-publishing repos (6)

Have a props `<Version>` but no NuGet push detected — likely future packages or
apps using version metadata. They should adopt the caller workflow when they start
publishing; no action before that.

| Repo | Vis | Version | Note |
|------|-----|---------|------|
| timewarp-software | PRI | 1.0.0-beta.1 | DevCli + dev.cs present; workflows but no push — closest to promotion |
| timewarp.enterprises | PRI | 1.0.0-beta.1 | DevCli present |
| bannamtalay.com | PRI | 1.0.0-beta.1 | site/app |
| crunchit | PRI | 0.0.1 | app |
| timewarp-tazor | PRI | 0.1.0-beta.1 | no workflows yet |
| timewarp-tui | PRI | 0.1.0-beta.1 | no workflows yet |

## Not applicable (38)

No version SSOT, no publish path — sites, apps, experiments, dormant repos:
.github, PdrMobile, TimeWarpCenter, TimeWarpHealth, TimeWarpHeroIcons, UI-Layer,
dotfiles, gambit, kahini, kivinjari, lynnthaimassage.com, staruml-export-plantuml,
timewarp-accounting, timewarp-ai, timewarp-ai.com, timewarp-amina,
timewarp-blazor-cli, timewarp-cloudflare, timewarp-code,
timewarp-dynamic-ip-updater, timewarp-edit, timewarp-financial, timewarp-flow,
timewarp-format, timewarp-forms, timewarp-kamba, timewarp-kijamii, timewarp-kuba,
timewarp-os, timewarp-parade, timewarp-proton, timewarp-source-code-generators,
timewarp-tailwind, timewarp-todo, timewarp-ui-for, timewarp-uml-mcp,
timewarp-zizi, timewarpengineering.github.io.

`.github` is N/A as a package repo but becomes **load-bearing** as the reusable
workflow host (Layer 1). timewarp-flow stays the agent-prefs SSOT (`ganda agents
sync`), which is a separate, working mechanism — do not merge the two.

## Rollout order (when migration is authorized — not started per operator)

1. Land 458-001…006 in Nuru (DevCli gains the correct gates + promotion/retest).
2. Stand up the reusable workflow in `.github`; convert Nuru to the caller.
3. Convert the 13 already-Nuru-shape repos (mechanical: replace workflow with
   caller, delete jsonc package lists, fix architecture's git-tag strategy,
   v-prefix tag policy for terminal/components).
4. Migrate the 6 nonconforming publishers (state, mediator, fixie, multiavatar,
   quickbooks, build-tasks) — each is a real migration; state first (its
   `release: created` draft-trigger and secret-key auth are the worst live risks).
5. Turn on `dev audit-convention` in merge mode org-wide + the scheduled sweep.

Raw audit data: session scratchpad `audit-results.json` (regenerate any time with
the same API queries; Layer 3's sweep makes this automatic).
