# Debt — Lauf 2026-08-28-rezepte-aus-der-datenbank

- 2026-08-28 Branch und Worktree heissen `recipes-postgres-migration`, obwohl im
  Grilling **gegen** Postgres entschieden wurde — Cosmos steht bereits und ist
  dauerhaft kostenlos. Der Name ist damit falsch und wurde bewusst gelassen, um
  den laufenden Worktree nicht mitten im Lauf zu wechseln. **Ausloeser:** der
  Merge — spaetestens die Commit-Nachricht muss sagen, dass kein Postgres
  entstanden ist.
- 2026-08-28 Der Laufordner traegt den 28.08., die Arbeit laeuft in den 29.08.
  hinein. Bewusst nicht umbenannt: das Datum markiert den Beginn des Laufs.
- 2026-08-29 Abnahmekriterium 5 (Hinweis mit alter und neuer Zahl) laesst
  `PlanEintrag` im **Tagebuch**-Slice um zwei Felder wachsen, obwohl dieser Lauf
  das Tagebuch ausdruecklich nicht anfassen wollte. Es folgt zwingend aus dem
  Tabu „Zahlen verschieben sich still". **Ausloeser:** Phase 2 muss entscheiden,
  in welchen Schnitt das gehoert und was mit bereits gespeicherten Plaenen
  passiert, die diese Felder nicht haben.
- 2026-08-29 `docs/architecture.md` wird **nicht** in Phase 2 fortgeschrieben,
  wie die Pipeline es verlangt, sondern am Ende jedes Schnitts — die Datei ist
  laut ihrer eigenen Kopfzeile das Bild des Systems, wie es *ist*, und ein
  Entwurf ist noch nichts davon. Bewusste Abweichung. **Ausloeser:** wenn ein
  Lauf abbricht, bevor ein Schnitt merged — dann steht der Entwurf nur im
  Laufordner und nirgends sonst.
- 2026-08-29 Die erlaubten Kategorien stehen zweimal: in
  `Stammdaten.Contracts` fuer die Pruefung und in `Woche.Mahlzeiten` fuer
  Beschriftung und Anteil. Ein Ringschluss zwischen den Slices waere teurer, ein
  Test haelt beide zusammen. **Ausloeser:** eine vierte Mahlzeit.
- 2026-08-29 Abnahmekriterium 9 braucht die alten JSON-Dateien als
  Vergleichsgrundlage, obwohl dieser Lauf sie aus dem Repo entfernt. Sie ziehen
  als eingefrorene Pruefgrundlage ins Testprojekt um. **Ausloeser:** sobald das
  erste Rezept ueber Claude Code angelegt oder geaendert wurde, ist der
  Vergleich erledigt und die Dateien koennen weg.
- 2026-08-29 **Entwurf korrigiert:** es sind **24 Rezepte**, nicht elf — die Zahl
  in `requirements.md` und `design.md` war geraten und ist berichtigt. Die
  Verteilung ist der eigentliche Befund: 15 Mittagsgerichte, aber nur drei
  Fruehstuecke und sechs Abendessen.
- 2026-08-29 Der Sammeltyp heisst `Stammdatensatz` und die Umsetzung
  `Stammdatendienst`, weil `Stammdaten` als Typname innerhalb von
  `Weekplan.Core.*` immer gegen den gleichnamigen Namensraum verliert. Abweichung
  vom Muster der anderen Slices, in `design.md` festgehalten. **Ausloeser:** der
  naechste Slice, dessen Name auch ein Typname sein soll — dann ist es eine Regel
  und gehoert in die Vorlage.
- 2026-08-29 Die Cosmos-Verbindung steht ab jetzt **zweimal** in der
  Konfiguration: `Tagebuch:Cosmos:Verbindung` und `Stammdaten:Cosmos:Verbindung`,
  mit demselben Wert. Bewusst so, weil das Muster je Slice sonst bricht und ein
  gemeinsamer Schluessel beide Slices koppeln wuerde. Preis: beim Rotieren des
  Schluessels muessen beide Secrets mit. **Ausloeser:** der dritte Slice mit
  Cosmos-Bedarf — dann lohnt ein gemeinsamer `Cosmos:Verbindung`.
- 2026-08-29 **Im Browser gefunden, nicht im Test:** das ETag kam im Client nie
  an. Ueber eine Herkunftsgrenze gibt der Browser nur eine kleine Liste von
  Kopfzeilen frei, und `ETag` gehoert nicht dazu — der Zwischenspeicher haette
  bei jeder Pruefung die vollen 49 KB neu geladen, ohne dass irgendetwas rot
  geworden waere. Behoben mit `WithExposedHeaders("ETag")`, und der Fall steht
  jetzt als Servertest. Die Lehre: bei getrennten Herkuenften ist jede Kopfzeile,
  auf die der Client zugreift, eine eigene Freigabe. **Ausloeser:** die naechste
  Kopfzeile, die der Client lesen soll.
- 2026-08-29 Der eingebaute Browser-Bereich liefert erneut eine Viewport-Breite
  von **0 px**, und `list_connected_browsers` ist wieder leer — dieselbe Schuld
  wie am 26. und 27.08. Damit ist in Schnitt A **keine Sichtpruefung moeglich**:
  geprueft wurde ueber DOM, Netzwerkverkehr und gemessene Zahlen, nicht ueber ein
  Bild. Die Abstaende gegen `design-system.md` sind damit **nicht** geprueft.
  **Ausloeser:** Schnitt B faengt die Rezeptansicht neu an — spaetestens dort
  muss ein Browser mit echter Flaeche verbunden sein.
- 2026-08-29 **Sicherheitsluecke beim Markdown gefunden und geschlossen.**
  `DisableHtml()` allein haelt nur eingebettetes Markup auf. Markdowns *eigene*
  Syntax geht daran vorbei: `[klick](javascript:alert(1))` wurde zu einem
  lebendigen `<a href="javascript:...">`, und `![bild](https://fremd/…)` zu einem
  `<img>`, das eine fremde Adresse nachlaedt — beides gegen Abnahmekriterium 4
  und gegen die Datenschutz-Zusage der README. Jetzt drei Sperren: kein HTML,
  keine Bilder, Verweise nur mit http, https oder mailto; entfernte Verweise
  behalten ihren Text. Fuenfzehn Tests halten das fest. **Lehre:** eine
  Bibliothek, die „HTML abschaltet", schaltet nicht ab, was ihre eigene Sprache
  kann. **Ausloeser:** jede weitere Markdig-Erweiterung — sie kann neue
  Ausgabeformen mitbringen.
