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

- **Nostr blip:** `documentation/posts/blips/2026-xx-xx-nuru-3.0-release-nostr.md` (220 chars, link
  included, no thread). Relays `wss://rilo.nostria.app` and `wss://ribo.nostria.app` still returned
  HTTP 530 on 2026-10-09. Replace them or wait for them to come back before day 0 (Stage checklist).

### Show HN (day 1, US Eastern morning)

**Title:** `Show HN: Nuru 3.0 – .NET CLI framework where every command is agent-callable`

**URL:** the blog post (not the repo).

**First comment (author, post immediately):**

> Hi HN, I maintain Nuru. It's a .NET CLI framework. You map route patterns like
> `deploy {env} --tag? {tag?}` to handlers, and a source generator emits the matcher at compile time,
> so apps are Native AOT friendly and there's no reflection at runtime.
>
> 3.0 is about agents. Every app answers `--capabilities` with a JSON list of its commands (typed
> parameters, options, query vs command) plus an `invocation` block. Then `--json-args` takes the
> values as one JSON object (inline, `@file`, or stdin) and binds them on the same path argv uses.
> I built it because watching agents fight shell quoting to call my own tools was painful, and an
> MCP server per CLI felt like the wrong unit.
>
> Before tagging, I had a Grok + Claude agent team review the codebase: 28 findings, 22 fixed
> before release. The review record, misses included, is in the repo:
> https://github.com/TimeWarpEngineering/timewarp-nuru/tree/master/kanban/done/482-complete-code-review-of-timewarpnuru-before-the-official-30-release/review
>
> Honest limitations: it's .NET 10 only. 3.0 breaks 2.x (contracts moved to TimeWarp.Mediator;
> there's a migration guide). The JSON contract is Nuru's own shape, not JSON Schema. Happy to answer
> anything.

### r/dotnet (day 1, afternoon)

**Title:** `I rebuilt my CLI framework's source generator so apps are AOT and agent-callable. Here's what broke.`

**Body:**

> I've maintained TimeWarp.Nuru, a route-based CLI framework for .NET, since 2025. For 3.0 I wanted
> two things: every app should be callable by an AI agent without an MCP server, and everything
> should stay source-generated so Native AOT keeps working. Three things I learned:
>
> **1. Agents need values, not command lines.** Shell quoting is where agents fail. Each app now
> prints its own contract (`--capabilities`) and takes values as JSON
> (`./app deploy --json-args '{"env":"staging","tag":"3.0.0"}'`). The route literal stays on argv,
> and unknown keys exit 1, so a wrong guess fails loudly instead of being ignored.
>
> **2. Fluent builders hide lifecycle bugs.** A review found that `.Map(...).WithHandler(...).Build()`
> without `.Done()` silently dropped the last route. The generator saw the chain, but the route was
> never registered. The fix was small. Finding it took a reviewer that reads every path.
>
> **3. Generated parsers need the same type coverage as handwritten ones.** Typed catch-alls
> (`{*ids:uint}`) fell back to a parser that returned raw strings for `uint`, `TimeSpan`, and
> `IPAddress`, so the generated array assignment didn't compile. It now emits `TryParse` for every
> built-in conversion.
>
> The 1-file version: `#:package TimeWarp.Nuru`, map two routes, call `.EnableCompletion()` and
> `.AddRepl()`, and `dotnet run app.cs` gives you help, completion for bash/zsh/fish/pwsh, and a
> REPL. `dotnet publish app.cs -c Release -r linux-x64` makes a 5.9 MB native binary that starts
> in ~3 ms.
>
> Write-up with code: <BLOG URL> · Repo: https://github.com/TimeWarpEngineering/timewarp-nuru
>
> What's your experience with agents calling your own CLIs? MCP, plain shell, something else?

Checked 2026-10-09 on beta.79: `sum {*ids:uint}` binds `uint[]`, and `sum 1 x` prints
`Error: Invalid value in 'ids'. Expected: uint[]`.

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
