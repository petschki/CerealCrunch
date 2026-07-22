# Nano-Banana-Prompts für CerealCrunch

Alle Grafik-Prompts für das Spiel, gruppiert nach Priorität. Fertige Bilder
einfach unter dem angegebenen Dateinamen ablegen — der Import in Unity
(Sprite-Einstellungen) passiert automatisch, Code-Änderungen sind nur nötig,
wo es dabeisteht.

## Stil-Baustein (an JEDEN Prompt anhängen)

> Bunter Comic-Cartoon-Stil mit kräftigen dunklen Outlines, warmen
> gesättigten Farben und weicher Beleuchtung, wie in modernen Match-3-Spielen
> (Homescapes, Gardenscapes). Kein Text, keine Schrift, keine Logos, keine
> Wasserzeichen im Bild.

**Für alle Café-Innenräume zusätzlich** `cafe_final.png` als Referenzbild
anhängen und ergänzen:

> Exakt dieselbe Kameraperspektive und derselbe Raum wie im Referenzbild:
> Erdgeschoss mit geschwungener Holztreppe links, Eingangstür in der Mitte,
> Tresen rechts, großes Fenster links. Querformat 16:9.

---

## 1. Renovierungsstufen (höchste Priorität)

Das Spiel blendet beim Renovieren zwischen Ganzbild-Zuständen über.
Ziel: `Assets/Resources/CerealCrunchCafe/cafe_state_0.png` … `cafe_state_4.png`
(die aktuellen Dateien sind nur Sepia-Platzhalter — überschreiben).
Querformat 16:9, mindestens 1408×768. **Keine Menschen, keine Gäste!**

### cafe_state_0 — Die Ruine

> Der Raum ist eine völlig verfallene Ruine: morsche, teils fehlende
> Holzdielen mit Löchern, abblätternde Tapeten und fleckiger Putz, das
> große Fenster mit Brettern vernagelt und zerbrochenen Scheiben, die
> geschwungene Treppe halb eingestürzt mit fehlenden Stufen und kaputtem
> Geländer, überall Schutt, Staub, Spinnweben, umgestürzte alte Möbel,
> herabhängende Kabel. Düsteres, staubiges Licht fällt durch Ritzen.

### cafe_state_1 — Entrümpelt & Boden roh

> Der Schutt ist weggeräumt, der Raum ist leer und besenrein. Die alten
> Dielen sind roh abgeschliffen (helles unbehandeltes Holz), die Wände noch
> fleckig und unverputzt. Am Fenster sind die Bretter entfernt, die
> Scheiben noch alt und trüb. Eine Holzleiter, Farbeimer und eine
> Werkzeugkiste stehen im Raum. Helleres Tageslicht.

### cafe_state_2 — Wände & Boden fertig

> Die Wände sind frisch verputzt und in warmem Creme gestrichen, der
> Holzboden ist neu verlegt und glänzt honigfarben. Fenster und Treppe
> sind noch alt und reparaturbedürftig. Der Raum ist leer, in einer Ecke
> liegen zusammengefaltete Malerplanen.

### cafe_state_3 — Fenster, Türen & Treppe erneuert

> Jetzt sind auch das große Fenster (neue klare Scheiben, frisch lackierte
> Rahmen), die Eingangstür und die geschwungene Holztreppe komplett
> restauriert: neues Geländer, alle Stufen intakt. Der helle freundliche
> Raum ist noch unmöbliert und wartet auf die Einrichtung. Sonnenlicht
> fällt herein.

### cafe_state_4 — Theke & Küche eingebaut

> Der große hölzerne Café-Tresen mit Marmorplatte ist eingebaut (rechts wie
> im Referenzbild), dahinter Regale mit einer silbernen Espressomaschine,
> Müslispendern und Gläsern. Der Gastraum davor ist noch komplett leer:
> keine Tische, keine Stühle, keine Deko. Warmes einladendes Licht.

---

## 2. Cerealia-Character-Sheet (für Rigging/Animation)

Ziel: Cerealia soll sich im Spiel bewegen können (winken, jubeln, nicken).
Dafür rigge ich sie in Unity mit Bones — ich brauche die Körperteile
**getrennt**. Referenzbild: `Tools/art/refs/cerealia_promo.png` (die
Werbefigur — Gesicht, Frisur, Kleidung genau übernehmen).

Datei: `Tools/art/refs/cerealia_parts.png` (ich zerlege sie dann in Layer).

> Character-Sheet der Comicfigur aus dem Referenzbild (junge Frau,
> lockiges kastanienbraunes Haar, blaues Haarband, gestreiftes Shirt,
> Küchenschürze, Jeansshorts) für Cutout-Animation. Auf neutralem
> einfarbigem Hintergrund, sauber voneinander getrennt angeordnet:
> Kopf mit neutralem Gesicht (Vorderansicht), Torso mit Schürze, linker
> Arm und rechter Arm jeweils in zwei Segmenten (Ober-/Unterarm mit Hand),
> linkes und rechtes Bein, dazu eine Reihe Extra-Elemente: offene Augen,
> geschlossene Augen (Blinzeln), Zwinker-Auge, lächelnder Mund, offener
> lachender Mund, staunender O-Mund, eine Hand mit Löffel. Alle Teile in
> gleicher Größe/Beleuchtung, Vorderansicht, keine Überlappungen.

Dazu eine Ganzkörper-Standpose als Ersatz für den aktuellen Chibi-Sprite
(`Assets/Resources/Cereals/cerealia.png`, quadratisch, transparenter oder
einfarbiger Hintergrund):

> Die Comicfigur aus dem Referenzbild als freundliche Ganzkörper-Standpose,
> Vorderansicht, winkend mit der rechten Hand, in der linken eine
> Müslischüssel. Freigestellt auf einfarbigem Hintergrund, quadratischer
> Bildausschnitt.

---

## 3. Großtante Ottilie (ersetzt meinen Chibi-Platzhalter)

Datei: `Assets/Resources/Cereals/aunt.png` (quadratisch). Referenzbild:
`cerealia_promo.png` anhängen, damit der Zeichenstil identisch ist.

> Im selben Comic-Stil wie die Figur im Referenzbild: eine herzliche alte
> Dame um die 80, „Großtante Ottilie". Silbergraues Haar im Dutt, runde
> Brille, Perlenohrringe, lila Strickjacke über geblümtem Kleid,
> Gehstock, warmes verschmitztes Lächeln. Ganzkörper-Standpose,
> Vorderansicht, winkend. Freigestellt auf einfarbigem Hintergrund,
> quadratischer Bildausschnitt.

---

## 4. Keyart / Titelbild ohne Text

Das aktuelle `keyart_title.png` hat eingebrannte Schriftzüge — Titeltext
setzen wir besser im Spiel (skalierbar, lokalisierbar). Gleiche Datei
überschreiben: `Assets/Resources/CerealCrunchCafe/keyart_title.png`.
Referenzbild: das bisherige Keyart anhängen.

> Dasselbe Motiv wie im Referenzbild, aber komplett OHNE Text, ohne
> Schilder mit Schrift und ohne Logos: das verfallene viktorianische Haus
> mit Veranda, davor die fröhliche Cerealia mit Müslischüssel und Löffel,
> ein geschwungener Strom aus Cornflakes, Beeren und Sternen fliegt von
> ihrer Schüssel zum Haus. Blühende Bäume, Garten. Querformat 16:9.

---

## 5. Fortschrittskarte im Querformat

Ersetzt die aktuelle Hochformat-Karte (`Assets/Resources/Cereals/path_map.png`).
**Achtung:** Nach dem Austausch muss ich die Node-Positionen im Code
anpassen (`LevelPathScreen.cs`, NodeAnchors) — Bild einfach ablegen und
Bescheid geben. Querformat 16:9 oder breiter.

> Verspielte Brettspiel-Landkarte für ein Frühstücks-Match-3-Spiel,
> Vogelperspektive leicht schräg: ein geschwungener Weg aus runden
> Holz-Spielfeldern schlängelt sich in S-Kurven von links unten nach
> rechts oben durch eine Frühstückslandschaft mit Müslibergen,
> Milchfluss, Erdbeerhügeln, Croissant-Felsen und Zuckerstreusel-Wiesen.
> Am Start ein kleiner Holzbogen, am Ziel ein Podest mit Pancake-Turm
> und Luftballons. 8 bis 10 deutlich sichtbare, gleichmäßig verteilte
> runde Wegfelder. Keine Beschriftung.

---

## 6. Später (nächste Kapitel)

Noch nicht einbauen — erst wenn Kapitel 2/3 dran sind:

- **Obergeschoss verfallen/fertig**: „Zeige das Obergeschoss desselben
  Hauses über der Treppe aus dem Referenzbild: ein verfallener Flur mit
  Türen zu kleinen Zimmern …" (gleiche Stufenlogik wie das Café)
- **Garten hinter dem Haus**: verwilderte Terrasse mit zugewachsenen
  Beeten → gemütlicher Frühstücksgarten mit Tischen unter einer Pergola
- **Zerealien-Charaktere neu**: die 7 Spielstein-Gesichter (Ray Sin,
  Hazel Nuts, Oatis, Cran Berry, B-Nana, Barry Blue, Corny Flake) im
  Stil der Werbefigur, je einzeln, quadratisch, freigestellt

## Checkliste nach dem Generieren

1. Datei unter dem exakten Namen an den angegebenen Ort legen (überschreiben)
2. Unity kurz fokussieren (importiert automatisch als Sprite)
3. Bei Karte (Nr. 5) und Character-Sheet (Nr. 2): mir Bescheid geben —
   da hängt Code/Rigging dran
