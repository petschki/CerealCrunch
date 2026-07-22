# Nano-Banana-Prompts: Renovierungsstufen des Cafés

Das Spiel blendet beim Renovieren zwischen Ganzbild-Zuständen über
(`Assets/Resources/CerealCrunchCafe/cafe_state_0.png` … `cafe_state_4.png`,
Finale = `cafe_final.png`). Die aktuellen state-Dateien sind nur automatisch
erzeugte Platzhalter (abgedunkelte Sepia-Versionen des fertigen Cafés) —
einfach durch echte Renders ersetzen, gleiche Dateinamen, Querformat 16:9
(z. B. 1408×768 oder größer).

**Wichtig für Konsistenz:** Immer `cafe_final.png` als Referenzbild anhängen
und diese Zusätze in jeden Prompt nehmen:

> Exakt dieselbe Kameraperspektive und derselbe Raum wie im Referenzbild
> (Erdgeschoss mit geschwungener Holztreppe links, Eingangstür Mitte,
> Tresen rechts, großes Fenster links). Comic-Stil mit kräftigen Outlines
> wie im Referenzbild. Keine Menschen, kein Text, keine Schilder mit Schrift.

## cafe_state_0 — Die Ruine (Startzustand)

„Der Raum ist eine völlig verfallene Ruine: morsche, teils fehlende
Holzdielen mit Löchern, abblätternde Tapeten und fleckiger Putz, das große
Fenster mit Brettern vernagelt und zerbrochenen Scheiben, die geschwungene
Treppe halb eingestürzt mit fehlenden Stufen und kaputtem Geländer, überall
Schutt, Staub, Spinnweben, umgestürzte alte Möbel, herabhängende Kabel.
Düsteres, staubiges Licht durch Ritzen."

## cafe_state_1 — Entrümpelt & Boden roh

„Der Schutt ist weggeräumt, der Raum ist leer und besenrein. Die alten
Dielen sind roh abgeschliffen (helles unbehandeltes Holz), die Wände noch
fleckig und unverputzt. Am Fenster sind die Bretter entfernt, die Scheiben
noch alt und trüb. Eine Leiter, Farbeimer und Werkzeugkiste stehen im Raum.
Helleres Tageslicht."

## cafe_state_2 — Wände & Boden fertig

„Die Wände sind frisch verputzt und in warmem Creme/Pastell gestrichen, der
Holzboden ist verlegt, geölt und glänzt honigfarben. Fenster und Treppe noch
alt und reparaturbedürftig. Der Raum ist leer, ein paar zusammengefaltete
Malerplanen in der Ecke."

## cafe_state_3 — Fenster, Türen & Treppe erneuert

„Jetzt sind auch das große Fenster (neue klare Scheiben, frisch lackierte
Rahmen), die Eingangstür und die geschwungene Holztreppe komplett
restauriert — neues Geländer, alle Stufen intakt. Der helle, freundliche
Raum ist noch unmöbliert und wartet auf die Einrichtung. Sonnenlicht fällt
herein."

## cafe_state_4 — Theke & Küche eingebaut

„Der große hölzerne Café-Tresen mit Marmorplatte ist eingebaut (rechts wie
im Referenzbild), dahinter Regale und eine silberne Espressomaschine,
Müslispender und Gläser. Der Gastraum davor ist noch leer — keine Tische,
keine Stühle, keine Deko. Warmes, einladendes Licht."

## Optional später

- `keyart_title.png`-Ersatz ohne eingebrannten Text (Titeltext lieber im Spiel setzen)
- Landscape-Fortschrittskarte als Ersatz für `path_map.png` (aktuell Portrait, wird im Querformat geletterboxt)
- Porträt der Großtante Ottilie im Stil der Cerealia-Werbefigur (ersetzt `Cereals/aunt.png`, Chibi-Platzhalter)
- Obergeschoss- und Garten-Szenen für die nächsten Kapitel
