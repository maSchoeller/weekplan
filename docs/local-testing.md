# Lokal starten und pruefen

## Client und Server

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

Beim ersten Start befuellt das Skript die oertliche Stammdatenablage aus
`tools/Weekplan.Stammdaten/altbestand/` — ohne sie liefert der Server keine
Rezepte und die App bleibt bei der Fehlerkarte stehen. Der Ordner
`src/Weekplan.Server/stammdaten/` liegt nicht im Repo.

Der erste Aufruf des Clients zeigt, ob die Trennung traegt: die Startseite ruft
`/health` auf der anderen Herkunft. Steht dort „Nicht erreichbar", ist entweder
der Server nicht oben oder seine `Cors:Origins` enthalten
`http://localhost:5180` nicht (`src/Weekplan.Server/appsettings.Development.json`).

## Tests

```powershell
dotnet test Weekplan.slnx
```

## Smoketest durch einen frischen Agenten

Zuerst der eingebaute Browser-Bereich gegen `http://localhost:5180`. Rendert er
keine Bilder — im Schwesterprojekt weddination war das so — auf das installierte
Chrome ausweichen.

### Was die Browser-Werkzeuge hier koennen — und was nicht

Nachgemessen am 2026-08-26 mit Kontrollproben, in **beiden** Browsern:

| | eingebauter Bereich | installiertes Chrome |
|---|---|---|
| Screenshot | **nein** (kompositiert keine Bilder) | ja |
| Text und Baum lesen | ja | ja |
| JavaScript ausfuehren | ja, nur synchron (kein `requestAnimationFrame`) | ja, auch `await` |
| Klick auf einen Knopf | **nein** | ja |
| Formular per Klick absenden | **nein** | ja |
| Enter/Leertaste aktivieren | **nein** | ja |
| Fensterbreite aendern | ja (Viewport-Emulation) | **nein** (Fenster bleibt, wie es ist) |

Die Kontrollprobe ist jeweils dieselbe: einen leeren `<button>` erzeugen,
fokussieren, Enter und Leertaste druecken, Klicks zaehlen. Im eingebauten
Bereich kamen **null** Aktivierungen, in Chrome **zwei**.

**Folge fuer den Smoketest:** Bedienung und Zeitmessung gehoeren ins
installierte Chrome. Ist es nicht verbunden, taugt der eingebaute Bereich noch
zum Lesen und Vermessen — Bedienung dann ueber `javascript_tool`
(`element.click()`, `form.requestSubmit()`, Eingaben ueber den nativen
`value`-Setter plus `new Event('input',{bubbles:true})`, sonst merkt Blazor die
Aenderung nicht). Und dort niemals rot melden, nur weil ein synthetischer Klick
nichts bewirkt hat.

**Handybreite** kommt vom eingebauten Bereich: Chrome laesst sein Fenster hier
nicht verkleinern (dasselbe Verhalten wie im Schwesterprojekt weddination),
der eingebaute Bereich emuliert dagegen sauber 375 px. Also: Bedienung und
Screenshots in Chrome, Schmalmessung im eingebauten Bereich.

Jeder Bildschirm wird bei **375 px** und bei Desktopbreite angesehen und gegen
`design-system.md` geprueft: Abstaende aus der Skala, nichts ueberlappt, nichts
ist abgeschnitten, Touch-Ziele ≥ 44 px, Fokus sichtbar, Status nie allein ueber
Farbe. Selbstgebaute Bedienelemente werden von Hand bedient — Maus und
Tab/Enter/Esc, jeweils zweimal hintereinander.

## Testdaten

Noch keine — es gibt keine Datenbank. Sobald Cosmos angebunden ist, steht hier
das Saatgut-Werkzeug und die Entwickler-Konten, mit denen sich ein Agent anmelden
darf. Nur Entwickler-Saatgut, niemals echte Zugangsdaten.
