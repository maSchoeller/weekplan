# Entwurf — Rezepte aus der Datenbank

## Teil 1 — Oberflaeche

### Die drei Bildschirme

**`/rezepte` — Uebersicht.** Ersetzt die heutige Kartenwand aus 24 Karten. Nach Mahlzeit
gruppiert, je Rezept eine Zeile, die als Ganzes ein Link ist. Keine Zutaten,
keine Schritte, kein Portionsfeld — die Uebersicht beantwortet nur „welches
Gericht nehme ich".

```
Rezepte
Alle Grammangaben pro Portion.

FRUEHSTUECK
┌──────────────────────────────────────────────┐
│ Overnight Oats mit Beeren                  › │
│ 512 kcal · 38 g Protein · 5 min · kalt       │
├──────────────────────────────────────────────┤
│ Rührei mit Vollkornbrot                    › │
│ 486 kcal · 34 g Protein · 12 min             │
└──────────────────────────────────────────────┘

MITTAG
┌──────────────────────────────────────────────┐
│ Chili sin Carne                            › │
│ 829 kcal · 52 g Protein · 40 min · kalt      │
└──────────────────────────────────────────────┘
```

Zeilenhoehe mindestens 44 px, ganze Zeile klickbar, Fokusring auf der Zeile.
`tabular-nums`, damit die Zahlenspalte beim Ueberfliegen nicht tanzt. „kalt"
steht als Wort, nicht als Farbe oder Symbol.

**`/rezepte/{id}` — Kochseite.** Reihenfolge folgt dem Ablauf: einstellen,
bereitlegen, kochen.

```
‹ Rezepte

Chili sin Carne
829 kcal · 52 g Protein · 40 min · kalt zu essen

Portionen [ 1 ]                      ← Hauptaktion, ohne Scrollen erreichbar

ZUTATEN
Kidneybohnen (Dose, abgetropft)              150 g
Passierte Tomaten                            180 g
…

ZUBEREITUNG
## Vorbereitung
Zwiebel und Karotte fein würfeln. **Räuchertofu** mit den
Händen grob zerbröseln – das ergibt die Hackstruktur.

## Am Herd
1. Öl erhitzen, Zwiebel und Karotte 5 Minuten anbraten,
   bis die Zwiebel glasig ist.
…
```

Der Anleitungstext bekommt `max-width: 62ch` wie der bestehende `.hinweis` —
laengerer Fliesstext ist der Grund, warum die Detailseite ueberhaupt existiert.
Zwischenueberschriften der Anleitung rendern als `h3`/`h4` unterhalb des
`h2 Zubereitung`, damit die Dokumentgliederung nicht springt. Tabellen scrollen
in ihrem eigenen Behaelter, nie die Seite.

Zurueck fuehrt ein Link `‹ Rezepte` oben links, nicht nur die Browsertaste —
am Handy ist er der einzige sichtbare Weg. `FocusOnNavigate` setzt den Fokus
ohnehin auf `h1`.

**`/` — Wochenplan, zwei neue Zustaende.** Ein Gericht, dessen Rezept sich seit
dem Planen geaendert hat, und eines, dessen Rezept es nicht mehr gibt:

```
Donnerstag · Büro                    ⚠ 1 Rezept geändert
1.842 / 1.900 kcal · 128 / 140 g Protein

MITTAG
  Chili sin Carne          [ 1 ]  Entfernen
  829 kcal · 52 g Protein
  ⚠ Geändert — vorher 760 kcal, 48 g   [ Zur Kenntnis ]

ABEND
  Linsen-Bolognese — entfernt      [ 1 ]  Entfernen
  Dieses Rezept gibt es nicht mehr.
  [ + Gericht wählen ]
```

Beide Hinweise tragen ein **Wort**, nicht nur die Warnfarbe (`--warn`,
`--warn-soft`) — Regel des Design-Systems. Der Tageskopf traegt den Vermerk
zusaetzlich, damit man beim Durchscrollen sieht, welcher der sieben Tage
betroffen ist. „Zur Kenntnis" schreibt die gemerkten Werte auf den aktuellen
Stand fort; die Rechnung selbst benutzt **immer** das aktuelle Rezept.

Der Name eines geloeschten Rezepts kommt aus seiner sprechenden Id
(`linsen-bolognese` → „Linsen-Bolognese"). Kein Feld waechst dafuer, und
`Wochenplan.razor` faellt heute schon auf die Id zurueck — es fehlt nur das Wort.

### Lade-, Leer- und Fehlerzustaende

| Lage | Was der Nutzer sieht |
|---|---|
| Zwischenspeicher da | Sofort die Rezepte. Im Hintergrund wird geprueft; kommt Neues, zeichnet die Seite still neu. |
| Kein Speicher, Server antwortet | „Wird geladen …" wie heute; beim Kaltstart einige Sekunden. |
| Kein Speicher, Server stumm | Die vorhandene Fehlerkarte des `MainLayout`: „Die Daten konnten nicht geladen werden" mit „Erneut versuchen". Nichts Halbes. |
| Kein Rezept in einer Kategorie | Die Gruppe entfaellt, wie heute. |
| Unbekannte Rezept-Id in der Adresse | `NotFound`-Seite des Routers, ergaenzt um einen Weg zurueck zur Uebersicht. |

## Teil 2 — Architektur

### Der neue Slice

```
Weekplan.Core.Stammdaten.Contracts   Rezept, Zutat, Trainingsdaten,
                                     Grundstockdaten, Stammdatensatz,
                                     IStammdaten, StammdatenFehlenException
Weekplan.Core.Stammdaten             Stammdatendienst, IAblage, Namen,
                                     DateiAblage, CosmosAblage
```

**Zu den Namen.** Der Sammeltyp heisst `Stammdatensatz` und die Umsetzung
`Stammdatendienst`, nicht beide `Stammdaten`: innerhalb von `Weekplan.Core.*`
gewinnt der gleichnamige Namensraum jede Namensaufloesung, und der Slice waere
sonst nur mit Vollqualifizierung benutzbar. Beim Tagebuch faellt das nicht auf,
weil dort kein Typ so heisst wie der Slice.

`Rezept` und `Zutat` ziehen von `Wochenplanung.Contracts` hierher;
`Wochenplanung.Contracts` referenziert kuenftig `Stammdaten.Contracts`. Die
Abhaengigkeit zeigt vom Rechner zum Datenhalter — richtig herum. Die Anzeigetypen
aus `Client/Daten/Stammdaten.cs` (Trainingsdaten, Grundstockdaten) ziehen
ebenfalls in die Contracts; der Client haelt keine Datenmodelle mehr.

`Rezept` bekommt `Anleitung` (Markdown) an Stelle von `Schritte`. Einen
Zeitstempel bekommt es **nicht**: der Hinweis im Wochenplan vergleicht die
gemerkten Zahlen mit den aktuellen und braucht dafuer kein Datum.

**Kategorien.** `Stammdaten.Contracts` fuehrt die erlaubten Werte
(`fruehstueck`, `mittag`, `abend`) als eigene Liste, weil die Pruefung sie
braucht und ein Zugriff auf `Woche.Mahlzeiten` einen Ringschluss ergaebe. Ein
Test haelt fest, dass beide Listen dieselben Schluessel fuehren.

### Ablage

```csharp
internal interface IAblage
{
    Task<T?> LesenAsync<T>(string art, string id, CancellationToken ct) where T : class;
    Task<IReadOnlyList<T>> AlleAsync<T>(string art, CancellationToken ct) where T : class;
    Task SchreibenAsync<T>(string art, string id, T inhalt, CancellationToken ct) where T : class;
}
```

Dieselbe Form wie `Tagebuch.IAblage`, nur mit `art` statt `nutzerId` als
Partition — und um `AlleAsync` erweitert, das das Tagebuch nicht braucht.
`LoeschenAsync` kommt erst mit Schnitt C dazu, wenn es den ersten Aufrufer hat. Zwei Umsetzungen, entschieden allein durch die Anwesenheit einer
Cosmos-Verbindung, exakt wie beim Tagebuch.

**Dokumente** im Behaelter `stammdaten`, Partitionsschluessel `/art`:

| art | id | Inhalt |
|---|---|---|
| `rezept` | `chili-sin-carne` | ein Rezept |
| `liste` | `training` | Trainingsdaten |
| `liste` | `grundstock` | Grundstockdaten |
| `liste` | `abteilungen` | Hinweis und Abteilungsliste |

Alle Rezepte lesen heisst: eine Abfrage **innerhalb einer Partition**. Der Start
liest zwei Partitionen. Huellenmuster (`id`, `art`, `inhalt`) wie in
`Tagebuch.CosmosAblage` — inklusive des dort dokumentierten Preises, dass man in
der Azure-Konsole eine Ebene tiefer liest.

### Pruefung beim Schreiben (Schnitt C)

Eine Stelle, `Pruefung.Pruefen(RezeptEingabe)`, wirft `StammdatenFehler` mit
einer Meldung, die die erlaubten Werte **aufzaehlt**:

- Name, Kategorie, Anleitung und mindestens eine Zutat sind Pflicht
- Kategorie aus der erlaubten Liste
- jede Zutat nennt eine Abteilung aus dem Dokument `abteilungen`
- `Kcal`, `Protein`, `ZeitMin` positiv; `G` oder `Stk` gesetzt
- Anleitung hoechstens 20 000 Zeichen
- Id wird aus dem Namen gebildet: klein, Umlaute aufgeloest, alles ausser
  `a–z`, `0–9` zu `-`. Anlegen bei vorhandener Id ist ein Fehler, Aendern bei
  fehlender Id ebenso.

`Vorrat` setzt der Aufrufer — kein Namensabgleich gegen den Grundstock, weil
„Olivenoel" und „Olivenoel (nativ extra)" sich nicht treffen und eine Automatik
dann still danebengreift.

### Weg zum Client

`GET /stammdaten`, oeffentlich, ohne Anmeldung, mengenbegrenzt (120/min, eigener
Topf neben der Anmeldung). Antwort ist das komplette `Stammdaten`-Objekt mit
einem starken ETag: SHA-256 ueber die serialisierte Antwort, gekuerzt. Der
Server haelt Antwort und ETag im Speicher und verwirft sie bei jedem Schreiben.
`If-None-Match` beantwortet er mit `304`.

Im Client:

```
Start → Zwischenspeicher lesen (localStorage) → sofort zeichnen
      → im Hintergrund GET mit If-None-Match
          200 → ersetzen, speichern, neu zeichnen
          304 → nichts
          Fehler → nichts, solange ein Speicher da ist
```

`Stammdatenlader` ruft nicht mehr die eigene Herkunft, sondern den Server;
`Program.cs` gibt ihm den Server-Client. Kein Nachsehen bei offener App.

### Pflege ueber Claude Code

`ModelContextProtocol.AspNetCore` 2.2.0, `app.MapMcp("/mcp")` im vorhandenen
Server. Ein Endpunktfilter prueft `Authorization: Bearer` gegen
`Mcp:Schluessel` in **fester Zeit**; fehlt der Schluessel in der Konfiguration,
wird `/mcp` gar nicht erst eingehaengt. Eigene Mengengrenze (30/min). Jede
Schreiboperation eine strukturierte Logzeile (Werkzeug, Rezept-Id, Ergebnis).

| Werkzeug | Wirkung |
|---|---|
| `rezepte_auflisten` | Id, Name, Kategorie, kcal, Protein, Zeit — ohne Anleitung |
| `rezept_lesen` | ein vollstaendiges Rezept |
| `rezept_anlegen` | neu; Id aus dem Namen, vorhandene Id ist ein Fehler |
| `rezept_aendern` | vorhandenes ersetzen; fehlende Id ist ein Fehler |
| `rezept_loeschen` | endgueltig |
| `abteilungen_lesen`, `grundstock_lesen`, `training_lesen` | nur lesen |

`.mcp.json` im Repo mit `${WEEKPLAN_MCP_SCHLUESSEL}` — die Adresse steht
versioniert, das Geheimnis nie.

### Erstbefuellung

`tools/Weekplan.Stammdaten` — ein Lauf, drei Schritte: die drei JSON-Dateien
aus `tools/Weekplan.Stammdaten/altbestand/` lesen, `schritte` zu einer nummerierten Markdown-Liste wandeln, Dokumente
schreiben. Danach **liest es jedes Dokument zurueck** und meldet je Rezept, ob
Name, Kategorie, kcal, Protein und jede einzelne Zutat uebereinstimmen. Ein
Unterschied ist ein Fehlschlag mit Rueckgabewert, keine Warnung im Vorbeigehen.

## Teil 3 — Schnitte

**A — Daten.** Slice, Contracts-Umzug, beide Ablagen, Behaelter, Werkzeug samt
Rueckvergleich, `GET /stammdaten` mit ETag, Client-Zwischenspeicher, Markdig und
Markdown-Anzeige in der heutigen Karte, Abbau der statischen App, Pages aus.
Danach laeuft die App vollstaendig aus der Datenbank.

**B — Ansicht.** `/rezepte` als Uebersicht, `/rezepte/{id}` als Kochseite,
mobil zuerst.

**C — Pflege.** `/mcp`, Schluessel, Werkzeuge, Protokoll, `.mcp.json` — und die
Folgen des Pflegens: `PlanEintrag` waechst um `KcalBeimPlanen` und
`ProteinBeimPlanen` (beide optional, damit gespeicherte Plaene ohne sie weiter
lesbar sind), Hinweis „geaendert" und Zustand „entfernt" im Wochenplan.

## Teil 4 — Entscheidungen und ihr Preis

| Entscheidung | Preis |
|---|---|
| Cosmos statt Postgres | Kein SQL, keine relationale Abfrage ueber Zutaten. Dafuer 0 EUR und kein neuer Baustein. |
| Zwei Ablagen hinter einer Naht | Eine Umsetzung, die in Produktion nie laeuft — dieselbe Schuld, die fuer das Tagebuch schon im Wurzel-`debt.md` steht. Dafuer bleibt `run-local` netzfrei. |
| Ein Dokument je Rezept | Der Start liest eine Abfrage statt eines Punktlesens. Dafuer kann keine Aenderung eine andere ueberschreiben. |
| Oeffentlicher Leseendpunkt | Die Rezepte sind fuer jeden lesbar, der die Adresse kennt. Bewusst: sie sind kein Geheimnis, und der Zugang ohne Anmeldung spart dem Client einen Anmeldezwang beim Blaettern. |
| `/mcp` am oeffentlichen Server | Ein Schreibweg aus dem Netz, den es bisher nicht gab. Abgesichert durch Schluessel, Mengengrenze und Protokoll — mehr nicht. |
| Markdig im Browser | Das WASM-Paket waechst einmalig. Dafuer bleibt der Server reiner Datenlieferant, und eingebettetes HTML ist abgeschaltet. |
| Rechnung nutzt immer das aktuelle Rezept | Die Bilanz eines abgehakten Tages kann sich aendern — aber sichtbar. Dafuer passen Zahlen und Zutaten der Einkaufsliste immer zusammen. |
| Kategorienliste doppelt | Drei Zeichenketten stehen an zwei Stellen. Ein Test haelt sie zusammen; ein Ringschluss zwischen zwei Slices waere teurer. |
| Statische App faellt jetzt | Der Uebergangskorridor endet, obwohl „Einkaufsliste ohne Netz abhaken" weiter fehlt. Dafuer gibt es genau eine Form von weekplan. |
