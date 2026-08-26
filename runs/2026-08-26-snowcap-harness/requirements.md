# Anforderungen — Snowcap-Harness nachtraeglich

**Art:** Wartungslauf. Stakeholder ist der Entwickler, Eingabe ist der Zustand
des Repos, nicht ein Nutzerwunsch.

## Problem

`weekplan` ist ohne Harness entstanden: zwei Commits, eine statische Seite, kein
Build, keine Tests, keine CI, und `.gitignore` schloss ausgerechnet `.claude/`
aus. Jede weitere Aenderung waere wieder freihaendig gelaufen.

Dazu kam im Gespraech ein zweites Ziel: die App soll vom localStorage-Aufbau auf
Client/Server in Azure wechseln, so guenstig wie moeglich, mit kostenloser
Datenbank. Damit wird die Stack-Frage des Bootstraps zur Cloud-Frage.

## Ziele

1. Der Harness aus `snowcap-template` liegt wortgleich im Repo und ist wirksam:
   Budget- und Referenzpruefung laufen in CI.
2. Der Bootstrap ist durchgefuehrt — Stack, Design-System, Architekturbild,
   Testinfrastruktur, Startweg, Smoketest-Verfahren sind entschieden und
   aufgeschrieben.
3. Es gibt etwas, das rot und gruen werden kann; ein echter rot→gruen-Zyklus ist
   tatsaechlich gelaufen.
4. Die bestehende App bleibt unangetastet und live.

## Bereits entschieden (im Gespraech, nicht erneut zu fragen)

- Umfang: Harness + Bootstrap + Geruest. Die Migration ist der **naechste** Lauf,
  mit eigenem Interview.
- Hosting: Client statisch auf Static Web Apps (Free), Server auf Container Apps
  (Consumption), Daten in Cosmos DB (Free Tier). Client und Server **getrennt**.
- Durchgaengig .NET 10.
- Design-System aus den globalen UI-Skills ableiten, Luecken der heutigen CSS als
  datierte Schuld.
- Kein `user-docs/`; die README bleibt Nutzerdoku.
- Konten, Sync, Offline und der Umgang mit vorhandenen localStorage-Daten sind
  **offen** und gehoeren ins Interview des Migrationslaufs.

## Abnahmekriterien

- `pwsh .claude/check-budget.ps1` und `check-references.ps1` melden gruen.
- `dotnet test Weekplan.slnx` ist gruen, und der rote Vorlauf ist belegt.
- `./run-local.ps1` startet beides; die Client-Startseite zeigt die Antwort des
  Servers von der anderen Herkunft — Beweis, dass Trennung und CORS stehen.
- Die alte App rendert unveraendert ihre fuenf Tabs.
- `.claude/skills/**` ist versioniert, `.claude/worktrees/` weiter ignoriert.

## Nicht-Ziele

Keine Azure-Ressourcen, kein Deploy, keine Anmeldung, keine Cosmos-Anbindung,
keine Portierung der uebrigen Rechenlogik, kein Umbau der bestehenden CSS.
