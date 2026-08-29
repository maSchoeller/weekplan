# Ernährungsplan — warum welche Gerichte im Pool stehen

`docs/plan.md` sagt, **wie** gerechnet wird. Dieses Dokument sagt, **welche
Gerichte** daraus folgen und warum. Es ist die Prämissensammlung für den
Rezeptpool: Wer ein Gericht anlegt, ändert oder aussortiert, misst es hieran.

Erhoben am 29.08.2026 im Gespräch, umgesetzt im Lauf
`runs/2026-08-29-stammdaten-ueber-mcp`. Der Pool wird über MCP gepflegt, nicht
in der App — siehe README, Abschnitt „Den Plan pflegen".

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
| Knusprigkeit (Panade, Frittiertes) | Ofen-Schnitzel in Cornflakes-Panade; geröstete Kichererbsen; Röstaromen aus dem Ofen beim Tikka |
| Salzige Bequemlichkeit (Wurstbrot) | Vesper nach `plan.md` §4; Eier-Muffins; Harzer |
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

Ein Beispiel-Werktag: **2.170 kcal bei 180 g Protein** — genau die
Phase-3-Zielaufnahme, am oberen Ende des Proteinkorridors.

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
   Zug mitgepflegt — er ist über MCP schreibbar.

---

## 8. Was hier bewusst nicht steht

- **Kein medizinischer Rat.** Es gilt derselbe Vorbehalt wie in `plan.md` §7:
  ein Defizit von 1.000 kcal neben sechs Trainingseinheiten ist ambitioniert
  und gehört vor dem Start beim Hausarzt besprochen.
- **Keine Annahmen über das Essverhalten am Abend.** Belegt ist: der Nutzer
  isst vor dem Lauf, das Essensfenster endet um 20:00, und er hat entschieden,
  es dabei zu belassen. Alles Weitere wäre erfunden.
- **Keine Regel zum Umgang mit Ausrutschern.** Die steht im Regelwerk der App
  (Rückfallregel) und gehört dorthin, nicht in die Gerichtsauswahl.
