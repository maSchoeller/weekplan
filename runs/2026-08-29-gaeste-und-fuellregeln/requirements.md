# Anforderungen — Gäste und Füllregeln

Erhoben am 29.08.2026 im Gespräch. Zwei Wünsche, ein Lauf: beide fassen die
Wochenplanung an, und der zweite entscheidet mit, wie der erste sich anfühlt.

---

## 1. Das Problem

**Portionen bedeuten heute zwei Dinge gleichzeitig.** Die Zahl am Gericht im
Wochenplan steuert die Tagesbilanz *und* die Einkaufsmenge. Isst jemand mit,
gibt es nur schlechte Wege:

- Portionen hochsetzen → der Einkauf stimmt, aber der Tag zeigt 4.000 kcal und
  wird nie grün. Die Bilanz, das eigentliche Werkzeug, ist unbrauchbar.
- Portionen lassen und im Kopf dazurechnen → die Bilanz stimmt, der Einkauf
  nicht. Beim Einkaufen wird regelmäßig etwas vergessen.

Beides ist im Alltag passiert. Der Besuch kommt in zwei Formen: Familie, die
mehrere Tage bleibt und alle Mahlzeiten mitisst, und spontan — mal einer, mal
drei, oft erst kurz vor dem Einkauf klar.

**Das automatische Füllen hält die eigenen Regeln nicht ein.** Es wählt nach
einer Bewertungsfunktion, die Wiederholung mit Strafpunkten belegt — und
widerspricht damit dem Betriebsmodell aus `docs/ernaehrungsplan.md`, wo genau
diese Wiederholung der Zweck ist. Konkret schiefgegangen ist:

1. **Mo–Fr steht fünfmal etwas anderes.** Meal Prep ist damit unmöglich;
   sonntags zwei Sorten vorzukochen passt auf keinen so gefüllten Plan.
2. **Fleisch und Wochenende sind vertauscht.** Werktags landet Fleisch im Plan,
   am Wochenende die aufgewärmte Box — genau umgekehrt zur Vereinbarung.
3. **Der Refeed-Tag bekommt Normaltags-Gerichte.** Samstag sind 3.332 kcal
   geplant; die normale Auswahl trägt das nicht, und laut `docs/plan.md` §1
   kostet ein Refeed ohne Rezepte ein bis zwei Defizit-Tage.

Das Ergebnis wirkt gewürfelt, obwohl es das nicht ist. Was fehlt, sind nicht
mehr Rechenpunkte, sondern die Regeln selbst.

---

## 2. Nutzer und Zusammenhang

Ein einziger Nutzer, sein eigener Plan. Zwei Situationen:

- **Sonntag am Rechner**, Woche planen und vorkochen. Hier wird gefüllt.
- **Unterwegs am Telefon**, kurz vor dem Einkauf oder wenn sich Besuch
  ankündigt. Hier werden Gäste eingetragen — das muss in einer Handbewegung
  gehen, mit dem Daumen, auf 375 px.

---

## 3. Ziele

1. Mitesser eintragen können, ohne die eigene Bilanz zu verfälschen.
2. Einkauf und Kochmenge wachsen dabei automatisch mit — kein Kopfrechnen.
3. Das automatische Füllen liefert eine Woche, die den Regeln aus
   `docs/ernaehrungsplan.md` entspricht und nicht nachbearbeitet werden muss.

---

## 4. Entschieden

### 4.1 Gäste

| Frage | Entscheidung |
|---|---|
| Maß | **Ein Gast = eine Portion wie meine.** Keine Aufschläge, keine halben Personen. |
| Eingabe | **Pro Tag eine Zahl**, gilt für alle drei Mahlzeiten. Einzelne Mahlzeiten sind abweichend setzbar. |
| Wirkung | Einkaufsliste, Kochmenge und Kochseite. **Nicht** die Bilanz. |
| Mehrere Gerichte auf einer Mahlzeit | Gäste essen, was ich esse: jeder Eintrag der Mahlzeit bekommt die Gästezahl zusätzlich. |
| Einfluss auf die Gerichtewahl | **Keiner.** Das Füllen wählt nach meinen Regeln; Gäste multiplizieren nur die Menge. |
| Beim Leeren und Füllen | **Gäste bleiben stehen.** Der Besuch ist eine Tatsache und hängt nicht am Plan. Beide Knöpfe fassen nur Gerichte an. |
| Kochseite aus dem Wochenplan | Zeigt die **Gesamtportionen des Tages** (ich + Gäste), Zutatenmengen entsprechend hochgerechnet. |

### 4.2 Füllregeln

| Frage | Entscheidung |
|---|---|
| Mo–Fr Mittag | **Zwei vorkochbare Sorten in zusammenhängenden Blöcken** von je zwei bis drei Tagen. |
| Mo–Fr Abend | Ebenso: zwei Sorten in Blöcken. |
| Frühstück | **Täglich rotieren** durch den Pool — die Mahlzeit, die Wiederholung am besten verträgt, und vorzubereiten ist nichts. |
| Sa/So | **Wochenendgerichte**: frisch gekocht, mit Fleisch. |
| Refeed-Tag | Nur **refeed-taugliche** Gerichte, Ziel ist die Refeed-Aufnahme. |
| Neue Merkmale am Gericht | Zwei Ja/Nein: **„Wochenendgericht"** und **„refeed-tauglich"**, neben dem vorhandenen „vorkochbar". Gepflegt über Claude Code. |
| Nochmal drücken | Gibt eine **andere, aber genauso regelkonforme** Woche. Die Rotation bleibt. |

---

## 5. Szenarien

**S1 — Besuch über drei Tage.** Freitagabend kommt Familie und bleibt bis
Sonntag. Der Nutzer öffnet die Woche, setzt an Freitag, Samstag und Sonntag je
„+2" — drei Eingaben. Am Freitag frühstücken die Gäste nicht mit, also setzt er
dort das Frühstück abweichend auf 0. Die Einkaufsliste wächst, die Tagesbilanz
zeigt weiter seine 2.165 kcal und wird grün wie sonst.

**S2 — Spontan im Laden.** Er steht mit dem Telefon vor dem Regal, ein Freund
kommt heute Abend dazu. Zwei Berührungen: der heutige Tag, „+1". Die
Einkaufsliste rechnet sich sofort um.

**S3 — Sonntag kochen.** Für Samstagabend sind zwei Gäste eingetragen. Er
öffnet das Gericht aus dem Wochenplan; die Kochseite steht auf 3 Portionen und
die Zutatenmengen sind dafür gerechnet. Er kocht los, ohne umzustellen.

**S4 — Woche füllen.** Sonntag, ein Klick. Mo–Mi steht mittags dieselbe
vorkochbare Sorte, Do–Fr eine zweite; abends ebenso zwei Sorten. Samstag steht
ein refeed-taugliches Wochenendgericht, Sonntag ein Wochenendgericht. Das
Frühstück wechselt täglich. Er muss nichts nachbessern.

**S5 — Nochmal würfeln.** Die Auswahl gefällt ihm nicht. Er drückt erneut; es
kommen andere Sorten, aber die Blöcke, das Wochenende und der Refeed-Tag sind
unverändert richtig.

---

## 6. Abnahmekriterien

1. An jedem Tag lässt sich eine Zahl zusätzlicher Esser setzen; sie gilt für
   alle drei Mahlzeiten des Tages, und jede Mahlzeit ist einzeln abweichend
   setzbar.
2. Einen Besuchstag einzutragen kostet **höchstens zwei Berührungen**, auch bei
   375 px Breite.
3. Tagessumme, Grün-Markierung und der Abgleich mit der Zielaufnahme zählen
   **ausschließlich die eigene Portion** — eine eingetragene Gästezahl
   verschiebt keine dieser Zahlen um ein kcal.
4. Die Einkaufsliste rechnet die Gästeportionen mit; ihre Mengen entsprechen
   der Summe aus eigenen und Gästeportionen.
5. Im Wochenplan ist an einem Tag mit Gästen ohne Umweg ablesbar, **wie viele
   Portionen zu kochen sind** und dass Gäste der Grund sind.
6. Die Kochseite, aus einem Wochenplan-Eintrag geöffnet, steht auf den
   Gesamtportionen dieses Tages und rechnet die Zutaten dafür hoch.
7. „Woche leeren" und „Woche automatisch füllen" lassen eingetragene Gäste
   unverändert stehen. Gerichte tauschen, entfernen oder den Refeed-Tag
   umstellen ebenso.
8. Nach „automatisch füllen" gilt für eine Woche mit vollem Pool:
   a. Mo–Fr stehen mittags **genau zwei** verschiedene Gerichte, jedes an
      **aufeinanderfolgenden** Tagen, jeder Block zwei oder drei Tage lang.
      Abends dasselbe.
   b. Alle werktags gewählten Mittag- und Abendgerichte tragen „vorkochbar".
   c. Sa und So tragen „Wochenendgericht" bei Mittag und Abend.
   d. Der Refeed-Tag trägt bei Mittag und Abend „refeed-tauglich".
   e. Das Frühstück ist an aufeinanderfolgenden Tagen verschieden, solange der
      Pool mehr als ein Frühstück hat.
9. Fehlt für eine Regel jede Auswahl (etwa kein einziges Wochenendgericht),
   füllt die App trotzdem — mit der nächstbesten Auswahl — statt den Tag leer
   zu lassen.
10. Zweimal hintereinander füllen ergibt eine andere Zusammenstellung, die alle
    Punkte aus 8 weiterhin erfüllt.
11. Die 18 Bestandsgerichte tragen die beiden neuen Merkmale passend gesetzt;
    Kriterium 8 ist mit dem echten Pool erfüllbar.

---

## 7. Nicht-Ziele

- **Keine Gäste-Bilanz.** Was die Gäste zu sich nehmen, wird nirgends gerechnet
  oder angezeigt. Es geht allein um Einkauf und Kochmenge.
- **Keine Personenverwaltung.** Gäste sind eine Zahl, keine Namen, keine
  Vorlieben, keine Allergien.
- **Keine abweichenden Gastportionen.** Kinder oder große Esser bildet der
  Nutzer ab, indem er die Zahl anpasst.
- **Kein Einfluss der Gäste auf die Gerichtewahl.** Auch nicht „an Besuchstagen
  frisch kochen".
- **Keine neue Mahlzeit und keine neue Kategorie.** Frühstück, Mittag, Abend
  bleiben.
- **Kein Vorplanen mehrerer Wochen.** Es gibt weiterhin eine Woche.

---

## 8. Die Refeed-Kollision

Der Refeed-Tag ist frei wählbar. Liegt er auf einem Werktag, stehen Refeed-Regel
und Meal-Prep-Regel gegeneinander. **Entschieden am 29.08.2026: der Refeed
gewinnt.** Er bekommt immer refeed-taugliche Gerichte, gleich welcher Wochentag.
Die Meal-Prep-Blöcke überspringen ihn: liegt der Refeed auf Mittwoch, bleiben
Mo/Di ein Block und Do/Fr ein zweiter, der Mittwoch fällt aus der Blockrechnung
heraus.

Abnahmekriterium 8a gilt damit für die **verbleibenden** Werktage: zwei Sorten,
zusammenhängende Blöcke, jeder Block zwei oder drei Tage — bei vier Werktagen
also zweimal zwei.
