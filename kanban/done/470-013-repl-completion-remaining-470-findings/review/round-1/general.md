# Round 1 — general
**Date:** 2026-09-23
**Scope reviewed:** local uncommitted product + test diff for M18/M19/M37/M44

## Summary

Closes the four parent-470 leftovers with small, targeted fixes: Shift+Enter → `HandleAddLineAsync` on Emacs/Vi/VSCode (matching Default), bash COMPREPLY via quoted prefix loop, zsh drops only `:directive` lines, and pwsh embeds `APP_PATH` in a single-quoted literal with `'` doubling. Gate tests (completion-20, repl-23, repl-32, repl-41) all pass. No incorrect behavior or missing required coverage found against the task brief.

## Issues

<!-- none -->
