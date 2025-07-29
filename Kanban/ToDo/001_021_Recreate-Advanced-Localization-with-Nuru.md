# Recreate Advanced.Localization with Nuru

## Description

Port the Cocona Advanced.Localization sample to Nuru, demonstrating internationalization support with resource files and culture-specific messages.

## Parent
001_Recreate-All-Cocona-Samples-Using-Nuru

## Requirements

- Create a Nuru CLI application with localization
- Implement multi-language support
- Use resource files for translations
- Create Overview.md comparing i18n approaches

## Checklist

### Implementation
- [ ] Set up localization infrastructure
- [ ] Create resource files (ja-JP, ko-KR, zh-CN)
- [ ] Implement culture detection
- [ ] Test multiple languages

### Documentation
- [ ] Create Overview.md with side-by-side comparison
- [ ] Document differences in:
  - [ ] Localization setup
  - [ ] Resource file usage
  - [ ] Culture selection
  - [ ] Message formatting

## Notes

Original Cocona sample location: `/home/steventcramer/worktrees/github.com/mayuki/Cocona/master/samples/Advanced.Localization/`

Key features to compare:
- ICoconaLocalizer interface
- Resource file structure
- Culture-specific messages
- Fallback behavior