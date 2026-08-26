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

### Tastatur laesst sich hier nicht automatisiert pruefen

Die synthetischen Tastendruecke beider Browser-Werkzeuge loesen die native
Aktivierung nicht aus: ein fokussierter `<button>` bleibt bei Enter und Leertaste
stumm — nachgewiesen 2026-08-26 mit einem frisch erzeugten, leeren
`<button>` als Kontrollprobe, in beiden Browsern, bei bestaetigtem Fokus
(`document.activeElement`). Der Knopf ist in Ordnung, das Werkzeug kann es nicht.

Folge fuer den Smoketest: **Enter/Leertaste/Esc niemals als rot melden**, wenn nur
der synthetische Tastendruck ausgeblieben ist. Pruefbar bleiben mit dem Werkzeug:
Fokus-Reihenfolge (Tab), Fokus-Sichtbarkeit (Screenshot) und der Mauspfad. Die
Aktivierung per Tastatur gehoert in einen bUnit- oder Playwright-Test, oder wird
von Hand geprueft.

Jeder Bildschirm wird bei **375 px** und bei Desktopbreite angesehen und gegen
`design-system.md` geprueft: Abstaende aus der Skala, nichts ueberlappt, nichts
ist abgeschnitten, Touch-Ziele ≥ 44 px, Fokus sichtbar, Status nie allein ueber
Farbe. Selbstgebaute Bedienelemente werden von Hand bedient — Maus und
Tab/Enter/Esc, jeweils zweimal hintereinander.

## Testdaten

Noch keine — es gibt keine Datenbank. Sobald Cosmos angebunden ist, steht hier
das Saatgut-Werkzeug und die Entwickler-Konten, mit denen sich ein Agent anmelden
darf. Nur Entwickler-Saatgut, niemals echte Zugangsdaten.
