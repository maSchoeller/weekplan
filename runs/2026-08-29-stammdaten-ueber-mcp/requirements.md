# Anforderungen — Stammdaten über MCP

## Das Problem

Der Plan ist als Rechenwerk gebaut, das mitwachsen muss. `docs/plan.md` §2 sagt
es selbst: „Deshalb ist das Gewicht in weekplan kein Eintrag in einer Tabelle,
sondern der Parameter, an dem alle anderen Werte hängen." Genau an dieser Stelle
brechen Konzepte ab.

Nur wächst bisher die **Rechnung** mit, nicht der **Plan**. Rezepte lassen sich
im Gespräch pflegen, alles andere steckt im Commit: Trainingsphasen,
Wocheneinheiten, MET-Werte, Kraftplan, Grundstock und die Abteilungsreihenfolge.
Wer seinen Plan ans echte Leben anpassen will — ein Knie, das zwickt, ein
Zieltermin, der sich verschiebt, ein Trainingsvolumen, das zu ehrgeizig war —
braucht dafür einen Entwicklerrechner.

Konkret blockiert das den nächsten Schritt. Der neue Gerichte-Pool
(`essprofil.md`) braucht einen anderen Grundstock als den heutigen: der ist auf
Risottoreis und Falafel-Kichererbsen zugeschnitten, gebraucht werden
Sojagranulat, Thunfisch, Couscous und Panko. **An den produktiven Grundstock
kommt heute niemand heran** — `tools/Weekplan.Stammdaten` befüllt einmalig, und
über MCP ist er nur lesbar.

Zwei kleinere Ärgernisse hängen mit dran:

- Der Wochenplan **sperrt** statt zu sortieren: ein Mittagsgericht ist im
  Abend-Slot gar nicht wählbar. Wer mittags kochen und abends die vorgekochte
  Box essen will, kann das nicht abbilden.
- Einem Rezept sieht man nicht an, ob es drei Tage im Kühlschrank hält. Die
  Zeitangabe verrät es nicht: der Thunfischsalat braucht 15 Minuten und hält,
  das Rührei braucht 8 und hält nicht. Bei einem Alltag, der auf zwei
  vorgekochten Sorten pro Woche steht, ist das die wichtigste fehlende Auskunft.

## Nutzer und Kontext

Eine Person, zwei Situationen — beide stehen schon in
`runs/2026-08-28-rezepte-aus-der-datenbank/requirements.md` und gelten
unverändert:

| Wann | Womit | Was |
|---|---|---|
| Abends, nebenbei | Laptop, im Gespräch mit Claude Code | Den Plan anpassen |
| Morgens, unterwegs, beim Kochen | Handy | Den Plan benutzen |

Gepflegt wird **nicht in der App**, sondern im Gespräch. Das ist die bestehende
Entscheidung aus dem Rezeptlauf und wird hier nur auf die übrigen Stammdaten
ausgedehnt.

## Bereits entschieden

Vom Nutzer im Vorfeld festgelegt, wird hier nicht erneut gefragt:

1. **Schreibbar werden:** Trainingsplan (Phasen, Wochentage, Einheiten,
   Kraftplan), die Verbrauchsrechnung (MET-Werte), Grundstock und Abteilungen.
2. **Lesend bleibt:** das Regelwerk — die sechs Regeln aus dem Tab Training.
3. **Nicht angefasst wird:** das Tagebuch. Gewicht, Verlauf und 7-Tage-Schnitt
   liegen getrennt und bleiben es.
4. Die Sperre im Wochenplan **fällt und wird zur Sortierung**; der Default
   bleibt erhalten.
5. Rezepte bekommen ein Merkmal **„vorkochbar"** als Ja/Nein, neben dem
   vorhandenen „kalt ok".
6. **Reihenfolge:** dieser Ausbau zuerst, der neue Gerichte-Pool danach.

## Ziele

1. **Der Plan lässt sich im Gespräch anpassen, nicht nur die Rezepte.** Wer
   sagt „nimm das Laufband am Montag auf 60 Minuten runter", hat es danach so.
2. **Keine Zahl läuft still weg.** Ändert sich etwas, das rückwirkend rechnet,
   sieht der Nutzer es — ohne danach suchen zu müssen.
3. **Der Wochenplan steht dem Alltag nicht im Weg.** Vorschlagen ja, verbieten
   nein.
4. **Meal Prep ist im Modell abgebildet**, nicht nur im Fließtext einer
   Anleitung.
5. **Der Grundstock wird erreichbar** — die Voraussetzung für den neuen Pool.

## Szenarien

**S1 — Der Plan wächst mit.** Nach acht Wochen ist der Nutzer schneller
abgenommen als geplant. Er sagt Claude Code, wie die Phasen jetzt aussehen
sollen; sie werden geändert wie ein Rezept geändert wird. Beim nächsten Öffnen
der App steht der neue Plan da, und ein Hinweis nennt, was sich verschoben hat.
Kein automatisches Umplanen — siehe „Die Leitplanke".

**S2 — Der Grundstock zum neuen Pool.** Der Gerichte-Pool wird ausgetauscht.
Im selben Gespräch wandern Sojagranulat, Thunfisch, Couscous und Panko in den
Vorratseinkauf, Risottoreis und die getrockneten Kichererbsen fliegen raus.
Kein Commit, kein Ausrollen.

**S3 — Das Knie zwickt.** Der Dienstagslauf fällt für zwei Wochen weg, dafür
längeres Laufband. Der Nutzer sagt es; Trainingsplan und Verbrauchsrechnung
stimmen danach wieder mit der Realität überein — und damit auch die
Zielaufnahme.

**S4 — Mittags kochen, abends die Box.** Am Donnerstag ist mittags Zeit und
abends nicht. Der Nutzer legt das vorgekochte Gulasch auf den Abend-Slot und
kocht mittags frisch. Die App lässt ihn.

**S5 — Die Woche füllt sich richtig.** Der Knopf, der die Woche automatisch
belegt, nimmt werktags vorkochbare Gerichte und lässt die frischen für das
Wochenende übrig.

## Abnahmekriterien

1. Trainingsphasen, Wocheneinheiten, Kraftplan, MET-Werte, Grundstock und
   Abteilungen sind über MCP **lesbar und schreibbar**; das Regelwerk bleibt
   ausschließlich lesbar.
2. Eine abgelehnte Änderung **zählt alle Verstöße auf und nennt die erlaubten
   Werte** — wie bei Rezepten, damit der Aufrufer korrigieren kann statt zu
   raten.
3. Ändert sich etwas, das rückwirkend rechnet, zeigt die App **beim nächsten
   Start einen Hinweis**, was sich geändert hat. Danach ist er weg.
4. Rezeptänderungen bleiben, wie sie sind: der Hinweis „Geändert — vorher …"
   steht weiterhin am betroffenen Tag im Wochenplan.
5. Wird eine Abteilung entfernt, in der noch Zutaten stehen, **wandern diese
   Zutaten in eine Sammelabteilung** am Ende der Einkaufsliste. Keine Zutat
   verschwindet, kein Rezept wird ungültig.
6. Im Wochenplan sind in jedem Mahlzeiten-Slot **alle** Gerichte wählbar; die
   zur Mahlzeit passenden stehen oben.
7. Ein Rezept trägt das Merkmal **„vorkochbar"**. Das automatische Füllen der
   Woche bevorzugt werktags die vorkochbaren.
8. Jede Schreiboperation hinterlässt eine Logzeile — wie heute bei Rezepten.
9. Das Tagebuch ist über MCP **nicht erreichbar**, auch nicht lesend.

## Nicht-Ziele

- **Das Regelwerk schreibbar machen.** Ausdrücklich abgelehnt.
- **Bearbeiten in der App selbst.** Gepflegt wird im Gespräch; die App zeigt
  und rechnet.
- **Rückgängigmachen oder Versionsstände.** Der Nutzer hat „dass ich es nicht
  rückgängig machen kann" als Albtraum *nicht* gewählt. YAGNI.
- **Eine Änderungshistorie zum Nachschlagen.** Ein Hinweis beim Start reicht.
- **Zugriff auf Gewicht und Gewichtsverlauf.** Bleibt hinter der Anmeldung.

## Die Leitplanke: nicht zu kompliziert

Nachgefragt, ob mit „ich bin 4 kg leichter, rechne alles neu" die Rechnung oder
der Zuschnitt der Phasen gemeint sei, antwortete der Nutzer:

> „Scheiß egal, ich möchte einfach flexibel mit der KI befragen und anpassen
> können, mach es nicht zu kompliziert."

Das ist die wichtigste Anforderung des ganzen Laufs und schlägt im Zweifel jede
andere. Konkret heißt sie:

- **Keine Sonderlogik fürs Umplanen.** Kein automatisches Neuzuschneiden von
  Phasen, keine Terminrechnung, kein Assistent. Wer die Phasen ändern will,
  sagt es und ändert sie — genauso wie bei einem Rezept.
- **Gleiche Form für alles.** Die neuen Werkzeuge sehen aus und verhalten sich
  wie die vorhandenen Rezeptwerkzeuge: lesen, ändern, Absage mit Begründung.
  Wer eines kennt, kennt alle.
- **Im Zweifel weglassen.** Jede Bequemlichkeit, die nicht in den Szenarien
  steht, ist ein Nicht-Ziel.

Szenario S1 bleibt damit ein ganz gewöhnlicher Fall von „Phasen ändern", kein
eigenes Feature.
