# Nuru verification skill: launch and drive the real CLI as a consumer, with evidence

## Description

Write a hand-authored verification skill for Nuru, modelled on the pstack `/create-verification-skill` pattern (Launch, Drive, Evidence, with Doctor and Cleanup only if needed). Do not run pstack's meta-skill and do not install the pstack plugin. Source: timewarp-intel evaluation 2026-10-21 poteto pstack guide, action B (Busara verdict REWORK), and research 2026-10-23 sdlc-factory-vs-ganda-walk (gap 1).

Busara's condition for B was to name one real failure that slipped past the existing checks (tests, REPL, `--capabilities` checks), and to build the skill around that flow. Gap 1 names the failures:

- nuru#242: builder configure APIs were advertised as live but were stubs.
- nuru#296: living docs still taught pre-3.0 APIs.
- state#647 (2026-10-13): local-feed packs poison the global NuGet cache under the release version, a consumer-side break that diff review cannot see.

Scope added 2026-10-23 on Steven's yes: a "use it like a consumer" check. An agent installs the shipped package and CLI the way a consumer would (from a clean NuGet cache or a clean temp feed, never from a local pack under the release version), then uses it end to end and records evidence. The check must be able to catch each of the three failure classes above:

- Advertised-but-stubbed APIs: call every public builder API the docs and README advertise and confirm it does something real.
- Stale docs: build and run the snippets from the README and living docs against the packed package and confirm they compile and behave as written.
- Cache poisoning: verify the consumer restore resolves the package from the intended source and version, and that a local pack does not shadow the release version in the global NuGet cache.

## Requirements

- The skill lives in `skills/` of this repo next to `tw-nuru` and `tw-cli-search`, follows the skill conventions, and is loadable by any bot.
- Launch: restore and run the shipped package and CLI from an isolated cache location, no repo project references.
- Drive: run real commands with a PTY or plain process, covering the three failure classes above.
- Evidence: write transcripts and exit codes to a path a PR description can quote.
- Doctor and Cleanup only if a real flow needs them.
- Decide where the check is invoked: as a ganda task-walk step, as a pre-release gate in `tw-release`, or both. Write the decision and its reason in Notes.

## Checklist

- [ ] Reproduce one of nuru#242, nuru#296 or state#647 against a shipped build as proof the check can fail
- [ ] Draft the skill under `skills/` using only the steps it needs
- [ ] Run it on the current release and attach the evidence
- [ ] Confirm it catches the reproduced failure
- [ ] Decide and document where it is invoked
- [ ] Open a PR with the evidence in the description

## Session

- Created: 2156863 (2026-10-23)

## Notes

- Origin: Steven approved the consumer-use scope on 2026-10-23, relayed by Amina; Steven confirmed publishing this task in chat the same day.
- The earlier REWORK verdict said to park if no missed failure could be named. nuru#242, nuru#296 and state#647 satisfy that condition.
- Authoring the skill itself still needs an owner and Steven's go-ahead before the task is claimed for work.
