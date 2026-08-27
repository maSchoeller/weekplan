# weekplan

Wochenplaner für Ernährung und Training: Gerichte auf Tage und Mahlzeiten legen,
Portionen einstellen, Einkaufsliste in Gramm erhalten — plus Trainingsplan mit
gewichtsabhängiger Verbrauchsrechnung.

**Live:**
- Neue Form (Konto, geräteübergreifend): https://gentle-moss-035769303.7.azurestaticapps.net
- Alte statische Form: https://maschoeller.github.io/weekplan

## Was drin ist

- **Woche** — Gerichte auf Wochentage und Mahlzeiten legen. Tagessumme kcal und Protein
  läuft mit und färbt sich, wenn das Ziel getroffen ist. Ein Knopf füllt die Woche
  automatisch und wählt die Portionen so, dass jeder Tag nah am Kalorienziel landet.
- **Einkauf** — Wochenliste (Frischware, aus dem Wochenplan aggregiert, nach
  Supermarkt-Abteilungen sortiert, abhakbar) und Grundstock (einmaliger Vorratseinkauf).
- **Rezepte** — 24 Gerichte mit Grammangaben pro Portion und Zubereitungsschritten,
  skalierbar über die Portionsanzahl.
- **Training** — Fünf Phasen mit Wochenplan, Verbrauchstabelle beim aktuellen Gewicht,
  Kraftplan A/B und Regelwerk.
- **Ich** — Gewicht, Zielgewicht, Größe, Alter, Zieltermin, Proteinfaktor und wahlweise ein
  eigenes Tempo in kg pro Woche, das das Defizit der Phase überschreibt. Daraus
  Grundumsatz, Zielaufnahme, Countdown, Gewichtsverlauf mit 7-Tage-Schnitt und
  Plateau-Erkennung.

## Zwei Formen, eine im Umbau

weekplan zieht gerade von einer reinen Browserseite auf Client und Server um.
Beide Formen laufen, beide liegen im Repo:

- **Die statische Seite** (`index.html`, `css/`, `js/`, `data/`) liegt weiter auf
  GitHub Pages. Sie ist unveraendert und funktioniert wie bisher.
- **Die Client/Server-Form** unter `src/` ist seit dem 27.08.2026 **in Azure
  ausgerollt**: ein Blazor-WebAssembly-Client auf Static Web Apps, eine Minimal
  API auf Container Apps, Daten in Cosmos DB. Sie loest das Problem, das die
  statische Form nicht loesen kann — dass Handy und Laptop getrennte Staende
  haben.

Der Server skaliert auf null, wenn ihn niemand braucht. **Nach einer laengeren
Pause dauert der erste Aufruf darum ein paar Sekunden**, danach ist er schnell.
Das ist der Preis dafuer, dass der Betrieb nichts kostet.

Details in [foundation.md](foundation.md) und
[docs/architecture.md](docs/architecture.md).

## Datenschutz

**In der statischen Form steht keine einzige persoenliche Zahl im Repo.**
`data/*.json` enthaelt ausschliesslich generische Rezepte und
Trainingsstrukturen. Gewicht, Zielgewicht, Zieltermin und der Gewichtsverlauf
liegen dort ausschliesslich im `localStorage` des Browsers und verlassen das
Geraet nicht. Kein Backend, keine Analytics, keine externen Requests. Daraus
folgt aber auch: Browserspeicher geloescht heisst Daten weg, und die Daten sind
pro Geraet.

**In der neuen Form aendert sich genau das** — und zwar bewusst: die Zahlen
liegen dann im Konto auf dem Server, damit Handy und Laptop denselben Stand
sehen. Was gleich bleibt: keine Analytics, keine externen Requests, keine
Registrierungsseite. Es gibt genau ein Konto, angelegt von Hand ueber
`tools/Weekplan.Konto`; ohne Anmeldung ist keine Zahl erreichbar. Ende-zu-Ende-
Verschluesselt ist es nicht — wer den Server betreibt, koennte die Daten lesen.

## Aufbau

```
index.html            Statische Form: Geruest und Tabs
css/styles.css        Gestaltung, hell und dunkel
js/app.js             Rechenlogik, Rendering, localStorage
data/*.json           Rezepte, Trainingsphasen, Grundstock

src/Weekplan.Client   Neue Form: Blazor WebAssembly, die fuenf Tabs
src/Weekplan.Server   Minimal API, CORS, Anmeldung, Tagebuch-Endpunkte
src/Weekplan.Core.*   Slices: Rechnen, Anmeldung, Tagebuch, Wochenplanung
tools/Weekplan.Konto  Legt das eine Konto an — es gibt keine Registrierung
tests/                xUnit: Rechenkern, Ablage, Endpunkte

.github/workflows/    ci.yml testet jeden Push, deploy.yml rollt main nach Azure
CLAUDE.md             Der Snowcap-Harness: jede Aenderung laeuft durch die Pipeline
foundation.md         Stack, Testbefehl, Startbefehl, Smoketest-Methode
design-system.md      Bindende Tokens, Spacing-Skala, Layout-Regeln
docs/architecture.md  Das eine aktuelle Bild des Systems
docs/plan.md          Methodik: Formeln, Phasenlogik, Begruendungen
debt.md               Bewusst eingegangene Schulden, datiert und mit Ausloeser
runs/                 Was in welchem Lauf entschieden wurde
```

## Lokal starten

Die neue Form, Client und Server zusammen:

```bash
pwsh ./run-local.ps1
```

Beim ersten Mal braucht es ein Konto:

```bash
dotnet run --project tools/Weekplan.Konto -- <benutzername> <passwort>
```

Fuer die ausgerollte App wird dasselbe Konto in Cosmos angelegt — dazu vorher die
Verbindung in die Umgebung setzen (PowerShell):

```powershell
$env:WEEKPLAN_COSMOS = (az cosmosdb keys list -n cosmos-weekplan-prod -g rg-weekplan-prod --type connection-strings --query "connectionStrings[0].connectionString" -o tsv)
```

Es gibt bewusst keine Registrierungsseite: das ist der einzige Weg, ein Konto
anzulegen, und es gibt keinen Weg, ein Passwort zurueckzusetzen.

Die statische Form laedt ihre Daten per `fetch`, ein Doppelklick auf
`index.html` genuegt also nicht:

```bash
npx serve .
```

Tests:

```bash
dotnet test Weekplan.slnx
```

## Rechengrundlage

Grundumsatz nach Mifflin-St Jeor, Sport über MET-Werte gewichtsabhängig als
Netto-Verbrauch. Details und Begründungen in [docs/plan.md](docs/plan.md).

## Kein medizinischer Rat

Planungswerkzeug, keine ärztliche Beratung. Details am Ende von
[docs/plan.md](docs/plan.md).
