# Entwurf — Gäste und Füllregeln

Grundlage: `requirements.md` desselben Laufs, `design-system.md`,
`docs/architecture.md`, `docs/ernaehrungsplan.md`.

---

## 1. Die eine Idee

**Portionen bekommen zwei Bedeutungen und je einen eigenen Ort.**

| Zahl | Wo sie steht | Wofür sie zählt |
|---|---|---|
| **Meine Portionen** | am Gericht, wie bisher (`PlanEintrag.Portionen`) | Tagesbilanz, Grün-Markierung, Zielabgleich |
| **Zusätzliche Esser** | am Tag bzw. an der Mahlzeit (neu) | Einkaufsliste, Kochmenge, Kochseite |

**Kochportionen = meine Portionen + zusätzliche Esser.** Eine Formel, eine
Stelle, überall dieselbe. Die Bilanz kennt die zweite Zahl gar nicht — sie kann
sie deshalb auch nicht verfälschen. Das ist Abnahmekriterium 3, gebaut statt
geprüft.

---

## 2. UX

### 2.1 Wochenplan — Tageskopf

Ruhe im Normalfall, Deutlichkeit im Besuchsfall.

```
OHNE GAESTE                          MIT 2 GAESTEN
┌────────────────────────────┐   ┌────────────────────────────┐
│ Samstag · Refeed           │   │ Samstag · Refeed           │
│ —              [+ Gäste]   │   │ —      Gäste [−] 2 [+]     │
│ 2.402 / 3.332 kcal         │   │ 2.402 / 3.332 kcal         │
└────────────────────────────┘   └────────────────────────────┘
```

- Bei 0 Gästen steht ein stiller Textknopf **„+ Gäste"** rechts im Tageskopf.
  Ein Tipp setzt 1 und macht daraus den Stepper. Ein zweiter Tipp auf `+` macht
  2 — **Abnahmekriterium 2 erfüllt** (höchstens zwei Berührungen).
- Der Stepper geht 0–8. Bei `−` auf 0 klappt er zurück zum stillen Knopf; die
  Mahlzeit-Ausnahmen dieses Tages fallen dabei mit weg (sonst bliebe unsichtbarer
  Zustand liegen).
- Beide Knöpfe sind 44 × 44 px, die Zahl dazwischen `tabular-nums`.

### 2.2 Wochenplan — Mahlzeit

Die Mahlzeitzeile trägt die Ausnahme, aber **nur wenn der Tag Gäste hat**. Ohne
Besuch sieht die Woche exakt aus wie heute.

```
┌──────────────────────────────────┐
│ Freitag           Gäste [−] 2 [+]│
│                                  │
│ Frühstück      0 Gäste [−][+] ↺  │   ← Ausnahme gesetzt
│   Skyr-Bowl          [1] [Weg]   │
│   693 kcal · 34 g                │
│                                  │
│ Mittag  · 3 kochen    [−] 2 [+]  │   ← folgt dem Tag
│   Linsen-Dal         [1] [Weg]   │
│   823 kcal · 62 g · 3 kochen     │
└──────────────────────────────────┘
```

- `· N kochen` steht neben der Mahlzeit-Überschrift, sobald dort Gäste wirken.
  Es steht auch an jedem Gericht in der Werte-Zeile — dort, wo man beim Kochen
  hinschaut. **Abnahmekriterium 5.**
- Der Stepper an der Mahlzeit löst sie beim ersten Gebrauch vom Tag. Dann
  erscheint `↺` („wieder wie am Tag"), ein Knopf mit sichtbarem Text für
  Bildschirmleser (`aria-label="Frühstück wieder wie am Tag"`).
- Ohne Ausnahme zeigt die Mahlzeit die Tageszahl, aber grau — sie ist
  abgeleitet, nicht gesetzt.

### 2.3 Wochenplan — Gericht wird anklickbar

Der Gerichtname ist heute ein `<span>`. Er wird ein `<a>` auf
`rezepte/{id}?portionen={kochportionen}`. Das ist der Weg zu
**Abnahmekriterium 6** und nebenbei die fehlende Brücke von der Planung zum
Kochen.

### 2.4 Kochseite

`/rezepte/{Id}` nimmt `?portionen=N` entgegen und stellt den vorhandenen
Portionsrechner darauf ein. Ohne den Parameter bleibt alles wie heute (1). Unter
dem Feld steht dann eine Zeile: *„Aus dem Wochenplan: 3 Portionen — du und
2 Gäste."* Damit ist die Zahl erklärt und nicht bloß gesetzt.

Kein neuer Zustand, keine Kopplung an Tag und Mahlzeit: die Seite bekommt eine
Zahl, sonst nichts. Preis siehe §5.

### 2.5 Einkauf

Der Hinweis über der Wochenliste bekommt einen Satz, wenn Gästeportionen
enthalten sind: *„Darin 7 Gästeportionen."* Damit steht keine erhöhte Menge
ohne Begründung da.

### 2.6 Zustände

| Zustand | Was passiert |
|---|---|
| Leer (keine Gerichte) | wie heute; der Gäste-Knopf steht trotzdem, Besuch plant man vor den Gerichten |
| Laden | unverändert — die Woche kommt in einem Zug |
| Fehler beim Schreiben | unverändert, der vorhandene Zustandsstreifen |
| Kein Gericht im Pool passt zur Regel | gefüllt wird trotzdem, siehe §4.4 |

### 2.7 Tastatur

Beide Stepper sind echte `<button>`. Tab-Reihenfolge im Tag: Gäste-Stepper →
Mahlzeit 1 (Stepper, Gericht-Link, Portionen, Entfernen, Auswahl) → Mahlzeit 2 →
Mahlzeit 3. Fokus wie im Design-System, 2 px `--accent`.

---

## 3. Datenmodell

### 3.1 Gäste am Wochenstand

`WochenStand` bekommt zwei optionale Sammlungen:

```csharp
public sealed record WochenStand(
    … Plan, RefeedTag, Rotation, HakenWoche, HakenGrundstock,
    IReadOnlyDictionary<string, int>? GaesteTag = null,        // "Sa" → 2
    IReadOnlyDictionary<string, int>? GaesteMahlzeit = null)   // "Sa|fruehstueck" → 0
{
    public int Gaeste(string tag, string mahlzeit);            // Ausnahme, sonst Tag, sonst 0
    public int Gaesteportionen(string tag, string mahlzeit);   // Gaeste × Zahl der Einträge
}
```

**Zwei Sammlungen statt einer** mit gemischten Schlüsseln: „gilt für den Tag" und
„gilt für diese Mahlzeit" sind verschiedene Aussagen, und die Ausnahme muss von
„nicht gesetzt" unterscheidbar bleiben — eine 0 an der Mahlzeit ist etwas
anderes als keine Angabe. Zwei Wörterbücher sagen das ohne Sonderwert.

**Optional mit `null`-Vorgabe**, weil in Cosmos Wochendokumente ohne diese Felder
liegen. `System.Text.Json` lässt sie dann aus, der Konstruktor setzt `null`, die
Lesemethoden behandeln `null` wie leer. Kein Migrationsschritt, kein Umschreiben
gespeicherter Wochen.

**`Gaeste` ist eine Methode auf dem Vertragstyp**, nicht auf dem Slice: sie liest
nur die eigenen Felder des Records, und Client wie Wochenplanung brauchen sie
beide. Sie über `IWochenplanung` zu führen hieße, für eine Nachschlagetabelle
einen Dienst zu injizieren.

### 3.2 Neue Merkmale am Rezept

`Rezept` und `Rezeptentwurf` bekommen je zwei `bool` **am Ende, mit Vorgabe
`false`**:

```csharp
bool Wochenende = false,   // frisch gekocht, gehört auf Sa/So
bool Refeed = false        // trägt die Refeed-Aufnahme
```

Vorgabe `false` heißt: alle 18 gespeicherten Rezepte laden weiter, alle
vorhandenen Aufrufstellen kompilieren weiter, und bis die Merkmale gepflegt sind
greift der Rückfall aus §4.4. Die MCP-Werkzeuge `rezept_anlegen` und
`rezept_aendern` nehmen `Rezeptentwurf` als Ganzes — sie erben die Felder ohne
Änderung an ihrer Signatur.

`Rezeptpruefung` bekommt **keine** neue Regel. Ein Gericht, das weder Werktag
noch Wochenende noch Refeed taugt, ist kein Datenfehler, sondern ein Gericht,
das man von Hand einplant.

---

## 4. Die Füllregeln

`AutomatischFuellen` wird umgebaut. Bisher: eine Bewertungsfunktion, die alles
gegeneinander tariert. Neu: **erst filtern, dann bewerten** — dieselbe Bauform,
die `Vorkochbar` schon trägt, und die Lehre aus dem Retro vom 29.08. („Zwei
Zahlen gegeneinander zu tarieren ist keine Regel, sondern ein Rätsel").

### 4.1 Die Art des Tages

```
Refeed-Tag        → tag.Kuerzel == woche.RefeedTag        (gewinnt immer)
Wochenende        → Sa oder So
Werktag           → alles Übrige
```

Der Refeed gewinnt gegen alles — entschieden in `requirements.md` §8.

### 4.2 Die Auswahl je Art

| Art | Filter auf den Pool |
|---|---|
| Werktag | `Prep` |
| Wochenende | `Wochenende` |
| Refeed | `Refeed` |

Jeder Filter mit Rückfall auf die volle Auswahl, wenn er leer läuft (§4.4).

### 4.3 Die Blöcke

Die Werktage in ihrer Reihenfolge (Mo…Fr, ohne einen dort liegenden Refeed-Tag)
werden in **zusammenhängende Blöcke von zwei oder drei Tagen** zerlegt:

| Werktage | Aufteilung |
|---|---|
| 5 | `3+2` oder `2+3` — die Rotation entscheidet |
| 4 | `2+2` |
| 3 | `3` |
| 2 | `2` |
| 1 | `1` |
| 0 | — |

Mittag und Abend bekommen **je Block ein Gericht**, und Block 2 ein anderes als
Block 1. Damit stehen Mo–Fr genau zwei Mittage und zwei Abende —
**Abnahmekriterium 8a**.

Wochenende und Refeed werden **je Tag einzeln** gewählt, jedes Gericht nur
einmal je Füllung.

Das **Frühstück rotiert täglich**: `fruehstueck[(i + rotation) % n]` — unverändert
zu heute, und damit an aufeinanderfolgenden Tagen verschieden
(**Abnahmekriterium 8e**).

### 4.4 Rückfall — nie leer lassen

Ein Filter, der nichts übrig lässt, gibt die Eingabe zurück:

```
Gefiltert(auswahl, merkmal) = auswahl.Where(merkmal) ist leer ? auswahl : gefiltert
```

Damit füllt die App auch dann, wenn kein einziges Wochenendgericht gepflegt ist
— sie nimmt die volle Auswahl. **Abnahmekriterium 9.** Genau das trägt den
Zustand direkt nach dem Ausrollen, solange die Merkmale noch nicht gesetzt sind.

### 4.5 Nochmal drücken

Kein Zufall, kein Zeitgeber, und **kein Blick auf die bisherige Woche** — die
wird überbügelt, nicht ausgewertet (siehe §5, Anmerkung des Nutzers vom
29.08.2026). Es bleibt allein die **Rotation**, und sie wirkt an drei Stellen:

1. **Frühstück:** `fruehstueck[(i + rotation) % n]` — wie heute.
2. **Blockaufteilung:** bei fünf Werktagen `3+2` bei gerader, `2+3` bei
   ungerader Rotation.
3. **Rang der Auswahl:** je Block werden alle Paare (Mittag, Abend) bewertet und
   sortiert; genommen wird nicht das beste, sondern das
   `rotation % 3`-beste. Alle drei stammen aus derselben gefilterten Auswahl,
   sind also gleich regelkonform — sie treffen das Kalorienziel nur
   unterschiedlich genau.

Nochmal drücken liefert damit eine andere, gleich richtige Woche
(**Abnahmekriterium 10**), ohne dass ein Zustand mehr gelesen wird als die eine
Zahl `Rotation`.

### 4.6 Die Bewertung, was von ihr bleibt

Innerhalb der gefilterten Auswahl bleibt die vorhandene Bewertung:
Kalorienabstand einfach, fehlendes Protein × 12, große Portionen × 20. **Der
Aufschlag für Wiederholung fällt weg** — Wiederholung ist jetzt Struktur, nicht
Makel, und wird von den Blöcken erzeugt, nicht bestraft.

Gewählt wird je Block: über alle Paare (Mittag, Abend) der gefilterten Auswahl
die Summe der Tagesbewertungen des Blocks; die Portionen werden **je Tag** frei
gewählt (1–2 je Mahlzeit), weil das Frühstück innerhalb des Blocks wechselt.

Aufwand im schlimmsten Fall: 8 Mittage × 7 Abende × 3 Tage × 8 Portionsbelegungen
≈ 1.300 Auswertungen je Block, zwei Blöcke. Läuft im Browser unter einer
Millisekunde.

---

## 5. Entscheidungen und ihr Preis

| Entscheidung | Preis |
|---|---|
| **Gäste im `WochenStand`, nicht im Profil.** Sie gehören zur geplanten Woche, werden mit ihr geschrieben und mit ihr gelesen. | Eine neue Woche erbt die Gäste der alten — es gibt nur eine Woche. Wer sie nicht mehr braucht, stellt sie zurück. Bewusst: „Gäste bleiben stehen" ist die Entscheidung aus den Anforderungen. |
| **Zwei Wörterbücher statt Sonderwert.** | Zwei Felder im Dokument statt einem. Dafür ist „Ausnahme 0" von „nicht gesetzt" unterscheidbar. |
| **Kochseite bekommt nur eine Zahl (`?portionen=`), nicht Tag und Mahlzeit.** | Ein gespeicherter Link trägt eine veraltete Zahl. Dafür bleibt die Kochseite frei vom Wochenplan-Modell, und es entsteht keine zweite Stelle, die Kochportionen rechnet. |
| **Gästezahl gilt für jeden Eintrag der Mahlzeit.** | Stehen zwei Gerichte auf einer Mahlzeit, wird beides für alle gekocht. Richtig gedacht („die Gäste essen, was ich esse"), aber wer einem Gast nur eines vorsetzt, kauft zu viel. |
| **Filter mit Rückfall statt Strafpunkten.** | Ein halb gepflegter Pool füllt still mit der vollen Auswahl — ohne Hinweis, dass eine Regel nicht greifen konnte. Siehe `debt.md`. |
| **Blöcke schlagen die Tagesgenauigkeit.** Ein Block-Gericht muss zwei bis drei Tagen gerecht werden. | Einzelne Tage landen weiter vom Ziel als bisher; das Grün wird seltener. Das ist der Preis für vorkochbare Wochen und war die ausdrückliche Bitte. |
| **Neue Merkmale mit Vorgabe `false`.** | Kein Migrationsschritt — dafür ist der Pool nach dem Ausrollen kurz „unmarkiert" und der Rückfall trägt. Die Pflege der 18 Rezepte passiert **nach dem Ausrollen**, freigegeben vom Nutzer am 29.08.2026. |

### Nachtrag vom 29.08.2026 — überbügeln statt schonen

Der Nutzer hat in der Abnahme dieses Entwurfs eine **allgemeine und künftige**
Regel gesetzt: gespeicherte Nutzerdaten dürfen beim Erweitern des Modells
überbügelt werden, statt dass Logik sie schont. Es gibt einen Nutzer und eine
Woche; eine verlorene Wochenplanung ist neu geklickt, eine Migrationsschicht
bleibt für immer.

Zwei Stellen dieses Entwurfs sind daraufhin einfacher geworden:

- §4.5 wertet die bisherige Woche **nicht** mehr aus. Die Rotation allein trägt
  die Abwechslung.
- Die `null`-Vorgabe der beiden Gäste-Sammlungen bleibt — sie ist kein
  Migrationsschritt, sondern die zwei Zeilen, die einen Absturz beim Lesen alter
  Dokumente verhindern. Weniger geht nicht, mehr wird nicht gebaut.

---

## 6. Was wo geändert wird

| Datei | Änderung |
|---|---|
| `Tagebuch.Contracts/Modell.cs` | `WochenStand` um `GaesteTag`, `GaesteMahlzeit`, `Gaeste()`, `Gaesteportionen()` |
| `Stammdaten.Contracts/Modell.cs` | `Rezept` um `Wochenende`, `Refeed` |
| `Stammdaten.Contracts/Rezeptentwurf.cs` | `Rezeptentwurf` um dieselben zwei |
| `Stammdaten/Stammdatendienst.cs` | die zwei Felder beim Bauen des `Rezept` durchreichen |
| `Wochenplanung.Contracts/Modell.cs` | `Einkaufsliste` um `Gaesteportionen` |
| `Wochenplanung/Wochenplanung.cs` | `Einkaufsliste` rechnet Gäste mit; `AutomatischFuellen` neu nach §4 |
| `Client/Pages/Wochenplan.razor` (+ `.css`) | Gäste-Stepper am Tag und an der Mahlzeit, „N kochen", Gericht als Link |
| `Client/Pages/Kochseite.razor` | `?portionen=` entgegennehmen, Herkunftszeile |
| `Client/Pages/Einkauf.razor` | Satz über Gästeportionen |
| `Client/Dienste/Zustand.cs` | `GaesteTagSetzen`, `GaesteMahlzeitSetzen`, `GaesteMahlzeitZuruecksetzen`, `Kochportionen` |
| `docs/architecture.md` | Fortschreibung |
| `README.md` | nutzersichtbare Änderung |
| `tests/Weekplan.Core.Tests/WochenplanungTests.cs` | Regeln 8a–8e, 9, 10, Gäste in der Einkaufsliste, Bilanz unberührt |

**Kein neuer Slice, keine neue Schnittstelle, kein neues Projekt.** Das Feature
passt vollständig in die vorhandene Naht: Gäste sind Tagebuchdaten, Regeln sind
Wochenplanung, Merkmale sind Stammdaten.

---

## 7. Prüfung

| Kriterium | Wie geprüft |
|---|---|
| 1, 2, 5 | Smoketest im Browser bei 375 px |
| 3 | Test: `Tagessumme` ist mit und ohne Gäste identisch |
| 4 | Test: Einkaufsliste mit 2 Gästen = dreifache Menge bei einer Portion |
| 6 | Smoketest: aus dem Wochenplan geöffnet, Feld steht auf der Gesamtzahl |
| 7 | Test: `AutomatischFuellen` und Leeren lassen `GaesteTag` unverändert |
| 8a–e | Test je Regel gegen einen Pool mit gesetzten Merkmalen |
| 9 | Test: Pool ohne ein einziges Wochenendgericht füllt trotzdem |
| 10 | Test: zweimal füllen ergibt andere Gerichte, 8a–e halten weiter |
| 11 | Pflege der 18 Rezepte über MCP, danach eine echte Füllung im Smoketest |
