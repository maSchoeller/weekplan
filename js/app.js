/* weekplan — Wochenplaner für Ernährung und Training
 *
 * Grundsatz: In diesem Repo steht keine einzige persönliche Zahl.
 * Gewicht, Zielgewicht, Größe, Alter und Zieltermin liegen ausschließlich
 * im localStorage des Browsers und verlassen das Gerät nicht.
 */

'use strict';

/* ── Konstanten ─────────────────────────────────────── */

const TAGE = [
  { k: 'Mo', name: 'Montag',     ort: 'Homeoffice' },
  { k: 'Di', name: 'Dienstag',   ort: 'Büro' },
  { k: 'Mi', name: 'Mittwoch',   ort: 'Büro' },
  { k: 'Do', name: 'Donnerstag', ort: 'Büro' },
  { k: 'Fr', name: 'Freitag',    ort: 'Homeoffice' },
  { k: 'Sa', name: 'Samstag',    ort: '—' },
  { k: 'So', name: 'Sonntag',    ort: '—' }
];

const SLOTS = [
  { k: 'fruehstueck', label: 'Frühstück', anteil: 0.32 },
  { k: 'mittag',      label: 'Mittag',    anteil: 0.38 },
  { k: 'abend',       label: 'Abend',     anteil: 0.30 }
];

/* Neutrale Startwerte — bewusst NICHT die Werte des Nutzers.
 * Sie dienen nur dazu, dass die Seite vor der ersten Eingabe rechnen kann. */
const PROFIL_DEFAULT = {
  gewicht: 80,
  ziel: 75,
  groesse: 180,
  alter: 35,
  zieltermin: '',
  proteinFaktor: 2.0,
  phase: 'p1',
  tempo: null,         // kg pro Woche; null = Defizit kommt aus der Phase
  eigene: false        // true, sobald der Nutzer eigene Werte gespeichert hat
};

// Energiegehalt von 1 kg Körperfett. Bindeglied zwischen Defizit und Tempo.
const KCAL_PRO_KG = 7700;

const LS = 'weekplan.v1';

/* ── Zustand ────────────────────────────────────────── */

let DATA = { rezepte: null, training: null, grundstock: null };

let state = {
  profil: Object.assign({}, PROFIL_DEFAULT),
  plan: leererPlan(),
  refeedTag: 'Sa',
  haken: { woche: {}, grundstock: {} },
  verlauf: [],
  rotation: 0
};

function leererPlan() {
  const p = {};
  TAGE.forEach(t => {
    p[t.k] = {};
    SLOTS.forEach(s => { p[t.k][s.k] = []; });
  });
  return p;
}

function speichern() {
  try { localStorage.setItem(LS, JSON.stringify(state)); }
  catch (e) { console.warn('Speichern fehlgeschlagen', e); }
}

function laden() {
  try {
    const roh = localStorage.getItem(LS);
    if (!roh) return;
    const g = JSON.parse(roh);
    state.profil   = Object.assign({}, PROFIL_DEFAULT, g.profil || {});
    state.plan     = Object.assign(leererPlan(), g.plan || {});
    state.refeedTag = g.refeedTag || 'Sa';
    state.haken    = Object.assign({ woche: {}, grundstock: {} }, g.haken || {});
    state.verlauf  = Array.isArray(g.verlauf) ? g.verlauf : [];
    state.rotation = g.rotation || 0;
  } catch (e) { console.warn('Laden fehlgeschlagen', e); }
}

/* ── Rechnen ────────────────────────────────────────── */

// Mifflin-St Jeor, männlich. Für andere Konstellationen ist der Wert eine Näherung.
function grundumsatz(p) {
  return 10 * p.gewicht + 6.25 * p.groesse - 5 * p.alter + 5;
}

// Alltagsumsatz ohne geplanten Sport (Bürojob, Faktor 1,28).
function alltagsumsatz(p) {
  return grundumsatz(p) * 1.28;
}

// Netto-Kalorien einer Einheit: Grundumsatz während der Einheit ist abgezogen.
function einheitKcal(met, minuten, kg) {
  return (met - 1) * 1.05 * kg * (minuten / 60);
}

function aktivePhase() {
  if (!DATA.training) return null;
  return DATA.training.phasen.find(f => f.id === state.profil.phase) || DATA.training.phasen[0];
}

function phaseSport(phase, kg) {
  if (!phase) return { proTag: [], woche: 0 };
  const met = DATA.training.metWerte;
  const proTag = phase.tage.map(t => {
    const kcal = t.einheiten.reduce((s, e) => {
      const m = met[e.typ];
      return s + (m ? einheitKcal(m.met, e.min, kg) : 0);
    }, 0);
    return { tag: t.tag, ort: t.ort, einheiten: t.einheiten, kcal: Math.round(kcal) };
  });
  return { proTag, woche: proTag.reduce((s, t) => s + t.kcal, 0) };
}

function bilanz() {
  const p = state.profil;
  const phase = aktivePhase();
  const sport = phaseSport(phase, p.gewicht);
  const sportSchnitt = sport.woche / 7;
  const gesamt = alltagsumsatz(p) + sportSchnitt;

  // Tempo und Defizit sind dieselbe Größe in zwei Einheiten. Normalerweise gibt die
  // Phase das Defizit vor und das Tempo folgt daraus. Ist ein eigenes Tempo gesetzt,
  // dreht sich die Rechnung um: Tagesdefizit = kg pro Woche × 7.700 / 7.
  const phasenDefizit = phase ? phase.defizitZiel : 0;
  const eigenesTempo = typeof p.tempo === 'number' && p.tempo > 0;
  const defizit = eigenesTempo
    ? Math.round(p.tempo * KCAL_PRO_KG / 7)
    : phasenDefizit;

  // Der Refeed-Tag läuft ohne Defizit. Damit die Wochenbilanz trotzdem
  // 7 × Tagesdefizit ergibt, tragen die übrigen 6 Tage je ein Siebtel mehr.
  const defizit6 = defizit * 7 / 6;

  return {
    phase,
    sport,
    grundumsatz: Math.round(grundumsatz(p)),
    alltag: Math.round(alltagsumsatz(p)),
    sportSchnitt: Math.round(sportSchnitt),
    gesamt: Math.round(gesamt),
    defizit,
    phasenDefizit,
    eigenesTempo,
    normal: Math.round(gesamt - defizit6),
    refeed: Math.round(gesamt),
    protein: Math.round(p.proteinFaktor * p.ziel),
    wochendefizit: Math.round(defizit * 7),
    kgProWoche: (defizit * 7 / KCAL_PRO_KG),
    phasenTempo: (phasenDefizit * 7 / KCAL_PRO_KG)
  };
}

function tagesZiel(tagKey) {
  const b = bilanz();
  return tagKey === state.refeedTag ? b.refeed : b.normal;
}

/* ── Hilfsfunktionen ────────────────────────────────── */

const $  = (sel, root) => (root || document).querySelector(sel);
const $$ = (sel, root) => Array.from((root || document).querySelectorAll(sel));

function rezeptById(id) {
  return DATA.rezepte.rezepte.find(r => r.id === id);
}

function poolFuer(kategorie) {
  return DATA.rezepte.rezepte.filter(r => r.kategorie === kategorie);
}

function gramm(g) {
  if (g >= 1000) {
    const kg = g / 1000;
    return (Math.round(kg * 100) / 100).toLocaleString('de-DE') + ' kg';
  }
  return Math.round(g).toLocaleString('de-DE') + ' g';
}

function zahl(n) {
  return Math.round(n).toLocaleString('de-DE');
}

/* Zahleneingabe aus einem Textfeld. Akzeptiert "97,4" genauso wie "97.4" —
 * ein <input type="number"> verwirft das Komma stillschweigend, und beim
 * täglichen Wiegen ist die Kommazahl der Normalfall. null bei Unsinn. */
function zahlAus(roh) {
  const s = String(roh == null ? '' : roh).trim().replace(',', '.');
  if (s === '') return null;
  const v = parseFloat(s);
  return isNaN(v) ? null : v;
}

/* Umgekehrte Richtung: Zahl so ins Feld schreiben, wie sie hier gelesen wird. */
function feldWert(n) {
  return typeof n === 'number' ? n.toLocaleString('de-DE', { maximumFractionDigits: 2 }) : '';
}

function el(tag, cls, text) {
  const n = document.createElement(tag);
  if (cls) n.className = cls;
  if (text != null) n.textContent = text;
  return n;
}

function tagSumme(tagKey) {
  let kcal = 0, protein = 0;
  SLOTS.forEach(s => {
    (state.plan[tagKey][s.k] || []).forEach(e => {
      const r = rezeptById(e.id);
      if (r) { kcal += r.kcal * e.portionen; protein += r.protein * e.portionen; }
    });
  });
  return { kcal: Math.round(kcal), protein: Math.round(protein) };
}

/* ── Tab: Woche ─────────────────────────────────────── */

function renderWoche() {
  const b = bilanz();
  const wrapEl = $('#wochenraster');
  wrapEl.innerHTML = '';

  $('#wocheHint').textContent =
    `Ziel an Normaltagen: ${zahl(b.normal)} kcal und ${b.protein} g Protein. ` +
    `Am Refeed-Tag (${state.refeedTag}): ${zahl(b.refeed)} kcal. ` +
    `Grün bedeutet: innerhalb von 100 kcal am Ziel und Protein erreicht.`;

  TAGE.forEach(t => {
    const istRefeed = t.k === state.refeedTag;
    const ziel = istRefeed ? b.refeed : b.normal;
    const s = tagSumme(t.k);
    const trifftKcal = Math.abs(s.kcal - ziel) <= 100;
    const trifftProtein = s.protein >= b.protein;

    const karte = el('div', 'tag' + (istRefeed ? ' is-refeed' : ''));

    const kopf = el('div', 'tag-kopf');
    kopf.append(el('span', 'tag-name', t.name + (istRefeed ? ' · Refeed' : '')));
    kopf.append(el('span', 'tag-ort', t.ort));
    karte.append(kopf);

    const summe = el('div', 'tag-summe');
    summe.innerHTML =
      `<span class="${trifftKcal ? 'ok' : 'off'}">${zahl(s.kcal)}</span> / ${zahl(ziel)} kcal · ` +
      `<span class="${trifftProtein ? 'ok' : 'off'}">${s.protein}</span> / ${b.protein} g Protein`;
    karte.append(summe);

    SLOTS.forEach(slot => {
      const box = el('div', 'slot');
      const skopf = el('div', 'slot-kopf');
      skopf.append(el('span', 'slot-label', slot.label));
      box.append(skopf);

      const sel = document.createElement('select');
      sel.append(new Option('+ Gericht wählen', ''));
      poolFuer(slot.k).forEach(r => {
        sel.append(new Option(`${r.name} — ${r.kcal} kcal / ${r.protein} g`, r.id));
      });
      sel.addEventListener('change', () => {
        if (!sel.value) return;
        state.plan[t.k][slot.k].push({ id: sel.value, portionen: 1 });
        sel.value = '';
        speichern(); renderAlles();
      });
      box.append(sel);

      state.plan[t.k][slot.k].forEach((eintrag, i) => {
        const r = rezeptById(eintrag.id);
        if (!r) return;
        const zeile = el('div', 'eintrag');

        const name = el('div', 'name');
        name.append(document.createTextNode(r.name));
        name.append(el('small', null,
          `${zahl(r.kcal * eintrag.portionen)} kcal · ${Math.round(r.protein * eintrag.portionen)} g Protein`));
        zeile.append(name);

        const num = document.createElement('input');
        num.type = 'number'; num.min = '1'; num.max = '10'; num.step = '1';
        num.value = eintrag.portionen;
        num.title = 'Portionen';
        num.addEventListener('change', () => {
          eintrag.portionen = Math.max(1, Math.min(10, parseInt(num.value, 10) || 1));
          speichern(); renderAlles();
        });
        zeile.append(num);

        const del = el('button', 'del', '×');
        del.title = 'Entfernen';
        del.addEventListener('click', () => {
          state.plan[t.k][slot.k].splice(i, 1);
          speichern(); renderAlles();
        });
        zeile.append(del);

        box.append(zeile);
      });

      karte.append(box);
    });

    const tPhase = b.sport.proTag.find(x => x.tag === t.k);
    if (tPhase) {
      const met = DATA.training.metWerte;
      const txt = tPhase.einheiten
        .map(e => `${met[e.typ] ? met[e.typ].label : e.typ} ${e.min} min`)
        .join(' · ');
      karte.append(el('div', 'trainingzeile', `Training: ${txt} — ${zahl(tPhase.kcal)} kcal`));
    }

    wrapEl.append(karte);
  });
}

/* Füllt die Woche so, dass jeder Tag nah am Kalorienziel landet, das Proteinziel
 * erreicht wird und sich innerhalb der Woche möglichst nichts wiederholt.
 *
 * Bewertet wird jede Kombination aus Mittag- und Abendgericht mit Portionen 1–2.
 * Die Strafterme steuern die Abwägung: Protein wiegt schwer (12 pro fehlendem Gramm),
 * Wiederholung ebenfalls (250 je bereits verplantem Auftritt), große Portionen leicht (20).
 */
function autofill() {
  const b = bilanz();
  const fr = poolFuer('fruehstueck');
  const mi = poolFuer('mittag');
  const ab = poolFuer('abend');
  if (!fr.length || !mi.length || !ab.length) return;

  state.rotation = (state.rotation + 1) % 1000;
  const off = state.rotation;
  const benutzt = {};

  state.plan = leererPlan();

  TAGE.forEach((t, i) => {
    const ziel = t.k === state.refeedTag ? b.refeed : b.normal;

    // Frühstück rotiert fest — es soll bewusst gleichförmig sein (Overnight Oats).
    const f = fr[(i + off) % fr.length];

    let bestM = mi[0], bestA = ab[0], bestScore = Infinity;
    let pF = 1, pM = 1, pA = 1;

    mi.forEach(m => ab.forEach(a => {
      for (let fp = 1; fp <= 2; fp++)
        for (let mp = 1; mp <= 2; mp++)
          for (let ap = 1; ap <= 2; ap++) {
            const kcal = f.kcal * fp + m.kcal * mp + a.kcal * ap;
            const prot = f.protein * fp + m.protein * mp + a.protein * ap;
            const score = Math.abs(kcal - ziel)
              + Math.max(0, b.protein - prot) * 12
              + ((benutzt[m.id] || 0) + (benutzt[a.id] || 0)) * 250
              + ((fp - 1) + (mp - 1) + (ap - 1)) * 20;
            if (score < bestScore) {
              bestScore = score; bestM = m; bestA = a;
              pF = fp; pM = mp; pA = ap;
            }
          }
    }));

    benutzt[bestM.id] = (benutzt[bestM.id] || 0) + 1;
    benutzt[bestA.id] = (benutzt[bestA.id] || 0) + 1;

    state.plan[t.k].fruehstueck = [{ id: f.id, portionen: pF }];
    state.plan[t.k].mittag      = [{ id: bestM.id, portionen: pM }];
    state.plan[t.k].abend       = [{ id: bestA.id, portionen: pA }];
  });

  speichern(); renderAlles();
}

/* ── Tab: Einkauf ───────────────────────────────────── */

/* Aggregiert alle geplanten Portionen zu einer Einkaufsliste.
 * Vorratsware (Öle, Gewürze, Sojasauce, Brühe) bleibt draußen — die steht im
 * Grundstock. 6 g Olivenöl und 4 g Gewürzmischung auf einer Wochenliste sind
 * rechnerisch korrekt und praktisch Unsinn. */
function wochenzutaten() {
  const map = new Map();
  let vorratÜbersprungen = 0;

  TAGE.forEach(t => SLOTS.forEach(s => {
    (state.plan[t.k][s.k] || []).forEach(e => {
      const r = rezeptById(e.id);
      if (!r) return;
      r.zutaten.forEach(z => {
        if (z.vorrat) { vorratÜbersprungen++; return; }
        const key = z.name + '|' + z.abt;
        if (!map.has(key)) {
          map.set(key, { name: z.name, abt: z.abt, g: 0, stk: 0, quellen: new Set() });
        }
        const eintrag = map.get(key);
        eintrag.g += z.g * e.portionen;
        if (z.stk) eintrag.stk += z.stk * e.portionen;
        eintrag.quellen.add(r.name);
      });
    });
  }));

  return { map, vorratÜbersprungen };
}

/* Eier werden in Stück angegeben, alles andere in Gramm. */
function mengeText(z) {
  return z.stk ? `${z.stk} Stück` : gramm(z.g);
}

function renderEinkauf() {
  const ziel = $('#listeWoche');
  ziel.innerHTML = '';
  const { map, vorratÜbersprungen } = wochenzutaten();

  if (map.size === 0) {
    ziel.append(el('p', 'leer',
      'Noch nichts geplant. Wechsle auf den Tab „Woche" und drücke „Woche automatisch füllen".'));
  } else {
    ziel.append(el('div', 'notice',
      `${map.size} Positionen aus deinem Wochenplan, nach Supermarkt-Abteilungen sortiert. ` +
      `Gleiche Zutaten aus verschiedenen Gerichten sind zusammengerechnet. ` +
      (vorratÜbersprungen
        ? `Öle und Gewürze stehen nicht hier, sondern im Grundstock — du kaufst sie nicht wöchentlich.`
        : '')));

    DATA.rezepte.abteilungen.forEach(abt => {
      const posten = Array.from(map.values())
        .filter(z => z.abt === abt)
        .sort((a, b) => a.name.localeCompare(b.name, 'de'));
      if (!posten.length) return;

      const block = el('div', 'abteilung');
      block.append(el('h3', null, abt));
      posten.forEach(z => {
        block.append(hakenZeile('woche', z.name, mengeText(z),
          Array.from(z.quellen).join(', ')));
      });
      ziel.append(block);
    });
  }

  const gs = $('#listeGrundstock');
  gs.innerHTML = '';
  gs.append(el('div', 'notice warn', DATA.grundstock.hinweis));
  DATA.grundstock.gruppen.forEach(gruppe => {
    const block = el('div', 'abteilung');
    block.append(el('h3', null, gruppe.name));
    gruppe.artikel.forEach(a => {
      block.append(hakenZeile('grundstock', a.name, a.menge, a.reichweite));
    });
    gs.append(block);
  });
}

function hakenZeile(bereich, name, menge, sub) {
  const zeile = el('label', 'zeile');
  const cb = document.createElement('input');
  cb.type = 'checkbox';
  cb.checked = !!state.haken[bereich][name];
  if (cb.checked) zeile.classList.add('done');

  cb.addEventListener('change', () => {
    if (cb.checked) state.haken[bereich][name] = true;
    else delete state.haken[bereich][name];
    zeile.classList.toggle('done', cb.checked);
    speichern();
  });

  const txt = el('span', 'txt');
  txt.append(document.createTextNode(name));
  if (sub && sub !== '—') txt.append(el('small', 'sub', sub));

  zeile.append(cb, txt, el('span', 'menge', menge));
  return zeile;
}

/* ── Tab: Rezepte ───────────────────────────────────── */

function renderRezepte() {
  const ziel = $('#rezepteListe');
  const kat = $('#panel-rezepte .btn-toggle.is-active').dataset.kat;
  const port = Math.max(1, parseInt($('#rezeptPortionen').value, 10) || 1);
  ziel.innerHTML = '';

  const liste = DATA.rezepte.rezepte.filter(r => kat === 'alle' || r.kategorie === kat);

  liste.forEach(r => {
    const d = document.createElement('details');
    d.className = 'rezept';

    const sum = document.createElement('summary');
    const t = el('span', 't', r.name);
    if (r.kalt) {
      const pill = el('span', 'pill', 'kalt ok');
      pill.title = 'Funktioniert auch kalt — gut für Bürotage';
      t.append(document.createTextNode(' '));
      t.append(pill);
    }
    sum.append(t);
    sum.append(el('span', 'm',
      `${zahl(r.kcal * port)} kcal · ${Math.round(r.protein * port)} g Protein · ${r.zeitMin} min`));
    d.append(sum);

    const body = el('div', 'rezept-body');
    body.append(el('h4', null, port === 1 ? 'Zutaten für 1 Portion' : `Zutaten für ${port} Portionen`));
    const ul = el('ul', 'zutatenliste');
    r.zutaten.forEach(z => {
      const li = document.createElement('li');
      li.append(el('span', null, z.name));
      const b = document.createElement('b');
      b.textContent = z.stk ? `${z.stk * port} Stück` : gramm(z.g * port);
      li.append(b);
      ul.append(li);
    });
    body.append(ul);

    body.append(el('h4', null, 'Zubereitung'));
    const ol = el('ol', 'schritte');
    r.schritte.forEach(s => ol.append(el('li', null, s)));
    body.append(ol);

    d.append(body);
    ziel.append(d);
  });
}

/* ── Tab: Training ──────────────────────────────────── */

function renderTraining() {
  const ziel = $('#trainingInhalt');
  ziel.innerHTML = '';
  const b = bilanz();
  const phase = b.phase;
  if (!phase) return;

  ziel.append(el('div', 'notice', phase.beschreibung));

  const kpis = el('div', 'kpis');
  [
    ['Zeitraum', phase.zeitraum, phase.wochen],
    ['Sport pro Woche', zahl(b.sport.woche), 'kcal netto'],
    ['Sport pro Tag', zahl(b.sportSchnitt), 'kcal im Schnitt'],
    ['Defizit' + (b.eigenesTempo ? ' (eigenes Tempo)' : ''), zahl(b.defizit),
      `kcal pro Tag · ${b.kgProWoche.toLocaleString('de-DE', { maximumFractionDigits: 2 })} kg/Woche`]
  ].forEach(([k, v, u]) => {
    const c = el('div', 'kpi');
    c.append(el('div', 'k', k), el('div', 'v', v), el('div', 'u', u));
    kpis.append(c);
  });
  const kartePhase = el('div', 'card');
  kartePhase.append(el('h2', null, phase.name));
  kartePhase.append(kpis);
  ziel.append(kartePhase);

  // Wochenplan
  const met = DATA.training.metWerte;
  const karteWoche = el('div', 'card');
  karteWoche.append(el('h2', null, 'Wochenplan'));
  const scroll = el('div', 'tabelle-scroll');
  const tbl = document.createElement('table');
  tbl.innerHTML = '<thead><tr><th>Tag</th><th>Ort</th><th>Einheiten</th><th class="num">kcal netto</th></tr></thead>';
  const tb = document.createElement('tbody');
  b.sport.proTag.forEach(t => {
    const tr = document.createElement('tr');
    const einheiten = t.einheiten.map(e => {
      const label = met[e.typ] ? met[e.typ].label : e.typ;
      return `${label}, ${e.min} min` + (e.notiz ? ` (${e.notiz})` : '');
    }).join('<br>');
    tr.innerHTML =
      `<td><b>${t.tag}</b></td><td>${t.ort}</td>` +
      `<td style="white-space:normal">${einheiten}</td>` +
      `<td class="num">${zahl(t.kcal)}</td>`;
    tb.append(tr);
  });
  const tr = document.createElement('tr');
  tr.className = 'summe';
  tr.innerHTML = `<td colspan="3">Summe pro Woche</td><td class="num">${zahl(b.sport.woche)}</td>`;
  tb.append(tr);
  tbl.append(tb);
  scroll.append(tbl);
  karteWoche.append(scroll);
  karteWoche.append(el('p', 'hint', DATA.training.hinweis));
  ziel.append(karteWoche);

  // Verbrauchstabelle beim aktuellen Gewicht
  const karteMet = el('div', 'card');
  karteMet.append(el('h2', null, `Verbrauch bei ${state.profil.gewicht} kg`));
  const s2 = el('div', 'tabelle-scroll');
  const t2 = document.createElement('table');
  t2.innerHTML = '<thead><tr><th>Einheit</th><th class="num">kcal / 30 min</th><th class="num">kcal / Stunde</th></tr></thead>';
  const b2 = document.createElement('tbody');
  Object.values(met).forEach(m => {
    const r = document.createElement('tr');
    r.innerHTML = `<td>${m.label}</td>` +
      `<td class="num">${zahl(einheitKcal(m.met, 30, state.profil.gewicht))}</td>` +
      `<td class="num">${zahl(einheitKcal(m.met, 60, state.profil.gewicht))}</td>`;
    b2.append(r);
  });
  t2.append(b2);
  s2.append(t2);
  karteMet.append(s2);
  ziel.append(karteMet);

  // Kraftplan
  const kp = DATA.training.kraftplan;
  const karteKraft = el('div', 'card');
  karteKraft.append(el('h2', null, 'Krafttraining'));
  karteKraft.append(el('p', 'hint', kp.equipment));
  karteKraft.append(el('p', 'hint', kp.prinzip));
  kp.einheiten.forEach(e => {
    karteKraft.append(el('h3', null, e.name));
    e.uebungen.forEach(u => {
      const box = el('div', 'uebung');
      const kopf = el('div', 'u-kopf');
      kopf.append(el('span', 'u-name', u.name));
      kopf.append(el('span', 'u-satz', `${u.saetze} × ${u.wdh}`));
      box.append(kopf);
      box.append(el('div', 'u-hint', u.hinweis));
      karteKraft.append(box);
    });
  });
  ziel.append(karteKraft);

  // Regeln
  const karteRegeln = el('div', 'card');
  karteRegeln.append(el('h2', null, 'Regeln'));
  DATA.training.regeln.forEach(r => {
    karteRegeln.append(el('h3', null, r.titel));
    karteRegeln.append(el('p', 'hint', r.text));
  });
  ziel.append(karteRegeln);
}

/* ── Tab: Ich ───────────────────────────────────────── */

function renderIch() {
  const p = state.profil;
  $('#f-gewicht').value = feldWert(p.gewicht);
  $('#f-ziel').value = feldWert(p.ziel);
  $('#f-groesse').value = p.groesse;
  $('#f-alter').value = p.alter;
  $('#f-zieltermin').value = p.zieltermin || '';
  $('#f-protein').value = feldWert(p.proteinFaktor);

  const b = bilanz();
  $('#f-tempo').value = b.eigenesTempo ? feldWert(p.tempo) : '';

  // Tempo und Defizit sind dasselbe in zwei Einheiten — hier steht, welche
  // Richtung gerade gilt und was das für die Kalorien bedeutet.
  const th = $('#tempoHinweis');
  th.innerHTML = '';
  th.append(el('p', 'hint', b.eigenesTempo
    ? `Eigenes Tempo aktiv: ${b.kgProWoche.toLocaleString('de-DE', { maximumFractionDigits: 2 })} kg pro Woche ` +
      `entsprechen ${zahl(b.defizit)} kcal Defizit pro Tag. Der Phasenwert wäre ` +
      `${b.phasenTempo.toLocaleString('de-DE', { maximumFractionDigits: 2 })} kg (${zahl(b.phasenDefizit)} kcal).`
    : `Tempo kommt aus „${b.phase ? b.phase.name : '—'}": ` +
      `${zahl(b.phasenDefizit)} kcal Defizit pro Tag entsprechen ` +
      `${b.phasenTempo.toLocaleString('de-DE', { maximumFractionDigits: 2 })} kg pro Woche. ` +
      `Trag oben einen eigenen Wert ein, um das zu überschreiben.`));

  const ziel = $('#rechnung');
  ziel.innerHTML = '';

  if (!p.eigene) {
    ziel.append(el('div', 'notice warn',
      'Das sind noch Platzhalterwerte. Trage oben deine echten Zahlen ein — erst dann stimmen Kalorienziele, ' +
      'Verbrauchstabelle und Einkaufsmengen.'));
  }

  const kpis = el('div', 'kpis');
  [
    ['Grundumsatz', zahl(b.grundumsatz), 'kcal'],
    ['Alltag ohne Sport', zahl(b.alltag), 'kcal'],
    ['Sport im Schnitt', zahl(b.sportSchnitt), 'kcal pro Tag'],
    ['Gesamtumsatz', zahl(b.gesamt), 'kcal pro Tag'],
    ['Normaltag essen', zahl(b.normal), 'kcal'],
    ['Refeed-Tag essen', zahl(b.refeed), 'kcal'],
    ['Protein täglich', b.protein, 'g'],
    ['Tempo' + (b.eigenesTempo ? ' (eigenes)' : ''),
      b.kgProWoche.toLocaleString('de-DE', { maximumFractionDigits: 2 }), 'kg pro Woche'],
    ['Defizit', zahl(b.defizit), 'kcal pro Tag']
  ].forEach(([k, v, u]) => {
    const c = el('div', 'kpi');
    c.append(el('div', 'k', k), el('div', 'v', v), el('div', 'u', u));
    kpis.append(c);
  });
  ziel.append(kpis);

  if (b.normal < b.grundumsatz) {
    // Höchstes Tempo, bei dem die Aufnahme noch genau auf dem Grundumsatz landet:
    // normal = gesamt − defizit × 7/6, gleichgesetzt mit dem Grundumsatz.
    const maxDefizit = (b.gesamt - b.grundumsatz) * 6 / 7;
    const maxTempo = Math.max(0, maxDefizit * 7 / KCAL_PRO_KG);
    ziel.append(el('div', 'notice warn',
      `Achtung: Die Zielaufnahme von ${zahl(b.normal)} kcal liegt unter deinem Grundumsatz von ` +
      `${zahl(b.grundumsatz)} kcal. Das ist über Wochen nicht tragfähig. ` +
      `Bei deinem aktuellen Sportvolumen wären maximal ` +
      `${maxTempo.toLocaleString('de-DE', { maximumFractionDigits: 2 })} kg pro Woche vertretbar — ` +
      `senke das Tempo oder erhöhe das Sportvolumen.`));
  }

  const rest = p.gewicht - p.ziel;
  if (rest > 0 && b.kgProWoche > 0) {
    const wochen = Math.ceil(rest / b.kgProWoche);
    const datum = new Date();
    datum.setDate(datum.getDate() + wochen * 7);
    ziel.append(el('p', 'hint',
      `Noch ${rest.toLocaleString('de-DE', { maximumFractionDigits: 1 })} kg. Bei diesem Tempo ` +
      `etwa ${wochen} Wochen, also bis ${datum.toLocaleDateString('de-DE', { day: '2-digit', month: 'long', year: 'numeric' })}. ` +
      `Der Wert sinkt automatisch mit — je leichter du wirst, desto weniger verbrauchst du.`));
  }

  renderVerlauf();
  renderStatus();
}

function schnitt7(bisIndex) {
  // Mittelwert der letzten 7 Einträge bis einschließlich bisIndex.
  const v = state.verlauf;
  const von = Math.max(0, bisIndex - 6);
  const teil = v.slice(von, bisIndex + 1);
  if (!teil.length) return null;
  return teil.reduce((s, e) => s + e.kg, 0) / teil.length;
}

function renderVerlauf() {
  const ziel = $('#verlauf');
  ziel.innerHTML = '';
  const v = state.verlauf;

  if (!v.length) {
    ziel.append(el('p', 'hint',
      'Noch keine Einträge. Täglich morgens wiegen, aber nur den 7-Tage-Schnitt bewerten — ' +
      'Tageswerte schwanken durch Wasser um 1–2 kg.'));
    $('#plateauHinweis').innerHTML = '';
    return;
  }

  // Plateau: 7-Tage-Schnitt heute gegen den Schnitt vor 14 Einträgen.
  const box = $('#plateauHinweis');
  box.innerHTML = '';
  if (v.length >= 15) {
    const jetzt = schnitt7(v.length - 1);
    const davor = schnitt7(v.length - 15);
    const diff = davor - jetzt;
    if (diff < 0.3) {
      const n = el('div', 'notice warn');
      n.textContent =
        `Plateau: Der 7-Tage-Schnitt hat sich in 14 Tagen nur um ` +
        `${diff.toLocaleString('de-DE', { maximumFractionDigits: 2 })} kg bewegt. ` +
        `Jetzt genau EINE Stellschraube um 150 kcal drehen — entweder weniger essen oder mehr Sport, nicht beides. ` +
        `Danach zwei Wochen abwarten.`;
      box.append(n);
    } else {
      box.append(el('p', 'hint',
        `Läuft: ${diff.toLocaleString('de-DE', { maximumFractionDigits: 2 })} kg im 7-Tage-Schnitt über die letzten 14 Einträge.`));
    }
  }

  const scroll = el('div', 'tabelle-scroll');
  const tbl = document.createElement('table');
  tbl.innerHTML = '<thead><tr><th>Datum</th><th class="num">kg</th><th class="num">7-Tage-Schnitt</th><th></th></tr></thead>';
  const tb = document.createElement('tbody');
  v.slice().reverse().forEach((e) => {
    const idx = v.indexOf(e);
    const s = schnitt7(idx);
    const tr = document.createElement('tr');
    tr.innerHTML =
      `<td>${new Date(e.datum).toLocaleDateString('de-DE')}</td>` +
      `<td class="num">${e.kg.toLocaleString('de-DE', { minimumFractionDigits: 1 })}</td>` +
      `<td class="num">${s ? s.toLocaleString('de-DE', { maximumFractionDigits: 2 }) : '—'}</td>`;
    const td = document.createElement('td');
    const del = el('button', 'del', '×');
    del.addEventListener('click', () => {
      state.verlauf.splice(idx, 1);
      speichern(); renderIch();
    });
    td.append(del);
    tr.append(td);
    tb.append(tr);
  });
  tbl.append(tb);
  scroll.append(tbl);
  ziel.append(scroll);
}

/* ── Kopfzeile ──────────────────────────────────────── */

function renderStatus() {
  const b = bilanz();
  const p = state.profil;
  const teile = [
    `<strong>${zahl(b.normal)}</strong> kcal · <strong>${b.protein}</strong> g Protein`
  ];
  if (p.zieltermin) {
    const tage = Math.ceil((new Date(p.zieltermin) - new Date()) / 86400000);
    if (tage > 0) teile.push(`noch <strong>${tage}</strong> Tage`);
  }
  if (p.gewicht > p.ziel) {
    teile.push(`noch <strong>${(p.gewicht - p.ziel).toLocaleString('de-DE', { maximumFractionDigits: 1 })}</strong> kg`);
  }
  $('#statusbar').innerHTML = teile.join(' &nbsp;·&nbsp; ');
}

/* ── Verdrahtung ────────────────────────────────────── */

function renderAlles() {
  renderStatus();
  renderWoche();
  renderEinkauf();
  renderRezepte();
  renderTraining();
}

function initTabs() {
  $$('#tabs .tab').forEach(btn => {
    btn.addEventListener('click', () => {
      $$('#tabs .tab').forEach(b => b.classList.remove('is-active'));
      $$('.panel').forEach(p => p.classList.remove('is-active'));
      btn.classList.add('is-active');
      $('#panel-' + btn.dataset.tab).classList.add('is-active');
      if (btn.dataset.tab === 'ich') renderIch();
      window.scrollTo(0, 0);
    });
  });
}

function initWoche() {
  $('#autofill').addEventListener('click', autofill);
  $('#clearweek').addEventListener('click', () => {
    state.plan = leererPlan();
    speichern(); renderAlles();
  });
  const sel = $('#refeedTag');
  sel.value = state.refeedTag;
  sel.addEventListener('change', () => {
    state.refeedTag = sel.value;
    speichern(); renderAlles();
  });
}

function initEinkauf() {
  $$('#panel-einkauf .btn-toggle').forEach(btn => {
    btn.addEventListener('click', () => {
      $$('#panel-einkauf .btn-toggle').forEach(b => b.classList.remove('is-active'));
      btn.classList.add('is-active');
      const woche = btn.dataset.liste === 'woche';
      $('#listeWoche').hidden = !woche;
      $('#listeGrundstock').hidden = woche;
    });
  });
  $('#resetChecks').addEventListener('click', () => {
    const woche = $('#listeWoche').hidden === false;
    state.haken[woche ? 'woche' : 'grundstock'] = {};
    speichern(); renderEinkauf();
  });
}

function initRezepte() {
  $$('#panel-rezepte .btn-toggle').forEach(btn => {
    btn.addEventListener('click', () => {
      $$('#panel-rezepte .btn-toggle').forEach(b => b.classList.remove('is-active'));
      btn.classList.add('is-active');
      renderRezepte();
    });
  });
  $('#rezeptPortionen').addEventListener('input', renderRezepte);
}

function initTraining() {
  const sel = $('#phaseSelect');
  DATA.training.phasen.forEach(f => sel.append(new Option(f.name, f.id)));
  sel.value = state.profil.phase;
  sel.addEventListener('change', () => {
    state.profil.phase = sel.value;
    speichern(); renderAlles(); renderIch();
  });
}

function initIch() {
  // Die Textfelder bringen keine eigene Bereichsprüfung mit, deshalb hier Grenzen.
  const felder = [
    ['#f-gewicht', 'gewicht',        30, 300],
    ['#f-ziel',    'ziel',           30, 300],
    ['#f-groesse', 'groesse',       120, 230],
    ['#f-alter',   'alter',          14, 100],
    ['#f-protein', 'proteinFaktor',   1,   3]
  ];
  felder.forEach(([sel, key, min, max]) => {
    $(sel).addEventListener('change', () => {
      const v = zahlAus($(sel).value);
      if (v === null) { renderIch(); return; }   // Unsinn: alten Wert zurückschreiben
      state.profil[key] = Math.min(max, Math.max(min, v));
      state.profil.eigene = true;
      speichern(); renderAlles(); renderIch();
    });
  });

  // Tempo überschreibt das Defizit der Phase. Leeres Feld heißt: wieder der Phase folgen.
  $('#f-tempo').addEventListener('change', () => {
    const roh = $('#f-tempo').value.trim();
    if (roh === '') {
      state.profil.tempo = null;
    } else {
      const v = zahlAus(roh);
      if (v === null || v <= 0) { renderIch(); return; }
      state.profil.tempo = Math.min(1.5, Math.max(0.1, v));
      state.profil.eigene = true;
    }
    speichern(); renderAlles(); renderIch();
  });

  $('#tempoReset').addEventListener('click', () => {
    state.profil.tempo = null;
    speichern(); renderAlles(); renderIch();
  });

  $('#f-zieltermin').addEventListener('change', () => {
    state.profil.zieltermin = $('#f-zieltermin').value;
    state.profil.eigene = true;
    speichern(); renderIch();
  });

  $('#w-datum').valueAsDate = new Date();
  $('#w-add').addEventListener('click', () => {
    const datum = $('#w-datum').value;
    const kg = zahlAus($('#w-kg').value);
    if (!datum || kg === null || kg <= 0) return;
    const i = state.verlauf.findIndex(e => e.datum === datum);
    if (i >= 0) state.verlauf[i].kg = kg;
    else state.verlauf.push({ datum, kg });
    state.verlauf.sort((a, b) => a.datum.localeCompare(b.datum));

    // Der jüngste Eintrag ist ab jetzt das Arbeitsgewicht — daran hängen
    // Verbrauchstabelle, Kalorienziel und Einkaufsmengen.
    state.profil.gewicht = state.verlauf[state.verlauf.length - 1].kg;
    state.profil.eigene = true;
    $('#w-kg').value = '';
    speichern(); renderAlles(); renderIch();
  });
}

async function start() {
  try {
    const [r, t, g] = await Promise.all([
      fetch('data/rezepte.json').then(x => x.json()),
      fetch('data/training.json').then(x => x.json()),
      fetch('data/grundstock.json').then(x => x.json())
    ]);
    DATA = { rezepte: r, training: t, grundstock: g };
  } catch (e) {
    document.querySelector('main').innerHTML =
      '<div class="notice warn" style="margin-top:20px">Die Daten konnten nicht geladen werden. ' +
      'Diese Seite braucht einen Webserver — direkt per Doppelklick aus dem Dateisystem geöffnet ' +
      'blockiert der Browser das Nachladen der JSON-Dateien. Nutze die veröffentlichte Adresse ' +
      'oder starte lokal einen Server (z. B. <code>npx serve</code>).</div>';
    console.error(e);
    return;
  }

  laden();
  initTabs();
  initWoche();
  initEinkauf();
  initRezepte();
  initTraining();
  initIch();
  renderAlles();
  renderIch();
}

start();
