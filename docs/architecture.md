# Architektur

Das eine aktuelle Bild des Systems. Phase 2 jedes Laufs schreibt hier fort —
was hier steht, gilt; was nicht hier steht, existiert nicht.

Stand: 2026-08-27. Das System ist gerade **zwischen zwei Formen**: die statische
App liegt weiter auf GitHub Pages, die Zielform ist gebaut (Lauf
`2026-08-26-cloud-migration`, Schnitt A) und **in Azure ausgerollt** (Lauf
`2026-08-27-azure-ausrollen`).

## Ist — die statische App

```
index.html      Geruest, fuenf Tabs (Woche, Einkauf, Rezepte, Training, Ich)
css/styles.css  Gestaltung, hell und dunkel
js/app.js       Rechenlogik, Rendering, localStorage — ein globaler Scope
data/*.json     Rezepte, Trainingsphasen, Grundstock
```

Kein Build, keine Abhaengigkeiten, kein Backend. Alle persoenlichen Werte liegen
im `localStorage` des Browsers und verlassen das Geraet nicht. Ausgeliefert von
GitHub Pages aus `main`.

Die Grenzen dieser Form, und der Grund fuer den Umbau: die Daten haengen am
einzelnen Browser. Was am Handy eingetragen wird, steht nicht am Laptop, und wer
den Browserspeicher loescht, verliert alles.

## Zielform — Client, Server, Datenbank

```
Weekplan.Client   Blazor WASM, statisches Artefakt
      |            HTTPS, eigene Herkunft, CORS
      v
Weekplan.Server   Minimal API, Container-Image
      |
      v
Cosmos DB         angebunden (CosmosAblage hinter IAblage)
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
- **Rezepte, Training, Grundstock** bleiben statische Dateien und ziehen mit dem
  Client um (`wwwroot/data/`). Kein Slice, kein Server, keine Datenbank — sie
  sind fuer alle gleich und aendern sich nur durch einen Commit.
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
| Daten | `cosmos-weekplan-prod` (Free Tier), db `weekplan`, Container `tagebuch` | — |

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

Die Haken der Einkaufsliste ohne Netz (**Schnitt B**) — dazu gehoert die
Aufloesung, wenn zwei Geraete denselben Posten gegensaetzlich haken. Sie wird
bewusst einfach ausfallen: je Posten gewinnt der spaetere Zeitstempel.

Eigene Rezepte anlegen und bearbeiten ist ein eigener Lauf danach.
