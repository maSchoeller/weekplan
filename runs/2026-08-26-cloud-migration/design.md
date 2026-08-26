# Design — Umzug in die Cloud (Schnitt A)

Schnitt A liefert die App vollwertig und geraeteuebergreifend: Anmeldung,
Datenhaltung, alle fuenf Tabs. Die Haken ohne Netz sind **Schnitt B** — sie sind
der einzige Teil, der ohne den Rest keinen Wert haette, und der einzige, der eine
Konfliktaufloesung braucht.

## UX-Konzept

Grundsatz: der Nutzer soll nichts neu lernen. Der Aufbau bleibt der von heute —
fuenf Tabs, dieselben Begriffe, dieselben Zahlen an denselben Stellen. Neu sind
genau drei Dinge: ein Anmeldebildschirm davor, ein Hinweis wenn etwas nicht
gespeichert werden konnte, und der Tab „Ich" beginnt mit dem Gewichtsfeld.

### Anmelden

Ein Bildschirm, eine Karte, mittig, hoechstens 380 px breit:

```
        weekplan

  Benutzername [___________]
  Passwort     [___________]

  [ Anmelden ]

  (Fehlerzeile, nur nach Fehlversuch)
```

Benutzername **und** Passwort, nicht nur Passwort: Passwortverwaltungen fuellen
so zuverlaessig aus, und ein einzelnes Feld auf einer oeffentlichen Adresse laedt
zum Durchprobieren ein. Die Felder tragen `autocomplete="username"` und
`current-password`. Nach Fehlversuch bleibt der Benutzername stehen, das
Passwort wird geleert, der Fokus springt dorthin. Die Fehlermeldung nennt nie,
welches der beiden falsch war.

Zustaende: Ruhe · sendet (Knopf gesperrt, Beschriftung „Meldet an …") · Fehler
(rote Zeile mit Wort, nicht nur Farbe) · zu viele Versuche („Zu viele Versuche.
Warte eine Minute.").

### Nach der Anmeldung

Wer ein gueltiges Merkmal auf dem Geraet hat, sieht den Anmeldebildschirm nie
wieder — die App oeffnet direkt die Woche. Ohne Merkmal fuehrt **jede** Adresse
zum Anmelden, auch ein direkter Aufruf von `/ich`.

### Die fuenf Tabs

Aufbau wie heute. Aenderungen:

1. **Ich beginnt mit dem Gewicht.** Ganz oben, ohne Scrollen, ein Feld „Gewicht
   heute" mit Zahlentastatur (`inputmode="decimal"`), daneben der Knopf
   „Eintragen". Enter im Feld traegt ein. Danach steht die Zahl sofort im
   Verlauf — gespeichert wird im Hintergrund. Erst darunter kommen Profil,
   Rechnung und Verlauf wie gehabt.
2. **Abstaende aus der Skala.** Jedes Polster und jeder Zwischenraum kommt aus
   `design-system.md`; Haken, Tabs und Auswahlfelder werden 44 px hoch.
3. **Ein Zustandsstreifen** unter der Kopfzeile, normalerweise unsichtbar. Er
   erscheint nur, wenn etwas nicht gespeichert werden konnte: „Nicht gespeichert
   — erneut versuchen" mit Knopf.

### Optimistisch speichern

Jede Eingabe wirkt sofort in der Oberflaeche; der Server folgt. Das ist der
einzige Weg, „zwei Sekunden" zu halten, ohne bei jedem Tastendruck zu warten.
Scheitert das Speichern, kommt der Zustandsstreifen — die Zahl bleibt sichtbar,
sie ist nur noch nicht sicher.

Geschrieben wird gebuendelt: nach 800 ms Ruhe, und sofort beim Tabwechsel oder
Verlassen der Seite. Ein Wochenplan-Klick loest also keinen einzelnen Schreibruf
aus, sondern der letzte einer Serie.

### Leer, laedt, Fehler

- **Laedt:** beim ersten Oeffnen eine ruhige Zeile „Wird geladen …" statt eines
  Geruests, das gleich wieder springt.
- **Leer:** frisches Konto → Woche ohne Gerichte, mit dem bestehenden Knopf
  „Woche automatisch fuellen" als Hauptaktion; Verlauf leer → „Noch kein Gewicht
  eingetragen."
- **Fehler beim Laden:** eine Karte mit Grund und „Erneut versuchen". Nie eine
  leere Seite.

## Architektur

`docs/architecture.md` ist fortgeschrieben; hier steht nur die Begruendung.

### Slices

| Slice | Verantwortung |
|---|---|
| `Weekplan.Core.Anmeldung` | Konto pruefen, Merkmal ausstellen und pruefen |
| `Weekplan.Core.Rechnen` | reine Rechnung, keine Persistenz (bestehend, wird erweitert) |
| `Weekplan.Core.Tagebuch` | die Daten, die dem Nutzer gehoeren: Profil, Verlauf, Woche, Haken |

`Rechnen` waechst um Alltagsumsatz, MET-Netto, Phasensport, Bilanz, Tagesziel,
7-Tage-Schnitt und Plateau-Erkennung — genau die Funktionen aus `js/app.js`, die
`docs/plan.md` beschreibt. Sie bleiben rein: Eingabe rein, Zahl raus, kein
Speicher, keine Uhr. Deshalb sind sie vollstaendig testbar, und deshalb liegt
dort das Gewicht der Tests.

Rezepte, Training und Grundstock bleiben statische Dateien und ziehen mit dem
Client um (`wwwroot/data/`). Kein Slice, kein Server, keine Datenbank — sie sind
fuer alle gleich und aendern sich nur durch einen Commit.

### Datenhaltung

Cosmos DB, ein Container, Partitionsschluessel `/nutzerId`. Drei Dokumente:

```
konto   { id:"konto",  nutzerId, benutzername, passwortHash }
profil  { id:"profil", nutzerId, gewicht, ziel, groesse, alter, zieltermin,
                       proteinFaktor, phase, tempo, verlauf:[{datum,kg}] }
woche   { id:"woche",  nutzerId, plan, refeedTag, rotation,
                       haken:{ woche:{}, grundstock:{} } }
```

`nutzerId` ist der kleingeschriebene Benutzername. Das spart eine
Suchmoeglichkeit ueber Partitionen hinweg — bei geschlossener Registrierung
kollidiert nichts. Ein App-Start liest zwei Dokumente aus einer Partition; das
sind einstellige RU. Der Free Tier mit 1000 RU/s wird nie eng.

Profil und Woche sind **getrennte** Dokumente, weil sie in verschiedenen
Rhythmen geschrieben werden — Gewicht taeglich, Plan sonntags. Und weil Schnitt B
die Haken einzeln nachtragen muss, ohne ein Profil zu ueberschreiben, das
inzwischen weitergezogen ist.

### Anmeldung

Passwort als Hash (`PasswordHasher<T>` aus `Microsoft.AspNetCore.Identity` —
die einzelne Klasse, nicht das ganze Identity-System). Das Konto legt ein kleines
Konsolenwerkzeug `tools/Weekplan.Konto` an; es gibt keine Registrierungsseite,
also auch keinen Weg, ueber den sich jemand anlegen koennte.

Der Anmeldeendpunkt ist mengenbegrenzt (eingebauter Rate Limiter), weil die
Adresse oeffentlich ist.

**Die architektonische Wette:** das Merkmal ist ein signiertes Token im Speicher
des Browsers, das der Client als `Authorization: Bearer` mitschickt — **kein
Cookie**. Grund: Client und Server liegen auf verschiedenen Herkuenften, ein
Cookie muesste `SameSite=None` sein, und genau solche Cookies schraenken Browser
zunehmend ein. Die vom Nutzer gewaehlte Trennung erzwingt diesen Weg.

**Ihr Preis:** ein erfolgreicher Skriptangriff im Client koennte das Token lesen;
ein Cookie mit `HttpOnly` koennte er nicht. Dagegen steht, dass Blazor
standardmaessig nichts als rohes HTML rendert und die App keine fremden Inhalte
anzeigt. Das Token hat kein Ablaufdatum — so wollte es der Nutzer. Ein Wechsel
des Signaturschluessels wirft alle Geraete hinaus; damit gibt es ein „ueberall
abmelden", ohne dass es gebaut werden musste.

### Datenfluss

```
Anmelden ─► POST /anmeldung ─► Token ─► localStorage
                                  │
Start ────► GET /tagebuch/profil ─┤  (Bearer)
            GET /tagebuch/woche ──┘
                                  │
Eingabe ──► optimistisch in die Oberflaeche
            └─ nach 800 ms Ruhe ─► PUT /tagebuch/profil bzw. /woche
```

## Entscheidungen und ihr Preis

| Entscheidung | Preis |
|---|---|
| Token statt Cookie | Skriptangriff koennte es lesen; die Trennung laesst wenig Wahl |
| Token ohne Ablauf | ein verlorenes, unversperrtes Geraet bleibt drin — so gewaehlt |
| Optimistisch speichern | die Oberflaeche kann kurz etwas zeigen, das noch nicht sicher ist; der Zustandsstreifen macht das sichtbar |
| Zwei Dokumente je Nutzer | zwei Lesevorgaenge beim Start statt einem |
| Rezepte bleiben statisch | Rezept aendern heisst weiterhin: Datei bearbeiten und veroeffentlichen |
| `nutzerId` = Benutzername | Benutzername ist spaeter nicht aenderbar, ohne die Daten umzuhaengen |
| Kein Ruecksetzweg | Passwort weg heisst Konto neu — die Daten duerfen weg |
