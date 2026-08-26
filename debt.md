# Debt

Known debt and learnings not yet ripe, kept across runs — appended by the retro
in phase 4, read by the next maintenance run. One dated line per entry.

- 2026-08-26 Feste Ports 5080/5180 statt dynamischer Vergabe — zwei Worktrees
  koennen nicht gleichzeitig laufen. **Ausloeser:** sobald parallel an zwei
  Branches gearbeitet wird, oder sobald der Cosmos-Emulator dazukommt (dann
  loest Aspire beides auf einmal).
- 2026-08-26 `css/styles.css` erfuellt die Spacing-Skala aus `design-system.md`
  nicht: 54 harte px-Werte, `--gap: 14px` liegt neben dem 4er-Raster.
  **Ausloeser:** wird ohnehin abgeloest, wenn die Zielform die statische App
  einholt — vorher nur anfassen, wenn ein Lauf diese Datei sowieso oeffnet.
- 2026-08-26 Vom Rechenkern in `js/app.js` ist nur der Grundumsatz nach .NET
  portiert. **Ausloeser:** der Migrationslauf holt MET-Netto, Bilanz, Tagesziel
  und Zutaten-Aggregation nach; bis dahin ist `docs/plan.md` die gemeinsame
  Wahrheit beider Formen.
- 2026-08-26 Kein `user-docs/` — bewusste Abweichung vom Bootstrap, die README
  ist die Nutzerdoku. **Ausloeser:** wenn die README fuer eine Aufgabe zu lang
  wird, um sie durchzugehen.
- 2026-08-26 Kein `static-web`-Preset, obwohl der Zuschnitt (Blazor WASM auf SWA
  Free + Minimal API auf Container Apps + Cosmos Free Tier) generalisierbar
  aussieht. **Ausloeser:** das zweite Projekt dieser Form — ein Learning wird im
  zweiten Lauf zur Regel, nicht im ersten.
- 2026-08-26 Zielform entschieden, aber nicht ausgerollt: keine Azure-Ressourcen,
  kein Deploy-Workflow, das `Dockerfile` ist ungebaut. **Ausloeser:** der erste
  Wunsch, die App von einem zweiten Geraet zu erreichen.
- 2026-08-26 Der Server hat keinen einzigen Test — `/health` ist nur durch den
  Smoketest gedeckt. **Ausloeser:** der erste Endpunkt mit Logik.
- 2026-08-26 Die Tastatur-Aktivierung selbstgebauter Bedienelemente ist mit den
  vorhandenen Browser-Werkzeugen nicht pruefbar — synthetische Tastendruecke
  loesen die native Aktivierung nicht aus (Kontrollprobe mit leerem `<button>`
  in beiden Browsern). Die Harness-Regel „Tab/Enter/Esc, jeweils zweimal" wuerde
  hier sonst reihenweise falsch rot melden. **Ausloeser:** das erste
  selbstgebaute Bedienelement mit eigener Tastensteuerung (Dialog, Menue,
  Popover) — dann braucht es einen bUnit- oder Playwright-Test dafuer.

## Lauf 2026-08-26-cloud-migration

Erledigt aus frueheren Laeufen:

- 2026-08-26 **Erledigt:** der Rechenkern ist vollstaendig portiert —
  Alltagsumsatz, MET-Netto, Phasensport, Bilanz, Tagesziel, 7-Tage-Schnitt und
  Plateau, mit Tests, die die Zahlen der alten App festhalten.
- 2026-08-26 **Erledigt:** der Server hat Tests — zehn Integrationstests fahren
  ihn hoch und pruefen Zugang, Trennung der Nutzer und die Endpunkte.

Neu, und ueber den Lauf hinaus:

- 2026-08-26 Zwei Ablagen hinter einer Naht: Cosmos fuer Azure, Dateien fuer
  lokal. Nur die Dateiablage ist gebaut und geprueft; Cosmos fehlt noch ganz.
  **Ausloeser:** sobald ein Cosmos-Konto steht — dann wird die Cosmos-Ablage
  gegen die echte Ressource gebaut und die Dateiablage auf ihren Nutzen geprueft.
- 2026-08-26 `ProfilStand` und `WochenStand` sind Records mit Sammlungen — ihr
  `==` vergleicht per Referenz, nicht per Inhalt. **Ausloeser:** die erste Stelle,
  die zwei Staende vergleicht, statt einfach zu schreiben.
- 2026-08-26 Der Client referenziert Umsetzungsprojekte, nicht nur Contracts —
  bewusst, weil die Rechnung im Browser laufen muss. **Ausloeser:** wenn eine
  Rechnung Daten braucht, die nur der Server hat.
- 2026-08-26 Kein Passwort-Ruecksetzweg, keine Geraeteverwaltung, kein Ablauf des
  Merkmals — alles drei so gewaehlt. **Ausloeser:** wenn der Gewichtsverlauf
  wieder unersetzlich wird, oder ein Geraet unversperrt verloren geht.
- 2026-08-26 **Erledigt am selben Tag:** Kriterium „zwei Sekunden beim Gewicht"
  ist gemessen — 51 ms beim ersten Mal, danach 7 bis 14 ms von der Eingabe bis
  zur sichtbaren Quittung, im verbundenen Chrome ueber fuenf Durchlaeufe.
- 2026-08-26 Einkaufsliste ohne Netz ist **nicht gebaut** — Schnitt B, samt der
  Frage, wie zwei Geraete denselben Posten gegensaetzlich haken.
  **Ausloeser:** der naechste Einkauf mit schlechtem Empfang.
- 2026-08-26 Die statische App und die neue Form liegen parallel im Repo und
  koennen auseinanderlaufen. **Ausloeser:** die erste Aenderung an der Rechnung,
  die nur eine der beiden bekommt — `docs/plan.md` ist bis dahin die gemeinsame
  Wahrheit.
