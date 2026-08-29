# Schulden — Stammdaten über MCP

- 2026-08-29 — Worktree `recipes-postgres-migration-ab1c2f` nicht entfernt,
  obwohl er alle drei Vetos besteht: sein Index wurde zur selben Minute
  angefasst, dort arbeitet eine andere Sitzung. Aufräumen bleibt offen.
- 2026-08-29 — Vierte Datei `essprofil.md` im Runordner, abweichend von der
  Konvention (requirements/design/debt). Das Ernährungsprofil wurde in diesem
  Lauf erhoben, gehört aber zum Folgelauf „neuer Gerichte-Pool". Ohne diese
  Datei wäre es nur im Gesprächsverlauf und damit verloren.
- 2026-08-29 (Entwurf) — `abteilungen_schreiben` schreibt Rezepte mit und ist
  nicht atomar. Bricht es zwischen Abteilungsliste und Rezepten ab, stehen
  beide kurz auseinander. Hingenommen: eine Zutat mit unbekannter Abteilung
  landet auf der Einkaufsliste unten, mehr passiert nicht.
- 2026-08-29 (Entwurf) — Kein Migrationsschritt für das neue Merkmal `prep`.
  Die 24 Bestandsrezepte gelten damit alle als nicht vorkochbar, obwohl das
  fachlich falsch ist. Grund: der Bestand wird im Folgelauf „neuer
  Gerichte-Pool" vollständig ersetzt. Fällt der Folgelauf aus, muss das
  nachgeholt werden.
- 2026-08-29 (Entwurf) — Der Starthinweis erscheint nur angemeldet: ohne
  Gewicht lässt sich keine Zielaufnahme rechnen. Ein abgemeldeter Nutzer
  erfährt von einer Planänderung nichts.
- 2026-08-29 (Umsetzung) — Der Starthinweis hat keinen automatischen Test. Die
  Entscheidung „nur melden, wenn sich die Zielaufnahme bewegt" liegt in
  `Zustand.FrischeStammdaten`, und fuer den Client gibt es kein Testprojekt;
  eines anzulegen waere ein eigener Entschluss. Geprueft wurde von Hand im
  Browser. Faellt beim naechsten Umbau des Laders als Erstes um.
- 2026-08-29 (Umsetzung) — Abweichung vom Entwurf beim automatischen Fuellen:
  Filter statt Strafpunkte. Begruendung steht in `design.md`.
- 2026-08-29 (Umsetzung) — Abweichung vom Entwurf in der Oberflaeche: „kalt ok"
  ist kein Chip, sondern Teil der Meta-Zeile. „vorkochbar" folgt darum demselben
  Muster statt einen Chip einzufuehren, den es sonst nirgends gibt.
- 2026-08-29 (Umsetzung) — Beim Prüfen im Browser einen echten Fehler gefunden
  und behoben: `Stammdatenlader.Vorheriger` las den alten Stand aus `_geladen`,
  das erst *nach* dem Start des Nachfrage-Tasks zugewiesen wird. Der Starthinweis
  blieb dadurch aus. Jetzt wird der abgelegte Stand mitgegeben. Lehre: ein
  Vorgänger, der an der Reihenfolge zweier Anweisungen hängt, ist keiner.
- 2026-08-29 (Umsetzung) — `docs/local-testing.md` behauptete seit dem 26.08.,
  der eingebaute Browser-Bereich könne keine Screenshots. Er kann es. Die
  übrigen Zeilen der Tabelle sind ungeprüft und könnten ebenso veraltet sein —
  eine vollständige Neumessung steht aus.
