# Ship the pull request build classifier in TimeWarp.Nuru.DevCli

## Description

The fleet standard for CI on pull requests was decided in timewarp-ganda (tasks 476, 479, 565, 587):

- one workflow, `.github/workflows/workflow.yml`, whose `pull_request` trigger has **no `paths` filter**, so every
  pull request reports the `CI` check;
- the decision "does this pull request need build and test?" is made in `dev workflow` (C#), not in YAML;
- the rule is `WorkflowChangeFilter.RequiresBuild`: a pull request skips build and test only when every changed
  path is a declared non-build path.

The rule exists today as five hand copies: `source/timewarp-ganda/services/workflow-change-filter.cs`, ganda's
`tools/dev-cli/endpoints/workflow-command.cs`, the factory template
`source/timewarp-ganda/templates/dev-cli/endpoints/workflow-command.cs.txt`, and the dev CLIs of
timewarp-build-tasks and timewarp-multiavatar. Fifteen of the seventeen repos in the `architecture` reposet have no
copy at all (survey 2026-11-16, in ganda 656). A change to the allowlist has to be made by hand in each copy.

Every repo's dev CLI already references `TimeWarp.Nuru.DevCli`, which ships shared endpoints and services as content
(`content/any/services/ci-mode.cs`, `attestation-verifier.cs`, …). Put the classifier there so there is one
implementation and the existing pin-currency audit carries rule changes to every repo.

## Requirements

1. Add the classifier to `source/timewarp-nuru-devcli/content/any/services/` (next to `ci-mode.cs`). Port the rule
   from ganda's `WorkflowChangeFilter.RequiresBuild` exactly; read that file's Design region and its tests
   (`tests/timewarp-ganda-tests/workflow-change-filter.cs`, `factory-workflow-non-build-paths.cs`) as the spec:
   - non-build paths: `kanban/**`, `documentation/**`, `assets/**`, `notes/**`, `.github/**` except
     `.github/workflows/**`, a file named `.gitignore`, and any `.md` outside `source/`, `tests/`, `tools/`, `msbuild/`;
   - `source/`, `tests/`, `tools/`, `msbuild/` always build, including markdown;
   - `.editorconfig` builds (warnings are errors);
   - an empty diff does not require a build.
2. Provide the whole decision, not only the path test, so a repo's `workflow` command needs one call: read
   `pull_request.base.sha` / `head.sha` from `GITHUB_EVENT_PATH`, run `git diff --name-only base...head`, apply the
   rule, and return build / skip with the message to print. An unreadable event, a failed diff, a push, a release,
   and a local run never skip.
3. Let a repo extend the lists without forking the rule: additional build roots and additional non-build paths,
   passed by the caller or read from the repo config the package already loads (`repo-config.cs`). Decide which and
   say why. A repo must not be able to make `source/`, `tests/`, `tools/`, `msbuild/`, or `.editorconfig` non-build.
4. Keep the type and member names greppable and stable; ganda's audit (ganda 655) checks that a repo's
   `workflow` command calls it. Record the exact call a consumer writes under `## Results`.
5. Tests in this repo for every rule row above and for the never-skip cases.
6. Document it in the DevCli package docs: the standard, the one call, and that `workflow.yml` must not carry
   `paths` on `pull_request` (the `push` trigger may keep its filter).
7. Release a `TimeWarp.Nuru.DevCli` version that includes it and record the version under `## Results`; ganda
   655 and the rollout (ganda 656) pin to it.
8. Use it in this repo: timewarp-nuru's own `workflow` command has no classification today (its `pull_request`
   trigger has no `paths` filter, so every kanban-only pull request runs the full pipeline).

## Checklist

- [ ] Classifier service in `timewarp-nuru-devcli` content, rule ported exactly from ganda
- [ ] One-call decision (event read, diff, rule, message); never-skip cases covered
- [ ] Repo extension point, with the protected roots not overridable
- [ ] Tests for each rule row and never-skip case
- [ ] Package docs updated
- [ ] timewarp-nuru's own `workflow` command uses it
- [ ] Released; version recorded in Results

## Related tasks

- `timewarp-ganda` 655 — audit enforces the standard; merge refusal names the fix
- `timewarp-ganda` 656 — rollout to the `architecture` reposet

## Session

- Created: 31bc0748-8dda-4ee8-912e-8d546f2d6990 (2026-11-16)

## Notes

Requested by Steven 2026-11-16 from a timewarp-state session, after two kanban-only pull requests (#688, #689)
were refused at merge for having no CI check: "we've addressed this in ganda a dozen times… figure out a solution
not a hack". Placing the classifier in TimeWarp.Nuru.DevCli was the agent's proposal; Steven approved filing it.
