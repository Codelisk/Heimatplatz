# Funktionstest — Astro-Web (`src/web`)

## Werkzeug

**Standard: Playwright-MCP** (`mcp__playwright__*`) — sauberes Profil, `browser_snapshot` fuer die
Struktur, `browser_resize` fuer Mobile/Desktop, `browser_console_messages` und
`browser_network_requests` laufen automatisch mit, `browser_take_screenshot` fuer Belege.

**Alternative: claude-in-chrome** (`mcp__claude-in-chrome__*`) — nur wenn bewusst im echten
Chrome-Profil des Nutzers getestet werden soll (bestehende Session, Extensions). Dann zuerst
`tabs_context_mcp`, neuen Tab anlegen, nie fremde Tabs kapern.

Das Playwright-Projekt unter `usertests/` ist **kein** Web-UI-Setup — seine `baseURL` zeigt auf
`http://localhost:5292`, also auf die **API**. Nicht als Web-Testbasis verwenden.

## Umgebungen

| Ziel | Web | API | Nutzung |
|---|---|---|---|
| **Test** (Standard) | `https://test.heimatplatz.at` | `https://test-api.heimatplatz.at` | Vollzugriff, auch schreibend |
| **Prod** | `https://heimatplatz.at` | `https://api.heimatplatz.at` | **nur lesend** — nichts anlegen/loeschen |
| **Lokal** | `npm run dev` in `src/web` | frei waehlbar | fuer ungebaute Aenderungen |

Lokal gegen die Test-API:

```bash
cd src/web
API_BASE_URL_SERVER=https://test-api.heimatplatz.at PUBLIC_API_BASE_URL=https://test-api.heimatplatz.at npm run dev
```

Astro 7 hat einen **Dev-Daemon**: ein zweiter `astro dev` meldet nur "Dev server already running"
und uebernimmt die neuen Env-Variablen **nicht**. Deshalb vorher:

```bash
cd src/web && npx astro dev status && npx astro dev stop
```

Der Port wechselt je Start (4321/4322/4325…) — aus der Ausgabe lesen, nicht raten.

## Testbenutzer (nur Test-DB / lokal)

Passwort fuer alle Seed-User: **`Test123!`**

| E-Mail | Rolle |
|---|---|
| `test.buyer@heimatplatz.dev` | Kaeufer |
| `test.seller@heimatplatz.dev` | Privater Verkaeufer |
| `test.broker@heimatplatz.dev` | Makler |
| `test.verwaltung@heimatplatz.dev` | Hausverwaltung |
| `max.mustermann@heimatplatz.dev` | Store-Screenshot-User (kuratierte Favoriten) |
| `admin@heimatplatz.dev` | Admin — Passwort **`Admin123!`** |

Ausserdem Demo-User wie `franz.huber@example.com`, `anna.schmidt@example.com` (ebenfalls `Test123!`).
Quelle: `src/api/src/Features/Auth/.../Data/Seeding/UserSeeder.cs`.

Fuer `/intern/**` wird Admin gebraucht.

## Routen als Ausgangsliste

Vollstaendig aus `src/web/src/pages/**` erheben (die Liste unten kann veralten — immer neu lesen):

**Oeffentlich:** `/` · `/immobilien/angebote/[id]` · `/zwangsversteigerungen/[slug]` ·
`/makler` · `/impressum` · `/datenschutz` · `/feedback` · `/feedback/anfrage` ·
`/beispiel-originalinserat` · `/404`

**Konto:** `/anmelden` · `/registrieren` · `/passwort-vergessen` · `/passwort-zuruecksetzen` ·
`/email-bestaetigen` · `/profil`

**Angemeldet:** `/favoriten` · `/blockiert` · `/meine-immobilien` · `/inserieren` ·
`/immobilien/bearbeiten` · `/filter-einstellungen` · `/benachrichtigungen`

**Karte/Sonstiges:** `/karte-embed` (wird von der MAUI-App als WebView genutzt!) · `/debug`

**Intern (Admin):** `/intern` · `/intern/immobilien` · `/intern/nutzer` · `/intern/feedback` ·
`/intern/kontakt` · `/intern/analytics` · `/intern/marketing/**` (schreiben, gesendet, vorlagen,
kontakte, eingang, firmenpool)

**Maschinen-Endpoints (auch pruefen):** `/robots.txt` · `/sitemap.xml` · `/llms.txt` ·
`/llms-full.txt` · `/api/health.json`

## Bekannte Fallen — vor dem Testen abhaken

**1. Debug-API-Override in localStorage.**
`heimatplatz:debug-api-url` (wirkt nur auf localhost/LAN) laesst **Client**-Fetches gegen eine
andere API laufen als **SSR**. Folge: eingebettete Municipality-GUIDs passen nicht, der Ort-Filter
liefert faelschlich 0 Treffer — ein klassischer Falschbefund. Vor dem Testen pruefen/loeschen:

```js
localStorage.getItem('heimatplatz:debug-api-url')
```

**2. Trefferzahl.**
`data-mobile-result-count` ist serverseitig statisch `0 Objekte` und wird erst clientseitig
gesetzt. Fuer SSR-/HTML-Pruefungen immer `data-result-count` heranziehen.

**3. CSP ist scharf geschaltet** (Caddyfile, Prod und Test).
CSP-Verstoesse tauchen in der Konsolen-API **nicht** auf. Zuverlaessig pruefen, auf jeder Seite
mit externen Ressourcen:

```js
// vor der Navigation registrieren
window.__csp = [];
addEventListener('securitypolicyviolation', e =>
  window.__csp.push({ uri: e.blockedURI, directive: e.violatedDirective, disp: e.disposition }));
// nach dem Laden auslesen + fremde Origins scannen
performance.getEntriesByType('resource').map(r => new URL(r.name).origin)
```

Neue externe Ressource (CDN, Font, Analytics, fremder Bild-Host) ohne Caddyfile-Eintrag = Befund.

**4. Preisformat.**
Soll ist `€ 520.000` — mit **schmalem geschuetztem Leerzeichen** (ICU). Textvergleiche mit
normalem Leerzeichen schlagen fehl; vor dem Vergleich ` `/` ` normalisieren. Sichtbar
falsch ist z. B. `520000 €` oder `349 000 €`.

**5. Zwangsversteigerungen sind ueberall default **aus**.**
Ohne gesetzten Typ-Filter duerfen keine ZV in der Liste stehen. Die URL enthaelt bei Default
**kein** `?type=…`. ZV-Objekte haben eigene Detailfelder (u. a. "Gericht") und eine eigene Route.

**6. Nur Haus / Grundstueck / ZV.**
Es gibt **keine Wohnungen** — taucht irgendwo eine Wohnungs-Kategorie, ein Wohnungs-Filter oder
ein Wohnungs-Objekt auf, ist das ein Befund.

**7. Prod-Datenbestand.**
Auf Prod lagen zeitweise fast nur Zwangsversteigerungen — leere Ergebnislisten dort sind nicht
automatisch ein Bug. Im Zweifel gegen Test gegenpruefen.

## Responsive — Pflicht auf jeder Seite

Jede Seite wird in **mindestens zwei Breiten** getestet: Desktop **und** Phone. Kein Bereich gilt
als geprueft, solange er nur in einer Breite angesehen wurde. Umschalten per
`browser_resize` (Playwright) bzw. `resize_window` (claude-in-chrome) — nicht nur das
Browserfenster schmalziehen und schaetzen.

| Breakpoint | Viewport | Pflicht |
|---|---|---|
| Phone | **390 × 844** | ja — jede Seite |
| Desktop | **1440 × 900** | ja — jede Seite |
| Kleines Phone | 360 × 640 | bei jeder Seite mit Formular, Tabelle oder Kartenliste |
| Tablet | 768 × 1024 | bei Seiten mit Split-/Spalten-Layout (Suche, Karte, `/intern/**`) |
| Breit | 1920 × 1080 | einmal ueber die Hauptseiten (uebergrosse Leerflaechen, gestreckte Bilder) |

Worauf in **Phone-Breite** besonders zu achten ist:

- **Kein horizontales Scrollen.** Gegenprobe: `document.documentElement.scrollWidth > window.innerWidth`
- Burger-Menue: oeffnet, schliesst, scrollt, Fokus bleibt gefangen, Hintergrund nicht scrollbar
- Sticky-Suchleiste und Filter-Chips: bleiben oben, verdecken nichts, Chips horizontal scrollbar
- Sheets/Dialoge: passen auf den Schirm, sind schliessbar, Tastatur verdeckt das aktive Feld nicht
- Formulare: einspaltig, Labels ueber dem Feld, Buttons volle Breite und erreichbar
- Tabellen und `/intern`-Listen: scrollen in einem eigenen Container statt die Seite zu sprengen
- Karte: Vollbreite, Pille/Steuerung nicht unter dem Header, Umschalter Karte↔Liste erreichbar
- Bildergalerie: Wischen funktioniert, Zaehler sichtbar
- Touch-Ziele gross genug, Buttons nicht auf 2 Zeilen umgebrochen
- Trefferzahl mobil: `data-mobile-result-count` (siehe Falle 2) — mobile Anzeige separat pruefen

Worauf in **Desktop-Breite** besonders zu achten ist:

- Maximalbreite greift — Text laeuft nicht ueber die ganze Bildschirmbreite
- Mehrspaltige Raster brechen sauber (keine einzelne Karte in letzter Reihe zerrissen)
- Hover- und Fokus-Zustaende vorhanden (auf Phone nicht pruefbar)
- Bilder nicht hochskaliert/unscharf, Seitenverhaeltnisse stimmen
- Split-Ansicht Karte/Liste: beide Haelften nutzbar, unabhaengig scrollbar

**Beim Wechsel der Breite nicht neu laden**, sondern zusaetzlich einmal live umschalten — so
zeigen sich Layouts, die erst beim Resize kaputtgehen (Karte, Sticky-Leiste, Sheets).
Anschliessend in der neuen Breite einmal neu laden und vergleichen.

Screenshots: pro auffaelliger Seite beide Breiten ablegen (`…-phone.png` / `…-desktop.png`).
Befunde immer **mit Viewport-Angabe** melden — „nur bei 390px" ist eine andere Meldung als
„in jeder Breite".

## Pflicht-Durchgaenge

Zusaetzlich zum Inventar diese Querschnitte fahren:

- **Gast vs. angemeldet** auf jeder oeffentlichen Seite (AuthGate-Hinweise, Favoriten-Buttons)
- **Responsive** — Phone und Desktop auf jeder Seite, siehe Abschnitt oben
- **Light und Dark** — Umschalter benutzen, nicht nur eine Variante
- **Session:** Login → Reload → Navigieren → Token-Refresh abwarten → Logout → geschuetzte Seite
  direkt aufrufen (muss sauber zur Anmeldung fuehren, nicht in einen Fehler)
- **Direktaufruf/Deep-Link** jeder Route statt nur Navigation ueber Links
- **Suche:** Freitext, Ort-Picker (Bezirk/Gemeinde), Preis-/Flaechen-Spannen, Typ-Chips,
  Sortierung, Paging, Filter zuruecksetzen — und ob die URL den Zustand widerspiegelt und ein
  Reload ihn wiederherstellt
- **Karte:** `/karte-embed` separat aufrufen (haengt an der MAUI-App), Marker-Klick, Pille,
  Karte/Liste-Umschaltung; Karte und Liste teilen dieselben Filter — Trefferzahl muss passen
- **Inserieren/Bearbeiten:** Entwurf anlegen, Fotos hochladen (mehrere, grosse Datei),
  Ansprechpartner, Original-Inserats-URL, veroeffentlichen, danach in `/meine-immobilien` und in
  der Suche nachsehen; Bearbeiten muss alle Felder **vorbefuellt** anzeigen
- **404:** eine erfundene URL, eine erfundene Objekt-ID und ein geloeschtes/verstecktes Objekt
