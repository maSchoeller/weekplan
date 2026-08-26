# Schulden dieses Laufs

- 2026-08-26 `.gitignore` schloss `.claude/` komplett aus. Geaendert auf
  `.claude/worktrees/` — behoben, hier nur als Fund festgehalten.
- 2026-08-26 Feste Ports 5080/5180 statt dynamischer Vergabe. Aspire waere die
  Loesung, ist aber ein zusaetzliches Teil ohne zweiten Bedarf. Folge: zwei
  Worktrees koennen nicht gleichzeitig laufen.
- 2026-08-26 `css/styles.css` erfuellt die neue Spacing-Skala nicht (54 harte
  px-Werte, `--gap: 14px` liegt nicht auf dem 4er-Raster).
- 2026-08-26 Vom Rechenkern ist nur der Grundumsatz portiert.
- 2026-08-26 Kein `user-docs/` — bewusste Abweichung vom Bootstrap.
- 2026-08-26 Kein `static-web`-Preset angelegt, obwohl der Zuschnitt
  SWA + Container Apps generalisierbar aussieht.
- 2026-08-26 Keine Azure-Ressourcen, kein Deploy — die Zielform ist entschieden,
  aber noch nicht einmal probeweise ausgerollt.
- 2026-08-26 Tastatur-Aktivierung ist mit den Browser-Werkzeugen nicht pruefbar
  (synthetische Tastendruecke loesen die native Aktivierung nicht aus; belegt
  mit einem leeren `<button>` als Kontrollprobe in beiden Browsern). In
  `docs/local-testing.md` festgehalten, damit kuenftige Smoketests hier nicht
  falsch rot melden.
