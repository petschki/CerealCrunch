# CerealCrunch

Match-3-Spiel (iOS, Hochformat) mit Frühstückszerealien-Charakteren: Ray Sin, Hazel Nuts, Oatis, Cran Berry, B-Nana, Barry Blue und Corny Flake.

Unity 6000.5.4f1 · keine externen Pakete.

## Features

- Match-3 mit erweiterten Regeln: gerade Reihen ab 3, 2×2-Quadrate/Rechtecke, Über-Eck-Ketten
- Level-System mit per Monte-Carlo-Simulation kalibrierter Schwierigkeitskurve (5 → 7 Sorten)
- Comic-Charaktere mit Namens-Popups bei großen Matches
- Ad-Gerüst (`IAdsProvider`): Interstitials mit Frequency Capping, Rewarded-Rettungszüge (+5) — aktuell Fake-Provider, echtes SDK andockbar
- Hoch- und Querformat, jeweils eigener Cartoon-Hintergrund

## Struktur

- `Assets/Scripts/` — Spiellogik (`CerealBoard`, `CerealPiece`, `FloatingText`) und `Ads/`
- `Assets/Editor/` — Sprite-Importeinstellungen, Scene-Builder (batchmode-fähig), Portrait-/Batching-Konfiguration
- `Assets/Resources/Cereals/` — Sprites und Hintergründe (generiert aus `Tools/art/*.svg` via `rsvg-convert`)
- `Tools/art/` — SVG-Quellen aller Grafiken
- `Tools/balance/` — Balance-Simulation (`uv run python balance.py`)

## Starten

Projekt in Unity öffnen, Szene `Assets/Scenes/Main.unity` laden, Play. Das Spielfeld wird zur Laufzeit per Code aufgebaut.
