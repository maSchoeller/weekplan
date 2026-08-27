# Foundation

- Stack: .NET 10, SDK gepinnt in `global.json`. Solution `Weekplan.slnx`,
  vertikale Slices nach `.claude/skills/pipeline/presets/dotnet-cloud.md`:
  `src/Weekplan.Server` (Minimal API), `src/Weekplan.Client` (Blazor WASM),
  `src/Weekplan.Core.<Feature>` + `.Contracts`, Tests unter `tests/`.
- **Client und Server sind getrennte Deployables.** Client statisch auf Azure
  Static Web Apps (Free), Server als Container-Image auf Azure Container Apps
  (Consumption, scale-to-zero), Daten in Cosmos DB (Free Tier). Steht seit dem
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
  (steht in `debt.md`).
- Smoketest: Browser gegen den laufenden Client, mobil (375 px) und Desktop.
  Zuerst der eingebaute Browser-Bereich; rendert er keine Bilder — wie im
  Schwesterprojekt weddination — auf das installierte Chrome ausweichen.
  Ablauf in `docs/local-testing.md`.
- Design-System: `design-system.md` — bindende Tokens, Spacing-Skala,
  Layout-Regeln. Weicht begruendet von `personal-ui-brand` ab (gruen statt
  Kobalt, Dunkelmodus bleibt, Systemschrift statt Webfont).
- Architektur: `docs/architecture.md`. Rechengrundlage: `docs/plan.md` — aelter
  als der Harness und weiter gueltig.
- Sprache: Oberflaeche und Laufdokumente deutsch, Harness-Dateien englisch.
- **Abweichung vom Bootstrap:** kein `user-docs/`. Die README ist die Nutzerdoku
  und wird bei nutzersichtbaren Aenderungen vor dem Tor aktualisiert.
- Uebergang: die alte statische App (`index.html`, `css/`, `js/`, `data/`) bleibt
  im Wurzelverzeichnis und auf GitHub Pages, bis die Migration durch ist.
