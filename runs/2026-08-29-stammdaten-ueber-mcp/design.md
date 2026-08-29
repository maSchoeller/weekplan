# Entwurf — Stammdaten über MCP

Grundlage: `requirements.md` dieses Laufs, `docs/architecture.md`,
`design-system.md`. Die Leitplanke „nicht zu kompliziert" entscheidet jeden
Zweifelsfall — im Zweifel weglassen, und alles Neue in der Form, die es schon
gibt.

## UX

### Der Starthinweis

Heute gibt es genau einen Hinweis dieser Art: im Wochenplan steht bei einem
Eintrag „**Geändert** — vorher 830 kcal, 52 g Protein" mit dem Knopf „Zur
Kenntnis". Der neue Hinweis ist derselbe Baustein, eine Ebene höher.

```
┌───────────────────────────────────────────────────────────┐
│ Plan geändert — deine Zielaufnahme liegt   [Zur Kenntnis] │
│ jetzt bei 2.190 statt 2.165 kcal am Tag.                  │
└───────────────────────────────────────────────────────────┘
```

Er sitzt im Layout über dem Seiteninhalt, gilt für jede Seite und verschwindet
auf Knopfdruck. Fläche `--warn-soft`, Text `--warn`, Radius
`--radius-control`, Polster `--sp-2 --sp-3` — identisch zu `.abweichung` in
`Wochenplan.razor.css`. Das Wort „geändert" trägt die Aussage, nicht die Farbe;
so verlangt es das Design-System.

**Wann er erscheint:** wenn die im Hintergrund nachgeladenen Trainingsdaten
sich von den zwischengespeicherten unterscheiden **und** sich dadurch die
Zielaufnahme verschiebt. Nur dann.

**Wann nicht:**

| Fall | Warum kein Hinweis |
|---|---|
| Kaltstart ohne Zwischenspeicher | Es gibt nichts zu vergleichen. |
| Grundstock oder Abteilungen geändert | Rechnet nicht rückwirkend — wäre Lärm. |
| Nur ein Phasenname oder Beschreibungstext geändert | Keine Zahl bewegt sich. |
| Nicht angemeldet | Ohne Gewicht keine Zielaufnahme. Bewusste Grenze. |

Der Hinweis nennt **eine Zahl, nicht einen Unterschied**. Ein vollständiges
Gegenüberstellen zweier Trainingsdokumente wäre die Änderungshistorie, die in
den Anforderungen ausdrücklich Nicht-Ziel ist.

### Das Gerichte-Feld im Wochenplan

Aus dem Filter wird eine Gruppierung im vorhandenen `<select>` — nativ, ohne
neue Komponente, ohne Tastaturarbeit:

```
+ Gericht wählen
├─ Für den Abend ──────────────────
│   Vesper, protein-optimiert (615 kcal)
│   Rührei mit Hüttenkäse (650 kcal)
└─ Andere Gerichte ────────────────
    Chili sin Carne (820 kcal)
    Linsen-Dal mit Reis (820 kcal)
```

Der Default bleibt sichtbar oben, alles andere ist erreichbar. Innerhalb jeder
Gruppe nach Namen sortiert wie bisher.

### Das Merkmal „vorkochbar"

Auf der Rezeptliste und der Kochseite als Chip neben dem vorhandenen
„kalt ok", gleiche Form, `--surface-2` und `--text-dim`:

```
  Chili sin Carne        820 kcal · 62 g · 40 min   [kalt ok] [vorkochbar]
```

Ein Wort, kein Symbol allein — dieselbe Regel wie beim Statusfarben-Verbot.

## Architektur

Fortschreibung von `docs/architecture.md`: der Abschnitt „Rezepte, Training,
Grundstock" sagt heute „schreibbar sind nur Rezepte". Das gilt nach diesem Lauf
nicht mehr; die Datei wird entsprechend geändert.

### Die Werkzeuge

Bisher acht, künftig elf. Aufgeteilt auf zwei Typen, weil `Rezeptwerkzeuge` mit
elf Werkzeugen kein Rezepttyp mehr wäre:

| Typ | Werkzeuge |
|---|---|
| `Rezeptwerkzeuge` | `rezepte_auflisten`, `rezept_lesen`, `rezept_anlegen`, `rezept_aendern`, `rezept_loeschen` |
| `Planwerkzeuge` (neu) | `abteilungen_lesen`, `abteilungen_schreiben`, `grundstock_lesen`, `grundstock_schreiben`, `training_lesen`, `training_schreiben` |

Die drei Lesewerkzeuge ziehen aus `Rezeptwerkzeuge` um. Jedes Schreibwerkzeug
**ersetzt sein Dokument vollständig** — genau wie `rezept_aendern`, und aus
demselben Grund: keine Patch-Sprache, keine Konfliktauflösung, keine zweite
Denkweise. Die Werkzeugbeschreibung sagt darum „vorher lesen, damit nichts
verlorengeht", wie es die Rezeptbeschreibung heute schon tut.

### Der Schreibschutz des Regelwerks ist ein Typ, keine Disziplin

`training_schreiben` nimmt **nicht** `Trainingsdaten`, sondern einen neuen
`Trainingsentwurf` — dieselben Felder **ohne** `Regeln`. Der Dienst setzt die
vorhandenen Regeln beim Schreiben wieder ein.

Damit ist „das Regelwerk bleibt lesend" nicht formulierbar zu umgehen: es gibt
keinen Weg, Regeln zu übergeben. Preis: ein zweiter Typ neben `Trainingsdaten`
und eine Zeile, die die alten Regeln zurückholt. Das ist billiger als jede
Prüfregel, die dasselbe nur behauptet.

`abteilungen_schreiben` bekommt analog einen `Abteilungsentwurf`
(Hinweis + Liste).

### Prüfregeln

`Pruefung.cs` wird zu `Rezeptpruefung.cs` und `Planpruefung.cs` — eine Datei
mit vier Validierern wäre nicht mehr überschaubar. Die Absage bleibt in ihrer
Form: alle Verstöße auf einmal, erlaubte Werte genannt.

**Training**

- Mindestens eine Phase; jede mit Kennung, Namen und `defizitZiel >= 0`.
- Jede Einheit nennt einen MET-Typ, den es in `metWerte` gibt, und `min > 0`.
- **`met >= 1` für jeden MET-Wert.** Die Formel aus `docs/plan.md` §1 lautet
  `(MET − 1) × 1,05 × kg × min/60`. Bei einem Wert unter 1 wird der Verbrauch
  negativ, der Gesamtumsatz sinkt und die Zielaufnahme fällt still — genau der
  Albtraum „Zahlen laufen still weg" aus den Anforderungen, und für eine Zeile
  Prüfcode zu verhindern.
- Kraftplan und Hinweis dürfen nicht leer sein.

**Grundstock** — jede Gruppe mit Namen, jeder Artikel mit Namen und Menge.

**Abteilungen** — mindestens eine, keine Leerstrings, keine Doppelten.

### Die Sammelabteilung

Wird eine Abteilung entfernt, in der noch Zutaten stehen, lehnt
`abteilungen_schreiben` **nicht** ab, sondern räumt auf:

1. Alle Rezepte lesen.
2. Zutaten, deren Abteilung es nicht mehr gibt, auf **„Sonstiges"** umschreiben.
3. „Sonstiges" ans **Ende** der Abteilungsliste hängen, falls es gebraucht wird
   und noch fehlt — ans Ende, weil die Reihenfolge der Weg durch den Laden ist
   und Unsortiertes zuletzt kommt.
4. Nur die betroffenen Rezepte zurückschreiben.
5. Die Antwort nennt die Zahl: „3 Zutaten in 2 Rezepten nach Sonstiges
   verschoben."

Preis: eine Schreiboperation mit Nebenwirkung auf andere Dokumente, und sie ist
nicht atomar — bricht sie in der Mitte ab, stehen Abteilungen und Rezepte
kurzzeitig auseinander. Das ist hinnehmbar, weil ein Rezept mit unbekannter
Abteilung nur auf der Einkaufsliste ganz unten landet und nichts kaputt geht.
Der Preis steht in `debt.md`.

Die Rückmeldung ist der eigentliche Schutz: der Nutzer erfährt im selben Atemzug,
was seine Änderung angerichtet hat.

### Das Merkmal `Prep`

`bool Prep` in `Rezept` und `Rezeptentwurf`, hinter `Kalt` — dieselbe Form,
dieselbe Stelle. Keine Prüfregel: ein bool kann nicht falsch sein.

`AutomatischFuellen` in `Weekplan.Core.Wochenplanung` **filtert** an Werktagen
auf Gerichte mit `Prep`; gibt es keine, bleibt die volle Auswahl.

> **Abweichung während der Umsetzung.** Geplant war ein Aufschlag in der
> Bewertungsfunktion. Der geriet gegen den vorhandenen Aufschlag für
> Wiederholung (250 Punkte): unter dessen Höhe zog das Füllen lieber ein
> frisches Gericht in die Woche, als dieselbe Box zweimal aufzutischen; darüber
> kippte es je nach Wochentag mal so und mal so, weil Wiederholung sich
> aufsummiert. Zwei Zahlen gegeneinander zu tarieren hätte niemand mehr
> vorhersagen können. Der Filter trifft die Absicht direkter — und der Alltag
> steht ohnehin auf zwei Sorten für je zwei bis drei Tage, dort ist
> Wiederholung der Normalfall und kein Makel.

**Keine Migration.** Bestehende Dokumente ohne das Feld werden zu `false`
deserialisiert. Das ist für den heutigen Bestand sachlich falsch — das Chili ist
vorkochbar —, aber der Bestand wird im Folgelauf ohnehin vollständig ersetzt.
Ein Migrationsschritt für Daten, die nächste Woche weg sind, ist Arbeit für den
Papierkorb. Steht in `debt.md`.

### Die Erkennung im Client

`Stammdatenlader` hält in `NachfragenAsync` bereits beide Stände in der Hand —
den zwischengespeicherten und den frischen. Genau dort entsteht der Hinweis, und
nirgends sonst: der Lader merkt sich beim Auffrischen die **vorherigen**
Trainingsdaten und gibt sie mit dem `Aufgefrischt`-Ereignis heraus.

Die neue Komponente `Planhinweis` im Layout rechnet mit dem vorhandenen
`Weekplan.Core.Rechnen`-Slice zweimal dieselbe Bilanz — einmal mit den alten,
einmal mit den neuen Trainingsdaten — und zeigt sich nur, wenn die Zielaufnahme
sich unterscheidet. Kein neuer Rechenweg, keine zweite Wahrheit.

### Was sich nicht ändert

- `GET /stammdaten` bleibt, wie es ist: alles auf einmal, öffentlich, mit ETag.
  Die neuen Felder reisen einfach mit.
- Der MCP-Schlüssel bleibt der einzige Schreibweg, die Mengenbegrenzung bleibt,
  jede Schreiboperation bleibt eine Logzeile.
- Das Tagebuch bleibt für MCP unerreichbar — es gibt kein Werkzeug dafür, und es
  entsteht auch keines.

## Entscheidungen und ihre Preise

| Entscheidung | Gewonnen | Bezahlt |
|---|---|---|
| Ganzdokument ersetzen statt Teiländerungen | Eine Form für alles, keine Konfliktlogik | Wer eine Minute ändern will, schickt alles zurück |
| Regelwerk über einen eigenen Typ aussperren | Schutz, der nicht zu umgehen ist | Ein zweiter Typ, eine Zeile Rückholen |
| Sammelabteilung statt Ablehnen | Nichts geht kaputt, nichts blockiert | Schreiboperation mit Nebenwirkung, nicht atomar |
| Hinweis nur fürs Training, nur bei Zahlenänderung | Kein Lärm, ein Satz | Ein geänderter Wochentag ohne kcal-Wirkung bleibt stumm |
| `Prep` ohne Migration | Keine Wegwerfarbeit | Bis zum Poollauf gilt jedes Gericht als nicht vorkochbar |
| `RezeptUngueltigException` → `StammdatenUngueltigException` | Ein Begriff für einen Vorgang | Berührt Prüfung, Dienst, Werkzeuge und Tests |
| `<optgroup>` statt eigener Auswahl | Nativ, tastaturfest, null CSS | Keine Suchfunktion im Feld |

## Schnitte für die Umsetzung

Ein Lauf, eine Anforderungsrunde — die Teilung betrifft nur die Lieferung:

**Schnitt A — der Schreibweg.** `Planwerkzeuge`, `Trainingsentwurf`,
`Abteilungsentwurf`, `Planpruefung`, Sammelabteilung, die drei
`IStammdaten`-Methoden, Umbenennung der Ausnahme. Danach ist der Grundstock
erreichbar und der Folgelauf entsperrt.

**Schnitt B — was die App zeigt.** `Prep` durch Modell, Autofill und
Oberfläche, Wochenplan-Gruppierung, `Planhinweis`.

## Tests

- `Planpruefung`: MET unter 1 wird abgelehnt; unbekannter MET-Typ in einer
  Einheit wird abgelehnt; die Absage nennt alle Verstöße auf einmal.
- `training_schreiben` kann die Regeln nicht ändern — sie stehen nach dem
  Schreiben unverändert da.
- Entfernte Abteilung: betroffene Zutaten stehen danach unter „Sonstiges",
  „Sonstiges" steht am Ende der Liste, unbeteiligte Rezepte sind unberührt.
- `AutomatischFuellen` nimmt werktags vorkochbare Gerichte, solange es welche
  gibt, und füllt weiter, wenn nicht.
- `Stammdatenlader` meldet eine Änderung nur, wenn ein Zwischenspeicher vorlag
  und sich die Trainingsdaten unterscheiden.
