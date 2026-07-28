# Funktionstest Web — 2026-07-28

## Rahmen
- Getesteter Stand: `worktree-funktionstest-web` @ `9bf8ecd` (Worktree, identisch master), lokaler Dev-Server `npm run dev` (Port 4323) gegen Test-API `https://test-api.heimatplatz.at`
- Ausnahme: `/intern/**` wurde gegen das **deployte Test-Web** (`test.heimatplatz.at`) getestet, weil der lokale Dev-Server kein `ADMIN_API_KEY` hat (Key liegt nur am Server)
- Gerät/Browser: Playwright (Chromium), Desktop 1440×900, Phone 390×844 (Spotchecks)
- Getestet als: Gast, test.buyer, test.seller, neu registrierter Wegwerf-User (QA Testlauf), Intern via IP-Schranke
- Umfang: kompletter Durchlauf inkl. Inserat veröffentlichen/bearbeiten/löschen, Feedback mit Bild, Intern-Antwort, CRM-Lead

## Zusammenfassung
Die Web-App ist in sehr gutem Zustand. Alle Kernflüsse funktionieren end-to-end und wurden per API-Gegenprobe verifiziert: Suche/Filter/Sortierung mit URL-Zustand, Karte (Split, Cluster→Bezirksfilter, Marker-Pille, präziser OÖ-Umriss), Detailseiten aller drei Objekttypen mit korrekter Feld-Sichtbarkeit, Auth komplett (Login/Registrieren/Reset/Session/Logout/Konto löschen), Favoriten/Blockieren, das komplette Inserieren (Fotos, Ansprechpartner, Original-URL, Veröffentlichen, Vorbefüllung beim Bearbeiten, Löschen mit Rückfrage), Feedback mit Bild-Anhang inkl. Intern-Antwort, Makler-Lead bis ins CRM, Intern-Moderation (Ausblenden/Einblenden). Der einzige nennenswerte Fehler: Im Intern-Feedback-Detail sind Bild-Anhänge kaputt (interner Docker-Hostname in der Bild-URL). Dazu eine fehlende Web/MAUI-Parität (Lasten-Sektion) und Kleinigkeiten.

| Schwere | Anzahl |
|---|---|
| S1 Blocker | 0 |
| S2 Schwer | 1 |
| S3 Mittel | 1 |
| S4 Kosmetik | 0 |
| Hinweis/Frage | 3 |

## Befunde

### B-01 · S2 · Intern/Feedback — Bild-Anhänge im Detail kaputt (interner Docker-Host in der URL)
- **Schritte:** 1. Feedback mit Bild-Anhang über /feedback senden (als test.seller). 2. Auf test.heimatplatz.at `/intern/feedback/detail/?id=…` öffnen.
- **Erwartet:** Bild-Anhang wird als Thumbnail angezeigt (wie im Nutzer-Thread `/feedback/anfrage`).
- **Tatsächlich:** Kaputtes Bild. Die Bild-URL lautet `https://api-test:8080/api/images/local?path=…` — der **interne Docker-Servicename** statt `https://test-api.heimatplatz.at`. Zusätzlich blockt die CSP (`img-src` enthält den Host nicht); selbst ohne CSP wäre der Host im Browser nicht auflösbar.
- **Screenshot:** shots/30-intern-feedback-detail.jpeg
- **Konsole/Log:** 2× `Loading the image 'https://api-test:8080/…' violates … img-src …`
- **Vermutete Stelle:** Intern-Feedback-Detail rendert die Attachment-URLs der Admin-API 1:1; die API baut sie aus ihrer eigenen (internen) Basis-URL. Nutzerseite betroffen? Nein — dort kommt die URL über die öffentliche Client-API-Basis.
- **Reproduzierbar:** ja (jedes Laden der Seite; getestet am deployten Test-Stand)

### B-02 · S3 · ZV-Detail Web — keine LASTEN-Sektion (Parität zur MAUI-App fehlt)
- **Schritte:** 1. ZV-Objekt mit Encumbrances öffnen, z. B. `/immobilien/angebote/99f4a5c5-…` („Haus in Traun", TypeSpecificData enthält Hypothek € 92.500 + Grundsteuer € 2.500). 2. Auch `/zwangsversteigerungen/[slug]`-Seiten prüfen.
- **Erwartet:** Lasten-Karte wie in der MAUI-App (seit 28.7., zwischen Beschreibung und Datenblatt, mit Summenzeile).
- **Tatsächlich:** Web rendert nirgends Encumbrances — `grep Encumbrance|Lasten` über `src/web/src` trifft nur die Draft-Serialisierung (`Encumbrances: []` in PropertyStateScript.astro:2383). Käufer sehen die Lasten im Web gar nicht.
- **Screenshot:** shots/04-zv-property-detail-full.jpeg (VERSTEIGERUNG-Tabelle ohne Lasten)
- **Reproduzierbar:** ja

### B-03 · ZURÜCKGEZOGEN (bewusst so, Entscheidung 28.7.) · Suche Desktop — „Filter zurücksetzen" nur mobil
Der Sammel-Reset lebt bewusst nur im Mobile-Filter-Akkordeon (`MobileFilterPanel.astro:58`); am Desktop liegen alle Filter einzeln sichtbar in der Suchleiste, ein Sammel-Reset ist dort nicht gewollt. In die „Bewusst so"-Liste der Erkenntnisse übernommen.

### H-01 · BEHOBEN (28.7., Entscheidung Daniel) · ZV-Sticky-Leiste — Zustandstext „Edikt offen" entfernt
Der Fallback („Edikt offen" bei fehlender Edikt-URL, Key `zv.edictPending`) war missverständlich — und ein praktisch toter Zweig: Der Edikte-Sync leitet die Edikt-URL IMMER aus der ExternalId ab (`ForeclosureAuctionSyncService.cs:419/465`), nur Test-Seeds haben keine. Fix: Fallback-Zweig entfernt — ohne URL wird in der Leiste nichts gerendert (`[slug].astro`), toter i18n-Key gelöscht (`foreclosures.ts`). Verifiziert: Linz-Auktion (ohne URL) zeigt nur Preis+Termin, Gmunden-Auktion (mit URL) weiterhin „Edikt ansehen".

### H-02 · Hinweis · Suche — persistierte Sortierung erst bei Interaktion in der URL
Die Sortierung wird über `heimatplatz:filter-preferences` persistiert und beim Laden von `/` angewandt, die URL bleibt aber zunächst leer; erst die nächste Interaktion (z. B. Favoriten-Klick) schreibt `?sort=…` per replaceState nach. Kurzzeitig divergieren URL und Listenzustand — kein Funktionsfehler, aber beim Link-Teilen geht die Sortierung verloren.

### H-03 · Hinweis · A11y — Bezirks-Aufklapp-Buttons im Ort-Picker ohne Accessible Name
Die Chevron-Buttons je Bezirk haben keinen aria-label/Textinhalt (erst mit Auswahl-Badge bekommt der Button einen Namen, z. B. „Stadt Linz 1 ausgewählt"). Screenreader hören 18× nur „Button". Checkboxen sind korrekt beschriftet.

## Testdaten-Artefakte (KEINE Befunde — geprüft und entkräftet)
- **ZV-Kanon-Canonical auf Test nicht prüfbar:** Seed-ZV-Properties haben keine Edikt-URL → `getForeclosureCanonicalPath` liefert null, canonical bleibt auf `/immobilien/angebote/…`. Auf Prod (Scraper setzt Edikt-URL) greift der Join. Code korrekt (property-mirror.ts:49).
- **Leere Lage-Karte im frischen Worktree:** `src/web/public/tiles/` ist gitignored — nach Kopie aus dem Haupt-Checkout rendert die Karte einwandfrei (shots/05).
- **/karte-embed ohne Parameter „leer":** Vertrag mit der MAUI-App verlangt `?ansicht=karte`; mit Param voll funktionsfähig inkl. `theme=light|dark`-Override (shots/16).
- **Teilen-Toast nicht beobachtbar:** Headless-Browser ohne Clipboard-Permission; Implementierung (navigator.share→Clipboard-Fallback+Toast) im Code verifiziert.

## Geprüft und in Ordnung

| # | Bereich | Funktion | Ergebnis |
|---|---|---|---|
| 1 | Start/Suche | Startseite, Liste, Trefferzahl (11 = 8 Haus + 3 Grund, ZV default aus) | ✓ shots/01,02 |
| 2 | Start/Suche | Ort-Picker Bezirk+Gemeinde, Multi-Select, URL `?region=`, Reload-Restore, Reset | ✓ |
| 3 | Start/Suche | Zeitraum (7 Tage → 4 Objekte, Datumsgrenzen korrekt) | ✓ |
| 4 | Start/Suche | Typ-Chips: ZV zuschalten → 13 Objekte inkl. beider ZV-Spiegel | ✓ |
| 5 | Start/Suche | Anbietertyp Privat/Makler | ✓ |
| 6 | Start/Suche | Sortierung (price-asc verifiziert), URL-Sync | ✓ |
| 7 | Start/Suche | Filter zurücksetzen (mobil; Desktop bewusst ohne — B-03 zurückgezogen) | ✓ |
| 8 | Karte | Split-Ansicht, Legende, Cluster-Klick = Bezirksfilter, Preis-Marker-Pille mit Foto/CTA, „Übersicht", Karte↔Liste konsistent (3/3) | ✓ shots/07,08 |
| 9 | Karte | /karte-embed mit ansicht=karte + theme-Override | ✓ shots/16 |
| 10 | Detail | Haus (Galerie 1/3-Lightbox mit Tastatur, Kontakte+Kopieren, Quelle, Preis/m²) | ✓ shots/09,10 |
| 11 | Detail | Grundstück (keine Wohnfläche/Zimmer/Gebäude, Widmung da) | ✓ |
| 12 | Detail | ZV-Property + ZV-Slug (Gericht, Versteigerungstabelle, Edikt-Link, „Kein Foto"-Platzhalter, Lage-Umkreis) | ✓ shots/04,05,06 · Lasten: B-02 |
| 13 | Detail | 404: erfundene Route/ID/Slug + gelöschtes Objekt → alle HTTP 404, deutsche Fehlseite | ✓ shots/11 |
| 14 | Statisch | /makler inkl. Broker-Lead-Formular (Pflichtfeld-Validierung, Honeypot „Fax", Ladezustand, Erfolg, POST 200 → CRM-Kontakt) | ✓ shots/12 |
| 15 | Statisch | /impressum, /datenschutz (Stammdaten aus DB, keine Platzhalter), /beispiel-originalinserat mit ?nr= | ✓ shots/13 |
| 16 | Auth | Login falsch/richtig, returnTo, Session-Reload, Logout → /anmelden | ✓ |
| 17 | Auth | Registrieren (Passwort-Mismatch-Fehler, Erfolg, Verifizierungs-Hinweis im Profil + Resend-Button) | ✓ |
| 18 | Auth | /passwort-vergessen (enumeration-sicher), /passwort-zuruecksetzen + /email-bestaetigen ohne Token → saubere Fehlertexte | ✓ |
| 19 | Auth | AuthGate auf /favoriten als Gast | ✓ shots/17 |
| 20 | Käufer | Favoriten: Liste (4 Referenz-Favoriten), Entfernen ohne Rückfrage (bewusst so), Wieder-Hinzufügen — API-verifiziert (DELETE/POST 200) | ✓ shots/18 |
| 21 | Käufer | Blockieren: Zyklus Wels blockieren→aufheben, Liste filtert blockierte aus (Zähler 10→9), /blockiert korrekt — API-verifiziert | ✓ |
| 22 | Käufer | /filter-einstellungen (Speichern mit Bestätigung „gespeichert und synchronisiert") | ✓ shots/19 |
| 23 | Käufer | /benachrichtigungen (Modi, Typ/Anbieter/Orte, Zustand = Server-Stand) | ✓ shots/20 |
| 24 | Käufer | /profil (Hero, Zähler=API, Profilform, Passwortform, Schnellzugriff) | ✓ shots/21 |
| 25 | Verkäufer | /inserieren: 2 Fotos hochgeladen (2/20), alle Felder, Merkmal per Enter, Ansprechpartner, Original-URL, Lage-Modus | ✓ shots/22,23 |
| 26 | Verkäufer | Veröffentlichen → „Inserat veröffentlicht.", sofort in /meine-immobilien + öffentl. Suche (12 Objekte, neuestes zuerst), Detailseite vollständig (Preis/m², Kontakt-Reihenfolge, Quelle-Button) | ✓ shots/24,25 |
| 27 | Verkäufer | Bearbeiten: ALLE Felder vorbefüllt (inkl. Fotos, Ansprechpartner, Original-URL); Preisänderung per API verifiziert (229.000) | ✓ |
| 28 | Verkäufer | Löschen mit Rückfrage („Immobilie wirklich löschen?") → aus Suche raus, API `Property:null`, Web-Detail 404 | ✓ |
| 29 | Verkäufer | Keine Server-Entwürfe hinterlassen (`/api/property-drafts` leer) | ✓ |
| 30 | Feedback | /feedback: Kategorien, Composer (Anhang/Foto/Mikro→Senden), Senden mit Bild (Upload+POST 200), Thread-Ansicht mit Bild, „Meine Anfragen" | ✓ shots/26–28 |
| 31 | Intern | Dashboard-Kennzahlen plausibel (12 Nutzer/13 Inserate/2 ZV/0 ausgeblendet) | ✓ shots/29 |
| 32 | Intern | /intern/feedback: Liste + Detail + Antwort → erscheint beim Nutzer als „Heimatplatz-Team" | ✓ · Bild: B-01 |
| 33 | Intern | /intern/nutzer (12 Zeilen inkl. Neuregistrierung) | ✓ |
| 34 | Intern | /intern/immobilien: Filter, ZV nur Ausblenden, Moderation Ausblenden→Einblenden per API verifiziert (13→12→13) | ✓ shots/31 |
| 35 | Intern | /intern/kontakt (Stammdaten vorbefüllt, lesend), /intern/analytics, Marketing-Unterseiten alle 200 + schreiben-Seite gerendert | ✓ |
| 36 | Intern | CRM: Broker-Lead angekommen (Status „Interessiert"), Detail, Löschen mit Rückfrage | ✓ |
| 37 | Endpoints | robots.txt (private Routen disallowed), sitemap.xml (ZV-Spiegel korrekt ausgenommen), llms.txt, llms-full.txt, health.json, property-image-map.json | ✓ |
| 38 | Debug | /debug (localhost-Gate, Schnell-Login, API-Umschalter sichtbar) | ✓ |
| 39 | Konto | Konto löschen (Inline-Rückfrage „Sind Sie sicher?") → Login danach 401 | ✓ |
| 40 | Theme | Umschalter System→Hell→Dunkel (html.dark), Dark-Startseite strukturgleich, karte-embed theme-Param | ✓ shots/14 |
| 41 | Responsive | Phone 390px: Start (Filter-Akkordeon, Karte-Pille), Filter offen, Detail (Sticky-Kontakt), Editor — je ohne H-Scroll | ✓ shots/02,03,32,33 |
| 42 | Konsole/CSP | Startseite, Details, Karte: 0 Fehler, 0 CSP-Violations (Listener), kein Debug-API-Override | ✓ |

## Nicht getestet / nicht testbar
- **E-Mail-Zustellung** (Verifizierung/Reset): Server nutzt Logging-Fallback, kein Postfach — nur UI-Zustände geprüft.
- **Browser-Push aktivieren**: Headless ohne Notification-Permission.
- **Sprachnachricht im Feedback**: kein Mikrofon im Testbrowser.
- **Marketing: KI-Generierung/Versand, IMAP-Sync, Firmenpool-Übernahme**: bewusst nicht ausgelöst (echte Mails/Scrapes).
- **Edikte-Sync-Trigger**: echter Scrape gegen edikte.justiz.gv.at — nicht ausgelöst.
- **Suche-Paging**: nur 13 Objekte auf Test — keine zweite Seite auslösbar.
- **/intern auf Worktree-Stand**: lokal ohne ADMIN_API_KEY — stattdessen deployter Test-Stand (älterer Build als 9bf8ecd).
- **Responsive vollständig**: Phone nur als Spotcheck auf 4 Seiten; Tablet/1920 nicht; Dark Mode nur Startseite.
- **ZV gegen die Suche auf Prod** (Falle 8): nicht erneut geprüft.

## Zurückgebaute Testdaten
- Favorit „Doppelhaushälfte Vöcklabruck" entfernt und wieder gesetzt → Referenzzustand (4 Favoriten) per API bestätigt.
- Blockierung „Baugrundstück in Wels" gesetzt und aufgehoben → nur Referenz-Blockierung (Mühlviertel) übrig, per API bestätigt.
- Inserat „QA-Testhaus Laakirchen" veröffentlicht, bearbeitet, **gelöscht** (API `Property:null`).
- Intern-Moderation Wels ausgeblendet und **wieder eingeblendet** (13 Objekte öffentlich).
- CRM-Lead „QA-Testlauf 28.07" **gelöscht**.
- Wegwerf-User `qa.testlauf.2807@heimatplatz.dev` per Konto-löschen **entfernt** (Login 401).
- test.buyer-Filtereinstellungen auf App-Defaults gespeichert (Neueste/Haus+Grund/Privat+Makler/Alle).
- **Verbleibt:** Feedback-Thread „Problem-Meldung 1" (28.07., mit Intern-Testantwort) — Nutzer können Threads nicht löschen; wie der 26.07.-Thread als QA-Artefakt gekennzeichnet („bitte ignorieren/löschen").
