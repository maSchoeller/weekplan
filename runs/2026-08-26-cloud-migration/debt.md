# Schulden dieses Laufs

- 2026-08-26 Branch `claude/snowcap-harness-nachtraeglich-42fb4c` wird fuer einen
  zweiten Lauf wiederverwendet statt geloescht — der Worktree ist der der Sitzung.
  Er sitzt nach dem Merge auf `main`, traegt also wieder genau einen Lauf.
- 2026-08-26 Kein Passwort-Ruecksetzweg — bewusst gewaehlt, weil die Daten weg
  duerfen. Wird zur Schuld, sobald der Verlauf wieder unersetzlich wird.
- 2026-08-26 Offline nur fuer die Haken der Einkaufsliste. Zwei Geraete koennen
  denselben Posten gegensaetzlich haken; die Aufloesung muss Phase 2 entscheiden,
  und sie wird bewusst einfach ausfallen.
- 2026-08-26 Keine Geraeteverwaltung und kein Ablauf der Anmeldung — ein
  verlorenes, unversperrtes Geraet bleibt drin.
- 2026-08-26 Der Tagebuch-Slice bekommt zwei Ablagen hinter einer Naht: Cosmos
  fuer Azure, Dateien auf der Platte fuer lokal. Grund: der Docker-Daemon laeuft
  auf dieser Maschine nicht, der Cosmos-Emulator faellt damit aus, und ohne
  lokale Ablage traegt weder `run-local.ps1` noch der Smoketest. **Ausloeser:**
  sobald Cosmos in Azure steht, pruefen ob die Dateiablage noch gebraucht wird —
  wenn nicht, faellt sie weg.
- 2026-08-26 `ProfilStand` und `WochenStand` sind Records mit Sammlungen — ihr
  `==` vergleicht die Sammlungen per **Referenz**, nicht per Inhalt. Heute
  harmlos; wird zur Falle, sobald das gebuendelte Speichern per Vergleich
  entscheidet, ob sich etwas geaendert hat. **Ausloeser:** die erste Stelle, die
  zwei Staende vergleicht, statt einfach zu schreiben.
- 2026-08-26 Der Client referenziert die Umsetzungsprojekte `Core.Rechnen` und
  `Core.Wochenplanung`, nicht nur deren Contracts — das Preset sagt, nur der Host
  duerfe das. Begruendung: die Rechnung laeuft im Browser, sonst kostet jede
  Portionsaenderung einen Serverruf und die zwei Sekunden beim Gewicht sind weg.
  Der Client ist hier also selbst ein Host. **Ausloeser:** wenn eine Rechnung
  Daten braucht, die nur der Server hat — dann wandert sie zurueck.
- 2026-08-26 `<Anmelden />` wurde als HTML-Element gerendert statt als Komponente,
  weil `Weekplan.Client.Pages` in `_Imports.razor` fehlte — der Build blieb gruen,
  die Seite blieb leer. Unbekannte Kleinbuchstaben-Tags sind fuer Razor gueltiges
  Markup. Behoben; hier festgehalten, weil kein Test dieser Art es faengt.
- 2026-08-26 Kriterium 7 („zwei Sekunden") ist **nicht gemessen**. Die Eingabe
  wirkt optimistisch, also ohne auf den Server zu warten — das ist der Mechanismus,
  der es traegt — aber die Zeit selbst konnte hier nicht sauber genommen werden:
  `requestAnimationFrame` laeuft im verborgenen Browser-Bereich nicht, und das
  installierte Chrome war nicht verbunden. **Ausloeser:** naechster Lauf mit
  sichtbarem Chrome, oder ein Messpunkt in der App.
- 2026-08-26 Kriterium 4 (Einkaufsliste ohne Netz) ist **nicht erfuellt** — es ist
  Schnitt B und war fuer diesen Lauf ausdruecklich ausgeklammert.
