# Anforderungen — Rezepte aus der Datenbank

**Art:** Feature (neues Verhalten, neue Oberflaeche, neue Architektur) → volle
Kette 1 → 2 → 3 → 4.

Der Lauf `2026-08-26-cloud-migration` hat ihn angekuendigt: „Rezepte bleiben fest
wie heute im Repo. Eigene Rezepte anlegen ist ein eigener Lauf danach." Das ist
dieser Lauf.

## Problem

Die 24 Rezepte stecken im Quellcode. Eines hinzuzufuegen heisst heute: Datei
aendern, einchecken, ausrollen, warten. Das ist so schwerfaellig, dass es seit
dem ersten Tag **kein einziges Mal passiert ist** — und daran haengt mehr als
Bequemlichkeit. Die 24 verteilen sich zudem sehr ungleich: **15 Mittagsgerichte,
aber nur drei fuers Fruehstueck und sechs fuer den Abend**. Die automatische
Wochenfuellung hat fuer zwei von drei Mahlzeiten also fast keine Wahl — und
irgendwann steht dieselbe Joghurtschuessel zum vierten Mal auf dem Plan.

Dazu kommt ein zweiter Bruch: Gewicht und Wochenplan liegen laengst in der
Datenbank und sind auf jedem Geraet gleich. Die Rezepte sind das einzige, was
noch im Programm klebt.

Und die Anleitungen selbst sind zu knapp. Fuenf Saetze reichen, wenn man das
Gericht kennt. Sie reichen nicht, wenn man es zum ersten Mal kocht.

## Nutzer und Kontext

Ein einziger Nutzer, drei Situationen:

| Wann | Wo | Was |
|---|---|---|
| Abends, nebenbei | Laptop, mit Claude Code | Ein Rezept anlegen oder nachbessern |
| Sonntag | Laptop, in Ruhe | Woche planen, Einkaufsliste ziehen |
| Beim Kochen | Handy in der Kueche, Haende beschaeftigt | Ein Rezept Schritt fuer Schritt abarbeiten |

Das Pflegen der Rezepte passiert **nicht in der App**. Es passiert im Gespraech
mit Claude Code auf dem Laptop — dort, wo der Nutzer ohnehin arbeitet.

## Ziele

1. **Ein Rezept entsteht im Gespraech, nicht im Ausrollen.** Beschreiben,
   pruefen, fertig — ohne Commit, ohne Deploy, ohne Wartezeit.
2. **Der Bestand darf wachsen.** Aus 24 werden fuenfzig, ohne dass die App
   unuebersichtlich wird — vor allem beim Fruehstueck und am Abend.
3. **Eine Anleitung, nach der man ein Gericht zum ersten Mal kocht.** Nicht nur
   was, sondern warum und woran man es erkennt.
4. **Nichts verschiebt sich still.** Aendert sich ein Rezept, sieht der Nutzer
   es an der Stelle, an der es zaehlt.

## Der eine Moment

**„Leg mir was Warmes fuer den Abend an, um 700 kcal, mindestens 45 g Protein,
unter 30 Minuten"** — und zwei Minuten spaeter steht das Rezept am Handy, mit
Zutaten in den richtigen Abteilungen und einer Anleitung, nach der man kochen
kann. Kein Commit, kein Ausrollen, kein Neuladen von Hand.

## Szenarien

1. **Neues Rezept aus einer groben Ansage.** Der Nutzer beschreibt Ziel,
   Kalorien, Protein und Zeitrahmen. Claude Code arbeitet Zutaten, Naehrwerte
   und Anleitung aus, prueft die Abteilungen gegen die erlaubte Liste und legt
   das Rezept an. Beim naechsten Oeffnen der App steht es da.
2. **Nachbessern.** Beim Kochen faellt auf, dass 45 g Reis zu wenig sind. Der
   Nutzer sagt es Claude Code, das Rezept wird geaendert. Steht das Rezept schon
   im Wochenplan, weist die App am betroffenen Tag auf die veraenderten Zahlen
   hin — mit alt und neu.
3. **Zum ersten Mal kochen.** Das Handy liegt in der Kueche. Der Nutzer oeffnet
   die Rezeptseite, waehlt das Gericht, stellt die Portionen ein und arbeitet die
   Anleitung ab — auf einer Seite, die nur dieses eine Rezept zeigt.
4. **Ausrangieren.** Ein Rezept hat sich nicht bewaehrt und soll aus der
   automatischen Wochenfuellung verschwinden. Es wird geloescht; ein Tag, an dem
   es noch stand, zeigt den Namen und einen Weg, den Platz neu zu belegen.

## Was nie passieren darf

- **Zahlen verschieben sich still.** Eine geplante, vielleicht abgehakte Woche
  darf sich nicht unbemerkt umrechnen, weil jemand ein Rezept angefasst hat.
- **Beim Einkaufen fehlt etwas.** Im Laden, oft mit schlechtem Empfang, muss die
  Liste vollstaendig da sein.
- **Ein Rezept verschwindet spurlos.** Ein geplanter Tag darf nie einfach leer
  sein; er muss sagen, was dort stand.

## Abnahmekriterien

1. Ein per Claude Code angelegtes Rezept ist in der ausgerollten App sichtbar,
   ohne dass etwas eingecheckt oder ausgerollt wurde.
2. Ein Rezept mit einer Abteilung, die es nicht gibt, wird **abgelehnt**, und die
   Absage nennt die erlaubten Abteilungen.
3. Die Rezeptseite zeigt eine Uebersicht nach Mahlzeit; ein Rezept oeffnet sich
   als eigene Seite mit Portionsrechner, Zutaten und vollstaendiger Anleitung.
4. Die Anleitung wird als formatierter Text dargestellt — Absaetze,
   Zwischenueberschriften, Listen, Tabellen. Kein Bild, kein eingebettetes HTML,
   kein Nachladen von fremden Adressen.
5. Aendern sich Kalorien oder Protein eines Rezepts, das im Wochenplan steht,
   markiert der betroffene Tag das sichtbar mit altem und neuem Wert.
6. Wird ein geplantes Rezept geloescht, zeigt der Tag seinen Namen mit dem
   Vermerk „entfernt" und einen Weg, den Platz neu zu belegen.
7. Nach einem Start mit Netz stehen **Rezepte und Trainingsphasen** aus dem
   Zwischenspeicher und werden ohne Wartezeit gezeigt, auch beim Kaltstart des
   Servers. **Berichtigt am 29.08.:** die Einkaufsliste gehoert nicht dazu — sie
   wird aus dem Wochenplan gerechnet, und der liegt im Tagebuch auf dem Server.
   Der urspruengliche Satz versprach das mit und widersprach damit dem
   Nicht-Ziel weiter unten. Ohne Netz zeigt die App weiterhin die Fehlerkarte.
8. Beim allerersten Start ohne erreichbaren Server zeigt die App eine klare
   Meldung mit einem Weg, es erneut zu versuchen — keine leere, halb
   funktionierende Oberflaeche.
9. Der Umzug ist verlustfrei: jedes der 24 Rezepte steht nach der Erstbefuellung
   mit gleichem Namen, gleicher Kategorie, gleichen Naehrwerten und jeder Zutat
   in der Datenbank, und die Einkaufsliste einer Beispielwoche ist vor und nach
   dem Umzug Posten fuer Posten dieselbe.
10. Die alte statische App ist abgeschaltet; es gibt genau eine Form von
    weekplan.

## Entschieden (im Grilling, nicht erneut zu fragen)

**Datenhaltung.** Kein Postgres — die urspruengliche Ansage ist im Gespraech
gefallen, weil Cosmos bereits steht und im Free Tier dauerhaft 0 EUR kostet. Es
ziehen **alle Stammdaten** um: Rezepte, Training, Grundstock. Das Tagebuch bleibt
unveraendert. Eigener Behaelter `stammdaten`, ein Dokument je Rezept.

**Weg zum Client.** Ein oeffentlich lesbarer Endpunkt ohne Anmeldung — Rezepte
sind kein Geheimnis. Der Client legt die Antwort im Browser ab, zeigt sie beim
Start sofort und prueft im Hintergrund auf Neues; waehrend die App offen ist,
wird nicht laufend nachgesehen.

**Anleitung.** Der ausfuehrliche Text **ersetzt** die bisherige Schrittliste; es
gibt nicht beides. Beim Umzug werden die vorhandenen Schritte zu einer
nummerierten Liste.

**Pflege.** Nur ueber Claude Code, ueber einen abgesicherten Zugang am
ausgerollten Server: eigener Schluessel, Mengenbegrenzung, jede Aenderung im
Protokoll. **Schreibbar sind nur Rezepte** — Trainingsphasen und MET-Werte sind
Rechengrundlage und bleiben dem Commit vorbehalten; lesbar ist alles. Die
Abteilungsliste ist fest. Die Kennzeichnung „Vorrat" setzt der Aufrufer, der den
Grundstock dafuer lesen kann. Die Id entsteht aus dem Namen.

**Keine Bedienoberflaeche zum Anlegen.** Rezepte werden nicht in der App
bearbeitet — das ist ausdruecklich Aufgabe von Claude Code.

**Abbau.** Die statische App (`index.html`, `css/`, `js/`, `data/`) faellt samt
GitHub Pages. `docs/plan.md` bleibt als Rechengrundlage.

**Schnitte.** A Datenumzug samt Endpunkt, Zwischenspeicher, Erstbefuellung und
Abbau · B Anleitung und neue Rezeptansicht · C Pflege ueber Claude Code.

## Nicht-Ziele

- **Keine Bilder** zu Rezepten. Ausdruecklich verworfen: fremde Adressen in der
  App, tote Verweise, springende Seiten.
- **Kein Bearbeiten in der App** — kein Formular, kein Editor, kein Anlegen am
  Handy.
- **Keine Historie und kein Zurueckholen** geloeschter oder geaenderter Rezepte
  ueber das Protokoll hinaus.
- **Kein Stilllegen** als eigener Zustand — geloescht ist geloescht.
- **Kein Umzug des Tagebuchs**, keine Aenderung an Anmeldung oder Rechnung.
- **Kein Rezepte-Teilen**, keine zweiten Nutzer, keine Rechteverwaltung.
- **Die Einkaufsliste ohne Netz abhaken** bleibt offen (Schnitt B des
  Umzugslaufs) — dieser Lauf macht die Liste lesbar, nicht abhakbar ohne Netz.
