# CerealCrunch

Match-3 game (iOS, portrait) starring breakfast cereal characters: Ray Sin, Hazel Nuts, Oatis, Cran Berry, B-Nana, Barry Blue and Corny Flake.

Unity 6000.5.4f1 · no external packages.

## Features

- Match-3 with extended rules: straight runs of 3+, 2×2 squares/rectangles, around-the-corner chains
- Level system with a difficulty curve calibrated via Monte Carlo simulation (5 → 7 cereal types)
- Cartoon characters with name callouts on big matches
- Ad scaffold (`IAdsProvider`): interstitials with frequency capping, rewarded rescue moves (+5) — currently a fake provider, real SDK pluggable
- Portrait and landscape support, each with its own cartoon background

## Structure

- `Assets/Scripts/` — game logic (`CerealBoard`, `CerealPiece`, `FloatingText`) and `Ads/`
- `Assets/Editor/` — sprite import settings, scene builder (batch-mode capable), portrait/batching configuration
- `Assets/Resources/Cereals/` — sprites and backgrounds (generated from `Tools/art/*.svg` via `rsvg-convert`)
- `Tools/art/` — SVG sources for all artwork
- `Tools/balance/` — balance simulation (`uv run python balance.py`)

## Getting started

Open the project in Unity, load the scene `Assets/Scenes/Main.unity`, press Play. The board is built at runtime from code.
