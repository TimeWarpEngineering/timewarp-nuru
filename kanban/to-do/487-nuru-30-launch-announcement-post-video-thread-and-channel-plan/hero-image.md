# Hero image brief

Every FreezeTeam post has a cover image (`Image:` in the stub frontmatter). It renders twice: as the
card thumbnail on the index (`object-cover w-full h-full`, so it is center-cropped to the card) and as
the full-width banner above the post body, which overlaps it by 8 rem (`-mt-32`). The 2025 Nuru post
used `nuru-hero-light.jpg`, 720×960 portrait, Grok-generated: a cartoon superhero in a green/blue/purple
suit with "NURU" on the chest, glowing hands, sunset beach, "superhero origin story" framing.

## Spec

| Item | Value |
|------|-------|
| File | `nuru-3-hero.jpg`, beside the stub in `input/posts/steven-t-cramer/2026/MM/` in TheFreezeTeamBlog |
| Size | **1600×900** (16:9). Landscape crops cleanly to the card and to X/LinkedIn/OG previews; keep the subject centered so `object-cover` never cuts the face |
| Weight | ≤ 250 KB JPEG, quality ~82 |
| Also used for | OG/Twitter card (set `og:image` if the layout supports it), LinkedIn post, dev.to cover (dev.to wants 1000×420; crop from the same file) |

## Concept: same hero, new sidekick

Continuity with 2025: the same Nuru superhero. The 3.0 story is "your agent can call your CLI", so
the hero now *hands something to a robot*.

Prompt for @Grok (MidJourney backend) or any generator:

> Bright comic-book illustration, 16:9. The same cheerful superhero from a 2025 cover — green, blue
> and purple suit with "NURU" across the chest, domino mask, glowing green hands — stands on a sunset
> beach. He is handing a glowing holographic scroll labeled "--capabilities" to a friendly chrome
> robot with a terminal screen for a face; the screen shows a small green ">_ " prompt. Both smiling.
> Dynamic light rays from the sun behind them. Clean vector-style lines, saturated colors, no text
> other than "NURU" on the chest and "--capabilities" on the scroll. Subject centered, extra beach and
> sky margin left and right.

Variants to request in the same thread, pick the best:
1. As above.
2. Hero and robot side by side at a terminal, the robot typing, hero pointing at a route on screen.
3. Night version: hero's glowing hands lighting a dark terminal for the robot ("nuru" = light).

Reject anything with mangled text, extra fingers, or a third character. If the generator will not
render "--capabilities" legibly, drop that label and keep "NURU" only.

## Fallback

If no acceptable image exists on launch day, reuse `nuru-hero-light.jpg` (720×960) from the 2025 post.
It still fits the card; it is just the same picture twice on the index. Do not ship without an image.

## Where it goes

- TheFreezeTeamBlog: `Image: nuru-3-hero.jpg` in the stub frontmatter, file beside it.
- This repo: nothing. Blog images live in the blog repo, like the 2025 post.
