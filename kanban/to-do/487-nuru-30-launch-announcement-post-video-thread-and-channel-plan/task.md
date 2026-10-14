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
- [x] Video recorded per `video-script.md`, rendered to mp4, under 90 s, with end card — **done 2026-10-09**: `assets/nuru-3-launch.mp4` (54 s, VHS)
- [x] Five code screenshots for thread posts 2–6 (one feature each) — **done 2026-10-14**: `assets/shot-2.png` … `shot-6.png` (VHS, 1600×900, published beta.80)
- [x] Hero image per `hero-image.md` (1600×900, same Nuru hero + robot sidekick); store beside the blog stub in TheFreezeTeamBlog. Blocks the stub PR: every FreezeTeam post has a cover image — **done 2026-10-09**: `assets/nuru-3-hero.jpg` (Grok Imagine, 1600×900) + OG crop
- [x] GitHub Release notes for `v3.0.0` drafted now in this folder (`release-notes.md`); pasted over `--generate-notes` output on release day: headline, agent contract, breaking changes, migration link

### Stage
- [ ] TheFreezeTeamBlog stub + image PR opened (frontmatter per the 2025 Nuru stub; lowercase shortcode path)
- [x] X thread text final in `x-thread.md`, screenshots in `assets/` in order (`shot-2.png` … `shot-6.png`). Nothing is posted from this task. Recapture `shot-4.png` when the demo pin changes from beta.80 to `3.0.0`.
- [x] Show HN title + first comment drafted (`channels.md`)
- [x] r/dotnet post drafted in the "what I learned" framing (`channels.md`)
- [ ] Nostr relays confirmed reachable (`ganda post config show`; both nostria relays returned 530 on 2026-10-08, 2026-10-09, and 2026-10-14)

### Launch gates (fix before the `v3.0.0` tag; each needs its own task)
- [x] **Generated bash completion hard-depends on bash-completion.** Shipped in 492 (beta.80). `--generate-completion bash` emits `_init_completion || return`; on a bash without `/usr/share/bash-completion` loaded, Tab prints `bash: _init_completion: command not found` and nothing completes (seen while recording the video). Add the usual fallback (`type _init_completion >/dev/null 2>&1 || _init_completion() { COMPREPLY=(); ... }` or the cobra-style shim).
- [x] **REPL crashes on redirected stdin.** Shipped in 493 (beta.80). `printf 'greet x\nexit\n' | app --interactive` throws `InvalidOperationException: Cannot read keys when ... console input has been redirected` from `ReplConsoleReader.ReadLineAsync` instead of falling back to line reads or exiting with a message. Agents and CI pipe input; this should degrade, not throw.
- [x] **The package does not enable interceptors.** Shipped in 488 (beta.80); `demo/app.cs.txt` no longer carries the `#:property` line. On `3.0.0-beta.79`, a fresh `dotnet new console` with `dotnet add package TimeWarp.Nuru`, and a `#:package TimeWarp.Nuru` runfile, both fail with `CS9137: The 'interceptors' feature is not enabled in this namespace`. `build/TimeWarp.Nuru.targets` does not set `<InterceptorsNamespaces>$(InterceptorsNamespaces);TimeWarp.Nuru.Generated</InterceptorsNamespaces>`. Only the repo's own `Directory.Build.props` files do, and no user doc mentions it. Every first-time user from the launch hits this. The fix is to set the property in the package's build props/targets, and then drop the `#:property` line from `demo/app.cs.txt`.
- [x] **Mediator dependency is stable `TimeWarp.Mediator` 14.0.0.** Decided (kanban 489, 2026-10-12): ship Mediator 14.0.0 first. It shipped 2026-10-13 (timewarp-mediator kanban 017). Nuru pins `TimeWarp.Mediator.Contracts` and `TimeWarp.Mediator.Generators` at `14.0.0` (489). No NU5104 suppression. Release notes and the migration guide state the stable dependency.
- [x] **Amuru dependency is stable `TimeWarp.Amuru` 2.0.0.** Decided (kanban 502, 2026-10-13): ship Amuru 2.0.0 first. It shipped 2026-10-14 ([v2.0.0](https://github.com/TimeWarpEngineering/timewarp-amuru/releases/tag/v2.0.0)). Nuru pins `TimeWarp.Amuru` and `TimeWarp.Amuru.Tools` at `2.0.0` (502). No NU5104 suppression. Release notes and the migration guide state the stable dependency. 3.0.0-beta.80 was the one beta on `2.0.0-beta.2`.
- [x] `documentation/user/guides/migrating-to-3.0.md:41-45` still describes hierarchical `--capabilities` output (`groups`/`commands`). The shipped output is a flat `endpoints` list. Line 4 still says "under review is `3.0.0-beta.79`". The release notes and blog link to this guide.
- [ ] Optional: `NURU_H002` flags a nested lambda's own parameter as a capture (`ids.Sum(i => (long)i)` inside a handler). This is a false positive a reader can hit with that snippet. The blog has no catch-all example.

### Re-verify on 3.0.0-beta.80 (2026-10-14, before the version bump)
All five launch gates have shipped (488, 492, 493, 489, 502). beta.80 is the last beta before `3.0.0`;
every master CI run since PR #317 packs Release-only with a config and integrity gate, so `3.0.0`
will be API-identical to beta.80. The binaries differ: beta.80 is a Debug build (kanban 503) and `3.0.0` is the first Release build, with stable Mediator and Amuru, so AOT size and startup must be re-measured on release day (see Launch). This block is the last pre-bump pass over the assets.
- [x] Run the **How to validate** smoke against `demo/app.cs.txt` (already `#:package TimeWarp.Nuru@3.0.0-beta.80`, no `#:property` line). Record the outputs in Results. The old CS9137 repro line is obsolete: without the property the app must now build and print `Hello, World!`.
- [x] Re-verify the three fixed gates on beta.80 from the demo app: `--generate-completion bash` output sources without `_init_completion` present; `printf 'greet x\nexit\n' | app -i` exits cleanly; fresh `#:package` runfile compiles. One line each in Results.
- [x] Re-read `release-notes.md`, `x-thread.md`, `channels.md`, the blog, and both blips against beta.80 `changelog.md` and the shipped dependency pins (Mediator 14.0.0, Amuru 2.0.0). Fix stale beta.79 references and anything the fixed gates changed (e.g. no "add InterceptorsNamespaces" workaround anywhere). Every thread post stays ≤ 280 chars.
- [x] Capture `shot-2.png` … `shot-6.png` for thread posts 2–6 into `assets/` (one feature each, from `x-thread.md`), using VHS like the video (`assets/nuru-3-launch.tape` is the reference) or a code-image tool already on the machine. If no tool can render them non-interactively, say so in Results and leave the Stage checkbox open.
- [x] Check the Nostr relays once (`ganda post config show`, then a TCP/HTTP probe of each relay); record reachable or 530 in Results.
- [x] Do **not** bump `<Version>`, tag, post, or open the TheFreezeTeamBlog PR. This task changes no product code; the optional `NURU_H002` nested-lambda item stays out of scope.

### Launch (human, in order — gated on `v3.0.0` existing; everything above is done beforehand)
- [ ] Release-day AOT re-measure, before posting: AOT-publish `demo/app.cs.txt` pinned to `3.0.0` (Release build), record `ls -lh` and the quiet median startup, recapture `shot-6.png`, and update the blog, the r/dotnet draft and thread post 6 to match (one startup figure everywhere). Steps in `assets/readme.md`. The current figures (6.0 MB, ~3 ms) come from the Debug-built beta.80.
- [ ] Day 0: GitHub Release published → blog live → X thread (Tue–Thu, US morning) → @Grok replies within the hour → Nostr
- [ ] Day 1: Show HN → r/dotnet
- [ ] Day 3–5: dev.to repost of the blog; LinkedIn with the video
- [ ] Day 7: process-story post ("Grok + Claude agent team reviewed 3.0", see `hooks.md` #2) as a second shot

### Measure
- [ ] Day 1 / 7 / 30: stars, forks, NuGet downloads, X impressions and profile clicks, HN points, recorded in Results

## Notes

- Reopened 2026-10-11 by Steven: moved to done while launch sequence and measurement were still open.
- Review trail: `review/review-framework.md`, `review/round-4/merged.md`, `review/disposition.md`. Effort 2, roster general, four rounds, disposition clean.
- Context that informed this plan: the linked X post from 2026-10-07 says Grok on X now routes to
  the best backend per task (Claude Opus 5.5 text, MidJourney images, Suno audio). @Grok replies and
  Grok-made media are native X content and get algorithmic preference over external links.
- Baseline 2026-10-08: 114 stars, 4 forks, latest release v3.0.0-beta.79 (2026-10-06).
- Previous assets: `documentation/posts/blogs/2025-07-28-nuru-cli-framework-launch.md`, eight blips
  (all over 280 chars), two source-generator presentations under `documentation/posts/presentations/`.
- Avoid: a slide "presentation" video (won't be watched), "Announcing vX" framing on Reddit (gets
  removed or ignored), and leading with "web-style routing" (the 2025 hook; now table stakes).

## Results

Re-checked 2026-10-14 against published nuget.org `TimeWarp.Nuru` 3.0.0-beta.80 (nuspec commit
`ab5ccfb1`). `<Version>` is still `3.0.0-beta.80`. Nothing was tagged, posted, or opened as a
TheFreezeTeamBlog PR. `NURU_H002` stayed out of scope.

This machine's global NuGet cache has a **local** pack of `3.0.0-beta.80` from the task-488
worktree (`~/.nuget/packages/timewarp.nuru/3.0.0-beta.80`, source scratchpad feed, commit
`bf64306d`). That pack still throws on piped REPL input and still emits `_init_completion || return`.
The smoke below used a clean `NUGET_PACKAGES` folder so restore came from nuget.org. Do not treat
the global cache copy as beta.80.

Smoke, from `demo/app.cs.txt` (no `#:property` line), outside the repo:

- `greet World` → `Hello, World!` (exit 0). Fresh `#:package` runfile compiles.
- `deploy staging` → `Deploying latest to staging`
- `deploy --json-args '{"env":"staging","tag":"3.0.0"}'` → `Deploying 3.0.0 to staging`
- `--capabilities` top-level keys: `name`, `version` (`1.0.0` outside a git repo), `endpoints`, `invocation`. `invocation` is `jsonArgs=--json-args`, `stdin=-`, `filePrefix=@`, `merge=argvOverridesJson`, `unknownKeys=error`. Endpoints: `greet {name}` query, `deploy {env} --tag? {tag?}` command.
- `deploy --json-args '{"env":"x","bogus":1}'` → `Error: Unknown key 'bogus' for 'deploy {env} --tag? {tag?}'. Known names: env, tag.` exit 1

Three gates on that same package:

- `--generate-completion bash` sources under `bash --noprofile --norc` with no `_init_completion` (`source_exit=0`). The script uses `declare -F _init_completion` and otherwise reads `COMP_WORDS`. `COMP_WORDS=(app dep)` yields `COMPREPLY=deploy`.
- `printf 'greet x\nexit\n' | dotnet run app.cs -- -i` prints `Hello, x!` and `Goodbye!`, exit 0.
- The runfile above compiles and prints `Hello, World!` with no `InterceptorsNamespaces` line. The package ships `build/TimeWarp.Nuru.props`.

Dependency pins: the **repo** (`Directory.Packages.props`) has `TimeWarp.Mediator.*` 14.0.0 and `TimeWarp.Amuru` / `TimeWarp.Amuru.Tools` 2.0.0, which is what the release notes and the migration guide say `3.0.0` will depend on. The **published beta.80 nuspec** still depends on `TimeWarp.Mediator` 14.0.0-beta.4 and `TimeWarp.Amuru` 2.0.0-beta.2. Release notes say that split explicitly.

Other checks: `sum {*ids:uint}` on beta.80 binds `uint[]`, and `sum 1 x` prints `Error: Invalid value in 'ids'. Expected: uint[].` `--capabilities --search deploy` exits 1 with "Search requires TimeWarp.Nuru.Search" (it does not return a filtered capabilities document). `--group-filter nope` returns `"endpoints": []`. The migration guide describes a flat `endpoints` list. No launch copy tells a reader to add `InterceptorsNamespaces`.

AOT of `demo/app.cs.txt` with `dotnet publish app.cs -c Release -r linux-x64` (PublishAot default) is a native ELF, **6,187,808 bytes**. `ls -lh` prints **6.0M**. Quiet `time` prints **0.003s**. Median of 20 runs: **2.52 ms**. The VHS frame `shot-6.png` reads **0.006s**, so thread post 6 now states no millisecond figure ("single-digit milliseconds"). The blog and the r/dotnet draft say **6.0 MB** and **~3 ms** (the quiet measurement). These are from the Debug-built beta.80; re-measure on 3.0.0 on release day.

Screenshots: `assets/shot-2.png` … `shot-6.png`, 1600×900. `shot-4.png` still shows the beta.80 package pin. Recapture it when the demo pin becomes `3.0.0`.

Nostr (`ganda post config show`): `wss://rilo.nostria.app` and `wss://ribo.nostria.app`. TCP 443 connects. HTTPS GET returns **530** for both. The Stage checkbox stays open.

Still open, and outside this repo session:

- TheFreezeTeamBlog stub PR (needs the release date).
- Replace the Nostr relays or wait until they stop returning 530.
- Day −1 baseline.
- Launch and Measure, after the `v3.0.0` tag.
- Re-record `assets/nuru-3-launch.mp4` (it is the 2026-10-09 beta.79 take).

Earlier corrections that still hold: `--json-args` has no `command` key; the REPL flag is `-i`; completion is bash, zsh, fish, and PowerShell; one top-level `invocation`; 28 review findings, 22 fixed before the tag; the skill is `tw-nuru`; optional options use `--tag? {tag?}`; `return await app.RunAsync(args)`.

Implementation review (effort 2, roster general) ran two rounds under `review/` on the beta.79 pass. Disposition: **clean**. Paths: `review/review-framework.md`, `review/round-2/merged.md`, `review/disposition.md`.

Round 3 (2026-10-14, effort 2, roster general) reviewed the beta.80 re-verify delta: 273 lines, commits `70aec67b` and `94e68fd2`. It raised 2 findings. Prior M1–M6 stay fixed.

- **M7 (bug, fixed in `76150f37`):** the AOT figures in the blog, the r/dotnet draft and thread post 6 were measured on the Debug-built beta.80 (kanban 503), and the release-day re-measure step had been dropped. The fix adds a release-day re-measure on Release-built `3.0.0` to the Launch checklist, `assets/readme.md`, `demo/readme.md`, the post-6 note, the r/dotnet draft and the blog. It also scopes "bit-identical" to the API.
- **M8 (suggestion, fixed in `76150f37`):** post 6 said ~6 ms while the blog and r/dotnet said ~3 ms. Post 6 now says "single-digit milliseconds" (190 chars), and the blog and r/dotnet keep ~3 ms.

Round 4 (2026-10-14, effort 2, roster general) re-checked `76150f37` and raised nothing new.

**Review disposition: clean.** Rounds: 4. Roster: general, effort 2. Final counts across M1–M8: bug 0 open / 4 fixed / 0 wontfix; suggestion 0 / 3 / 0; nit 0 / 1 / 0. Nothing was wontfix or escalated. Paths: `review/review-framework.md`, `review/round-4/merged.md`, `review/disposition.md`.

### How to validate

**Smoke**

```bash
cd kanban/to-do/487-nuru-30-launch-announcement-post-video-thread-and-channel-plan/demo
d=$(mktemp -d) && cp app.cs.txt "$d"/app.cs && cd "$d"
# This machine's global cache has a local task-488 pack of beta.80. Use a fresh folder.
export NUGET_PACKAGES="$(mktemp -d)"
dotnet run app.cs -- greet World
dotnet run app.cs -- deploy staging
dotnet run app.cs -- deploy --json-args '{"env":"staging","tag":"3.0.0"}'
dotnet run app.cs -- --capabilities | grep -A6 '"invocation"'
dotnet run app.cs -- deploy --json-args '{"env":"x","bogus":1}'; echo "exit=$?"
printf 'greet x\nexit\n' | dotnet run app.cs -- -i; echo "repl_exit=$?"
dotnet run app.cs -- --generate-completion bash > completion.bash
bash --noprofile --norc -c 'source completion.bash; echo source_exit=$?'
python3 -c "import re,sys;t=open(sys.argv[1],encoding='utf-8').read();print([len(q) for q in re.findall(r'^\s*> (.+)$',t,re.M)])" \
  "$OLDPWD/../x-thread.md"
```

**Expect**

- `Hello, World!`, then `Deploying latest to staging`, then `Deploying 3.0.0 to staging`.
- An `invocation` object with `"jsonArgs": "--json-args"`, `"stdin": "-"`, `"filePrefix": "@"`, `"merge": "argvOverridesJson"`, `"unknownKeys": "error"`.
- `Error: Unknown key 'bogus' for 'deploy {env} --tag? {tag?}'. Known names: env, tag.` and `exit=1`.
- The piped REPL prints `Hello, x!` and `Goodbye!`, and `repl_exit=0`.
- `source_exit=0` with no `_init_completion: command not found`. There is no `#:property` line to delete; `greet World` already succeeded.
- Every thread post length is ≤ 280. Post 6 matches `shot-6.png` (`6.0 MB`, no ms figure). The blog's startup sentence stays at about 3 ms, which is the quiet-shell median, not the VHS frame.

## Session

- Created: claude 2412bd45 (2026-10-08), from a brainstorm with the maintainer
- 2026-10-08: dropped the depends-on edge on `3.0.0`; only the Launch section is release-gated (maintainer direction)
- 2026-10-09: video + hero image produced by claude 2412bd45 (VHS, Grok Imagine); see `assets/readme.md`; two more launch gates found while recording
- 2026-10-09 claude (implement oracle): verified the launch claims against beta.79 from nuget.org. Wrote the blog, blips, release notes, final thread text, and HN/Reddit copy. Corrected the video script and the review counts. Measured AOT size and startup. Added Launch gates after finding CS9137 for package consumers.
- 2026-10-09 grok (review oracle): session `01a11c82-bdbc-75d2-ad87-8d0ebcd6a339`. Effort 2, roster general. Round 1 reviewer `01a11c84-d196-7301-92f6-c2959d9d394a`. Round 2 reviewer `01a11c96-bbb5-7cf1-8a72-37c8cb0f2b03`. Disposition clean.
- Review oracle: review by implementer-grok (grok, model grok-4.7), session not reported, max-turns 120 — 2026-10-08T17:40:53Z
- 2026-10-14 grok (implement oracle): session `01a139bd-a2ce-7353-83cb-f2ed2ed16bc6`. Re-verified the launch assets against published nuget.org `3.0.0-beta.80`, captured `shot-2.png` … `shot-6.png`, and recorded Nostr 530.
- Implement oracle: implemented by implementer-grok (grok, model grok-4.7), session not reported, max-turns 500, Done — 2026-10-14T09:49:53Z
- 2026-10-14 claude (review oracle, claude-opus-5-5, headless, session id not exposed): round 3, effort 2, roster general, on the beta.80 delta. 2 open findings (M7 bug, M8 suggestion) in `review/round-3/merged.md`. Blocked on a fix pass on this id.
- 2026-10-14 claude (fix pass, claude-fable-5-1): round-3 M7 and M8. Added a release-day AOT re-measure step (Launch checklist, `assets/readme.md`, `demo/readme.md`, post 6 note); scoped "bit-identical" to the API and noted beta.80 is a Debug build. One startup figure: blog and r/dotnet keep the quiet ~3 ms median; post 6 now says "single-digit milliseconds" because a VHS retake (warm-up, five-run loop) still reads 5 to 6 ms in the recorder, so `shot-6.png` was not recaptured and the post no longer contradicts it. Disposition is left to round 4.
- 2026-10-14 claude (review oracle, claude-opus-5-5, headless, session id not exposed): round 4, effort 2, roster general, on fix commit `76150f37`. M7 and M8 verified fixed, nothing new. Disposition clean (`review/disposition.md`).
- Review oracle: review by reviewer-claude (claude, model claude-opus-5-5), session not reported, max-turns 120 — 2026-10-14T09:57:22Z
- 2026-10-14 claude (cockpit): moved back to to-do after the walk's kanban-done step; Launch and Measure stay open and this kitchen is the release-day runbook (same reason as the 2026-10-11 reopen). Moved the blog's RELEASE DAY comment below the feature list so it no longer splits it.
