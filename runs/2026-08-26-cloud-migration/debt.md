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
