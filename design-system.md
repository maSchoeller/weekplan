# weekplan Design-System

Eigenständige Produktmarke: ruhig, sachlich, nah am Papier-Wochenplan. Das Werkzeug
soll man morgens um sieben mit einer Hand bedienen können. Jede UI-Entscheidung in
diesem Projekt bindet sich an diese Datei. WCAG 2.2 AA ist Untergrenze, nie Deckel.

## Bewusste Abweichung von der persönlichen Marke

Die globale Skill `personal-ui-brand` setzt Kobaltblau als Aktionsfarbe, verbietet
Dunkelmodus-Tokens und schreibt `Source Sans 3` vor. weekplan weicht in drei
Punkten ab — begründet, nicht aus Versehen:

1. **Grün statt Kobalt.** `#2f6f4e` ist seit dem ersten Commit die Farbe für
   „Ziel getroffen". Die Farbe trägt hier Bedeutung, nicht nur Marke.
2. **Dunkelmodus bleibt.** Die App wird abends und früh morgens benutzt; der
   Modus ist da und wegzunehmen wäre ein Rückschritt für den Nutzer.
3. **Systemschrift statt Webfont.** Das Projekt macht **keine externen Requests** —
   das ist eine Datenschutz-Zusage der README, kein Stilmittel. Ein Google-Font
   würde sie brechen.

Alles Übrige der persönlichen Marke gilt: 4-px-Raster, mittlere Radien, Fokus
sichtbar, Status nie allein über Farbe, kurze funktionale Übergänge.

## Farbtokens

Hell ist der Grundfall, Dunkel schaltet über `prefers-color-scheme`. Kein Wert
steht ausschließlich in einem Media-Block.

| Token | Hell | Dunkel | Rolle |
|---|---|---|---|
| `--bg` | `#f7f7f5` | `#16171a` | Seitengrund |
| `--surface` | `#ffffff` | `#1e2024` | Karten, Leisten, Felder |
| `--surface-2` | `#f0efec` | `#26292e` | Zweite Ebene, Zebrastreifen |
| `--border` | `#dedcd6` | `#33373d` | Trennung von Flächen |
| `--text` | `#1c1b19` | `#e9e8e4` | Überschriften, Fließtext |
| `--text-dim` | `#6a6862` | `#9a9a96` | Meta, Einheiten, Zustände |
| `--accent` | `#2f6f4e` | `#6cc296` | Aktion, Fokus, „Ziel erreicht" |
| `--accent-soft` | `#e3efe8` | `#1e3a2c` | Ruhige Hervorhebung |
| `--warn` | `#9a5b12` | `#d2a05a` | Warnung, immer mit Wort |
| `--warn-soft` | `#fbf0e0` | `#3a2f1c` | Warnfläche |
| `--bad` | `#a33a2c` | `#e0796a` | Fehler, immer mit Wort |

Gemessene Kontraste (Verhältnis zu ihrem dokumentierten Grund):
`--text-dim` auf `--bg` 5,1:1 hell / 6,4:1 dunkel · `--accent` auf `--surface`
6,0:1 hell / 7,7:1 dunkel. Wird ein Wert geändert, wird neu gemessen.

**Status nie allein über Farbe.** Grün, Warn und Fehler tragen immer ein Wort
und, wo vorhanden, ein Icon — Zahlenfelder, die sich nur einfärben, sind für
farbfehlsichtige Nutzer stumm.

## Spacing — verbindlich

Basis 4 px. Erlaubt sind **ausschließlich** diese Stufen:

| Token | Wert | Verwendung |
|---|---|---|
| `--sp-1` | `4px` | Zeichen zu Zeichen, Icon zu Label |
| `--sp-2` | `8px` | innerhalb eines Bausteins |
| `--sp-3` | `12px` | Feldpolster, Zeilen einer Liste |
| `--sp-4` | `16px` | Kartenpolster, Rand zum Fensterrand |
| `--sp-6` | `24px` | zwischen Bausteinen |
| `--sp-8` | `32px` | zwischen Abschnitten |
| `--sp-12` | `48px` | zwischen Bereichen einer Seite |

Jedes `margin`, `padding` und `gap` im Projekt kommt aus dieser Skala. Ein
Zwischenwert ist kein Stilmittel, sondern ein Fehler.

## Form, Schrift, Bewegung

- Radien: `--radius-control: 8px` für Knöpfe, Felder, Chips; `--radius-card: 12px`
  für Karten und Dialoge. Schatten nur für Schwebendes, und dann schwach.
- Schrift: `system-ui, -apple-system, "Segoe UI", Roboto, sans-serif`, 16px Basis,
  Zeilenhöhe 1.55. Hierarchie entsteht über Größe, Gewicht und Abstand — keine
  zweite Schriftfamilie, keine Zierschrift.
- Zahlen in Tabellen und Summen: `font-variant-numeric: tabular-nums`, sonst
  tanzen die Spalten beim Nachrechnen.
- Übergänge 150–200 ms, nur funktional. `prefers-reduced-motion: reduce` schaltet
  sie ab.

## Layout-Grundregeln

- **Touch-Ziele ≥ 44 × 44 px.** Gilt auch für Haken in Listen und Tabs.
- **Fokus ist immer sichtbar**: 2 px Outline in `--accent`, 2 px Offset. Niemals
  `outline: none` ohne Ersatz.
- Textbreite höchstens ~70 Zeichen; Seitenbreite `--maxw: 1120px`, Rand zum
  Fensterrand `--sp-4`.
- Mobil zuerst: ab 375 px Breite darf nichts überlappen, abgeschnitten sein oder
  waagerecht scrollen. Breite Tabellen scrollen in ihrem eigenen Container, nie
  der Seitenkörper.
- Jeder Bildschirm hat genau eine Hauptaktion, und die ist ohne Scrollen
  erreichbar.
- Leer-, Lade- und Fehlerzustand gehören zu jedem Bildschirm dazu und werden mit
  entworfen, nicht nachgereicht.

## Im Blazor-Client

Das globale Stylesheet (`wwwroot/css/app.css`) enthält **nur** die Tokens dieser
Datei und die Grundregeln für `body`, Fokus und Bewegung. Alles Weitere liegt in
CSS-Isolation je Komponente (`.razor.css`). Kein Bootstrap, kein Utility-Framework.
