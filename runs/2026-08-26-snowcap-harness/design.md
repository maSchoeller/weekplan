# Design — Snowcap-Harness nachtraeglich

Wartungslauf ohne neues Nutzerverhalten: was angefasst wird und warum.

## Harness

Wortgleiche Kopie aus `snowcap-template` HEAD — CLAUDE.md, die Pipeline-Skill
samt Phasen und Presets, die Abhaengigkeiten-Skill, beide Pruefskripte,
`harness.yml`, `debt.md`, `.editorconfig`. Wortgleich, damit spaetere
Cherry-Picks vom Remote `template` ohne Konflikt greifen. Angepasst wird genau
ein Abschnitt: „Template home" in CLAUDE.md bekommt Projektwortlaut, weil
weekplan das Template als zweites Remote traegt statt es zu sein.

`.gitignore` musste sich aendern: `.claude/` schloss den Harness selbst aus.
Jetzt wird nur `.claude/worktrees/` ignoriert.

## Bootstrap

- **Stack.** .NET 10, SDK in `global.json` gepinnt, damit die Vorschau-SDK auf
  dieser Maschine nicht gegen die CI auseinanderlaeuft.
- **Zwei Deployables.** Client (Blazor WASM, statisch → Static Web Apps Free) und
  Server (Minimal API, Container → Container Apps). Diese Trennung war die
  Entscheidung des Stakeholders. Ihr Preis ist ausdruecklich benannt: CORS statt
  `/api`-Routing im selben Haus, und eine selbstgebaute Anmeldung statt der
  eingebauten von SWA. Beides steht in `foundation.md` und `docs/architecture.md`.
- **Warum nicht SWA managed functions.** Die haetten `/api` und Anmeldung
  geschenkt, koennen aber hoechstens `dotnet-isolated:9.0` — .NET 10 gibt es dort
  nicht, und .NET 8 wie 9 laufen im November 2026 aus. „Bring your own functions"
  gaebe es nur im Standard-Plan, also nicht kostenlos. Container Apps loest das,
  ohne den Preis zu aendern.
- **Design-System.** Abgeleitet aus `personal-ui-brand` und
  `ux-interface-design`, mit drei begruendeten Abweichungen (gruen statt Kobalt,
  Dunkelmodus bleibt, Systemschrift statt Webfont — die App macht keine externen
  Requests, das ist eine Zusage der README). Neu und bindend ist die
  Spacing-Skala; die heutige CSS erfuellt sie noch nicht, das ist Schuld.
- **Testinfrastruktur.** xUnit, `dotnet test Weekplan.slnx`, CI in `ci.yml`
  getrennt von `harness.yml`, damit dessen Wortlaut cherry-pick-faehig bleibt.

## Geruest

Ein laufendes Skelett statt eines leeren: der Client ruft `/health` auf der
anderen Herkunft und zeigt das Ergebnis. Das ist die kleinste Sache, die genau
die eine architektonische Wette beweist, die dieser Lauf eingeht — die Trennung.

Der rot→gruen-Zyklus laeuft an **einer** Funktion: Grundumsatz nach
Mifflin-St Jeor. Die Formel steht schon in `docs/plan.md`, es faellt also keine
fachliche Entscheidung nebenbei. Sie kommt ueber `AddRechnen()` aus dem Slice,
die Umsetzung bleibt `internal` — damit ist die Slice-Grenze nicht nur
beschrieben, sondern einmal benutzt.

## YAGNI — bewusst weggelassen

Keine Komponenten-RCL (erst beim zweiten Bedarf einer Steuerung), kein Aspire
(erst wenn der Cosmos-Emulator dazukommt; bis dahin feste Ports), keine
Cosmos-Anbindung (erst mit der ersten persistierten Entitaet), keine Anmeldung,
kein `static-web`-Preset (ein Learning wird im zweiten Lauf zur Regel, nicht im
ersten).
