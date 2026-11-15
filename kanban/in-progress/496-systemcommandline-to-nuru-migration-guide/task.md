# System.CommandLine to Nuru migration guide

## Description

Write a user guide for porting a CLI from System.CommandLine to TimeWarp.Nuru, centered on the three things
that push System.CommandLine users to look elsewhere: Native AOT, trimming, and the 2.0 API churn
(beta -> 2.0 renames and removed APIs). Requested by Steven on 2026-10-12 after the first Nuru competitor
adoption scan (timewarp-intel PR #4).

## Relationship to task 490

Task 490 fixes `documentation/user/guides/migrating-to-3.0.md`, which is for **Nuru 2.x -> 3.0** users. This
task is a separate guide for **System.CommandLine -> Nuru** users. Overlap: both should describe the same 3.0
surface (e.g. `--capabilities` flat `endpoints` output). Cross-link the two guides, and land or rebase on 490
first so this guide does not copy the stale hierarchical `--capabilities` description.

## Deliverables

1. **Human docs page** (single source of truth): `documentation/user/guides/migrating-from-system-commandline.md`.
2. **Migration skill** at `skills/tw-nuru-migrate-system-commandline/SKILL.md`, next to the repo's existing
   `skills/tw-nuru` and `skills/tw-cli-search`, so our agents and outside agents can be pointed at it. It holds the
   step-by-step porting recipe, the API mapping table and pitfalls (`--` passthrough, free-form trailing args,
   verbosity/config binding, global-tool packaging, AOT/trim warnings), and links to the docs page for the
   full explanation instead of duplicating it. Keep the two in sync: the docs page owns the prose, the skill
   owns the agent recipe.

## Requirements

- Docs page linked from the guides overview; skill follows the format of `skills/tw-nuru/SKILL.md` (frontmatter name/description) and links to the docs page.
- Concept map: RootCommand/Command/Option<T>/Argument<T>/SetAction (and the old SetHandler) -> Nuru route patterns, options, handlers.
- AOT and trimming: what System.CommandLine needs (reflection, trim warnings) vs. Nuru's source-generated path; show a `PublishAot` publish with zero warnings.
- 2.0 API churn: a table of beta-era APIs (beta4 SetHandler, InvocationContext, CommandLineBuilder, etc.) and their Nuru equivalent, so users still on betas can skip straight to Nuru.
- Cover global-tool packaging, `--` passthrough, free-form trailing args, verbosity/config binding.
- Cross-link `migrating-to-3.0.md` (task 490).

## Validation

An agent ports these repos in order **by following the skill only**, as **private test runs, no outreach**
(no PRs, issues or comments on them). Any step the agent has to invent becomes a fix in the skill and/or docs page.

1. https://github.com/craigktreasure/SlnUp - baseline single command plus global-tool packaging.
2. https://github.com/ebrahim-s-ebrahim/litmus - option and argument mapping, JSON output.
3. https://github.com/xt0rted/dotnet-run-script - passthrough and `--` handling.
4. https://github.com/WeihanLi/dotnet-httpie - free-form `key=value` and `header:value` tail (hardest parsing case).
5. https://github.com/grate-devs/grate - mid-size, many options, verbosity and config binding.

## Checklist

- [ ] Docs page written and linked from guides overview
- [ ] Skill written in `skills/`, linking to the docs page
- [ ] Cross-linked with migrating-to-3.0 (490)
- [ ] Ports 1-5 done privately by an agent following the skill; invented steps folded back into skill/docs
- [ ] AOT publish of at least one port with zero trim/AOT warnings
- [ ] PR merged

## Notes

- System.CommandLine was the biggest group in the scan (49 of 205 repos). AOT pain shows up across every library.
- kendryte/nncase and QutEcoacoustics/emu are still on System.CommandLine 2.0 betas.
- Validation targets should be small and active: 10-999 stars, push in the last 90 days.
- Excluded as poor fits: GordonBeeming/copilot_here, Nimblesite/DataProvider.
- Source data: timewarp-intel PR #4 (merged 2026-10-12).

## Session

- Created: grok-bot (2026-10-12)
