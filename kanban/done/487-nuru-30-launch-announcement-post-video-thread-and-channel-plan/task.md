# Nuru 3.0 launch: announcement post, video, thread, and channel plan

## Description

Plan and produce the public launch of TimeWarp.Nuru `3.0.0`. The 2025 launch (blog + X, 2025-07-28)
did not move the needle: the repo sits at 114 stars and 4 forks after 15 months. Attention is scarce
in 2026, so this launch is built around one AI-era hook, one native video, and the channels that
actually produce GitHub stars, not just impressions.

This folder holds the working drafts so the writing is reviewed like code:

| File | Purpose |
|------|---------|
| `hooks.md` | Hook candidates with the recommendation; decide one before any production |
| `video-script.md` | 60-second terminal screencast script |
| `x-thread.md` | Seven-post X thread draft, with the @Grok reply prompts |
| `channels.md` | Channel sequence, timing, formats per channel, Show HN and r/dotnet copy, and the measurement plan |
| `release-notes.md` | `v3.0.0` GitHub Release notes, pasted over `--generate-notes` on release day |
| `demo/` | `app.cs.txt` (copy out as `app.cs`), the one-file app used by the video, blog, and screenshots, plus how to run it |

Blog body goes in `documentation/posts/blogs/2026-xx-xx-nuru-3.0-release.md` (this repo) and is
pulled into thefreezeteam.com via the existing `Git` shortcode stub (see TheFreezeTeamBlog task 001
for the path-case trap). X/Nostr short form goes in `documentation/posts/blips/` and must be ≤ 280
characters for `ganda post`.

## Requirements

- Hook chosen and recorded in `hooks.md` before video or thread work starts.
- Every claim in the post is backed by shipped behavior in `3.0.0`: `--capabilities` with the
  `invocation` contract (479), `--json-args` binding (478), `nuru search`/`index`, REPL key-binding
  profiles, AOT (~3.3–4.8 MB binaries, ~7–10 ms startup per `documentation/user`), TimeWarp.Mediator
  contracts, migration guide.
- Video is a terminal screencast (asciinema → mp4), uploaded natively to X, not a link.
- Nothing is posted from this task. Publishing is a human action after review; the task ends with
  the assets ready and the sequence scheduled.
- **Build everything before the release.** Decide, Produce, and Stage run now against `3.0.0-beta.79`,
  which is API-identical to what `3.0.0` will ship. Only the Launch section waits for the `v3.0.0` tag;
  the release day should be a push-button day, not a writing day.

## Checklist

### Decide
- [x] Pick the hook (`hooks.md`): #1 agent contract for launch, #2 process story on day 7 — decided 2026-10-08
- [ ] Confirm the star/download/analytics baseline on launch day minus one (`channels.md` table). This is a day −1 action and can't be done early.

### Produce
- [x] Blog body in `documentation/posts/blogs/` (long form; the canonical link for HN/Reddit): `2026-xx-xx-nuru-3.0-release.md`
- [x] 280-char blip in `documentation/posts/blips/` for X post 1 and Nostr: `2026-xx-xx-nuru-3.0-release.md` (144 chars, X) and `…-nostr.md` (220 chars, with link)
- [ ] Video recorded per `video-script.md`, rendered to mp4, under 90 s, with end card
- [ ] Five code screenshots for thread posts 2–6 (one feature each)
- [ ] Hero image per `hero-image.md` (1600×900, same Nuru hero + robot sidekick); store beside the blog stub in TheFreezeTeamBlog. Blocks the stub PR: every FreezeTeam post has a cover image
- [x] GitHub Release notes for `v3.0.0` drafted now in this folder (`release-notes.md`); pasted over `--generate-notes` output on release day: headline, agent contract, breaking changes, migration link

### Stage
- [ ] TheFreezeTeamBlog stub + image PR opened (frontmatter per the 2025 Nuru stub; lowercase shortcode path)
- [ ] X thread text final in `x-thread.md`, screenshots attached in order. The text is final (2026-10-09); `shot-2.png` … `shot-6.png` are still to capture.
- [x] Show HN title + first comment drafted (`channels.md`)
- [x] r/dotnet post drafted in the "what I learned" framing (`channels.md`)
- [ ] Nostr relays confirmed reachable (`ganda post config show`; both nostria relays returned 530 on 2026-10-08, and again on 2026-10-09)

### Launch gates (fix before the `v3.0.0` tag; each needs its own task)
- [ ] **The package does not enable interceptors.** On `3.0.0-beta.79`, a fresh `dotnet new console` with `dotnet add package TimeWarp.Nuru`, and a `#:package TimeWarp.Nuru` runfile, both fail with `CS9137: The 'interceptors' feature is not enabled in this namespace`. `build/TimeWarp.Nuru.targets` does not set `<InterceptorsNamespaces>$(InterceptorsNamespaces);TimeWarp.Nuru.Generated</InterceptorsNamespaces>`. Only the repo's own `Directory.Build.props` files do, and no user doc mentions it. Every first-time user from the launch hits this. The fix is to set the property in the package's build props/targets, and then drop the `#:property` line from `demo/app.cs.txt`.
- [ ] **3.0.0 depends on prerelease `TimeWarp.Mediator 14.0.0-beta.4`.** A stable package with a prerelease dependency gets NU5104 and forces consumers onto a beta. Ship Mediator 14.0.0 first, or accept and document it.
- [ ] `documentation/user/guides/migrating-to-3.0.md:41-45` still describes hierarchical `--capabilities` output (`groups`/`commands`). The shipped output is a flat `endpoints` list. Line 4 still says "under review is `3.0.0-beta.79`". The release notes and blog link to this guide.
- [ ] Optional: `NURU_H002` flags a nested lambda's own parameter as a capture (`ids.Sum(i => (long)i)` inside a handler). This is a false positive a reader can hit with that snippet. The blog has no catch-all example.

### Launch (human, in order — gated on `v3.0.0` existing; everything above is done beforehand)
- [ ] Day 0: GitHub Release published → blog live → X thread (Tue–Thu, US morning) → @Grok replies within the hour → Nostr
- [ ] Day 1: Show HN → r/dotnet
- [ ] Day 3–5: dev.to repost of the blog; LinkedIn with the video
- [ ] Day 7: process-story post ("Grok + Claude agent team reviewed 3.0", see `hooks.md` #2) as a second shot

### Measure
- [ ] Day 1 / 7 / 30: stars, forks, NuGet downloads, X impressions and profile clicks, HN points, recorded in Results

## Notes

- Review trail: `review/review-framework.md`, `review/round-2/merged.md`, `review/disposition.md`. Effort 2, roster general, two rounds, disposition clean.
- Context that informed this plan: the linked X post from 2026-10-07 says Grok on X now routes to
  the best backend per task (Claude Opus 5.5 text, MidJourney images, Suno audio). @Grok replies and
  Grok-made media are native X content and get algorithmic preference over external links.
- Baseline 2026-10-08: 114 stars, 4 forks, latest release v3.0.0-beta.79 (2026-10-06).
- Previous assets: `documentation/posts/blogs/2025-07-28-nuru-cli-framework-launch.md`, eight blips
  (all over 280 chars), two source-generator presentations under `documentation/posts/presentations/`.
- Avoid: a slide "presentation" video (won't be watched), "Announcing vX" framing on Reddit (gets
  removed or ignored), and leading with "web-style routing" (the 2025 hook; now table stakes).

## Results

The assets that can be produced from a repo session are done and verified against the published
`TimeWarp.Nuru 3.0.0-beta.79`:

- **Blog body:** `documentation/posts/blogs/2026-xx-xx-nuru-3.0-release.md`, titled "CLIs your agents can call".
- **Blips:** `documentation/posts/blips/2026-xx-xx-nuru-3.0-release.md` for X post 1 (144 chars) and `…-nostr.md` (220 chars).
- **Release notes:** `release-notes.md`, the `v3.0.0` GitHub Release body.
- **X thread:** `x-thread.md`, final text. Every post is ≤ 280 chars.
- **Show HN and r/dotnet copy:** in `channels.md`.
- **Video and demo app:** `video-script.md` was corrected, and `demo/app.cs.txt` plus `demo/readme.md` were added.

Earlier drafts had claims that didn't match shipped behavior. These are fixed:

- `--json-args` JSON has no `command` key. The route literal stays on argv.
- The REPL flag is `-i`, not `--repl`.
- Completion covers bash, zsh, fish, and PowerShell, not just bash and zsh.
- `invocation` is one top-level object, not one per route.
- The review had 28 findings. 22 were fixed before the tag: 20 of them, plus a changelog backfill, in 15 PRs (#283–#297, stacked in #298), and two more on the review task (#282). Six are scheduled after 3.0. The draft said 21 findings, all fixed in those 15 PRs.
- The skill is `tw-nuru`.
- AOT numbers are now measured rather than copied from the docs. `demo/app.cs.txt` with REPL and completion is a **5.9 MB** binary that runs in a **2.9 ms median** (20 runs, linux-x64, WSL2). `performance.md` lists 3.3 MB and 4.8 MB for Direct and Mediator AOT on .NET 9, with startup under 1 ms. It does not say those binaries omit REPL or completion.
- An optional option is `--tag? {tag?}`. `--tag {tag?}` makes the option required.
- `return await app.RunAsync(args)` is needed, because a bare `await` exits 0 on error.

The new **Launch gates** section records four defects found while verifying. The first, CS9137 for
every new package consumer, blocks the launch's main promise ("one package"). Each needs its own
task. This task changes no product code.

Implementation review (effort 2, roster general) ran two rounds under `review/`. Round 1 raised 3 bugs, 2 suggestions, and 1 nit. All six are fixed. Round 2 raised nothing new. Disposition: **clean**. Final counts: bug 0 open / 3 fixed, suggestion 0 open / 2 fixed, nit 0 open / 1 fixed. No wontfix and no escalation. Paths: `review/review-framework.md`, `review/round-2/merged.md`, `review/disposition.md`.

Still open, and outside what a repo session can do:

- Record the video.
- Capture the five screenshots.
- Make the hero image (@Grok).
- Open the TheFreezeTeamBlog stub PR. It needs the release date.
- Replace the Nostr relays or wait for them to come back.
- Record the day −1 baseline.
- Run Launch and Measure.

### How to validate

**Smoke**

```bash
cd kanban/to-do/487-nuru-30-launch-announcement-post-video-thread-and-channel-plan/demo
d=$(mktemp -d) && cp app.cs.txt "$d"/app.cs && cd "$d"
dotnet run app.cs -- greet World
dotnet run app.cs -- deploy staging
dotnet run app.cs -- deploy --json-args '{"env":"staging","tag":"3.0.0"}'
dotnet run app.cs -- --capabilities | grep -A6 '"invocation"'
dotnet run app.cs -- deploy --json-args '{"env":"x","bogus":1}'; echo "exit=$?"
sed -i '/^#:property InterceptorsNamespaces/d' app.cs && dotnet run app.cs -- greet World   # launch gate repro
python3 -c "import re,sys;t=open(sys.argv[1],encoding='utf-8').read();print([len(q) for q in re.findall(r'^\s*> (.+)$',t,re.M)])" \
  "$OLDPWD/../x-thread.md"
```

**Expect**

- The first five commands print, in order:
  - `Hello, World!`
  - `Deploying latest to staging`
  - `Deploying 3.0.0 to staging`
  - an `invocation` object with `"jsonArgs": "--json-args"`, `"stdin": "-"`, `"filePrefix": "@"`, `"merge": "argvOverridesJson"`, `"unknownKeys": "error"`
  - `Error: Unknown key 'bogus' … Known names: env, tag.` with `exit=1`
- With the `#:property` line removed, the build fails with `CS9137`. This is the open launch gate. Once the package fix ships, it prints `Hello, World!`.
- Every thread post length is ≤ 280.
- Every claim in the blog, the release notes, and the thread matches the outputs above, or `changelog.md` under `3.0.0-beta.79`.

## Session

- Created: claude 2412bd45 (2026-10-08), from a brainstorm with the maintainer
- 2026-10-08: dropped the depends-on edge on `3.0.0`; only the Launch section is release-gated (maintainer direction)
- 2026-10-09 claude (implement oracle): verified the launch claims against beta.79 from nuget.org. Wrote the blog, blips, release notes, final thread text, and HN/Reddit copy. Corrected the video script and the review counts. Measured AOT size and startup. Added Launch gates after finding CS9137 for package consumers.
- 2026-10-09 grok (review oracle): session `01a11c82-bdbc-75d2-ad87-8d0ebcd6a339`. Effort 2, roster general. Round 1 reviewer `01a11c84-d196-7301-92f6-c2959d9d394a`. Round 2 reviewer `01a11c96-bbb5-7cf1-8a72-37c8cb0f2b03`. Disposition clean.
- Review oracle: review by implementer-grok (grok, model grok-4.7), session not reported, max-turns 120 — 2026-10-08T17:40:53Z
