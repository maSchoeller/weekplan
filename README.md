# weekplan

Wochenplaner für Ernährung und Training: Gerichte auf Tage und Mahlzeiten legen,
Portionen einstellen, Einkaufsliste in Gramm erhalten — plus Trainingsplan mit
gewichtsabhängiger Verbrauchsrechnung.

**Live:** https://gentle-moss-035769303.7.azurestaticapps.net

## Was drin ist

- **Woche** — Gerichte auf Wochentage und Mahlzeiten legen. Tagessumme kcal und Protein
  läuft mit und färbt sich, wenn das Ziel getroffen ist. Ein Knopf füllt die Woche
  automatisch: werktags zwei vorkochbare Sorten in zusammenhängenden Blöcken von
  zwei bis drei Tagen (Meal Prep ist die Betriebsart, nicht die Ausnahme), am
  Wochenende frisch gekochte Gerichte, am Refeed-Tag solche, die dessen höhere
  Aufnahme tragen; das Frühstück wechselt täglich. Nochmal drücken gibt eine
  andere, genauso regelkonforme Woche. Vorgeschlagen wird nach Mahlzeit,
  gesperrt ist nichts — mittags darf auch ein Abendgericht stehen.
- **Gäste** — Am Tageskopf steht, wie viele zusätzlich mitessen; einzelne
  Mahlzeiten lassen sich abweichend setzen („freitags frühstücken sie nicht
  mit"). Die Zahl wächst Einkaufsliste und Kochmenge, **die eigene Bilanz bleibt
  unberührt** — Tagessumme und Zielabgleich zählen nur die eigene Portion. Ein
  Tippen auf den Gerichtnamen öffnet die Kochseite mit den Zutatenmengen für
  alle Esser. Gäste bleiben stehen, wenn die Woche geleert oder neu gefüllt wird.
- **Einkauf** — Wochenliste (Frischware, aus dem Wochenplan aggregiert, nach
  Supermarkt-Abteilungen sortiert, abhakbar) und Grundstock (einmaliger Vorratseinkauf).
  Stecken Gästeportionen darin, steht es über der Liste.
- **Rezepte** — 18 Gerichte. Die Übersicht ist nach Mahlzeit gruppiert und nennt
  je Gericht kcal, Protein, Zeit sowie, ob es kalt schmeckt und ob es sich
  vorkochen lässt; ein Tippen öffnet die Kochseite mit Portionsrechner, Zutaten
  in Gramm und einer ausführlichen Anleitung. Die Rezepte liegen in der
  Datenbank, nicht im Quellcode.
- **Training** — Fünf Phasen mit Wochenplan, Verbrauchstabelle beim aktuellen Gewicht,
  Kraftplan A/B und Regelwerk.
- **Ich** — Gewicht, Zielgewicht, Größe, Alter, Zieltermin, Proteinfaktor und wahlweise ein
  eigenes Tempo in kg pro Woche, das das Defizit der Phase überschreibt. Daraus
  Grundumsatz, Zielaufnahme, Countdown, Gewichtsverlauf mit 7-Tage-Schnitt und
  Plateau-Erkennung.

## Eine Form

weekplan ist ein Blazor-WebAssembly-Client auf Azure Static Web Apps, eine
Minimal API auf Container Apps und Cosmos DB. Die urspruengliche statische
Browserseite ist mit dem Lauf `2026-08-28-rezepte-aus-der-datenbank`
**abgeschaltet und entfernt** — sie konnte nicht loesen, dass Handy und Laptop
getrennte Staende haben.

Der Server skaliert auf null, wenn ihn niemand braucht. **Nach einer laengeren
Pause dauert der erste Aufruf darum ein paar Sekunden**, danach ist er schnell.
Das ist der Preis dafuer, dass der Betrieb nichts kostet.

Details in [foundation.md](foundation.md) und
[docs/architecture.md](docs/architecture.md).

## Datenschutz

**Im Repo steht keine einzige persoenliche Zahl.** Gewicht, Zielgewicht,
Zieltermin und der Gewichtsverlauf liegen im Konto auf dem Server, damit Handy
und Laptop denselben Stand sehen. Die Rezepte, Trainingsphasen und der
Grundstock liegen daneben in derselben Datenbank — sie sind generisch und
gehoeren keinem Nutzer, weshalb sie ohne Anmeldung lesbar sind.

Was gilt: keine Analytics, keine externen Requests, keine
Registrierungsseite. Es gibt genau ein Konto, angelegt von Hand ueber
`tools/Weekplan.Konto`; ohne Anmeldung ist keine Zahl erreichbar. Ende-zu-Ende-
Verschluesselt ist es nicht — wer den Server betreibt, koennte die Daten lesen.

## Aufbau

```
src/Weekplan.Client      Blazor WebAssembly, die fuenf Tabs
src/Weekplan.Server      Minimal API, CORS, Anmeldung, Tagebuch, Stammdaten
src/Weekplan.Core.*      Slices: Rechnen, Anmeldung, Tagebuch, Wochenplanung,
                         Stammdaten
tools/Weekplan.Konto     Legt das eine Konto an — es gibt keine Registrierung
tools/Weekplan.Stammdaten  Bringt Rezepte, Training und Grundstock einmalig in
                         die Datenbank und prueft danach jedes Feld nach
.mcp.json                Der Pflegeweg fuer Rezepte ueber Claude Code
tests/                   xUnit: Rechenkern, Ablagen, Endpunkte, Umzug

.github/workflows/    ci.yml testet jeden Push, deploy.yml rollt main nach Azure
CLAUDE.md             Der Snowcap-Harness: jede Aenderung laeuft durch die Pipeline
foundation.md         Stack, Testbefehl, Startbefehl, Smoketest-Methode
design-system.md      Bindende Tokens, Spacing-Skala, Layout-Regeln
docs/architecture.md  Das eine aktuelle Bild des Systems
docs/plan.md          Methodik: Formeln, Phasenlogik, Begruendungen
docs/ernaehrungsplan.md  Warum welche Gerichte im Pool stehen — Praemissen
                      und Pruefliste fuer neue Rezepte
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

Die Rezepte, Trainingsphasen und der Grundstock kommen aus der Datenbank.
Oertlich befuellt `run-local.ps1` sie beim ersten Start selbst; fuer die
ausgerollte App einmalig von Hand, mit derselben `WEEKPLAN_COSMOS` wie oben:

```bash
dotnet run --project tools/Weekplan.Stammdaten
```

### Den Plan pflegen

Der Plan wird nicht in der App bearbeitet, sondern im Gespraech mit Claude Code.
Der ausgerollte Server bietet dafuer einen MCP-Endpunkt; `.mcp.json` im Repo
kennt die Adresse, der Schluessel kommt aus der Umgebung.

**Einmal einrichten, dann nie wieder.** MCP-Server verbinden sich genau einmal,
beim Sitzungsstart. Eine Variable, die man spaeter in der laufenden Sitzung
setzt, kommt zu spaet — deshalb steht der Schluessel nicht in einem Befehl,
sondern in `.claude/settings.local.json`:

```json
{
  "env": { "WEEKPLAN_MCP_SCHLUESSEL": "<der Schluessel>" },
  "enableAllProjectMcpServers": true
}
```

Claude Code liest diese Datei vor dem Start der MCP-Server, `.mcp.json` loest
`${WEEKPLAN_MCP_SCHLUESSEL}` daraus auf, und der Server haengt in jeder neuen
Sitzung von selbst dran. Die Datei steht in `.gitignore` und gehoert nie ins
Repo. Den Schluessel holt man sich so (PowerShell, **nicht** Git Bash — MSYS
zerlegt die Azure-Ressourcenpfade):

```powershell
az containerapp secret show -n weekplan-prod-api -g rg-weekplan-prod --subscription 36f5bb87-134c-4aad-ac07-9be4b0453ef2 --secret-name mcp-schluessel --query value -o tsv
```

Haengt der Server einmal nicht dran und ein Neustart ist gerade unpassend, geht
es auch ohne: JSON-RPC per HTTP direkt gegen `/mcp`, Header
`Authorization: Bearer <schluessel>` und
`Accept: application/json, text/event-stream`. Der Server ist zustandslos und
antwortet als Ereignisstrom; die Nutzlast steht hinter `data: `. Die Nutzlast
dabei in einem Skript bauen, nicht auf der Kommandozeile — Umlaute in Zutaten
ueberleben den Weg ueber die Shell nicht, und die Abteilungspruefung lehnt
`Gem?se` dann zu Recht ab.

Danach genuegt eine Ansage wie „leg mir was Warmes fuer den Abend an, um 700 kcal,
mindestens 45 g Protein, unter 30 Minuten" — oder „nimm das Laufband am Montag
auf 60 Minuten runter".

| Was | Lesen | Schreiben |
|---|---|---|
| Rezepte | ja | ja |
| Trainingsplan, MET-Werte, Kraftplan | ja | ja |
| Grundstock, Abteilungen | ja | ja |
| Regelwerk (die sechs Regeln) | ja | **nein** |
| Gewicht, Verlauf, Wochenplan | **nein** | **nein** |

Jedes Schreiben ersetzt sein Dokument **vollstaendig** — also vorher lesen,
damit nichts verlorengeht. Aendert sich dabei etwas, das rueckwirkend rechnet,
zeigt die App beim naechsten Start einen Hinweis mit der alten und der neuen
Zielaufnahme.

**Bevor ein Gericht angelegt wird**, gehoert
[docs/ernaehrungsplan.md](docs/ernaehrungsplan.md) gelesen: dort stehen die
Praemissen des Pools — Portionsziele, die Untergrenzen bei Protein je 100 kcal,
die No-Go-Zutaten und eine Pruefliste.

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
