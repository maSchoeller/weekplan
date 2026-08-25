# Methodik

Dieses Dokument erklärt, wie `weekplan` rechnet und warum der Plan so aufgebaut ist.
Alle konkreten Zahlen kommen aus dem Tab **Ich** der Anwendung und liegen nur im Browser
des Nutzers — hier stehen ausschließlich Formeln und ein Beispiel mit Platzhalterwerten.

---

## 1. Die Rechnung

### Grundumsatz

Mifflin-St Jeor (männlich):

```
Grundumsatz = 10 × Gewicht_kg + 6,25 × Größe_cm − 5 × Alter + 5
```

### Alltagsumsatz ohne Sport

```
Alltagsumsatz = Grundumsatz × 1,28
```

Der Faktor 1,28 steht für einen Bürojob mit wenig Bewegung. Geplanter Sport ist bewusst
**nicht** enthalten — er wird einzeln gerechnet, sonst wird er doppelt gezählt.

### Sport

```
kcal_netto = (MET − 1) × 1,05 × Gewicht_kg × Minuten / 60
```

Die `− 1` zieht den Grundumsatz während der Einheit ab. Das Ergebnis ist der
**zusätzliche** Verbrauch, und nur der zählt fürs Defizit.

| Einheit | MET |
|---|---|
| Laufband 5 km/h (Tipptempo) | 3,5 |
| Laufband 6 km/h | 4,5 |
| Gehen / Spaziergang | 3,5 |
| Laufen locker (7:00/km) | 8,3 |
| Laufen Dauerlauf (6:30/km) | 9,3 |
| Laufen zügig (5:45/km) | 10,8 |
| Intervalltraining | 11,5 |
| Krafttraining | 3,5 |

**Warum nicht die Fitnessuhr?** Uhren zeigen brutto statt netto und überschätzen den
Verbrauch beim Gehen zusätzlich um 20–30 %. Wer nach Uhrenwerten isst, hat ein
stillschweigend um mehrere hundert Kalorien kleineres Defizit und sucht den Fehler
wochenlang woanders. Die Uhr ist für Puls und Tempo da, nicht für Kalorien.

### Zielaufnahme

```
Gesamtumsatz    = Alltagsumsatz + Sport_pro_Woche / 7
Normaltag       = Gesamtumsatz − Tagesdefizit × 7/6
Refeed-Tag      = Gesamtumsatz
Protein täglich = Faktor × Zielgewicht        (Faktor 1,8–2,2)
```

### Tempo statt Defizit vorgeben

Tempo und Defizit sind dieselbe Größe in zwei Einheiten, verbunden über den Energiegehalt
von Körperfett (~7.700 kcal pro kg):

```
Tagesdefizit = Tempo_kg_pro_Woche × 7.700 / 7
Tempo        = Tagesdefizit × 7 / 7.700
```

Normalerweise gibt die Phase das Defizit vor und das Tempo folgt daraus. Wird im Tab **Ich**
ein eigenes Tempo eingetragen, dreht sich die Rechnung um — der Phasenwert wird
überschrieben, bis das Feld wieder geleert wird.

Wichtig dabei: **Dasselbe Tempo bedeutet in verschiedenen Phasen unterschiedlich viel
Verzicht am Teller**, weil das Sportvolumen unterschiedlich ist. 0,9 kg pro Woche heißt in
Phase 1 (wenig Sport) deutlich weniger essen als in Phase 3.

Die Anwendung warnt, sobald die resultierende Zielaufnahme unter den Grundumsatz fällt, und
nennt das höchste bei diesem Sportvolumen noch vertretbare Tempo.

Der Faktor `7/6` ist der Kern der Refeed-Logik: Ein Tag pro Woche läuft ohne Defizit.
Damit die **Wochenbilanz** trotzdem sieben Tagesdefizite ergibt, tragen die übrigen sechs
Tage je ein Siebtel mehr. Ein Cheat Day, der nicht eingepreist wird, kostet je nach
Ausmaß ein bis zwei komplette Defizit-Tage — und zwar unsichtbar.

### Beispielrechnung (Platzhalterwerte)

80 kg, 180 cm, 35 Jahre, Zielgewicht 75 kg, Phase 3:

| | |
|---|---|
| Grundumsatz | 1.730 kcal |
| Alltagsumsatz | 2.214 kcal |
| Sport (Phase 3, gewichtsabhängig) | ~4.090 kcal/Woche → 584 kcal/Tag |
| Gesamtumsatz | 2.798 kcal |
| Normaltag (Defizit 1.000) | 1.632 kcal |
| Refeed-Tag | 2.798 kcal |

Bei höherem Startgewicht verschieben sich alle Werte nach oben — die Anwendung rechnet
das bei jeder Gewichtseingabe neu.

---

## 2. Warum die Werte mitwachsen müssen

Wer 13 kg abnimmt, senkt seinen Verbrauch **doppelt**: Der Grundumsatz sinkt (rund
130 kcal pro Tag), und jede Laufeinheit kostet weniger, weil weniger Masse bewegt wird
(rund 13 % weniger pro Stunde).

In Summe schrumpft ein Wochendefizit von 7.000 kcal auf etwa 5.400 kcal, **ohne dass
irgendetwas falsch gemacht wurde**. Das Tempo fällt von 0,9 auf 0,7 kg pro Woche.

Genau an dieser Stelle brechen Konzepte ab: Der Erfolg fühlt sich wie ein Misserfolg an.
Deshalb ist das Gewicht in `weekplan` kein Eintrag in einer Tabelle, sondern der
Parameter, an dem alle anderen Werte hängen.

---

## 3. Phasenaufbau

| Phase | Dauer | Defizit | Kern |
|---|---|---|---|
| 1 — Anlauf | 2 Wochen | 600 | Termine setzen, Umgebung umbauen. Bewusst zu wenig Volumen. |
| 2 — Aufbau | 2 Wochen | 800 | Laufband auf 2 h, Läufe etwas länger. |
| 3 — Vollphase | ~12 Wochen | 1.000 | Volles Programm bis zum Zielgewicht. Keine Intervalle. |
| 4 — Erhaltung + Wettkampf | ~3 Monate | 250 | Energie zurück, Tempoarbeit, Halbmarathon-Ziel. |
| 5 — Feinschliff | ~8 Wochen | 400 | Letzte Kilos langsam, Kraft bleibt, Intensität raus. |

**Warum nicht sofort Phase 3?** Der Sprung von Null auf sechs Einheiten pro Woche ist
der häufigste Abbruchgrund — nicht mangelnder Wille, sondern Füße, Waden und
Achillessehnen, die vier Wochen brauchen, um sich anzupassen. In Phase 1 und 2 ist das
Defizit ohnehin kleiner, das Volumen wird dort also gar nicht gebraucht.

**Warum keine Intervalle im tiefen Defizit?** Harte Einheiten kosten Regeneration, die
im Defizit nicht zur Verfügung steht. Intensität kommt in Phase 4, wenn wieder gegessen
wird.

---

## 4. Ernährungsstruktur

**Drei Mahlzeiten, kein Shake.** Protein kommt aus dem Essen — Proteinpulver wird in
Overnight Oats, Skyr und Quark eingerührt. In warme Saucen gerührt flockt es aus,
deshalb taucht es dort nicht auf.

| Mahlzeit | Anteil |
|---|---|
| Frühstück | 32 % |
| Mittag | 38 % |
| Abend | 30 % |

**Essensfenster 08:00–20:00, nach 16:00 nur geplantes Protein.** Das ursprüngliche
Fenster bis 16:00 hatte einen guten Zweck — es verbietet Naschen strukturell. Es
kollidiert aber mit Abendtraining: Wer um 19 Uhr läuft und bis zum Frühstück nichts isst,
liegt 16 Stunden ohne Protein und verliert im Defizit Muskeln. Die Regel „nach 16 Uhr nur
noch das geplante Abendessen" erhält die Schutzfunktion und löst das Problem.

**Das Abendessen ist Pflicht, nicht Option.** Ohne Abendessen liegt die Tagesaufnahme
rund 650 kcal unter Ziel — aus 1.000 kcal Defizit werden 1.650. Das ist kein
beschleunigter Fortschritt, sondern der Weg zu Muskelabbau und Heißhungerabenden.

**Vesper statt Kochen ist erlaubt — aber mit den richtigen Bausteinen:**

| Variante | kcal | Protein | Protein je 100 kcal |
|---|---|---|---|
| 3 Eier + 30 g Butter + 60 g Gouda + 2 Scheiben Brot | ~840 | 43 g | 5,1 g |
| 3 Eier + 100 g Hüttenkäse + 60 g Harzer + 1 Scheibe Brot | ~614 | 59 g | 9,6 g |

Butter und Gouda sind fast reines Fett. Harzer Käse liefert 30 g Protein auf 125 kcal und
ist damit die effizienteste Proteinquelle im Supermarkt.

---

## 5. Bürotage

Meal Prep scheitert selten am Kochen und oft an der Logistik. Deshalb ist im Rezeptpool
markiert, welche Gerichte **kalt funktionieren** (`kalt ok`). Wenn im Büro keine
Mikrowelle verfügbar oder sie regelmäßig besetzt ist, gehören diese Gerichte auf die
Bürotage — sonst entsteht genau die Reibung, an der die Entscheidung dann doch zugunsten
der Kantine kippt.

Portionen von 800–900 kcal sind groß: 600–700 g. Meal-Prep-Boxen mit 800 ml Volumen
einplanen, kleinere laufen über.

---

## 6. Regeln

Die vollständigen Regeltexte stehen in `data/training.json` und werden im Tab **Training**
angezeigt: Plateau-Regel, Waage-Regel, Protein-nach-16-Uhr-Regel, Refeed-Regel,
Rückfallregel und Schlaf-Regel.

Die wichtigste in Kurzform:

> **Plateau-Regel** — Erst 14 Tage Stillstand im 7-Tage-Schnitt sind ein Plateau. Dann
> genau EINE Stellschraube um 150 kcal drehen, nicht drei gleichzeitig. Wer mehreres
> ändert, weiß hinterher nicht, was gewirkt hat.

---

## 7. Kein medizinischer Rat

Dieses Repo ist ein Rechen- und Planungswerkzeug, keine ärztliche Beratung. Ein Defizit
von 1.000 kcal pro Tag in Kombination mit sechs Trainingseinheiten pro Woche ist
ambitioniert. Vor dem Start gehört ein Check beim Hausarzt dazu, insbesondere bei
Vorerkrankungen, Medikamenteneinnahme oder orthopädischer Vorgeschichte.
