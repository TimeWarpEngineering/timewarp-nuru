# Create Nuru mascot

## Description

Create **Nuru**, the original TimeWarp mascot hero for this repo.  Nuru means *light* in Swahili, and the hero is themed on that meaning.

Brand guide: https://github.com/TimeWarpEngineering/timewarp-intel/pull/51 (`brand/timewarp-brand-style.md`, section 0, "Repo heroes").

Proposed look (from the roster; a proposal, adjust with Steven): Cyan suit, green ribbon scarf, glowing terminal-window shield with a `>` and route arrows branching out.  Chest letter: **N**.

**Reference hero, approved by Steven:** the terminal-shield concept (`brand/mascots/nuru-hero.jpg`) is the approved reference.  Refine it into final art; keep the shield, scarf and N.  Every other repo hero is measured against this one.

## Goal (implementer restatement)

Ship the already-built Nuru hero into this repo and make the README hero load on the NuGet package page as well as on GitHub. Do not redraw the character. Nuru stays an original hero: shield, green ribbon scarf, and a single chest letter N.

Acceptance:

- `assets/mascot/` holds five byte-for-byte copies from `/home/steve/masoko/mascot-kit/`: `nuru-hero.png` (1024x1024, white), `nuru-hero-dark.png` (1024x1024, Night #06161E), `nuru-hero-transparent.png`, `nuru-og-card.png` (1200x630), and `nuru-end-card.png` (1920x1080).
- `readme.md` opens with a centered image of the white hero. The `src` is `https://raw.githubusercontent.com/TimeWarpEngineering/timewarp-nuru/master/assets/mascot/nuru-hero.png`, because the root readme is packed as the NuGet readme and nuget.org cannot resolve a relative path. Alt text and width stay as they are. Every other README line stays.
- Checklist items for the white hero, the dark variant, the README, the OG card, and the video end card stay ticked. Steven's approval of the final art stays open.
- Product edits stay in `assets/mascot/`, `readme.md`, and this task file. Round-1 M1 is marked fixed on the live merge ledger.

## Requirements

- Style: in the spirit of Steven's hand-drawn SOLID heroes: cartoon, thick #06161E outline, flat colors, blue eye mask, one big chest letter, cheeky helpful pose.  Use `brand/source/x-header.jpg` as the style reference.
- Never reuse the SOLID heroes, their bodies or their moulds.  They are for the SOLID principles only.  Nuru is an original character.
- Costume colors from the logo palette (Cyan #00CCFF, Ocean #004F78, Violet #6953B1, Deep Violet #352078, Lilac #C5B4FF, Leaf #6FA749, Ribbon #50852C, Mint #D2FFB4, Hero Red #D92C31).
- Only the single chest letter **N** in the art.  Image models garble text; every other word is added with code (Pillow) afterwards.
- No real data or charts in the illustration.

## Checklist

- [x] Hero art on white (PNG, transparent or white background, at least 1024 px)
- [x] Dark variant on Night #06161E
- [x] Use in the README header
- [x] Use in the OG card (1200x630, code-drawn text)
- [x] Use in the video end card
- [ ] Steven approves the final art

## Review

- Round 1: M1 fixed on the live ledger (implement-fix-2). The hero `src` is the raw master URL. Round 2 has not run.

## Notes

- Filed by Masoko (marketing) on 2026-10-22 at Steven's request.

## Implementation notes (Masoko, 2026-10-23)

The final art and derived cards are already built.  Do not regenerate or redraw anything.  Copy and wire them in:

- Source kit (outside the repo): `/home/steve/masoko/mascot-kit/`, files `nuru-hero.png`, `nuru-hero-dark.png`, `nuru-hero-transparent.png`, `nuru-og-card.png`, `nuru-end-card.png`.
- Copy all five into `assets/mascot/` in this repo (create the folder).  Keep the file names.  Binary files, copy byte for byte.
  - `nuru-hero.png`: 1024x1024 hero on white (checklist item 1).
  - `nuru-hero-dark.png`: hero with white sticker outline on Night #06161E (item 2).
  - `nuru-hero-transparent.png`: transparent cut-out, extra.
  - `nuru-og-card.png`: 1200x630 OG card, text drawn by code (item 4).
  - `nuru-end-card.png`: 1920x1080 still end card for videos (item 5).
- README header (item 3): add the hero at the very top of `readme.md`, centered, for example `<p align="center"><img src="assets/mascot/nuru-hero.png" alt="Nuru, the TimeWarp Nuru mascot" width="240" /></p>`.  Keep every existing README line.  Use plain ASCII, no emoji.
- Tick checklist items 1 to 5.  Leave "Steven approves the final art" unticked: that is Steven's call.
- Chest letter is N.  Do not add prose claims beyond what this task states (Swahili meaning is in the Description).
- Changes stay to `assets/mascot/`, `readme.md` and this task file.  No code changes.

## Session

- 2026-10-23 implementer (grok, headless task work, session 01a167a3-a28a-7652-a299-671dcb56b766): copied the five kit PNGs into `assets/mascot/`, added the README header, and left Steven's approval open. `ganda repo audit` passed (30 passed, 0 failed). No code changes.
- Implement oracle: implemented by implementer-grok (grok, model grok-4.7), session not reported, max-turns 500, Done — 2026-10-23T07:26:01Z
- 2026-10-23 review oracle round 1 (Claude Opus 5.5, headless task work, effort 1, roster: general): 1 open suggestion (M1). The README hero uses a relative image path, which breaks on the NuGet package page. A fix pass is requested. See `review/round-1/merged.md`.
- Review oracle: review by reviewer-claude (claude, model claude-opus-5-5), session not reported, max-turns 80 — 2026-10-23T07:27:39Z
- 2026-10-23 implement-fix-2 (grok, headless task work, session 01a167a8-83e6-7270-ae61-fcbf2a7fb570): pointed the README hero `src` at `https://raw.githubusercontent.com/TimeWarpEngineering/timewarp-nuru/master/assets/mascot/nuru-hero.png` so the packed NuGet readme can resolve it. Alt text and width are unchanged. Marked M1 fixed in `review/round-1/merged.md`. Steven's approval stays open. `ganda repo audit` passed (30 passed, 0 failed, 1 skipped). The five PNGs still match the kit.

## Results

Copied the built Nuru art into `assets/mascot/` and pointed the README at the white hero. Nothing was redrawn. The chest letter on the hero is N. Steven's approval of the final art stays open.

- `nuru-hero.png`: 1024x1024 RGB, corner pixels #FFFFFF. SHA-256 `47c8dfa8d29bb561ac16ac045ef1f07e755de4fe2c94b2358c73cd3fe21de0eb`.
- `nuru-hero-dark.png`: 1024x1024 RGB, corner pixels #06161E, white sticker outline. SHA-256 `8c8e79e4c8f1c36ebe59d8a7804d0ce9139d26a47c5102153f4d47706c21d024`.
- `nuru-hero-transparent.png`: 1024x1024 RGBA. Corner alpha is 0 (706213 fully transparent pixels). SHA-256 `9554c4fabe7dc683a1bbc442a1b9f3d46d7b7143df945c44bf3ad9ac84eb4375`.
- `nuru-og-card.png`: 1200x630 RGB. SHA-256 `438d0e19c6807fee68ff4846c1f2c95ea475647e37567ae3524b1b4bd57194d6`.
- `nuru-end-card.png`: 1920x1080 RGB. SHA-256 `3dd10b85487dc9605bf5de2125973137fa042cb9f214fb454c82e13a1840f94f`.

Each file matches `/home/steve/masoko/mascot-kit/` byte for byte (`cmp` exit 0). `readme.md` gained only the centered hero line at the top. The previous README bytes are unchanged.

Fix pass (implement-fix-2): that hero line now uses an absolute `src`, `https://raw.githubusercontent.com/TimeWarpEngineering/timewarp-nuru/master/assets/mascot/nuru-hero.png`. The root `readme.md` is `PackageReadmeFile` (`source/Directory.Build.props`), and nuget.org cannot resolve a relative image. Alt text and width are unchanged. The five PNGs were not touched. The raw URL returns 404 until this branch merges to master. `ganda repo audit` on this pass passed (30 passed, 0 failed, 1 skipped) with nothing to fix.

### How to validate

Smoke:

```bash
file assets/mascot/nuru-hero.png assets/mascot/nuru-hero-dark.png assets/mascot/nuru-hero-transparent.png assets/mascot/nuru-og-card.png assets/mascot/nuru-end-card.png
sha256sum assets/mascot/nuru-hero.png assets/mascot/nuru-hero-dark.png assets/mascot/nuru-hero-transparent.png assets/mascot/nuru-og-card.png assets/mascot/nuru-end-card.png
for f in nuru-hero.png nuru-hero-dark.png nuru-hero-transparent.png nuru-og-card.png nuru-end-card.png; do cmp "/home/steve/masoko/mascot-kit/$f" "assets/mascot/$f"; done
head -n 1 readme.md
```

Expect: `file` reports 1024x1024 for the three heroes, 1200x630 for `nuru-og-card.png`, and 1920x1080 for `nuru-end-card.png`. The SHA-256 lines match the digests in Results. Each `cmp` is silent and exits 0. The first README line is `<p align="center"><img src="https://raw.githubusercontent.com/TimeWarpEngineering/timewarp-nuru/master/assets/mascot/nuru-hero.png" alt="Nuru, the TimeWarp Nuru mascot" width="240" /></p>`. The checklist item "Steven approves the final art" stays unticked.

Not in scope: a live 200 from that raw URL. The file is not on `master` until this branch merges.

### Review

- Round 1 (effort 1, general): 0 bug, 1 suggestion, 0 nit. After the fix pass the live ledger is 0 open, 1 fixed, 0 wontfix. Disposition is not written. The host runs review round 2 to confirm M1.
- Artifacts: `review/review-framework.md`, `review/round-1/general.md`, `review/round-1/merged.md`.
