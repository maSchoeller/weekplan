# Lokal starten und pruefen

## Die Zielform (Client + Server)

```powershell
./run-local.ps1
```

Startet beides und gibt die Adressen aus:

| Teil | Adresse |
|---|---|
| Server | `http://localhost:5080` (`/health`) |
| Client | `http://localhost:5180` |

Strg+C beendet beide. Die Ports sind fest — zwei Worktrees gleichzeitig gehen
nicht, der zweite scheitert an der belegten Adresse.

Der erste Aufruf des Clients zeigt, ob die Trennung traegt: die Startseite ruft
`/health` auf der anderen Herkunft. Steht dort „Nicht erreichbar", ist entweder
der Server nicht oben oder seine `Cors:Origins` enthalten
`http://localhost:5180` nicht (`src/Weekplan.Server/appsettings.Development.json`).

## Die alte statische App

Sie laedt ihre Daten per `fetch`, ein Doppelklick auf `index.html` genuegt also
nicht — der Browser blockiert das aus dem Dateisystem:

```bash
npx serve .
```

## Tests

```powershell
dotnet test Weekplan.slnx
```

## Smoketest durch einen frischen Agenten

Zuerst der eingebaute Browser-Bereich gegen `http://localhost:5180`. Rendert er
keine Bilder — im Schwesterprojekt weddination war das so — auf das installierte
Chrome ausweichen.

### Was die Browser-Werkzeuge hier koennen — und was nicht

Nachgemessen am 2026-08-26, mit Kontrollproben:

| | eingebauter Bereich | installiertes Chrome |
|---|---|---|
| Screenshot | **nein** (keine Bilder) | ja |
| Text und Baum lesen | ja | ja |
| JavaScript ausfuehren | ja (nur synchron) | ja |
| Klick auf `@onclick` | **nein** | ja |
| Formular absenden per Klick | **nein** | **nein** |
| Enter/Leertaste aktivieren | **nein** | **nein** |
| Fenstergroesse setzen | ja | ja |

Zwei Kontrollproben stuetzen das: ein frisch erzeugter, leerer `<button>` blieb
bei Enter und Leertaste in **beiden** Browsern stumm, und derselbe Knopf, der
auf einen Werkzeugklick nicht reagierte, loeste per `element.click()` sofort aus.
Das Werkzeug fuehrt die **Standardaktion** des Browsers nicht aus.

**Folge fuer den Smoketest:** Bedienung im eingebauten Bereich ueber
`javascript_tool` ausloesen — `element.click()` fuer Knoepfe,
`form.requestSubmit()` fuer Formulare, und Eingaben ueber den nativen
`value`-Setter plus `new Event('input',{bubbles:true})`, sonst merkt Blazor die
Aenderung nicht. Niemals rot melden, nur weil ein synthetischer Klick oder
Tastendruck nichts bewirkt hat. Und `requestAnimationFrame` laeuft im
verborgenen Bereich nicht — Zeitmessungen brauchen das sichtbare Chrome.

Jeder Bildschirm wird bei **375 px** und bei Desktopbreite angesehen und gegen
`design-system.md` geprueft: Abstaende aus der Skala, nichts ueberlappt, nichts
ist abgeschnitten, Touch-Ziele ≥ 44 px, Fokus sichtbar, Status nie allein ueber
Farbe. Selbstgebaute Bedienelemente werden von Hand bedient — Maus und
Tab/Enter/Esc, jeweils zweimal hintereinander.

## Testdaten

Noch keine — es gibt keine Datenbank. Sobald Cosmos angebunden ist, steht hier
das Saatgut-Werkzeug und die Entwickler-Konten, mit denen sich ein Agent anmelden
darf. Nur Entwickler-Saatgut, niemals echte Zugangsdaten.
