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

## 2026-08-27 — Umzug nach Azure

- 2026-08-27 **Ausloeser vom 26.08. geprueft, Schuld bleibt:** „sobald Cosmos in
  Azure steht, pruefen ob die Dateiablage noch gebraucht wird". Cosmos steht —
  die Dateiablage bleibt trotzdem. Sie traegt `run-local.ps1`, den Smoketest und
  die schnellen Tests, und es laeuft weiterhin kein Cosmos-Emulator. Sie faellt
  weg, sobald lokal einer laeuft.
- 2026-08-27 Die Cosmos-Verbindung ist ein Schluessel als Container-App-Secret,
  keine Managed Identity. Grund: das Konto-Werkzeug muss vom Laptop an die
  Datenbank, und ein Konto anzulegen ist der einzige Weg ueberhaupt hinein.
  **Ausloeser:** sobald es einen zweiten Weg gibt, ein Konto anzulegen (etwa ein
  Server-Endpunkt mit Einmal-Code), kann der Server auf Managed Identity
  umgestellt und der Schluessel abgeschaltet werden.
- 2026-08-27 Die Cosmos-Tests laufen **nicht** im Standardlauf — CI und Deploy
  filtern `Ablage!=Cosmos`, weil der Test eine echte Verbindung braucht. Sie
  liefen einmal von Hand, gruen, gegen die Produktionsdatenbank. **Ausloeser:**
  jede Aenderung an `CosmosAblage` — dann muessen sie wieder von Hand laufen,
  und daran erinnert nichts ausser dieser Zeile.
- 2026-08-27 Der Dockerfile pinnt `sdk:10.0.100` und wiederholt damit die
  Fassung aus `global.json` an zweiter Stelle. Noetig, weil das Sammeltag
  `sdk:10.0` inzwischen 10.0.400 bringt und `rollForward: latestPatch` das
  ablehnt. Im Retro bewusst so belassen: der Pin soll hart bleiben.
  **Ausloeser:** die naechste SDK-Anhebung in `global.json` — die Dockerfile-
  Zeile muss mit, sonst bricht das Ausrollen.
- 2026-08-27 Die Cosmos-Dokumente tragen den Inhalt unter `inhalt` statt flach,
  anders als im `design.md` des Umzugslaufs skizziert. Grund: `IAblage` kennt
  den Inhaltstyp nicht. Preis: in der Azure-Konsole liest man eine Ebene tiefer.
  **Ausloeser:** wenn jemand ueber Felder des Inhalts abfragen will — dann
  braucht es flache Felder oder einen Index darauf.
- 2026-08-27 Kein Warmhalten: `minReplicas: 0` heisst, der erste Ruf nach einer
  Pause wartet auf den Kaltstart. Akzeptanzkriterium 7 („zwei Sekunden") gilt
  damit **im warmen Fall**, im kalten nicht — und gemessen ist es weiterhin
  nicht (Schuld vom 26.08.). Im Retro bewusst so belassen. **Ausloeser:** wenn
  der Kaltstart im Alltag stoert — `minReplicas: 1` kostet rund 10 EUR/Monat.
- 2026-08-27 Der Anmeldeschluessel liegt nur als Container-App-Secret. Geht die
  Container App verloren, sind alle Geraete abgemeldet — was zugleich das
  eingebaute „ueberall abmelden" ist. **Ausloeser:** wenn ein Verlust der
  Anmeldung teurer wird als heute.
- 2026-08-27 **Behoben, hier als Lehre:** `.gitignore` trug `daten/` fuer den
  Datenordner des Servers und verschluckte damit auch
  `src/Weekplan.Client/Daten/` — git vergleicht auf Windows ohne Ruecksicht auf
  Gross- und Kleinschreibung. Zwei Quelldateien lagen nie im Repo, der Client
  war aus einem frischen Klon nie baubar, und **kein Test hat es gesehen**, weil
  jeder Testlauf aus dem Arbeitsverzeichnis baut. Erst der erste Deploy-Lauf
  fiel darauf. Die Regel: Ignoriermuster fuer Ordner tragen einen Pfad
  (`/src/Weekplan.Server/daten/`), nie bloss einen Namen; geprueft wird es mit
  `git archive HEAD | tar -x` in ein leeres Verzeichnis und einem Bau dort.
  **Ausloeser:** die naechste `.gitignore`-Zeile, die nur aus einem Namen
  besteht.
- 2026-08-27 Der Smoketest lief **nicht** durch einen Unteragenten mit frischen
  Augen, wie Phase 4 es verlangt, sondern von derselben Hand, die umgesetzt hat
  — die Sitzung darf keine Agenten starten. Die frischen Augen fehlen also.
  **Ausloeser:** der naechste Lauf ohne diese Einschraenkung holt sie nach.
- 2026-08-27 Reihenfolge in Phase 4 umgedreht: erst das Tor (Merge auf `main`),
  dann der Smoketest. Anders geht es bei einem Ausrollauf nicht — die Adresse,
  gegen die geprueft wird, entsteht erst durch den Merge. Der Deploy-Workflow
  faengt das teilweise ab: er testet, prueft `/health` und den Client, bevor er
  gruen meldet.
- 2026-08-27 Das installierte Chrome war erneut nicht verbunden
  (`list_connected_browsers` leer) — dieselbe Schuld wie am 26.08. Der Smoketest
  lief darum im eingebauten Browser-Bereich, der keine Bildschirmfotos
  ausliefert; geprueft wurde ueber den Zugaenglichkeitsbaum und gemessene
  Kastenmasse statt ueber ein Bild. **Ausloeser:** naechster Lauf mit
  verbundenem Chrome.

## 2026-08-29 — Rezepte aus der Datenbank

Erledigt aus frueheren Laeufen:

- 2026-08-29 **Erledigt:** „Eigene Rezepte anlegen ist ein eigener Lauf danach"
  (26.08.). Rezepte, Training und Grundstock liegen in Cosmos, gepflegt wird
  ueber `/mcp` mit Claude Code.
- 2026-08-29 **Erledigt:** „Die statische App und die neue Form koennen
  auseinanderlaufen" (26.08.). Die statische App ist abgeschaltet und entfernt;
  `docs/plan.md` bleibt als Rechengrundlage.

Lehren aus diesem Lauf — drei Fehler, die **kein Test von selbst** gezeigt hat:

- 2026-08-29 **Eine Bibliothek, die „HTML abschaltet", schaltet nicht ab, was
  ihre eigene Sprache kann.** Markdigs `DisableHtml` entschaerfte eingebettetes
  Markup, liess aber `[klick](javascript:…)` als lebendigen Verweis und
  `![bild](fremd)` als nachladendes Bild durch — gegen ein Abnahmekriterium und
  gegen die Datenschutz-Zusage der README. **Regel:** bei jedem Renderer fremden
  Textes die *eigene* Syntax der Sprache durchspielen, nicht nur den offenkundig
  gefaehrlichen Nachbarn.
- 2026-08-29 **Ueber Herkunftsgrenzen ist jede Kopfzeile eine eigene Freigabe.**
  Das ETag kam im Client nie an, weil `Access-Control-Expose-Headers` fehlte —
  der Zwischenspeicher waere wirkungslos geblieben, lautlos und gruen.
- 2026-08-29 **Werkzeuge fuer Agenten ueber ihr Protokoll testen, nie als
  Methoden.** Das MCP-SDK ersetzte die Absage einer Pruefung durch „An error
  occurred invoking …"; ein Methodentest haette das nie gesehen. `McpException`
  reicht die Meldung durch.
- 2026-08-29 **Eine Methode ohne Aufrufer ist kein gebauter Mechanismus.**
  `Stammdatenausgabe.Verwerfen()` existierte von Anfang an, wurde aber nirgends
  gerufen; ein neu angelegtes Rezept waere erst nach einem Serverneustart
  sichtbar geworden.
- 2026-08-29 **Eine Meldung ist erst gut, wenn sie auf jedem Weg dorthin gut
  ist.** Die Fehlerkarte zeigte „TypeError: Failed to fetch", weil der deutsche
  Satz nur im Stammdaten-Weg lag und Profil und Woche ueber einen anderen Weg
  gehen.

Neu, und ueber den Lauf hinaus:

- 2026-08-29 **Die Einkaufsliste steht ohne Netz weiterhin nicht zur
  Verfuegung.** Rezepte und Trainingsphasen kommen aus dem Browserspeicher, der
  Wochenplan aber aus dem Tagebuch auf dem Server. Abnahmekriterium 7 war zu
  weit formuliert und widersprach dem eigenen Nicht-Ziel; es ist berichtigt.
  **Ausloeser:** der naechste Einkauf mit schlechtem Empfang — dann braucht es
  einen Zwischenspeicher fuer Profil und Woche und die Aufloesung, wenn zwei
  Geraete denselben Posten gegensaetzlich haken.
- 2026-08-29 Typnamen weichen vom Slice-Muster ab: der Sammeltyp heisst
  `Stammdatensatz`, die Umsetzung `Stammdatendienst`, die Kochseite als
  Komponente `Kochseite`. Grund ist jedes Mal derselbe Namensschatten — ein Typ,
  der wie sein Namensraum oder wie ein Vertragstyp heisst, verdeckt diesen in
  jeder Datei. **Ausloeser:** der naechste Slice, dessen Name auch ein Typname
  sein soll; dann ist es eine Regel und gehoert in die Vorlage.
- 2026-08-29 Die erlaubten Kategorien stehen zweimal — in
  `Stammdaten.Contracts` fuer die Pruefung, in `Woche.Mahlzeiten` fuer
  Beschriftung und Anteil. Ein Ringschluss zwischen den Slices waere teurer, ein
  Test haelt beide zusammen. **Ausloeser:** eine vierte Mahlzeit.
- 2026-08-29 Die alten JSON-Dateien liegen eingefroren unter
  `tools/Weekplan.Stammdaten/altbestand/` und dienen dem Umzugsnachweis.
  **Ausloeser:** sobald das erste Rezept ueber Claude Code angelegt oder
  geaendert wurde, ist der Vergleich erledigt und die Dateien koennen weg.
- 2026-08-29 Die Cosmos-Verbindung steht zweimal in der Konfiguration
  (`Tagebuch:` und `Stammdaten:`), aber nur einmal als Secret — beide
  Umgebungsvariablen zeigen auf `cosmos-verbindung`. **Ausloeser:** der dritte
  Slice mit Cosmos-Bedarf.
- 2026-08-29 `docs/architecture.md` wurde nicht in Phase 2 fortgeschrieben,
  sondern am Ende jedes Schnitts — die Datei beschreibt, was **ist**, und ein
  Entwurf ist das noch nicht. Bewusste Abweichung von der Pipeline.
  **Ausloeser:** wenn ein Lauf abbricht, bevor ein Schnitt merged; dann steht
  der Entwurf nur im Laufordner.
- 2026-08-29 **Dritter Lauf in Folge ohne Bildschirmfotos:** der eingebaute
  Browser-Bereich liefert eine Viewport-Breite von 0 px, `list_connected_browsers`
  ist leer. Geprueft wurde ueber Zugaenglichkeitsbaum und gemessene Kastenmasse.
  Die Mobil-Emulation liefert immerhin echte 375 px — Abstaende und Treffer sind
  damit messbar, das Aussehen nicht. **Ausloeser:** ein verbundenes Chrome.
- 2026-08-29 **Dritter Lauf in Folge ohne frische Augen im Smoketest:** die
  Sitzung darf keine Unteragenten starten. Geprueft hat dieselbe Hand, die
  umgesetzt hat. **Ausloeser:** der naechste Lauf ohne diese Einschraenkung.
- 2026-08-29 **Ein Vorzustand, der an der Reihenfolge zweier Anweisungen haengt,
  ist keiner.** `Stammdatenlader.Vorheriger` las den alten Stand aus `_geladen`
  — einem Feld, das erst *nach* dem Start des Hintergrund-Tasks zugewiesen wird.
  Der Starthinweis blieb dadurch aus, und zwar lautlos. Behoben, indem der
  abgelegte Stand mitgegeben wird. **Ausloeser:** beim zweiten Vorkommen wird
  daraus eine Harness-Regel; bis dahin steht sie hier.
- 2026-08-29 **Zwei Zahlen gegeneinander zu tarieren ist keine Regel, sondern
  ein Raetsel.** Der geplante Aufschlag fuer „nicht vorkochbar" kaempfte in
  `AutomatischFuellen` gegen den vorhandenen Aufschlag fuer Wiederholung:
  darunter zog das Fuellen frische Gerichte in die Woche, darueber kippte es je
  nach Wochentag. Ein Filter mit Rueckfall traf die Absicht direkt. **Ausloeser:**
  wenn das naechste Mal eine Bewertungsfunktion um einen Term wachsen soll —
  erst pruefen, ob eine Auswahl davor es auch tut.
- 2026-08-29 `abteilungen_schreiben` schreibt Rezepte mit und ist nicht atomar.
  Bricht es dazwischen ab, stehen Abteilungsliste und Rezepte kurz auseinander.
  **Ausloeser:** wenn Zutaten unerklaerlich unter „Sonstiges" auftauchen.
- 2026-08-29 Das Merkmal `prep` ist bei allen 24 Bestandsrezepten `false`, obwohl
  Chili, Dal und Gulasch drei Tage halten. Kein Migrationsschritt, weil der Pool
  im Lauf „neuer Gerichte-Pool" ohnehin ersetzt wird. **Ausloeser:** faellt
  dieser Folgelauf aus, muss es nachgetragen werden.
- 2026-08-29 Der Starthinweis zur geaenderten Zielaufnahme erscheint nur
  angemeldet — ohne Gewicht laesst sich keine Zielaufnahme rechnen — und hat
  keinen automatischen Test, weil es fuer den Client kein Testprojekt gibt.
  **Ausloeser:** sobald eine zweite Sache im Client eine Zusicherung braucht,
  lohnt das Testprojekt.
