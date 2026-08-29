# Ernährungsplan — warum welche Gerichte im Pool stehen

`docs/plan.md` sagt, **wie** gerechnet wird. Dieses Dokument sagt, **welche
Gerichte** daraus folgen und warum. Es ist die Prämissensammlung für den
Rezeptpool: Wer ein Gericht anlegt, ändert oder aussortiert, misst es hieran.

Erhoben am 29.08.2026 im Gespräch, umgesetzt im Lauf
`runs/2026-08-29-stammdaten-ueber-mcp`. Der Pool wird über MCP gepflegt, nicht
in der App — siehe README, Abschnitt „Den Plan pflegen".

Am **29.08.2026** im Gespräch mit dem Ernährungscoach überarbeitet: vier
Gerichte ersetzt, eine Mengenänderung, und alle achtzehn Gerichte auf Geschmack
durchgegangen. Was dabei entschieden wurde, steht in §8.

---

## 1. Die Zahlen, aus denen alles folgt

Persönliche Werte stehen sonst nirgends im Repo; hier stehen sie, weil ohne sie
keine Portionsgröße begründbar wäre.

| | |
|---|---|
| Gewicht / Größe / Alter | 96 kg / 196 cm / 27 Jahre |
| Zielgewicht | 80 kg |
| Grundumsatz (Mifflin-St Jeor) | 2.055 kcal |
| Alltagsumsatz (× 1,28) | 2.630 kcal |
| **Normaltag Phase 1** (Defizit 600) | **2.408 kcal** |
| **Normaltag Phase 3** (Defizit 1.000) | **2.165 kcal** |
| Refeed-Tag Phase 3 (Samstag) | 3.332 kcal |
| Protein (Faktor 1,8–2,2 × Zielgewicht) | **144–176 g pro Tag** |

**Die Zielaufnahme liegt in jeder Phase über dem Grundumsatz.** Die Warnung der
App greift also nicht, und „schnell abnehmen" ist hier keine riskante Ansage,
sondern eine gut gepolsterte: 16 kg bei rund 0,9 kg pro Woche sind etwa
18 Wochen.

**Portionsziele nach dem 32/38/30-Schlüssel, Phase 3:**
Frühstück **693** · Mittag **823** · Abend **650** kcal.

Der Portionsrechner der App skaliert von dort; die Rezeptwerte sind die
Portionsgröße bei Faktor 1,0. Für Phase 1 entspricht das etwa Faktor 1,1.

---

## 2. Der Maßstab: Protein je 100 kcal

Das ist die eine Zahl, an der ein Gericht in diesen Pool kommt oder nicht.
Der Gedanke steht schon in `plan.md` §4 in der Vesper-Tabelle: dieselben
Bausteine, anders gewählt, ergeben 9,6 statt 5,1 g Protein je 100 kcal.

Im tiefen Defizit leistet Protein drei Dinge gleichzeitig — es schützt die
Muskelmasse, es sättigt am stärksten von allen Makronährstoffen, und es ist am
teuersten zu verstoffwechseln. Wer die Zahl hochhält, macht das Defizit
aushaltbar, ohne die Kalorien anzufassen.

**Der alte Pool (bis 29.08.2026) und seine Schwachstelle:**

| | alt | neu | Untergrenze |
|---|---|---|---|
| Frühstück | 9,5 | **9,6** | ≥ 9 |
| Mittag | **6,1** | **7,6** | ≥ 7 |
| Abend | 9,0 | **8,6** | ≥ 9 |

Um 160 g Protein aus 2.165 kcal zu holen, braucht es im Schnitt 7,4. Der Mittag
lag darunter und zog die Tagesbilanz mit — ausgerechnet die Mahlzeit, die bis
zum Abendessen tragen muss.

**Zwei Gerichte unterschreiten ihre Untergrenze bewusst:**

- *Ofen-Feta mit Kichererbsen* (7,7) — Feta und Kichererbsen tragen ihr Fett
  und ihre Kohlenhydrate mit; mehr Skyr als die vorhandenen 250 g wäre kein
  Gericht mehr, sondern eine Quarkspeise mit Einlage.
- *Lachspfanne* (8,4) — der Fettgehalt ist beim Lachs der Zweck, nicht der
  Fehler. Omega-3 wird hier gegen Proteindichte eingetauscht.

**Die Wochenendgerichte messen sich gar nicht an diesen Grenzen.** Große Pasta
(5,7), Burger (7,0) und Ofen-Schnitzel (8,9) stehen für den Refeed-Tag im Pool.
Dort ist die Kalorienmenge der Zweck — der Refeed-Tag liegt ohnehin über dem
Gesamtumsatz, und Protein ist an einem Tag, an dem 3.332 kcal geplant sind, kein
knappes Gut. Wer diese drei an der Werktagsgrenze misst, misst das Falsche.

**Die stärksten Bausteine**, wenn ein Gericht Protein braucht:

| Zutat | Protein je 100 kcal |
|---|---|
| Harzer Käse | 24 |
| Hähnchenbrust | 21 |
| Thunfisch im eigenen Saft | 24 |
| Magerquark | 19 |
| Skyr | 17 |
| Sojagranulat (trocken) | 14 |
| Hüttenkäse | 17 |
| Kichererbsen, Linsen | 6–7 |

Daran liegt es, dass Milchprodukte in fast jedem Gericht auftauchen: sie sind
die einzigen Bausteine, die Protein ohne nennenswerte Beiladung liefern.

---

## 3. Der Rahmen

**Werktags vegetarisch und Fisch, Fleisch am Wochenende.** Entscheidung des
Nutzers, und aus zwei Gründen die richtige. Erstens vertragen sich Meal Prep
und Fleisch schlecht: gegartes Hähnchen ist nach drei Tagen im Kühlschrank
grenzwertig, vegetarische Topfgerichte halten vier und schmecken am dritten
besser. Zweitens sitzt der Fleisch-Anker damit genau dort, wo ohnehin frisch
gekocht wird — Samstag und Sonntag.

**Meal Prep ist die Betriebsart, nicht die Ausnahme.** Sonntags zwei Sorten für
je zwei bis drei Tage. Daraus folgt: **Wiederholung ist der Normalfall, kein
Makel.** Das automatische Füllen der Woche wählt an Werktagen darum nur aus den
Gerichten mit dem Merkmal `prep`.

**Fast immer Homeoffice.** Die `kalt ok`-Auflage aus `plan.md` §5 ist damit
weitgehend hinfällig — sie bleibt ein Komfortmerkmal, keine Anforderung mehr.

**Fisch:** Thunfisch aus der Dose und Lachs. Kein Räucherfisch.

**Der Refeed-Tag braucht eigene Gerichte.** Samstag sind 3.332 kcal geplant.
Ein Refeed ohne Rezepte ist kein Refeed, sondern ein Freifahrtschein — und der
kostet laut `plan.md` §1 ein bis zwei komplette Defizit-Tage, unsichtbar.
Deshalb stehen Pasta, Burger, Ofenhähnchen und Ofen-Schnitzel im Pool.

---

## 4. Was nicht in ein Gericht darf

- **Gurken**, jede Art
- **Rohe Paprika** (Paprikapulver ist ausdrücklich erwünscht)
- **Rohe Zwiebeln** — gekocht, gebraten oder geschmort sind sie in Ordnung
- **Rohe Oliven** (Olivenöl ist ausdrücklich erwünscht)
- **Radieschen** — seit dem 29.08.2026. Ihr Job im Pool war ein einziger: etwas
  Rohes, das knackt. **Kohlrabi** macht das besser, ist milder, hält im
  Kühlschrank eine Woche und kostet 25 kcal auf 100 g. Er steht jetzt im
  Kräuterquark (feine Würfel) und im Vesper (Stifte).

Keine Allergien, keine Unverträglichkeiten.

Dazu die technische Schranke: Jede Zutat muss eine **Abteilung** nennen, die es
gibt. Die Liste steht in den Stammdaten und ist die Reihenfolge des Wegs durch
den Laden — sie wird nicht aus Bequemlichkeit erweitert.

---

## 5. Die Anker — was ersetzt wird, nicht verboten

Die heutigen Gewohnheiten sind der Ausgangspunkt, nicht der Gegner. Jeder Anker
bekommt einen Nachbau statt eines Verbots:

| Anker | Nachbau im Pool |
|---|---|
| Knusprigkeit (Panade, Frittiertes) | Ofen-Schnitzel in Cornflakes-Panade; geröstete Kichererbsen im Bulgursalat und im Ofen-Feta; Grillfunktion in den letzten drei Minuten bei Tikka, Gyros und Schnitzel |
| Salzige Bequemlichkeit (Wurstbrot) | Vesper nach `plan.md` §4; Blech-Omelett; Harzer |
| Kein-Bock-Abende (Lieferdienst) | Zwei Abendgerichte unter fünf Minuten, die immer im Kühlschrank stehen |
| Mittags Lieferdienst | Die Box gewinnt gegen die Bestellung nicht über Willenskraft, sondern über die Uhr: zwei Minuten Mikrowelle gegen 35 Minuten Wartezeit |

Und ein Anker, der schon in die richtige Richtung zeigt: **Joghurt mit Obst und
Haferflocken** isst der Nutzer bereits. Die Joghurt-Quark-Schüssel ist genau
dieses Gericht, nur mit Magerquark verstärkt — kein neues Gericht, ein
nachgerechnetes.

---

## 6. Der Aufbau des Pools

18 Gerichte, seit dem 29.08.2026.

| Zweck | Anzahl | kcal-Ziel | Zeit |
|---|---|---|---|
| Frühstück | 3 | ~700 | alle unter 5 Minuten |
| Mittag, werktags | 6 | ~820 | vorkochbar, halten 3 Tage |
| Abend, werktags | 5 | ~650 | zwei davon unter 5 Minuten |
| Wochenende | 4 | 650–950 | frisch gekocht, mit Fleisch |

**Achtung bei den Kategorien.** Das Datenmodell kennt nur `fruehstueck`,
`mittag` und `abend`. Die vier Wochenendgerichte liegen darum unter `mittag`
und `abend` mit — in der App erscheinen sie als 3 / 8 / 7. Die Kategorie ist
seit dem Lauf ohnehin nur noch eine Vorsortierung im Wochenplan und keine
Sperre: jedes Gericht ist in jedem Slot wählbar.

Ein Beispiel-Werktag — Overnight Oats, Linsen mit Spätzle, Rührei mit Hüttenkäse:
**2.190 kcal bei 189 g Protein.** Das sind 25 kcal über der Phase-3-Zielaufnahme
und liegt am oberen Ende des Proteinkorridors.

**Drei süße gegen herzhafte Frühstücke: zwei zu eins.** Bei drei Gerichten
wiederholt sich nichts innerhalb einer Arbeitswoche, wenn man mag, und bei
Meal Prep ist das Frühstück die Mahlzeit, die Wiederholung am besten verträgt.

---

## 7. Prüfliste für ein neues Gericht

Bevor ein Gericht in den Pool geht:

1. **Trifft es seine Untergrenze bei Protein je 100 kcal?** Frühstück ≥ 9,
   Mittag ≥ 7, Abend ≥ 9. Wenn nicht — steht der Grund dafür geschrieben, so
   wie bei Ofen-Feta und Lachspfanne?
2. **Sind kcal und Protein aus den Zutatenmengen gerechnet?** Nicht geschätzt,
   nicht vom alten Rezept übernommen. Der alte Pool hatte hier Ausreißer.
3. **Passt die Portionsgröße** zu 693 / 823 / 650 kcal, plus minus etwa 10 %?
4. **Kommt eine der No-Go-Zutaten roh vor?**
5. **Wenn es werktags gekocht wird: trägt es das Merkmal `prep`?** Hält es
   wirklich drei Tage und wärmt es gut auf? Ei-Gerichte tun das nicht.
6. **Nennt jede Zutat eine vorhandene Abteilung?** Und steht `vorrat=true` bei
   allem, was im Grundstock liegt — sonst landet es auf der Wochenliste.
7. **Ist die Anleitung nach dem Muster gegliedert?** Vorbereitung / Kochen /
   Anrichten, und die Begründung als eigener **Warum**-Absatz am Ende — nicht
   als nummerierter Schritt, in dem nichts zu tun ist.
8. **Steht in jedem Schritt, der es braucht, das Warum?** „Topf vom Herd nehmen,
   bevor das Paprikapulver hineinkommt" ist eine Anweisung; „sonst wird es
   bitter" macht sie merkbar.
9. **Braucht es neue Vorratszutaten?** Dann gehört der Grundstock im selben
   Zug mitgepflegt — er ist über MCP schreibbar. Und was durch die Änderung
   verwaist, fliegt im selben Zug raus.
10. **Ist es bis an die Grenze gewürzt?** Säure zum Schluss, eine Umami-Schicht,
    Röstaromen, ein Textur-Kontrast, Zitronenabrieb. Kostet die Würzung zusammen
    weniger als 20 kcal je Portion? Wenn nicht, muss an anderer Stelle etwas
    weichen — die Hebel und die Regel stehen in §8.

---

## 8. Würzen bis an die Grenze

Am 29.08.2026 wurde jedes Gericht im Pool ein zweites Mal angefasst — nicht an
den Mengen, sondern am Geschmack. Der Anlass ist einfach: Ein Plan, der achtzehn
Wochen tragen soll, scheitert selten an der Rechnung und oft daran, dass das
Essen langweilig wird. Ein Gericht, das nicht schmeckt, wird nicht gekocht, und
ein Gericht, das nicht gekocht wird, hat keine Nährwerte.

**Vier Gerichte sind ausgetauscht worden:**

| Raus | Rein | Grund |
|---|---|---|
| Eier-Muffins mit Spinat und Feta | Blech-Omelett mit Spinat und Feta | Nicht das Vorbacken war die Hürde, sondern die zwölf Mulden. Eine Form, einmal gießen, einmal schneiden — und der Feta kommt jetzt obenauf statt untergehoben, damit er bräunt. |
| Linsen-Dal mit Reis | Linsen mit Spätzle | Gut bürgerlich statt orientalisch. Der Pool hat mit Tikka und Chili genug Gewürzküche, und schwäbische Linsen halten drei Tage. |
| Ofenhähnchen mit Kartoffeln und Wurzelgemüse | Hähnchen-Gyros vom Blech | Gleiche Blech-Logik, gleiche Zeit, gleiche Kalorien — aber eine Joghurtmarinade und geröstete ganze Gewürze statt Salz und Pfeffer auf nacktem Fleisch. |
| Radieschen (Kräuterquark, Vesper) | Kohlrabi | Mag der Nutzer nicht. Siehe §4. |

Dazu eine Mengenänderung: **die Zwiebel in der großen Pasta von 60 auf 20 g.**
Der Verlust ist echt — sie hat die Süße und den Körper der Sauce getragen.
Bezahlt wird er mit drei statt zwei Minuten geröstetem Tomatenmark, doppelt so
viel Knoblauch in Scheiben und vier Gramm Ahornsirup.

### Die Regel

**Bei jedem Konflikt zwischen Geschmack und Kalorien gewinnen die Kalorien.**
Praktisch heißt das: Was ein Gericht an Würzung dazubekommt, darf zusammen keine
20 kcal je Portion kosten. Wo es mehr wurde — Senf, Harissa, ein Löffel
Ahornsirup —, ist an anderer Stelle ein Gramm Öl weggefallen. Neun Kalorien je
Gramm machen Öl zur billigsten Stellschraube, die es gibt, und niemand schmeckt
das eine Gramm.

### Die sieben Hebel, in dieser Reihenfolge

| Hebel | kcal je Portion | Wo er jetzt im Pool steht |
|---|---|---|
| Salz zum richtigen Zeitpunkt | 0 | Linsen erst am Ende, Rührei erst vom Herd, Kartoffeln schon im Kochwasser, Ofenkartoffeln noch einmal heiß aus dem Ofen |
| Säure zum Schluss | 0–3 | Apfelessig in Gulasch, Chili und Linsen; Balsamico in der Bolognese; Zitrone über allem, was aus dem Ofen kommt |
| Zitronenabrieb | 0 | Tikka, Ofen-Feta, Bulgursalat, Panade, beide Frühstücke, jede Skyr-Sauce |
| Röstaromen | 0 | Blech leer vorheizen, Soja dunkelbraun statt braun braten, Grillfunktion in den letzten drei Minuten |
| Umami | 5–15 | Steinpilzpulver in Gulasch und Bolognese, Sojasauce statt Salz, Tomatenmark zwei Minuten dunkel rösten, Parmesanrinde mitkochen |
| Textur-Kontrast | 0 | Geröstete Kichererbsen, geröstete Nüsse, geröstete Haferflocken |
| Schärfe und Rauch | 9–15 | Geräuchertes Paprikapulver, Harissa, Chiliflocken |

### Zwei Dinge, die nichts kosten und am meisten bringen

**Ganze Gewürze rösten und mörsern.** Koriandersaat und Kreuzkümmel eine Minute
in der trockenen Pfanne, dann im Mörser. Der Unterschied zum Streuer ist größer
als der zwischen zwei Rezepten. Steht jetzt im Tikka und im Gyros, der Kümmel
angedrückt im Gulasch.

**Kalt gegessen braucht ein Fünftel mehr.** Kälte dämpft Salz und Säure. Chili,
Bulgursalat und Kräuterquark tragen den Hinweis darum ausdrücklich im Rezept —
sie werden warm abgeschmeckt und kalt gegessen, und genau dort ging der
Geschmack bisher verloren.

### Was der Grundstock dafür trägt

Neu: geräuchertes Paprikapulver, gemahlene Steinpilze, Apfelessig, Dijon-Senf,
Harissa, Ahornsirup, Vanilleextrakt sowie Koriander- und Fenchelsaat ganz.
Zusammen rund 15 € auf vier Wochen — der billigste Teil der Einkaufsliste und
der, der den größten Teil des Geschmacks trägt.

Weggefallen, weil vom gestrichenen Dal verwaist: rote Linsen, Kurkuma,
Senfsamen. Und das Muffinblech, das jetzt eine Auflaufform ist.

**Noch offen:** Belugalinsen, Erdnussmus und Currypulver stehen im Grundstock,
werden aber von keinem Gericht im Pool gebraucht — schon vor dieser
Überarbeitung nicht. Entweder es kommt ein Gericht dazu, das sie nutzt, oder sie
gehören von der Liste.

---

## 9. Was hier bewusst nicht steht

- **Kein medizinischer Rat.** Es gilt derselbe Vorbehalt wie in `plan.md` §7:
  ein Defizit von 1.000 kcal neben sechs Trainingseinheiten ist ambitioniert
  und gehört vor dem Start beim Hausarzt besprochen.
- **Keine Annahmen über das Essverhalten am Abend.** Belegt ist: der Nutzer
  isst vor dem Lauf, das Essensfenster endet um 20:00, und er hat entschieden,
  es dabei zu belassen. Alles Weitere wäre erfunden.
- **Keine Regel zum Umgang mit Ausrutschern.** Die steht im Regelwerk der App
  (Rückfallregel) und gehört dorthin, nicht in die Gerichtsauswahl.
