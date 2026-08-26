# Schulden dieses Laufs

- 2026-08-26 Patch-Lauf ohne eigenes requirements.md/design.md — so sieht die
  Triage den kurzen Weg vor.
- 2026-08-26 Die Werkzeug-Tabelle in docs/local-testing.md war falsch: sie
  behauptete, Klick und Tastatur wuerden in **beiden** Browsern nichts
  ausloesen. Tatsaechlich betrifft das nur den eingebauten Bereich — im richtig
  verbundenen Chrome loesen Klick, Formularabsenden, Enter und Leertaste aus
  (Kontrollprobe: zwei Aktivierungen statt null). Ursache des Irrtums: die
  Chrome-Verbindung war zum Zeitpunkt der ersten Probe schon abgerissen, und ich
  habe das Ergebnis trotzdem beiden Browsern zugeschrieben. Korrigiert.
