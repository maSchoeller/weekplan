# weekplan

Wochenplaner für Ernährung und Training: Gerichte auf Tage und Mahlzeiten legen,
Portionen einstellen, Einkaufsliste in Gramm erhalten — plus Trainingsplan mit
gewichtsabhängiger Verbrauchsrechnung.

**Live:** https://maschoeller.github.io/weekplan

## Was drin ist

- **Woche** — Gerichte auf Wochentage und Mahlzeiten legen. Tagessumme kcal und Protein
  läuft mit und färbt sich, wenn das Ziel getroffen ist. Ein Knopf füllt die Woche
  automatisch und wählt die Portionen so, dass jeder Tag nah am Kalorienziel landet.
- **Einkauf** — Wochenliste (Frischware, aus dem Wochenplan aggregiert, nach
  Supermarkt-Abteilungen sortiert, abhakbar) und Grundstock (einmaliger Vorratseinkauf).
- **Rezepte** — 24 Gerichte mit Grammangaben pro Portion und Zubereitungsschritten,
  skalierbar über die Portionsanzahl.
- **Training** — Fünf Phasen mit Wochenplan, Verbrauchstabelle beim aktuellen Gewicht,
  Kraftplan A/B und Regelwerk.
- **Ich** — Gewicht, Zielgewicht, Größe, Alter, Zieltermin, Proteinfaktor. Daraus
  Grundumsatz, Zielaufnahme, Countdown, Gewichtsverlauf mit 7-Tage-Schnitt und
  Plateau-Erkennung.

## Datenschutz

**In diesem Repo steht keine einzige persönliche Zahl.** `data/*.json` enthält
ausschließlich generische Rezepte und Trainingsstrukturen. Gewicht, Zielgewicht,
Zieltermin und der Gewichtsverlauf liegen ausschließlich im `localStorage` des
Browsers und verlassen das Gerät nicht. Es gibt kein Backend, keine Analytics, keine
externen Requests.

Daraus folgt auch: Wer den Browserspeicher löscht, verliert seine Daten. Und die Daten
sind pro Gerät — was am Handy eingetragen wird, steht nicht am Laptop.

## Aufbau

```
index.html          Gerüst und Tabs
css/styles.css      Gestaltung, hell und dunkel
js/app.js           Rechenlogik, Rendering, localStorage
data/rezepte.json   24 Rezepte mit Zutaten in Gramm
data/training.json  Phasen, MET-Werte, Kraftplan, Regeln
data/grundstock.json Vorratseinkauf
docs/plan.md        Methodik: Formeln, Phasenlogik, Begründungen
```

Kein Build, keine Dependencies, kein Framework. Ein Rezept ändern heißt: JSON editieren
und committen. Deployment läuft über GitHub Pages aus `main`.

## Lokal starten

Die Seite lädt ihre Daten per `fetch`, deshalb funktioniert Doppelklick auf `index.html`
nicht — der Browser blockiert das aus dem Dateisystem. Stattdessen:

```bash
npx serve .
```

## Rechengrundlage

Grundumsatz nach Mifflin-St Jeor, Sport über MET-Werte gewichtsabhängig als
Netto-Verbrauch. Details und Begründungen in [docs/plan.md](docs/plan.md).

## Kein medizinischer Rat

Planungswerkzeug, keine ärztliche Beratung. Details am Ende von
[docs/plan.md](docs/plan.md).
