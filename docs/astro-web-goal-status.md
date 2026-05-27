# Astro Web App Goal Status

Stand: 2026-05-27 (aktualisiert nach Paritaetspruefung)

## Ziel

Die Astro-JS-Web-App soll im Prinzip wie die Uno Platform WASM-App funktionieren und den relevanten Funktionsumfang abdecken. Die Optik darf Astro-/Web-typisch bleiben, soll sich aber an der Uno-App orientieren. Waehrend der Umsetzung hat SEO fuer Google-Sichtbarkeit in Oberoesterreich Prioritaet.

Status: nutzbar. Alle produktiven Uno-Presentation-Seiten haben Astro-Pendants. Auth, Favoriten/Blockieren und Property-CRUD sind via Client-JS und Backend-API verdrahtet. Offene Punkte sind ueberwiegend Polish und Audit-Tiefe.

## Bisher Erledigt

### Web-App-Grundlage

- Astro 6 unter `src/web/` mit Tailwind 4, Starwind UI, Content Collections.
- BaseLayout mit SEO-Metadaten, Canonicals, OG/Twitter, JSON-LD-Array, sitemap-index, llms.txt, robots.txt.
- SiteHeader mit Page-Context-Title (wie Uno), mobiler Filter-Toggle, Side-Drawer mit Auth-/Gast-Sektionen.
- SiteFooter mit Web-App-Links, SEO-Landings, Sitemap/Robots/LLMs, Datenschutz, Impressum.
- PropertyStateScript.astro zentralisiert Auth-, Favoriten-, Block-, Filter-, Notification- und Property-CRUD-Logik client-seitig.

### SEO fuer Oberoesterreich

- Oeffentliche Immobilienseiten mit Canonicals, SEO-Metadaten und strukturierten Daten (Organization, WebSite, RealEstateAgent, BreadcrumbList, Offer/Residence).
- `robots.txt`, `sitemap-index.xml`, `llms.txt` und Noindex-Regeln fuer private/API-nahe Seiten.
- Regionale Landingpages fuer Oberoesterreich und alle Bezirke.
- 80 lokale Ortsseiten unter `/immobilien/orte/.../`.
- Globaler Orte-Hub `/immobilien/orte/` mit A-Z- und Regionsstruktur.
- Ratgeber-Seiten fuer wichtige Suchintentionen.
- Suchintent-Seiten: Haus/Wohnung/Grundstueck kaufen, Privat/Makler, Zwangsversteigerungen in Oberoesterreich.
- FAQ-Bloecke und `FAQPage` JSON-LD auf relevanten SEO-Seiten.
- Interne Verlinkung Region <-> Ort <-> Suche <-> Footer <-> Header.

### Immobiliensuche

- Suchseite `/immobilien/` mit Filtern: Typ (Wohnung/Haus/Grund/ZV), Anbieter (Privat/Makler/Portal), Region, Alter, Sortierung.
- Pagination fuer Suchergebnisse.
- Live-API-Angebote werden in Suchlisten integriert.
- API-Bilder werden vorgeprueft, Fallback auf OG-SVG.
- Gespeicherte Filterpraeferenzen wirken auf die Suche.
- Multi-Ort-Filter aus den Uno-OrtPickern: speichert mehrere Orte in `SelectedOrtes`; manuelle Region-Auswahl ueberschreibt gespeicherte Orte; Matching nur ueber Orts-/Regionsmetadaten.

### Detailseiten

- Statische Detailseiten unter `/immobilien/[slug]/`.
- API-Detailseiten unter `/immobilien/angebote/[id]/`.
- Zwangsversteigerungs-Detailseiten unter `/zwangsversteigerungen/[slug]/` mit KPIs (Termin, Schaetzwert, Mindestgebot, Flaeche), Versteigerung, Basisdaten, Rechtsdaten und Dokumenten (Edikt, Grundriss, Lageplan, Lang-/Kurzschaetzung).
- Kontaktbereich (Sidebar) und Bottom-Contact-Dock auf allen Detailseiten.
- Schema.org Offer + Residence JSON-LD pro Immobilie.

### Favoriten, Blockieren und Benutzerlisten

- Favoriten-, Blockiert- und Meine-Immobilien-Seiten.
- Kartenaktionen fuer Favorit, Blockieren und Drei-Punkt-Menue auf Cards UND Detailseiten.
- Persistenz lokal (localStorage) plus API-Sync (`/api/favorites`, `/api/blocked`) wenn eingeloggt.
- Zwangsversteigerungen werden in passenden Listen beruecksichtigt.

### Auth, Profil und Konto

- Login `/anmelden/` und Registrierung `/registrieren/` mit echter `apiRequest`-Integration gegen `/api/auth/login` und `/api/auth/register`.
- Validierung in JS (Pflichtfelder, Passwoerter-Match, Anbietertyp wenn Seller, Firmenname wenn Broker).
- Session in `localStorage` unter `heimatplatz:session` mit AccessToken, RefreshToken, UserId, Email, FullName, ExpiresAt.
- JWT-Rollen-Extraktion (Buyer/Seller) aus `user_role`-Claim.
- `data-auth-only` / `data-guest-only` / `data-auth-role-seller` / `data-auth-role-buyer` Sichtbarkeit auf Header, Side-Drawer und Profil.
- Profilseite zeigt Initialen, Name, Email und Rollen-Badge.
- Bearer-Token wird automatisch auf alle API-Calls gesetzt (ausser `skipAuth: true`).
- Logout-Aktion im Side-Drawer.

### Inserieren und Bearbeiten

- Inserieren `/inserieren/` und Bearbeiten `/immobilien/bearbeiten/`.
- Formularvalidierung an Uno-Regeln angepasst: Titel >= 10, Beschreibung >= 50, Preis/Adresse/Ort/PLZ Pflicht, mindestens ein Foto.
- Fotoauswahl mit 20-Bilder-Grenze, Vorschau, Loeschen.
- Bildupload via `/api/properties/images` und Persistierung als Property mit `apiRequest`.
- Ortsauswahl ueber Gemeinden in Oberoesterreich (`/api/locations`).
- Versteigerungsfelder fuer Zwangsversteigerungen (Gericht, Aktenzeichen, Auktionstermin, Mindestgebot, Schaetzwert, Status, Besichtigung, Edikt-URL, Einlagezahl, Katastralgemeinde, Grundstuecksnummer, Flaechen, Widmung, Zustand, Notizen).
- Typabhaengige Felder: Haus zeigt Wohnflaeche/Zimmer/Baujahr; Grundstueck blendet diese aus und speichert sie nicht.
- Edit-Modus laedt Existing Property via API und prefillt Felder.

### Filter- und Benachrichtigungseinstellungen

- Filter `/filter-einstellungen/` mit Multi-Ort-Auswahl (Linz, Steyr, Wels und alle Bezirke mit Untergemeinden), Zeitraum, Immobilientyp, Anbietertyp, Sortierung.
- Persistenz lokal plus Sync mit `/api/auth/filter-preferences` wenn eingeloggt.
- Benachrichtigungen `/benachrichtigungen/` mit Enable-Toggle, FilterMode (SameAsSearch/Custom/All), Custom-Filter, Multi-Ort.
- Auto-Sichtbarkeit der Custom-Filter abhaengig von Mode und Enable.
- Persistenz lokal plus Sync mit `/api/notifications/preferences` wenn eingeloggt.

## Zuletzt Verifiziert

```powershell
npm run check  # 0 Fehler, 0 Warnungen, 0 Hints
npm run build  # erfolgreich, 314 Seiten, sitemap-index erzeugt
```

Browser-Verifikation mit Playwright (Desktop 1280px und Mobile 390px):

- Home Desktop: HomeSearch-Sektion mit Filter-Chips, statische und Live-Angebote, Pagination.
- Home Mobile: Filter-Toggle expandiert/kollabiert, keine horizontalen Overflows.
- Suche Desktop: Filter, Pagination, Live-Angebote integriert.
- Inserieren Desktop und Mobile: Form-Felder vollstaendig, Layout sauber.
- Zwangsversteigerung-Detail: KPIs, Rechtsdaten, Dokumente, JSON-LD korrekt.
- Property-Detail: Hero, Galerie-Badge, Kontakt-Sidebar plus Bottom-Dock, JSON-LD mit Offer/Residence.
- Filter-Einstellungen: Multi-Ort-Auswahl Linz/Linz-Land per default expandiert, andere Bezirke kollabiert.
- Benachrichtigungen: Custom/SameAsSearch/All-Modi.
- Profil Gast: zeigt Hinweis und Anmelden-Button.

## Bekannte Offene Punkte

Diese Punkte sind als sinnvolle naechste Schritte vermerkt, aber nicht blockierend:

- Apartment/Wohnung-Filter in den `filter-einstellungen` und Notification-Preferences fehlt, weil das Uno-DTO (`FilterPreferencesDto`, `NotificationPreferenceDto`) keinen `IsWohnungSelected`-Slot hat. Aktuell wirkt die Wohnung-Auswahl auf Home/Suche, wird aber nicht persistiert.
- End-to-End-Tests fuer Auth-Login mit echtem Backend (gegen `https://heimatplatz-api.azurewebsites.net`).
- Visueller A/B-Vergleich gegen die Uno-WASM-Variante (sofern verfuegbar) fuer Designentscheidungen.
- Optional: Mobile Filter-Sektion und Page-eigener Filter sind redundant sichtbar wenn beide expandiert sind. Ggf. eine ausblenden, wenn die andere aktiv ist.
- Optional: `RealEstateListing` als zusaetzlicher `@type` neben `Offer` fuer noch praezisere Search-Snippets.

## Naechster Sinnvoller Schritt

Mit dem aktuellen Stand ist die Astro-App produktiv nutzbar. Der naechste sinnvolle Schritt haengt von der Roadmap ab:

1. **Wenn SEO-Wachstum Prio hat:** weitere Suchintent-Seiten und Long-Tail-Landings (z.B. `/immobilien/haus-mit-garten-linz/`, `/immobilien/wohnung-mit-balkon-wels/`) plus dazugehoerige FAQ-Bloecke ergaenzen.
2. **Wenn UX-Polish Prio hat:** Mobile Filter-Sektionen konsolidieren, Hero-Image auf Detailseiten groesser darstellen, Galerie-Carousel fuer mehrere Bilder.
3. **Wenn Daten-Tiefe Prio hat:** das Backend um `IsWohnungSelected` erweitern und in beiden Frontends nachziehen.
4. **Wenn Auth-Audit Prio hat:** rollenbasierte Server-Side Guards via Astro Middleware (SSR-only Seiten fuer Seller/Buyer), aktuell laufen alle Guards client-seitig.
