# Architektur

Das eine aktuelle Bild des Systems. Phase 2 jedes Laufs schreibt hier fort —
was hier steht, gilt; was nicht hier steht, existiert nicht.

Stand: 2026-08-29. Das System hat **eine** Form. Die statische App ist mit dem
Lauf `2026-08-28-rezepte-aus-der-datenbank` (Schnitt A) abgeschaltet und aus dem
Repo entfernt; der Uebergangskorridor ist damit geschlossen.

## Client, Server, Datenbank

```
Weekplan.Client   Blazor WASM, statisches Artefakt
      |            HTTPS, eigene Herkunft, CORS
      v
Weekplan.Server   Minimal API, Container-Image
      |
      v
Cosmos DB         zwei Behaelter: tagebuch (je Nutzer), stammdaten (fuer alle)
```

- **Slices.** Ein Feature ist ein csproj-Paar: `Weekplan.Core.<Feature>` mit der
  Umsetzung und `Weekplan.Core.<Feature>.Contracts` mit den oeffentlichen Typen.
  Features sehen einander nur ueber `.Contracts`; nur der Server referenziert
  Umsetzungen. Jede Umsetzung hat genau einen Eingang, eine
  `Add<Feature>()`-Erweiterung, damit ihre Typen `internal` bleiben koennen.
  Grenzverletzungen sind Compilerfehler — das ist der Zweck.
- **Slices.** `Weekplan.Core.Anmeldung` (Konto pruefen, Merkmal ausstellen und
  pruefen), `Weekplan.Core.Rechnen` (reine Rechnung, keine Persistenz),
  `Weekplan.Core.Tagebuch` (Profil, Gewichtsverlauf, Wochenplan, Haken).
  Heute gebaut ist davon `Rechnen` mit `IGrundumsatzRechner`; es waechst um
  Alltagsumsatz, MET-Netto, Phasensport, Bilanz, Tagesziel, 7-Tage-Schnitt und
  Plateau-Erkennung — die Funktionen, die `docs/plan.md` beschreibt. Sie bleiben
  rein, deshalb liegt dort das Gewicht der Tests.
- **Rezepte, Training, Grundstock** liegen seit dem 29.08.2026 in der Datenbank,
  im eigenen Slice `Weekplan.Core.Stammdaten` und im eigenen Behaelter
  `stammdaten` (Partitionsschluessel `/art`, 400 RU/s). Ein Dokument je Rezept,
  je ein Dokument fuer `training`, `grundstock` und `abteilungen`. Der Grund fuer
  den Umzug: ein Rezept hinzuzufuegen hiess vorher einchecken und ausrollen, und
  ist deshalb nie passiert.
  - Der Sammeltyp heisst `Stammdatensatz` und die Umsetzung `Stammdatendienst` —
    `Stammdaten` als Typname verliert innerhalb von `Weekplan.Core.*` gegen den
    gleichnamigen Namensraum.
  - Die Zubereitung ist **ein Markdown-Feld** (`Anleitung`) und keine Schrittliste
    mehr. Gerendert wird es im Browser mit Markdig, mit abgeschaltetem
    HTML-Durchlass — ein Rezepttext kann kein Skript in die Seite tragen.
  - **`GET /stammdaten`** liefert alles auf einmal, oeffentlich und ohne
    Anmeldung: die Daten gehoeren keinem Nutzer. Die Antwort traegt ein starkes
    ETag (halber SHA-256 ueber die Nutzlast); der Client legt sie im
    `localStorage` ab, zeigt sie beim Start sofort und fragt erst danach mit
    `If-None-Match` nach. Der Kaltstart wird dadurch unsichtbar und die App
    bleibt ohne Netz benutzbar.
  - Befuellt wird einmalig mit `tools/Weekplan.Stammdaten`, das danach jedes
    Dokument zurueckliest und Feld fuer Feld vergleicht.
  - **Gepflegt wird ueber `/mcp`**, nicht in der App: ein MCP-Endpunkt im
    selben Server, gesichert durch einen eigenen langlebigen Schluessel
    (`Mcp:Schluessel`, Container-App-Secret), mengenbegrenzt, jede Schreib-
    operation eine Logzeile. Fehlt der Schluessel in der Konfiguration, wird der
    Endpunkt **gar nicht eingehaengt** — lokal ist er damit standardmaessig aus.
    Eine Absage zaehlt alle Verstoesse auf und nennt die erlaubten Werte — sie
    reist als `McpException`, sonst verschluckt das SDK die Meldung.
  - **Elf Werkzeuge** seit dem 29.08.2026, deutsch benannt, auf zwei Typen
    geteilt: `Rezeptwerkzeuge` fuer die Rezepte, `Planwerkzeuge` fuer
    Trainingsplan, Grundstock und Abteilungen. Die Trennung verlaeuft am
    Gegenstand, nicht an Lesen und Schreiben.
    - **Schreibbar sind jetzt auch Trainingsplan, MET-Werte, Kraftplan,
      Grundstock und Abteilungen.** Jedes Schreibwerkzeug ersetzt sein Dokument
      vollstaendig — keine Teilaenderung, keine Konfliktaufloesung, eine Form
      fuer alles.
    - **Das Regelwerk bleibt lesend, und zwar durch den Typ:**
      `Trainingsentwurf` hat kein Regelfeld, es gibt also keinen Weg, Regeln zu
      uebergeben. Der Dienst legt die vorhandenen beim Schreiben zurueck.
    - **MET-Werte muessen mindestens 1 sein.** Die Rechnung `(MET − 1) × …`
      ergaebe darunter einen negativen Verbrauch und senkte die Zielaufnahme
      still — die Pruefung faengt das ab.
    - **Faellt eine Abteilung weg, in der noch Zutaten stehen**, wandern diese
      Zutaten in die Sammelabteilung `Sonstiges` ans Ende der Liste, und die
      Antwort nennt die Zahl. Nichts verschwindet, kein Rezept wird ungueltig.
      Der Vorgang schreibt Rezepte mit und ist nicht atomar.
    - Das **Tagebuch bleibt fuer MCP unerreichbar**, auch lesend. Ein
      durchgesickerter MCP-Schluessel oeffnet keine Nutzerdaten.
  - **Ein Rezept traegt `Prep`** neben `Kalt`: haelt es drei Tage im Kuehlschrank
    und waermt gut auf. Das automatische Fuellen der Woche waehlt an Werktagen
    nur aus den vorkochbaren; gibt es keine, bleibt die volle Auswahl. Ein
    Filter und keine Strafpunkte — gegen den Aufschlag fuer Wiederholung
    tariert, waere das Ergebnis nicht mehr vorhersagbar gewesen.
  - **Der Wochenplan sperrt nicht mehr, er sortiert.** Im Mahlzeiten-Slot stehen
    die passenden Gerichte in einer `optgroup` oben, alle anderen darunter —
    sonst liesse sich „mittags kochen, abends die Box" nicht abbilden.
  - **Aenderungen an der Rechnung fallen beim Start auf.** Der `Stammdatenlader`
    haelt beim Auffrischen alten und neuen Stand gleichzeitig; weichen die
    Zielaufnahmen voneinander ab, zeigt das Layout einen Streifen „Plan
    geaendert — deine Zielaufnahme liegt jetzt bei X statt Y kcal am Tag" mit
    „Zur Kenntnis". Verglichen wird nicht, **was** sich geaendert hat, sondern
    **was es bewirkt**: Grundstock und Abteilungen bewegen keine Zahl, ein
    Phasenname auch nicht. Gerechnet wird beim Anzeigen und nicht beim
    Auffrischen — sonst gewinnt das Nachladen gegen das Profil, und ohne Gewicht
    gibt es keine Zielaufnahme.
  - **Ein Planeintrag merkt sich die Naehrwerte**, mit denen geplant wurde
    (`KcalBeimPlanen`, `ProteinBeimPlanen`, beide optional). Gerechnet wird
    weiter mit dem aktuellen Rezept; die gemerkten Zahlen tragen allein den
    Hinweis „geaendert — vorher …" im Wochenplan. Ein geloeschtes Rezept zeigt
    der Tag als Namen aus seiner sprechenden Kennung, mit dem Vermerk
    „entfernt".
  - **Zwei Seiten** statt einer Kartenwand: `/rezepte` ist eine nach Mahlzeit
    gruppierte Liste, `/rezepte/{id}` die Kochseite mit Portionsrechner,
    Zutaten und Anleitung. Die Komponente heisst `Kochseite`, nicht `Rezept` —
    eine Komponente dieses Namens verdeckte sonst den Vertragstyp.
- **Datenhaltung.** Zwei Umsetzungen hinter einer Naht (`IAblage`): `DateiAblage`
  fuer lokal und die Tests, `CosmosAblage` fuer Azure. Welche gilt, entscheidet
  allein die Anwesenheit von `Tagebuch:Cosmos:Verbindung` — kein Schalter.
  Cosmos DB, ein Container, Partitionsschluessel `/nutzerId`,
  drei Dokumente je Nutzer: `konto`, `profil`, `woche`. `nutzerId` ist der
  kleingeschriebene Benutzername; bei geschlossener Registrierung kollidiert
  nichts, und ein App-Start liest zwei Dokumente aus einer Partition. Profil und
  Woche sind getrennt, weil sie in verschiedenen Rhythmen geschrieben werden und
  weil die Haken spaeter einzeln nachgetragen werden muessen.
- **Anmeldung.** Passwort-Hash (`PasswordHasher<T>`), Konto per Konsolenwerkzeug
  `tools/Weekplan.Konto` angelegt, keine Registrierungsseite, mengenbegrenzter
  Anmeldeendpunkt. Das Merkmal ist ein **signiertes Token im Browserspeicher**,
  gesendet als `Authorization: Bearer` — kein Cookie, weil ein Cookie ueber
  Herkunftsgrenzen `SameSite=None` braeuchte und Browser genau solche zunehmend
  einschraenken. Preis: ein Skriptangriff koennte das Token lesen. Ein Wechsel
  des Signaturschluessels wirft alle Geraete hinaus.
- **Client.** Referenziert die `.Contracts` direkt, also getypte Aufrufe ohne
  Codegenerierung. Das globale Stylesheet traegt nur die Tokens aus
  `design-system.md`, alles Weitere liegt in CSS-Isolation je Komponente.
  Eingaben wirken optimistisch; geschrieben wird gebuendelt nach 800 ms Ruhe und
  sofort beim Verlassen.
- **Trennung und ihr Preis.** Client und Server liegen auf verschiedenen
  Herkuenften. Der Server fuehrt darum eine Liste erlaubter Herkuenfte
  (`Cors:Origins`), der Client kennt die Server-Adresse aus
  `wwwroot/appsettings.json`.

## Korridor — was zwischen den Formen gilt

Beide Formen liegen gleichzeitig im Repo. Die statische App bleibt unangetastet
und live, bis die Zielform sie fachlich einholt; erst dann wird sie abgeloest.
Solange gilt: eine Aenderung an der Rechnung, die beide Formen betrifft, hat
`docs/plan.md` als gemeinsame Wahrheit — die Formeln stehen dort, nicht im Code.

## Ausgerollt — was in Azure steht

Subscription „Weekplan Production", Ressourcengruppe `rg-weekplan-prod`,
Region `westeurope`. Erwartete Kosten: 0 EUR.

| Was | Name | Adresse |
|---|---|---|
| Client | `weekplan-prod-web` (Static Web App, Free) | https://gentle-moss-035769303.7.azurestaticapps.net |
| Server | `weekplan-prod-api` (Container App, 0.25 CPU, min 0 / max 1) | https://weekplan-prod-api.redpebble-2b37be10.westeurope.azurecontainerapps.io |
| Daten | `cosmos-weekplan-prod` (Free Tier), db `weekplan`, Container `tagebuch` (400 RU/s) und `stammdaten` (400 RU/s) | — |
| Pflege | MCP am Server, `/mcp`, Schluessel als Container-App-Secret `mcp-schluessel` | siehe `.mcp.json` |

**GitHub Pages ist seit dem 29.08.2026 abgeschaltet.** Die alte Adresse
`maschoeller.github.io/weekplan` antwortet noch eine Weile aus dem
CDN-Zwischenspeicher und faellt dann weg.

Die Umgebungsvariablen der Container App: `Anmeldung__Schluessel`,
`Tagebuch__Cosmos__Verbindung` und `Stammdaten__Cosmos__Verbindung` — die
letzten beiden zeigen auf **dasselbe** Secret `cosmos-verbindung` —,
`Mcp__Schluessel` und `Cors__Origins__0`.

- **Der Weg dorthin** ist `.github/workflows/deploy.yml` und nur der: Push auf
  `main` testet, baut das Image nach `ghcr.io`, tauscht es in der Container App
  und laedt den Client in die Static Web App. Die Azure-Anmeldung laeuft ueber
  OIDC (App Registration `weekplan-deploy`, Federated Credential auf
  `refs/heads/main`) — **kein Azure-Geheimnis im Repo**.
- **Geheimnisse.** Anmeldeschluessel und Cosmos-Verbindung liegen als Secrets an
  der Container App, der Deploytoken der Static Web App als GitHub-Secret. Sonst
  nirgends.
- **Der Kaltstart.** `minReplicas: 0` heisst: nach einer Pause weckt der erste
  Ruf den Server erst auf. Das ist der Preis fuer 0 EUR und bewusst gewaehlt.
- **Das Konto** entsteht weiterhin nur von Hand, jetzt auch gegen Cosmos:
  `WEEKPLAN_COSMOS` setzen und `tools/Weekplan.Konto` laufen lassen. Es gibt
  keine Registrierung, also auch keinen Weg hinein ausser diesem.

## Offen

Die Haken der Einkaufsliste ohne Netz — dazu gehoert die Aufloesung, wenn zwei
Geraete denselben Posten gegensaetzlich haken. Sie wird bewusst einfach
ausfallen: je Posten gewinnt der spaetere Zeitstempel.

Aus dem laufenden Lauf `2026-08-28-rezepte-aus-der-datenbank`: **Schnitt B**
(Uebersicht und Kochseite) und **Schnitt C** (Pflege der Rezepte ueber Claude
Code, `/mcp`).
