# Foundation

- Stack: .NET 10, SDK gepinnt in `global.json`. Solution `Weekplan.slnx`,
  vertikale Slices nach `.claude/skills/pipeline/presets/dotnet-cloud.md`:
  `src/Weekplan.Server` (Minimal API), `src/Weekplan.Client` (Blazor WASM),
  `src/Weekplan.Core.<Feature>` + `.Contracts`, Tests unter `tests/`.
- **Client und Server sind getrennte Deployables.** Client statisch auf Azure
  Static Web Apps (Free), Server als Container-Image auf Azure Container Apps
  (Consumption, scale-to-zero), Daten in Cosmos DB (Free Tier), zwei Behaelter:
  `tagebuch` (je Nutzer) und `stammdaten` (Rezepte, Training, Grundstock). Steht seit dem
  27.08.2026 in der Subscription „Weekplan Production", `rg-weekplan-prod`,
  westeurope. Preis:
  CORS, und die Anmeldung muss selbst gebaut werden — SWAs eingebaute Auth gilt
  nur fuer `/api` im selben SWA. Server-Adresse im Client:
  `wwwroot/appsettings.json`; erlaubte Herkuenfte im Server: `Cors:Origins`.
- Testbefehl: `dotnet test Weekplan.slnx --filter "Ablage!=Cosmos"`. Ohne den
  Filter laufen auch die Cosmos-Tests mit, und die brauchen `WEEKPLAN_COSMOS` in
  der Umgebung — siehe `tests/Weekplan.Core.Tests/TagebuchInCosmosTests.cs`.
- Ausrollbefehl: keiner. **Ein Push auf `main` rollt aus**
  (`.github/workflows/deploy.yml`): testen, Image nach `ghcr.io`, Container App
  aktualisieren, Client in die Static Web App. Von Hand anstossen geht mit
  `gh workflow run deploy.yml`. Was in Azure steht, listet
  `docs/architecture.md`.
- Startbefehl: `./run-local.ps1` — Server `http://localhost:5080`, Client
  `http://localhost:5180`. Feste Ports, also kein paralleler zweiter Worktree
  (steht in `debt.md`). Beim ersten Start befuellt es
  `src/Weekplan.Server/stammdaten/` aus `tools/Weekplan.Stammdaten/altbestand/`;
  ohne diesen Ordner liefert der Server keine Rezepte. Der MCP-Pflegeweg ist
  lokal **aus**, solange `Mcp__Schluessel` nicht in der Umgebung steht.
- **`az` niemals aus Git Bash.** Die Pfadumsetzung von MSYS macht aus
  `--partition-key-path "/art"` ein `C:/Program Files/Git/art`. Fuer `az` das
  PowerShell-Werkzeug nehmen (oder `MSYS_NO_PATHCONV=1`).
- Smoketest: Browser gegen den laufenden Client, mobil (375 px) und Desktop.
  Zuerst der eingebaute Browser-Bereich, Ablauf in `docs/local-testing.md`.
  **Die Faehigkeitstabelle dort ist ein Messwert mit Datum, keine Tatsache** —
  am 29.08. stimmten vier von sieben Zeilen nicht mehr. Vor dem Verlassen auf
  eine Zeile die Stichprobe machen. Kurzfassung: Screenshots und
  `requestAnimationFrame` gehen nur bei **sichtbarem** Bereich, Klicks nur ueber
  `ref`, die Tastatur gar nicht — die gehoert ins installierte Chrome.
- Design-System: `design-system.md` — bindende Tokens, Spacing-Skala,
  Layout-Regeln. Weicht begruendet von `personal-ui-brand` ab (gruen statt
  Kobalt, Dunkelmodus bleibt, Systemschrift statt Webfont).
- Architektur: `docs/architecture.md`. Rechengrundlage: `docs/plan.md` — aelter
  als der Harness und weiter gueltig.
- Sprache: Oberflaeche und Laufdokumente deutsch, Harness-Dateien englisch.
- **Abweichung vom Bootstrap:** kein `user-docs/`. Die README ist die Nutzerdoku
  und wird bei nutzersichtbaren Aenderungen vor dem Tor aktualisiert.
- Die alte statische App ist seit dem 29.08.2026 abgeschaltet und entfernt. Es
  gibt genau eine Form von weekplan.
