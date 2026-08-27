# Design — weekplan nach Azure ausrollen

**Art:** Schnitt eines bereits durchgesprochenen Features. Die Anforderungen
stehen in `runs/2026-08-26-cloud-migration/requirements.md` und werden **nicht**
neu gestellt. Schnitt A hat die App gebaut; dieser Lauf stellt sie dorthin, wo
Handy und Laptop sie erreichen. Kette: 2 → 3 → 4.

Ohne diesen Lauf sind die Akzeptanzkriterien 1, 2, 3, 5 und 6 nicht pruefbar —
sie handeln alle von zwei Geraeten, und zwei Geraete gibt es erst mit einer
oeffentlichen Adresse.

## Zielbild

```
  Browser (Handy, Laptop)
        |
        +--------------> Static Web App  (Free)      weekplan-prod-web
        |                Blazor WASM, statische Dateien
        |                *.azurestaticapps.net
        |
        +--------------> Container App   (Consumption, scale-to-zero)
                         Minimal API, Image aus ghcr.io    weekplan-prod-api
                                |
                                v
                         Cosmos DB (Free Tier)       cosmos-weekplan-prod
                         db `weekplan`, container `tagebuch`, PK /nutzerId
```

Alles in einer **eigenen Subscription** "Weekplan Production", darin eine
Ressourcengruppe `rg-weekplan-prod`, Region `westeurope`.

**Warum eigene Subscription:** vom Nutzer so entschieden. Der Preis ist ein
Mehr an Verwaltung — die Provider muessen dort neu registriert, die
Rollenzuweisung fuer das Ausrollen neu gelegt werden. Der Gewinn: Cosmos Free
Tier ist genau **einmal je Subscription** zu haben, und in der Hochzeits-
Subscription bleibt er damit fuer die Hochzeit reserviert. Beide Projekte
koennen kostenlos eine Datenbank haben.

**Warum westeurope:** dieselbe Region wie das Schwesterprojekt, und Cosmos Free
Tier sowie Container Apps Consumption sind dort verfuegbar.

## Vorlage: wedding-planner

Das Schwesterprojekt laeuft in derselben Form und liefert die erprobten
Antworten. Uebernommen wird:

| Was | Wie dort | Warum uebernehmen |
|---|---|---|
| Registry | `ghcr.io`, kein ACR | ACR Basic kostet ~5 EUR/Monat; ghcr ist beim oeffentlichen Repo kostenlos |
| Container | 0.25 CPU, 0.5 Gi, min 0 / max 1 | scale-to-zero, ein einziger Nutzer — Kosten gehen gegen null |
| Ingress | extern, Port 8080 | der Dockerfile setzt `ASPNETCORE_HTTP_PORTS=8080` |
| Azure-Anmeldung im CI | OIDC (`azure/login` mit Client-Id) | kein Passwort und kein Secret-Ablauf im Repo |
| Client-Ausrollen | `Azure/static-web-apps-deploy` mit Token | trennt SWA vom Repo-Hook, ein Workflow fuer beides |

Nicht uebernommen: Azure SQL. weekplan bekommt Cosmos, wie im `design.md` des
Umzugslaufs entworfen.

## Was am Code geaendert wird

### 1. Der Dockerfile ist kaputt — er wird repariert

Er kopiert `Core.Rechnen` und `Server`, aber `Weekplan.Server.csproj`
referenziert ausserdem `Core.Anmeldung`, `Core.Anmeldung.Contracts`,
`Core.Tagebuch` und `Core.Tagebuch.Contracts`. `docker build` bricht heute ab.

Gegenmittel gegen die Wiederholung: der Deploy-Workflow baut das Image, also
faellt ein fehlendes Projekt kuenftig **vor** dem Ausrollen auf und nicht
danach.

### 2. `CosmosAblage` — die zweite Umsetzung hinter der bestehenden Naht

`IAblage` ist genau fuer diesen Fall geschnitten worden: ein Dokument je
(Nutzer, Name), was Cosmos mit Partitionsschluessel und `id` ohnehin ist. Neu
kommen dazu:

- `src/Weekplan.Core.Tagebuch/CosmosAblage.cs` — `internal sealed`, wie
  `DateiAblage`. `LesenAsync` ist ein Punktlesen (`ReadItemAsync` mit `id` und
  Partitionsschluessel), `SchreibenAsync` ein `UpsertItemAsync`. `NotFound`
  wird zu `null`, nicht zu einer Ausnahme.
- Ein Huellentyp, der `{ id, nutzerId, inhalt }` traegt. Grund: `IAblage`
  speichert beliebige `T`, Cosmos braucht aber `id` und `nutzerId` **auf der
  obersten Ebene** des Dokuments. Der Inhalt haengt darunter.
- `AddTagebuchInCosmos(verbindung, datenbank, container)` als zweiter Eingang
  neben `AddTagebuchInDateien`. Die Umsetzungen bleiben `internal`.
- Paket `Microsoft.Azure.Cosmos`.

**Preis:** die Dokumentform weicht von der im Umzugs-`design.md` skizzierten ab —
dort standen die Felder flach (`profil { id, nutzerId, gewicht, ... }`), hier
liegen sie unter `inhalt`. Der Grund ist die Naht: `IAblage` kennt `T` nicht und
kann darum nicht flach schreiben, ohne fuer jeden Typ etwas zu wissen. Flach zu
schreiben hiesse, die Naht aufzugeben. Die Naht ist mehr wert — sie ist es, die
den lokalen Betrieb ohne Cosmos-Emulator ueberhaupt traegt.

### 3. `Program.cs` waehlt die Ablage nach Konfiguration

Liegt `Tagebuch:Cosmos:Verbindung` vor → Cosmos. Sonst → Dateien. Kein Schalter,
kein Flag: die Anwesenheit der Verbindung **ist** die Entscheidung. Lokal steht
sie nicht in `appsettings.Development.json`, in Azure kommt sie als Secret.

### 4. `tools/Weekplan.Konto` erreicht auch Cosmos

Das Konto entsteht weiterhin von Hand — es gibt keine Registrierungsseite, und
das ist Absicht. Damit der Nutzer das Konto in der ausgerollten App anlegen
kann, nimmt das Werkzeug die Cosmos-Verbindung aus der Umgebungsvariablen
`WEEKPLAN_COSMOS`. Ist sie gesetzt, schreibt es nach Cosmos, sonst wie bisher in
den Ordner. Das Passwort steht damit nie in diesem Verlauf und nie in einer
Datei — es geht durch die Hand des Nutzers direkt in den Hash.

### 5. Der Client kennt zwei Adressen

`wwwroot/appsettings.json` traegt kuenftig die **Produktionsadresse** der
Container App, `wwwroot/appsettings.Development.json` (neu) den `localhost:5080`
von `run-local.ps1`. Blazor WASM laedt beide, die Umgebungsdatei gewinnt. So
braucht weder der Workflow eine Textersetzung noch der Entwickler eine
Handbewegung.

Die Produktionsadresse steht damit im oeffentlichen Repo. Das ist kein Verlust:
sie ist ohnehin oeffentlich, sobald der Client sie im Browser aufruft.

### 6. `staticwebapp.config.json`

Blazor WASM ist eine Einzelseiten-App: ein direkter Aufruf von `/ich` muss auf
`index.html` zurueckfallen, sonst gibt es 404 statt Anmeldung — und
Akzeptanzkriterium 5 sagt ausdruecklich, dass **jede** Adresse zur Anmeldung
fuehrt. Dazu die MIME-Typen fuer `.wasm`, `.dat`, `.blat`.

## Was an Infrastruktur entsteht

| Ressource | Name | Tarif | Kosten |
|---|---|---|---|
| Subscription | Weekplan Production | MCA | — |
| Ressourcengruppe | `rg-weekplan-prod` | — | — |
| Cosmos DB | `cosmos-weekplan-prod` | Free Tier, 400 RU/s manuell | 0 EUR |
| Container Apps Umgebung | `weekplan-prod-env` | Consumption | 0 EUR im Leerlauf |
| Container App | `weekplan-prod-api` | 0.25 CPU / 0.5 Gi, min 0 | ~0 EUR bei einem Nutzer |
| Static Web App | `weekplan-prod-web` | Free | 0 EUR |
| App Registration | `weekplan-deploy` | — | — |

Erwartete Rechnung: **0 EUR**, solange der Free Tier von Cosmos steht und die
Container App die meiste Zeit auf null Replikaten liegt. Das erste Aufwachen
kostet den Nutzer dafuer ein paar Sekunden Wartezeit — bei einem Werkzeug, das
morgens einmal geoeffnet wird, ist das der richtige Tausch.

### Geheimnisse

Drei Werte duerfen nicht ins Repo:

| Wert | Wo er lebt | Wer ihn braucht |
|---|---|---|
| `Anmeldung:Schluessel` | Container-App-Secret, zufaellig erzeugt (32 Byte base64) | der Server, zum Signieren der Merkmale |
| Cosmos-Verbindung | Container-App-Secret | der Server und einmalig das Konto-Werkzeug |
| SWA-Deploytoken | GitHub-Secret `AZURE_STATIC_WEB_APPS_API_TOKEN` | der Workflow |

Fuer Azure meldet sich der Workflow **ohne** Geheimnis an: OIDC mit einer
Federated Credential auf `repo:maSchoeller/weekplan:ref:refs/heads/main`.
Nur dieser Branch dieses Repos kann sich als diese Identitaet ausgeben.

**Preis:** die Cosmos-Verbindung ist ein Schluessel, kein Managed Identity.
Mit Managed Identity gaebe es gar kein Geheimnis — aber dann koennte das
Konto-Werkzeug vom Laptop des Nutzers nicht mehr an die Datenbank, und das
Anlegen des einen Kontos ist der einzige Weg ueberhaupt hinein. Das steht in
`debt.md`.

## Die Reihenfolge — und warum sie so ist

Es gibt eine Henne-Ei-Lage: der Server muss die Adresse des Clients kennen
(CORS), der Client die des Servers (`Api:BaseUrl`). Beide Adressen vergibt Azure
erst beim Anlegen. Darum:

1. **Subscription** anlegen, Provider registrieren (`Microsoft.App`,
   `Microsoft.DocumentDB`, `Microsoft.Web`, `Microsoft.OperationalInsights`).
2. **Ressourcengruppe**, **Cosmos**, **Container-Apps-Umgebung**, **Static Web
   App** anlegen. Jetzt stehen beide Adressen fest.
3. **Code aendern** (die sechs Punkte oben), Adressen eintragen, `dotnet test`
   gruen.
4. **Container App** anlegen — mit einem Platzhalter-Image, denn das eigene gibt
   es noch nicht. CORS-Herkunft und beide Secrets sind hier schon gesetzt.
5. **OIDC** einrichten: App Registration, Federated Credential, Rolle
   Contributor auf `rg-weekplan-prod`.
6. **`deploy.yml`** schreiben, Repo-Variablen und -Secret setzen, committen und
   auf `main` bringen. Der Workflow baut, testet, schiebt das Image nach ghcr,
   tauscht das Platzhalter-Image aus und laedt den Client hoch.
7. **Pruefen**: `/health` antwortet, der Client laedt, der Anmeldebildschirm
   steht, ein falsches Passwort wird abgewiesen.
8. **Uebergeben**: der Nutzer legt sein Konto mit dem Werkzeug an und meldet
   sich in Chrome an.

Schritt 8 gehoert bewusst dem Nutzer: er waehlt das Passwort, er tippt es ein.
Damit steht es in keinem Verlauf, in keiner Datei und in keiner Prozessliste
ausser seiner eigenen.

## Entscheidungen und ihr Preis

| Entscheidung | Preis |
|---|---|
| Eigene Subscription | mehr Verwaltung; dafuer bleibt der Cosmos Free Tier der Hochzeit erhalten |
| Cosmos statt Azure Files | eine Stunde Mehrarbeit und ein neues Paket; dafuer loest sich die Ablage-Schuld vom 26.08. auf |
| Inhalt unter `inhalt` statt flach | die Dokumente lesen sich in der Azure-Konsole eine Ebene tiefer; dafuer bleibt `IAblage` schmal |
| ghcr statt ACR | das Image ist oeffentlich lesbar — es enthaelt nur kompilierten Code, keine Geheimnisse |
| Verbindungsschluessel statt Managed Identity | ein Geheimnis mehr; dafuer kommt das Konto-Werkzeug ueberhaupt an die Datenbank |
| Platzhalter-Image beim Anlegen | die App ist ein paar Minuten lang da, aber falsch; dafuer keine Sonderbehandlung des ersten Laufs |
| scale-to-zero | der erste Aufruf nach einer Pause wartet ein paar Sekunden auf den Kaltstart |
| Produktionsadresse im oeffentlichen Repo | keiner — sie ist ohnehin oeffentlich |
