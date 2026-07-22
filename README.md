# CerealCrunch

Match-3 renovation game (iOS, landscape) starring breakfast cereal characters:
Ray Sin, Hazel Nuts, Oatis, Cran Berry, B-Nana, Barry Blue and Corny Flake.

The story: Cerealia inherits a run-down townhouse from her great-aunt Ottilie
and turns the ground floor into a breakfast café — funded star by star through
match-3 levels (Homescapes-style meta).

Unity 6000.5.4f1 · no external packages.

## Features

- Match-3 with extended rules: straight runs of 3+, 2×2 squares/rectangles, around-the-corner chains
- Renovation meta: story intro, café screen with full-image crossfade stages, star currency (1 per level win)
- Level system with a difficulty curve calibrated via Monte Carlo simulation (5 → 7 cereal types)
- Board-game style progress map with hopping mascot
- Cartoon characters with name callouts on big matches
- Procedural audio (SFX + seamless music loop) generated from `Tools/audio/generate.py`
- Comic fonts (Lilita One display / Baloo 2 body), TMP assets created at runtime
- Ad scaffold (`IAdsProvider`): interstitials with frequency capping, rewarded rescue moves (+5) — currently a fake provider, real SDK pluggable

## Art pipeline

Scene artwork (café stages, key art, progress map, board background, pieces,
characters, app icon) is AI-generated (Nano Banana / gpt-image) — prompts and
workflow live in `Tools/art/NANO_BANANA_PROMPTS.md`. Renovation progress uses
full-image stage crossfades instead of overlay layers because AI renders are
not pixel-aligned. Remaining small UI elements (cell tiles, buttons, panels,
star) come from `Tools/art/*.svg` via `rsvg-convert`.

## Structure

- `Assets/Scripts/` — game logic (`CerealBoard`, `CerealPiece`, `FloatingText`, `RenovationState`, `AudioManager`), `UI/` (GameUI, CafeScreen, StoryIntroScreen, LevelPathScreen) and `Ads/`
- `Assets/Editor/` — sprite import settings, scene builder, landscape/game-view/app-icon configuration, iOS build script, `DevAutoPlay` (automated play-mode screenshots)
- `Assets/Resources/` — `Cereals/` (pieces, map, backgrounds, characters), `CerealCrunchCafe/` (café stages, key art), `Audio/`, `Fonts/`
- `Tools/art/` — prompt catalog, SVG sources, reference renders (`refs/`)
- `Tools/audio/` — procedural audio generator (`uv run python generate.py`)
- `Tools/balance/` — balance simulation (`uv run python balance.py`)

## Getting started

Open the project in Unity, load the scene `Assets/Scenes/Main.unity`, press
Play (the game view switches itself to 19.5:9 landscape). The board and all
UI are built at runtime from code.

Tip: tick `ResetProgressOnStart` on the `Board` object to wipe progress,
stars and story state on every start while developing.

## iOS build

`Tools → CerealCrunch → Build iOS (Xcode Project)` writes an Xcode project to
`Builds/iOS` (bundle id `at.kombinat.cerealcrunch`), or run the same menu
method via `-batchmode -executeMethod BuildIos.Build`. Sign and deploy via
`xcodebuild` / `xcrun devicectl` (see commit history for the exact commands).
