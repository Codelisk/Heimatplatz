# Funktionstest Web (Astro) — 27.07.2026

## Rahmen

- **Getesteter Stand:** `master` @ `6ec8d28` (lokal sauber). **Achtung:** getestet wurde das auf
  test.heimatplatz.at **deployte** Bundle — das kann älter als `6ec8d28` sein, es wurde für diesen
  Lauf nicht neu deployt.
- **Umgebung:** Test — Web `https://test.heimatplatz.at`, API `https://test-api.heimatplatz.at`
  (per Netzwerk-Log bestätigt), kein `heimatplatz:debug-api-url`-Override gesetzt.
- **Browser:** Chromium (Playwright), Desktop 1440×900 und Phone 390×844.
- **Getestet als:** Gast · `test.buyer@heimatplatz.dev` (Käufer) · `test.seller@heimatplatz.dev`
  (privater Verkäufer).
- **Umfang:** öffentliche Seiten, Suche/Filter/Karte, Detailseiten, Konto- und Verkäufer-Bereich,
  Responsive Phone+Desktop, Light+Dark. **Nicht** getestet: `/intern/**`, Registrierung,
  Mailstrecken, Inserat-Veröffentlichung (siehe unten).

## Zusammenfassung

Die Web-App macht insgesamt einen stabilen und sorgfältig gebauten Eindruck. Suche, Filter,
URL-Zustand, Reload-Wiederherstellung, Sortierung, Zeitraum, Karte mit Cluster/Pin/Vorschau,
Blockier-Logik, Auth-Flows und die Detailseiten funktionieren durchgehend korrekt. Handwerklich
auffällig gut: die Fokus- und `inert`-Behandlung der Navigation, das Schließen der Lightbox mit
Fokus-Rückgabe, die Open-Redirect-Absicherung bei `returnTo` und die durchgängig deutschen,
höflichen Fehlermeldungen. Auf Phone gibt es auf keiner geprüften Seite horizontales Scrollen.

Kein Blocker und nichts Schweres. Der zunächst gravierend aussehende Befund „ZV-Objekte im UI
unerreichbar" hat sich als **reines Test-Daten-Artefakt** entpuppt und ist unter
„Geklärt — keine Fehler" dokumentiert. Übrig bleiben zwei Default-/Hygiene-Themen, ein auf **Prod**
verifiziertes Doppel-URL-Problem und Kosmetik.

| Schwere | Anzahl |
|---|---|
| S1 Blocker | 0 |
| S2 Schwer | 0 |
| S3 Mittel | 3 |
| S4 Kosmetik | 4 |
| Hinweis/Frage | 3 |

---

## Befunde

### B-08 · S3 · Prod: Jede Zwangsversteigerung ist unter zwei indexierbaren URLs erreichbar

- **Umgebung:** **Produktion** (`heimatplatz.at`) — nur lesend geprüft. Auf Test nicht sichtbar,
  weil dort der Property-Spiegel nicht läuft (siehe „Geklärt").
- **Schritte:**
  1. `https://heimatplatz.at/sitemap.xml` laden → 12 Einträge `/immobilien/angebote/…` und
     13 Einträge `/zwangsversteigerungen/…`.
  2. `GET https://api.heimatplatz.at/api/properties?PropertyTypesJson=["Foreclosure"]` → 12 Objekte,
     `GET /api/foreclosure-auctions` → 17 Auktionen. 12 davon sind als Property gespiegelt.
  3. Ein Objekt in beiden Listen suchen, z. B. **5252 Aspach, Mehrfamilienhaus**.
  4. Beide URLs laden und vergleichen:
     - `/immobilien/angebote/4182215e-6f86-4876-a9d1-e6988867fb07/`
     - `/zwangsversteigerungen/zwangsversteigerung-5252-aspach-mehrfamilienhaus-019f64a4-dc10-7680-8c29-5831d20f42cf/`
- **Erwartet:** Ein Objekt, eine indexierbare URL — oder zwei URLs, bei denen die eine per
  `canonical` auf die andere zeigt.
- **Tatsächlich:** Beide Seiten zeigen dasselbe Objekt (identisch: Mindestgebot `€ 175.000`,
  Aktenzeichen `8 E 8/24d`, Termin `22.09.2026, 09:00`, 750 m²), beide stehen in der Sitemap,
  beide haben `robots: index,follow` und **je einen auf sich selbst zeigenden `canonical`**.
  Titel unterschiedlich („Zwangsversteigerung: Mehrfamilienhaus in Aspach" vs.
  „Mehrfamilienhaus in 5252 Aspach"), Inhalt gleich. Das ist Duplicate Content für 12 Objekte,
  aktuell live.
- **Ursache:** `src/web/src/pages/sitemap.xml.ts:33-43` nimmt **beide** Quellen auf —
  `fetchApiProperties()` (enthält die gespiegelten ZV-Properties) *und* `fetchForeclosureAuctions()`.
  Der Spiegel entsteht in `ForeclosurePropertySyncService.SyncToPropertiesAsync()`.
- **Konsole/Log:** unauffällig
- **Reproduzierbar:** ja
- **Anmerkung:** Zu entscheiden ist, welcher Pfad der kanonische sein soll. Naheliegend: die
  `/zwangsversteigerungen/<slug>`-URL (sprechender Slug, ZV-spezifische Darstellung) als Kanon,
  gespiegelte ZV-Properties aus der Sitemap ausschließen und auf der Angebots-Route ein
  `canonical` auf den Slug setzen.

### B-02 · S3 · Benachrichtigungen — „Zwangsversteigerungen" ist beim Aktivieren vorausgewählt

- **Schritte:**
  1. Als `test.buyer@heimatplatz.dev` anmelden.
  2. `/benachrichtigungen/` öffnen (Benachrichtigungen aus).
  3. Den Schalter „Benachrichtigungen" aktivieren.
  4. Den nun eingeblendeten Block „Immobilientyp" ansehen.
- **Erwartet:** Vorauswahl passend zum Suchstandard — Haus und Grund an, **ZV aus**
  (die Suche schickt im Default nachweislich `PropertyTypesJson=["House","Land"]`).
- **Tatsächlich:** Haus, Grund **und Zwangsversteigerungen** sind angehakt; zusätzlich ist der Modus
  „Eigene Filter" vorausgewählt statt „Wie Filtereinstellungen". Wer den Schalter umlegt und
  speichert, abonniert unbemerkt ZV-Benachrichtigungen.
- **Screenshot:** shots/14-benachrichtigungen-zv-vorausgewaehlt.png
- **Konsole/Log:** unauffällig
- **Reproduzierbar:** ja

### B-03 · S3 · /meine-immobilien/ — 403-Request bei jedem Aufruf durch Nicht-Verkäufer

- **Schritte:**
  1. Als `test.buyer@heimatplatz.dev` (Käufer, Anbieten nicht aktiviert) anmelden.
  2. `/meine-immobilien/` aufrufen.
  3. Netzwerk und Konsole ansehen, Seite neu laden.
- **Erwartet:** Die Seite zeigt den Verkäufer-Hinweis und fragt gar nicht erst Inserate ab.
- **Tatsächlich:** Der Hinweis („Anbieten ist noch nicht aktiviert") erscheint korrekt, das
  Client-Script feuert aber trotzdem `GET /api/properties/user` → **403**, bei jedem Laden.
  In der Konsole landen ein Fehler und eine Warnung:
  `[Heimatplatz] API user list load failed ApiRequestError: Online-Anfrage fehlgeschlagen: 403`.
- **Erwarteter Nutzerschaden:** keiner sichtbar — aber unnötiger fehlschlagender Request und
  Fehlerrauschen, das echte Fehler in der Telemetrie überdeckt.
- **Konsole/Log:** siehe oben
- **Vermutete Stelle:** `src/web/src/components/properties/PropertyStateScript.astro`
- **Reproduzierbar:** ja (2× reproduziert)

### B-04 · S4 · Inserats-Editor — Preis-Platzhalter unformatiert („350000")

- **Schritte:** Als Verkäufer `/inserieren/` öffnen, Feld „Preis" ansehen.
- **Erwartet:** Platzhalter im Hausformat, also `350.000` (die ganze Seite zeigt `€ 349.000`).
- **Tatsächlich:** `€ 350000`. Ursache: `<input type="number">`, damit sind Tausenderpunkte auch
  bei der Eingabe nicht möglich. Gleiches gilt für `foreclosureMinimumBid` (250000) und
  `foreclosureEstimatedValue` (500000).
- **Screenshot:** shots/15-inserieren-desktop.png
- **Reproduzierbar:** ja

### B-05 · S4 · Inserats-Editor auf Phone — Titel-Platzhalter wird abgeschnitten

- **Schritte:** Viewport 390×844, als Verkäufer `/inserieren/` öffnen, Titelfeld ansehen.
- **Erwartet:** Platzhalter vollständig lesbar oder gekürzt formuliert.
- **Tatsächlich:** „Titel – z.B. Haus mit Ga" — der Text läuft aus dem Feld heraus. Auf Desktop
  passt er. Nur 390px betroffen.
- **Screenshot:** shots/20-inserieren-phone.png
- **Reproduzierbar:** ja

### B-06 · S4 · Kartenraster — Hochformat-Fotos werden mit Balken dargestellt

- **Schritte:** `/favoriten/` als `test.buyer` öffnen (oder `/?region=braunau`), Karte
  „Bungalow in Braunau" mit den Nachbarkarten vergleichen.
- **Erwartet:** einheitlich gefüllte Kartenbilder.
- **Tatsächlich:** Das Foto ist hochformatig (1600×2000) und bekommt `object-fit: contain`, alle
  querformatigen bekommen `cover`. Ergebnis: eine Karte mit grauen Seitenbalken in einem sonst
  randlosen Raster. Verhalten ist auf allen Seiten gleich (kein Seiten-Unterschied), tritt also
  bei jedem hochkant fotografierten Handybild auf.
- **Screenshot:** shots/12-favoriten-buyer.png (dritte Karte)
- **Reproduzierbar:** ja
- **Anmerkung:** Möglicherweise Absicht („nicht beschneiden"). Falls ja, bitte als gewollt
  abhaken — optisch fällt es im Raster deutlich auf.

### B-07 · S4 · Suchleiste — „Zeitraum"-Auswahl ohne zugänglichen Namen

- **Schritte:** Startseite Desktop, Accessibility-Baum der Suchleiste ansehen.
- **Erwartet:** wie beim Nachbarelement — `combobox "Sortierung"`.
- **Tatsächlich:** Die sichtbare Zeitraum-Auswahl wird als namenloses `combobox` exponiert; der
  Text „Zeitraum" ist nur die erste (selektierte) Option, also rein visuell. Die **verborgene**
  Variante im Mobil-Filterpanel hat korrekt `aria-label="Zeitraum"` — die sichtbare nicht.
- **Reproduzierbar:** ja

---

## Geklärt — keine Fehler

### G-01 (vormals B-01) · ZV-Objekte auf Test nicht in Suche und Karte — Test-Daten-Artefakt

**Beobachtung im Test:** 8 Objekte unter `/zwangsversteigerungen/…` haben vollständige
Detailseiten und stehen in Sitemap und `llms.txt`, erscheinen aber bei aktivem ZV-Filter nicht in
Liste oder Karte (dort nur 1 Objekt). Es gibt auch keine Listenseite und keinen internen Link.

**Kein Produktfehler.** Die Architektur sieht eine Brücke vor: `ForeclosurePropertySyncService`
spiegelt jede aktive Auktion in eine `Property` mit `SourceName = ForeclosureAuctionConstants.SourceName`
und `SourceId = auction.ExternalId`. Über diesen Spiegel findet der ZV-Filter die Objekte.

Der Spiegel greift nur für Auktionen mit gesetzter `ExternalId`:

```csharp
// ForeclosurePropertySyncService.cs
var activeAuctions = await dbContext.Set<ForeclosureAuction>()
    .Where(a => a.IsActive && a.ExternalId != null)
    .ToListAsync(ct);
```

- **Prod:** `EdikteScraper` setzt `ExternalId` aus dem Edikt-Link (`ExtractExternalId(href)`) →
  Spiegel läuft. Verifiziert: 12 der 17 Prod-Auktionen liegen als ZV-Property vor und sind damit
  über den ZV-Filter auffindbar.
- **Test:** `ForeclosureAuctionSeeder` legt 8 Auktionen an und setzt `ExternalId` **nie** → alle 8
  fallen aus dem Spiegel-Query. Die 2 ZV-Treffer auf Test stammen aus dem Property-Seeder, nicht
  aus dem Spiegel.

**Konsequenz für künftige Testläufe:** Zwangsversteigerungen lassen sich auf Test **nicht**
sinnvoll gegen die Suche prüfen — der Bestand dort ist strukturell anders als auf Prod. Entweder
gegen Prod gegenprüfen (lesend) oder den Seeder um `ExternalId` ergänzen. Als Falle in
`.claude/skills/funktionstest/references/web.md` aufgenommen.

---

## Hinweise / Fragen (keine Fehler)

- **H-01 — Login landet standardmäßig auf `/favoriten/`.** `DEFAULT_LOGIN_REDIRECT = "/favoriten/"`
  in `src/web/src/lib/navigation.ts:1`, während die Registrierung auf `/` geht. Wer `/anmelden/`
  direkt aufruft, landet nach dem Login auf den Favoriten. Über Header und AuthGate wird immer ein
  `returnTo` mitgegeben, dort stimmt der Rücksprung. Bewusst so? Sonst wäre `/` konsistenter.
- **H-02 — Detailseite auf Phone:** Preis, Wohnfläche, Grund und Zimmer stehen je in einer eigenen
  vollbreiten Box untereinander. Ein 2×2-Raster würde die vier Kennzahlen ohne Scrollen zeigen.
- **H-03 — Leerer Inserats-Editor zeigt sofort einen Warnhinweis** („Mindestens ein Foto ist für
  ein veröffentlichtes Inserat erforderlich.") in Warnfarbe, bevor der Nutzer etwas getan hat.
  Als neutraler Hinweis gesetzt wäre das freundlicher.

---

## Geprüft und in Ordnung

**Suche und Filter**

| Funktion | Ergebnis |
|---|---|
| Startseite lädt, 11 Objekte, Karten vollständig | ok |
| Ort-Picker: Bezirke/Gemeinden, Auswahl „Stadt Linz" | ok — 1 Objekt, `?region=linz` |
| Reload stellt Filterzustand aus URL wieder her | ok — Picker zeigt „Linz / 1 ausgewählt" |
| Typ-Chips Haus/Grund, **ZV im Default aus** | ok — API bekommt `["House","Land"]`, URL ohne `type` |
| ZV-Chip an → URL `?type=house,land,foreclosure` | ok (Datenlage siehe B-01) |
| Anbietertyp Privat/Makler | ok |
| Sortierung „Preis aufsteigend" | ok — 95.000 → 890.000, `?sort=price-asc` |
| Zeitraum „7 Tage" | ok — 4 Treffer, alle ≥ 21.07., kombiniert mit `sort` |
| Singular/Plural der Trefferzahl | ok — „1 Objekt" / „11 Objekte" |
| Keine Wohnungen im Bestand/Filter | ok |

**Karte**

| Funktion | Ergebnis |
|---|---|
| Umschalten Liste ↔ Karte, `?ansicht=karte` | ok |
| Legende „12 Inserate auf der Karte" = Trefferzahl | ok |
| Bezirks-Cluster anklickbar, filtert (`?region=braunau`) | ok |
| Preis-Pins erst in der Zoomstufe interaktiv | ok (vorher bewusst `opacity:0`) |
| Pin-Klick öffnet Vorschau mit Foto/Preis/„Ungefähre Lage" | ok |
| Karte und Liste teilen dieselben Filter | ok |

**Detailseiten**

| Funktion | Ergebnis |
|---|---|
| Haus-Detail: Kennzahlen, Basisdaten, Gebäude, Ausstattung, Kosten | ok |
| Preisformat `€ 349.000` (schmales NBSP) | ok, auch in `aria-label` der Karten-Pins |
| ZV-Detail über `/immobilien/angebote/…`: Mindestgebot, Gericht | ok |
| ZV-Detail über `/zwangsversteigerungen/[slug]`: Termin, Schätzwert, Aktenzeichen, Besichtigung | ok |
| Lage-Karte lädt lazy beim Scrollen, Radius statt Punkt | ok |
| Lightbox: Zähler 1/3, Pfeiltasten, Thumbnails | ok |
| Lightbox: Escape schließt, Body-Scroll zurück, Fokus zurück auf Auslöser | ok |
| Kontakt-Sidebar sticky, „Original-Inserat öffnen" | ok |

**Konto und Rechte**

| Funktion | Ergebnis |
|---|---|
| Gast auf `/favoriten/` → AuthGate mit Anmelden/Registrieren | ok |
| Gast klickt Favoriten-Herz → Dialog „Anmeldung erforderlich" | ok |
| Login mit falschem Passwort → „Ungültige E-Mail-Adresse oder Passwort." | ok, verrät nicht ob die Adresse existiert |
| Login korrekt, `returnTo=/inserieren/` | ok |
| Abmelden → `/anmelden/?returnTo=…` | ok |
| Blockierte Objekte werden aus der Liste gefiltert | ok — 2 blockiert → 9 statt 11 |
| Käufer auf `/meine-immobilien/` und `/inserieren/` → Verkäufer-Hinweis | ok (Request siehe B-03) |
| Profil: Stammdaten, Anbietertyp, Passwort, Konto löschen | ok |
| Passwort-Formular leer → Fokus aufs erste Pflichtfeld, kein Submit | ok |
| Passwort-Bestätigung abweichend → deutsche Meldung „…stimmen nicht überein" | ok |
| Inserats-Editor leer abschicken → blockiert, Fokus auf PLZ | ok |
| `returnTo`-Absicherung gegen Open Redirect | ok — Origin-Prüfung, `//` und `/\` blockiert |

**Technik und Auslieferung**

| Prüfung | Ergebnis |
|---|---|
| CSP ist **enforce** (Probe `img-src` gegen example.com blockiert) | ok |
| Alle geladenen Ressourcen von erlaubten Origins (self, test-api, analytics) | ok |
| `X-Robots-Tag: noindex, nofollow` auf Test | ok |
| `robots.txt` sperrt Konto-, Debug-, Intern- und API-Pfade | ok |
| Statuscodes: 12 öffentliche Routen 200, erfundene URL und erfundene Objekt-ID je 404 | ok |
| `/llms.txt`, `/llms-full.txt`, `/sitemap.xml`, `/api/health.json` | ok |
| Konsole auf Startseite und Detailseite | sauber |

**Darstellung**

| Prüfung | Ergebnis |
|---|---|
| Phone 390×844: Startseite, Detail, Editor — **kein** horizontales Scrollen | ok (`scrollWidth` 375 ≤ 390, keine überstehenden Elemente) |
| Phone: Filter klappt auf, Chips und Ort-Picker bedienbar | ok |
| Phone: Burger-Menü öffnet, Fokus wandert hinein, Hintergrund nicht scrollbar | ok |
| Phone: Escape schließt, `inert` gesetzt, Fokus zurück auf den Auslöser | ok |
| Desktop: geschlossene Navigation ist `inert` + `aria-hidden`, nicht im Tab-Fluss | ok |
| Light-Theme: strukturgleich zu Dark, Kontraste in Ordnung | ok |
| Sie-Form und deutsche Texte durchgehend, keine Platzhalter/Resource-Keys | ok |

---

## Nicht getestet / nicht testbar

- **Inserat anlegen und veröffentlichen (E2E)** inkl. Foto-Upload, KI-Beschreibung,
  Ansprechpartner und anschließender Kontrolle in `/meine-immobilien/` und der Suche. Nur der
  Editor-Aufbau und die Pflichtfeld-Validierung wurden geprüft. Bewusst ausgelassen, um den
  Testdatenbestand nicht zu vergrößern und wegen des Umfangs.
- **`/intern/**` (Admin-Bereich)** — Nutzer-, Immobilien-, Feedback-, Kontakt-, Analytics- und der
  gesamte Marketing-/CRM-Bereich. Braucht den Admin-Login und wäre ein eigener Durchlauf.
- **Registrierung, E-Mail-Bestätigung, Passwort-vergessen** — Mailstrecke nicht ausgelöst.
- **Feedback-Nachricht abschicken** (inkl. Bild/Sprachnachricht) — nur die Seite angesehen.
- **`/karte-embed`** — nur Statuscode geprüft, nicht als eingebettete Ansicht bedient (hängt an
  der MAUI-App).
- **Rollen `test.broker` und `test.verwaltung`** — nicht angemeldet.
- **Tablet 768 und 1920 breit** — nur Phone und Desktop geprüft.
- **Paging** — der Testbestand ist mit 11–12 Objekten zu klein für eine zweite Seite.
- Getestet wurde das **deployte** Test-Bundle; ob es dem lokalen `6ec8d28` entspricht, wurde nicht
  verifiziert.
