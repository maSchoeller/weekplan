# Debt — Lauf 2026-08-29 Gaeste und Fuellregeln

- 2026-08-29 Der Rueckfall beim Fuellen ist still: greift ein Filter nicht
  (kein Wochenendgericht gepflegt), nimmt die App die volle Auswahl, ohne es
  zu sagen. Der Nutzer sieht eine regelwidrige Woche und keinen Grund.
  **Ausloeser:** wenn eine gefuellte Woche einmal unerklaerlich aussieht — dann
  braucht es einen Vermerk am Tag statt nur des Rueckfalls.
- 2026-08-29 Die Kochseite bekommt die Portionszahl als Adressparameter
  (`?portionen=3`), nicht Tag und Mahlzeit. Ein gemerkter Link traegt darum
  eine veraltete Zahl. **Ausloeser:** wenn die Kochseite eine zweite Angabe aus
  dem Wochenplan braucht — dann lohnt der Bezug auf Tag und Mahlzeit.
- 2026-08-29 Die Worktree-Pflege der Kickstart-Schritt 0 lief nicht: der
  Klassifizierer hat `git worktree remove` abgelehnt. Zwei saubere, leere
  Worktrees (`recipes-postgres-migration-ab1c2f`, `weekplan-mpc-gerichte-874239`)
  liegen weiter auf der Platte. **Ausloeser:** der naechste Lauf, oder eine
  Bash-Regel fuer `git worktree` in den Einstellungen.

## Aus der Umsetzung (Phase 3)

- 2026-08-29 **Der Fokus geht verloren, wenn aus „+ Gaeste" der Stepper wird.**
  Blazor ersetzt an der Stelle ein `<button>` durch ein `<span>`; der Fokus
  faellt auf `body`, und ein Tastaturnutzer muss sich zurueckhangeln. Innerhalb
  des Steppers bleibt der Fokus (zweimal Enter geprueft: 2 → 4). Dieselbe Form
  hat die App schon bei „Entfernen", es ist also kein neues Muster.
  **Ausloeser:** wenn die App das erste Mal ernsthaft mit der Tastatur bedient
  wird — dann braucht es Fokusverwaltung an allen drei Stellen auf einmal.
- 2026-08-29 Der Rueckfall greift lokal ueberall: kein einziges der 24
  Bestandsrezepte traegt `wochenende` oder `refeed`, also wird am Wochenende
  und am Refeed-Tag aus der vollen Auswahl gewaehlt. Sichtbar im Smoketest:
  Samstag bekam dasselbe Chili wie Mo-Mi. Loest sich mit der Pflege nach dem
  Ausrollen; bis dahin ist es der Beleg fuer Abnahmekriterium 9.
- 2026-08-29 Die Gaeste einzelner Tage ueberleben `vergeben` nicht ueber die
  Bloecke hinweg — genauer: die Einzeltage (Refeed, Wochenende) schliessen
  einander aus, die Bloecke wissen davon aber nichts. Bei gepflegten Merkmalen
  sind die Mengen ohnehin disjunkt. **Ausloeser:** wenn ein Wochenendgericht
  auch `prep` traegt und dann doppelt in der Woche steht.
