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
| `channels.md` | Channel sequence, timing, formats per channel, and the measurement plan |

Blog body goes in `documentation/posts/blogs/2026-xx-xx-nuru-3.0-release.md` (this repo) and is
pulled into thefreezeteam.com via the existing `Git` shortcode stub (see TheFreezeTeamBlog task 001
for the path-case trap). X/Nostr short form goes in `documentation/posts/blips/` and must be ≤ 280
characters for `ganda post`.

## Depends on

- `3.0.0` tagged and released (follows the beta.79 dogfood pass; see 483 Results)

## Requirements

- Hook chosen and recorded in `hooks.md` before video or thread work starts.
- Every claim in the post is backed by shipped behavior in `3.0.0`: `--capabilities` with the
  `invocation` contract (479), `--json-args` binding (478), `nuru search`/`index`, REPL key-binding
  profiles, AOT (~3.3–4.8 MB binaries, ~7–10 ms startup per `documentation/user`), TimeWarp.Mediator
  contracts, migration guide.
- Video is a terminal screencast (asciinema → mp4), uploaded natively to X, not a link.
- Nothing is posted from this task. Publishing is a human action after review; the task ends with
  the assets ready and the sequence scheduled.

## Checklist

### Decide
- [ ] Pick the hook (`hooks.md`), one sentence, recorded with the reason
- [ ] Confirm the star/download/analytics baseline on launch day minus one (`channels.md` table)

### Produce
- [ ] Blog body in `documentation/posts/blogs/` (long form; the canonical link for HN/Reddit)
- [ ] 280-char blip in `documentation/posts/blips/` for X post 1 and Nostr
- [ ] Video recorded per `video-script.md`, rendered to mp4, under 90 s, with end card
- [ ] Five code screenshots for thread posts 2–6 (one feature each)
- [ ] Hero image: ask @Grok (MidJourney backend) in-thread, or make one; store beside the blog stub in TheFreezeTeamBlog
- [ ] GitHub Release notes for `v3.0.0` rewritten by hand (not just `--generate-notes`): headline, agent contract, breaking changes, migration link

### Stage
- [ ] TheFreezeTeamBlog stub + image PR opened (frontmatter per the 2025 Nuru stub; lowercase shortcode path)
- [ ] X thread text final in `x-thread.md`, screenshots attached in order
- [ ] Show HN title + first comment drafted (`channels.md`)
- [ ] r/dotnet post drafted in the "what I learned" framing (`channels.md`)
- [ ] Nostr relays confirmed reachable (`ganda post config show`; both nostria relays returned 530 on 2026-10-08)

### Launch (human, in order)
- [ ] Day 0: GitHub Release published → blog live → X thread (Tue–Thu, US morning) → @Grok replies within the hour → Nostr
- [ ] Day 1: Show HN → r/dotnet
- [ ] Day 3–5: dev.to repost of the blog; LinkedIn with the video
- [ ] Day 7: process-story post ("Grok + Claude agent team reviewed 3.0", see `hooks.md` #2) as a second shot

### Measure
- [ ] Day 1 / 7 / 30: stars, forks, NuGet downloads, X impressions and profile clicks, HN points, recorded in Results

## Notes

- Context that informed this plan: the linked X post from 2026-10-07 says Grok on X now routes to
  the best backend per task (Claude Opus 5.5 text, MidJourney images, Suno audio). @Grok replies and
  Grok-made media are native X content and get algorithmic preference over external links.
- Baseline 2026-10-08: 114 stars, 4 forks, latest release v3.0.0-beta.79 (2026-10-06).
- Previous assets: `documentation/posts/blogs/2025-07-28-nuru-cli-framework-launch.md`, eight blips
  (all over 280 chars), two source-generator presentations under `documentation/posts/presentations/`.
- Avoid: a slide "presentation" video (won't be watched), "Announcing vX" framing on Reddit (gets
  removed or ignored), and leading with "web-style routing" (the 2025 hook; now table stakes).

## Session

- Created: claude 2412bd45 (2026-10-08), from a brainstorm with the maintainer
