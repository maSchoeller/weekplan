# Architektur

Das eine aktuelle Bild des Systems. Phase 2 jedes Laufs schreibt hier fort —
was hier steht, gilt; was nicht hier steht, existiert nicht.

Stand: 2026-08-26. Das System ist gerade **zwischen zwei Formen**: die
ausgelieferte App ist noch die statische, das Geruest der Zielform steht.

## Ist — die statische App

```
index.html      Geruest, fuenf Tabs (Woche, Einkauf, Rezepte, Training, Ich)
css/styles.css  Gestaltung, hell und dunkel
js/app.js       Rechenlogik, Rendering, localStorage — ein globaler Scope
data/*.json     Rezepte, Trainingsphasen, Grundstock
```

Kein Build, keine Abhaengigkeiten, kein Backend. Alle persoenlichen Werte liegen
im `localStorage` des Browsers und verlassen das Geraet nicht. Ausgeliefert von
GitHub Pages aus `main`.

Die Grenzen dieser Form, und der Grund fuer den Umbau: die Daten haengen am
einzelnen Browser. Was am Handy eingetragen wird, steht nicht am Laptop, und wer
den Browserspeicher loescht, verliert alles.

## Ziel — Client, Server, Datenbank

```
Weekplan.Client   Blazor WASM, statisches Artefakt
      |            HTTPS, eigene Herkunft, CORS
      v
Weekplan.Server   Minimal API, Container-Image
      |
      v
Cosmos DB         noch nicht angebunden
```

- **Slices.** Ein Feature ist ein csproj-Paar: `Weekplan.Core.<Feature>` mit der
  Umsetzung und `Weekplan.Core.<Feature>.Contracts` mit den oeffentlichen Typen.
  Features sehen einander nur ueber `.Contracts`; nur der Server referenziert
  Umsetzungen. Jede Umsetzung hat genau einen Eingang, eine
  `Add<Feature>()`-Erweiterung, damit ihre Typen `internal` bleiben koennen.
  Grenzverletzungen sind Compilerfehler — das ist der Zweck.
- **Heute vorhanden:** `Weekplan.Core.Rechnen` mit `IGrundumsatzRechner`
  (Mifflin-St Jeor). Der Rest des Rechenkerns aus `js/app.js` — MET-Netto,
  Bilanz, Tagesziel, Zutaten-Aggregation — folgt im Migrationslauf.
- **Client.** Referenziert die `.Contracts` direkt, also getypte Aufrufe ohne
  Codegenerierung. Das globale Stylesheet traegt nur die Tokens aus
  `design-system.md`, alles Weitere liegt in CSS-Isolation je Komponente.
- **Trennung und ihr Preis.** Client und Server liegen auf verschiedenen
  Herkuenften. Der Server fuehrt darum eine Liste erlaubter Herkuenfte
  (`Cors:Origins`), der Client kennt die Server-Adresse aus
  `wwwroot/appsettings.json`. Die Anmeldung muss selbst gebaut werden.

## Korridor — was zwischen den Formen gilt

Beide Formen liegen gleichzeitig im Repo. Die statische App bleibt unangetastet
und live, bis die Zielform sie fachlich einholt; erst dann wird sie abgeloest.
Solange gilt: eine Aenderung an der Rechnung, die beide Formen betrifft, hat
`docs/plan.md` als gemeinsame Wahrheit — die Formeln stehen dort, nicht im Code.

## Offen — gehoert in den Migrationslauf

Konten und Anmeldung, Synchronisation zwischen Geraeten, Verhalten ohne Netz,
und was mit vorhandenen `localStorage`-Daten passiert. Nichts davon ist hier
entschieden, und nichts davon wird nebenbei entschieden.
