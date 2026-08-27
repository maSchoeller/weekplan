# Schulden dieses Laufs

- 2026-08-27 **Ausloeser vom 26.08. geprueft, Schuld bleibt bestehen:** „sobald
  Cosmos in Azure steht, pruefen ob die Dateiablage noch gebraucht wird". Cosmos
  steht — die Dateiablage bleibt trotzdem. Sie traegt `run-local.ps1`, den
  Smoketest und die schnellen Tests, und es gibt weiterhin keinen laufenden
  Cosmos-Emulator. Sie faellt erst weg, wenn lokal ein Emulator laeuft.
- 2026-08-27 Die Cosmos-Verbindung ist ein Schluessel als Container-App-Secret,
  keine Managed Identity. Grund: das Konto-Werkzeug muss vom Laptop des Nutzers
  an die Datenbank, und ein Konto anzulegen ist der einzige Weg ueberhaupt
  hinein. **Ausloeser:** sobald es einen zweiten Weg gibt, ein Konto anzulegen
  (etwa ein Server-Endpunkt mit Einmal-Code), kann der Server auf Managed
  Identity umgestellt und der Schluessel abgeschaltet werden.
- 2026-08-27 Die Cosmos-Tests laufen **nicht** im Standardlauf — CI und Deploy
  filtern `Ablage!=Cosmos`, weil der Test eine echte Verbindung braucht. Sie
  liefen einmal von Hand, gruen, gegen die Produktionsdatenbank. **Ausloeser:**
  jede Aenderung an `CosmosAblage` — dann muessen sie wieder von Hand laufen,
  und niemand erinnert daran ausser dieser Zeile.
- 2026-08-27 Der Dockerfile pinnt `sdk:10.0.100` und wiederholt damit die
  Fassung aus `global.json` an einer zweiten Stelle. Noetig, weil das Sammeltag
  `sdk:10.0` inzwischen 10.0.400 bringt und `rollForward: latestPatch` das
  ablehnt. **Ausloeser:** die naechste SDK-Anhebung in `global.json` — der
  Dockerfile muss mit, sonst bricht das Ausrollen.
- 2026-08-27 Die Cosmos-Dokumente tragen den Inhalt unter `inhalt` statt flach,
  anders als im `design.md` des Umzugslaufs skizziert. Grund: `IAblage` kennt
  den Inhaltstyp nicht. Preis: in der Azure-Konsole liest man eine Ebene tiefer.
  **Ausloeser:** wenn jemand ueber Felder des Inhalts abfragen will — dann
  braucht es flache Felder oder einen Index darauf.
- 2026-08-27 Kein Warmhalten: `minReplicas: 0` heisst, der erste Ruf nach einer
  Pause wartet auf den Kaltstart. Akzeptanzkriterium 7 („zwei Sekunden") ist
  damit **im kalten Fall nicht erfuellt** — im warmen schon, und es war ohnehin
  nie gemessen (Schuld vom 26.08.). **Ausloeser:** wenn der Kaltstart im Alltag
  stoert, kostet `minReplicas: 1` rund 10 EUR/Monat.
- 2026-08-27 Der Anmeldeschluessel liegt nur als Container-App-Secret. Er ist
  nirgends sonst gesichert; geht die Container App verloren, sind alle Geraete
  abgemeldet (was zugleich das eingebaute „ueberall abmelden" ist).
  **Ausloeser:** wenn ein Verlust der Anmeldung teurer wird als heute.
- 2026-08-27 Der Smoketest lief **nicht** durch einen Unteragenten mit frischen
  Augen, wie Phase 4 es verlangt, sondern von derselben Hand, die umgesetzt hat.
  Grund: die Sitzung darf keine Agenten starten. Die frischen Augen fehlen also.
  **Ausloeser:** der naechste Lauf ohne diese Einschraenkung holt sie nach.
- 2026-08-27 Reihenfolge in Phase 4 umgedreht: erst das Tor (Merge auf `main`),
  dann der Smoketest. Anders geht es bei einem Ausrollauf nicht — die Adresse,
  gegen die geprueft wird, entsteht erst durch den Merge. Der Deploy-Workflow
  faengt das teilweise ab: er testet, prueft `/health` und den Client, bevor er
  gruen meldet.
- 2026-08-27 **Aufgedeckt und behoben, hier als Lehre:** `.gitignore` trug
  `daten/` fuer den Datenordner des Servers und verschluckte damit auch
  `src/Weekplan.Client/Daten/` — git vergleicht auf Windows ohne Ruecksicht auf
  Gross- und Kleinschreibung. Zwei Quelldateien lagen nie im Repo, der Client
  war aus einem frischen Klon nie baubar, und **kein Test hat das gesehen**,
  weil jeder Testlauf aus dem Arbeitsverzeichnis baut. Erst der erste
  Deploy-Lauf fiel darauf. Seitdem: Ignoriermuster fuer Ordner tragen einen
  Pfad (`/src/Weekplan.Server/daten/`), nie bloss einen Namen. Geprueft wird es
  mit `git archive HEAD | tar -x` in ein leeres Verzeichnis und einem Bau dort.
  **Ausloeser:** die naechste Zeile in `.gitignore`, die nur aus einem Namen
  besteht.
