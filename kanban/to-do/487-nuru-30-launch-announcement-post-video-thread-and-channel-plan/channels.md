# Channels, sequence, and measurement

## Where stars come from (be honest)

| Channel | Produces | Notes |
|---------|----------|-------|
| X thread + video | impressions, profile clicks | Rarely stars. Native video and @Grok replies are the reach lever. |
| Show HN | stars, forks, HN points | Historically the largest star driver for dev tools. One shot; title matters. |
| r/dotnet, r/csharp | stars, comments | Hostile to "announcing". Use the "what I learned" framing. |
| FreezeTeam blog | the canonical link | Required so HN/Reddit don't link straight to GitHub. |
| dev.to | long-tail search | Repost of the blog, 3–5 days later, canonical URL set. |
| Nostr | small, loyal | `ganda post <blip> --platform nostr` once relays are up. |
| LinkedIn | .NET enterprise devs | Video + three lines. Underrated for .NET. |
| NuGet readme | converts package viewers | Make sure the package readme leads with the 3.0 hook. |

## Sequence

| Day | Action |
|-----|--------|
| −1 | Record baseline (table below). Blog stub PR in TheFreezeTeamBlog ready. |
| 0 (Tue–Thu, 9–10 am US Eastern) | Publish `v3.0.0` GitHub Release with hand-written notes → merge blog stub (site deploys) → X thread → @Grok replies within 60 min → Nostr blip |
| 1 | Show HN (morning US Eastern) → r/dotnet (afternoon) |
| 3–5 | dev.to repost; LinkedIn video post |
| 7 | Second-shot X thread: agent-team code review story |
| 30 | Measurement row; decide whether a third piece (video walkthrough of the agent contract) is worth it |

## Per-channel copy

- **Show HN title:** `Show HN: Nuru 3.0 – .NET CLI framework where every command is agent-callable`
  First comment (author): what it is in two sentences, why the agent contract exists, link to the
  review record as proof of rigor, and one honest limitation (e.g. .NET 10 only; AOT trims
  reflection-based converters).
- **r/dotnet title:** `I rebuilt my CLI framework's source generator so apps are AOT and agent-callable. Here's what broke.`
  Body: three concrete lessons from the 482 review, code snippets, link at the end.
- **Nostr blip:** post 1 text, no thread.

## Baseline and measurement

| Metric | Day −1 | Day 1 | Day 7 | Day 30 |
|--------|--------|-------|-------|--------|
| GitHub stars | 114 (2026-10-08) | | | |
| GitHub forks | 4 | | | |
| NuGet downloads, TimeWarp.Nuru (all versions) | | | | |
| X post 1 impressions / profile clicks | | | | |
| HN points / comments | | | | |
| Blog page views | | | | |

Fill from GitHub Insights, nuget.org package page, X analytics, HN item page, and the site analytics.
