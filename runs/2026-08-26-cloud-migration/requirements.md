# Anforderungen — Umzug in die Cloud

**Art:** Feature (neues Verhalten, neue Architektur) → volle Kette 1 → 2 → 3 → 4.

## Problem

weekplan rechnet gut, aber die Daten haengen am einzelnen Browser. Der Nutzer
plant sonntags am Laptop, schaut unterwegs am Handy nach und traegt taeglich sein
Gewicht ein — und diese drei Wege sehen heute drei verschiedene Staende. Was
sonntags geplant wurde, ist im Supermarkt nicht da. Ein neues Handy oder ein
anderer Browser heisst: wieder bei null.

Das ist kein Rechenproblem, sondern ein Ortsproblem. Die Zahlen muessen dorthin,
wo alle Geraete sie sehen.

## Nutzer und Kontext

Ein einziger Nutzer, drei Situationen:

| Wann | Wo | Was |
|---|---|---|
| Sonntag | Laptop, in Ruhe | Woche planen, Einkaufsliste ziehen |
| Werktags morgens | Handy oder Laptop | Gewicht eintragen |
| Unterwegs, im Laden | Handy, oft schlechter Empfang | Liste lesen und abhaken |

## Ziele

1. **Ein Stand ueber alle Geraete.** Was auf einem Geraet eingetragen wird, steht
   auf dem naechsten.
2. **Ein Geraetewechsel kostet nichts.** Anmelden, und alles ist da.
3. **Der Umzug aendert nichts an der Rechnung.** Formeln, Phasen und Mengen
   bleiben wie in `docs/plan.md` beschrieben.

## Der eine Moment

**Morgens das Gewicht eintippen.** Zahl von der Waage, zwei Sekunden, egal an
welchem Geraet — und Kurve, Schnitt und Countdown stimmen sofort ueberall. Daran
misst der Nutzer, ob der Umbau gelungen ist.

## Entschieden (im Interview, nicht erneut zu fragen)

- **Nutzerkreis:** nur der eine Nutzer. **Keine Registrierungsseite** — das Konto
  wird einmal von Hand angelegt, danach kann sich niemand sonst eines erstellen.
- **Vertraulichkeit:** dass die Zahlen beim Betreiber liegen, ist in Ordnung,
  solange niemand sonst an sie herankommt — kein Mensch, kein Link, keine
  Adresse. Ende-zu-Ende-Verschluesselung ist ausdruecklich **nicht** verlangt.
- **Anmeldung:** eigenes Passwort. **Einmal pro Geraet, danach nie wieder** —
  kein Ablauf, keine erneute Abfrage. Kein Abmelden von Geraeten aus der Ferne
  (das Geraet ist selbst gesperrt). **Kein Passwort-Ruecksetzweg**: geht es
  verloren, wird das Konto neu angelegt.
- **Ohne Netz:** die Einkaufsliste ist **lesbar und abhakbar**; die Haken wandern
  hoch, sobald wieder Netz da ist. Mehr braucht es offline nicht.
- **Altdaten:** was heute im Browser steht, darf weg. Sauberer Start.
- **Rezepte:** bleiben fest wie heute im Repo. Eigene Rezepte anlegen ist ein
  eigener Lauf danach.

## Szenarien

1. **Sonntag.** Der Nutzer fuellt am Laptop die Woche und hakt den Grundstock ab.
   Montag im Laden zeigt das Handy dieselbe Liste, ohne dass er etwas tun musste.
2. **Morgens.** Er tippt am Handy 84,3 ein. Abends sieht er am Laptop dieselbe
   Zahl im Verlauf, mit korrektem 7-Tage-Schnitt.
3. **Im Laden ohne Empfang.** Die Liste ist da, er hakt sechs Posten ab. An der
   Kasse kommt das Netz zurueck; die Haken stehen danach auch am Laptop.
4. **Neues Handy.** Er meldet sich einmal an. Woche, Liste, Verlauf sind da.
5. **Fremder ruft die Adresse auf.** Er sieht eine Anmeldung und sonst nichts.

## Akzeptanzkriterien

1. Nach einmaliger Anmeldung oeffnet sich die App auf demselben Geraet ohne
   erneute Eingabe — auch nach Tagen und nach einem Browser-Neustart.
2. Ein auf Geraet A eingetragenes Gewicht steht auf Geraet B beim naechsten
   Laden, samt 7-Tage-Schnitt, Countdown und Plateau-Erkennung.
3. Ein auf dem Laptop gefuellter Wochenplan erzeugt am Handy dieselbe Woche und
   dieselbe Einkaufsliste in Gramm.
4. Die Einkaufsliste ist ohne Netzverbindung sichtbar und abhakbar; nach
   Rueckkehr des Netzes stehen die Haken auch auf dem anderen Geraet.
5. Ohne Anmeldung ist keine einzige Zahl erreichbar — es gibt keine Adresse, die
   daran vorbeifuehrt.
6. Es existiert keine Registrierungsseite.
7. Gewicht eintragen dauert am Handy vom Oeffnen bis „gespeichert" hoechstens
   zwei Sekunden.
8. Grundumsatz, Sportverbrauch, Zielaufnahme und Einkaufsmengen liefern
   dieselben Zahlen wie die heutige App bei gleichen Eingaben.

## Nicht-Ziele

Rezepte bearbeiten · mehrere Nutzer · Passwort zuruecksetzen · Geraeteverwaltung
· Uebernahme der vorhandenen localStorage-Daten · vollstaendiges Offline-
Bearbeiten (nur die Haken der Einkaufsliste) · Ende-zu-Ende-Verschluesselung.
