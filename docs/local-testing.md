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

Neu vermessen am **2026-08-29**. Vier der sieben Zeilen stimmten nicht mehr —
diese Tabelle ist ein **Messwert mit Datum, keine Tatsache**. Wer sich auf eine
Zeile stuetzt, macht vorher die Stichprobe.

| | eingebauter Bereich | installiertes Chrome |
|---|---|---|
| Screenshot | ja — **nur wenn der Bereich sichtbar ist** | ja |
| Text und Baum lesen | ja | ja |
| JavaScript, auch `await` | ja | ja |
| `requestAnimationFrame` | **nur wenn der Bereich sichtbar ist** | ja |
| Klick auf einen Knopf | ja — **ueber `ref`**, nicht ueber Koordinaten | ja |
| Formular per Klick absenden | ja (ueber `ref`) | ja |
| Enter/Leertaste aktivieren | **nein** (Fokus bleibt, nichts loest aus) | ja |
| Fensterbreite aendern | ja (Viewport-Emulation) | **nein** (Fenster bleibt, wie es ist) |

**Die eine Regel hinter drei Zeilen:** Was das Kompositieren von Bildern
braucht — Screenshots, `requestAnimationFrame` —, geht nur, solange der
Browser-Bereich tatsaechlich **angezeigt** wird. Ist er zugeklappt, meldet der
Screenshot „not compositing frames" und ein `await requestAnimationFrame(…)`
laeuft in den Zeitausfall. Das ist kein Fehler und kein Grund, rot zu melden;
der Bereich muss nur offen sein.

**Klicks brauchen `ref`.** `computer{action:"left_click", ref:"ref_9"}` aus
`read_page` wirkt; `coordinate` verlangt einen vorherigen Screenshot und
scheitert deshalb bei zugeklapptem Bereich mit. Die Kontrollprobe: leerer
`<button>`, Klick per `ref` → **eine** Aktivierung; `<form>` mit
Submit-Knopf, Klick per `ref` → **ein** Absenden.

**Die Tastatur bleibt aus.** Fokussierter `<button>`, dann Enter, Leertaste,
Enter: **null** Aktivierungen, der Fokus blieb dabei stehen. Tastaturwege
gehoeren weiterhin ins installierte Chrome — und nur dort darf eine ausbleibende
Aktivierung rot gemeldet werden.

**Folge fuer den Smoketest:** Lesen, Vermessen, Schmalmessung und einfache
Bedienung gehen im eingebauten Bereich. Tastaturpfade und Zeitmessung gehoeren
ins installierte Chrome. Fuer Blazor-Eingaben ueber `javascript_tool` weiterhin
den nativen `value`-Setter plus `new Event('input',{bubbles:true})` — sonst
merkt Blazor die Aenderung nicht.

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
