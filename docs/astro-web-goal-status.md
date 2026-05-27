# Astro Web App Goal Status

Stand: 2026-05-27

## Ziel

Die Astro-JS-Web-App soll im Prinzip wie die Uno Platform WASM-App funktionieren und den relevanten Funktionsumfang abdecken. Die Optik darf Astro-/Web-typisch bleiben, soll sich aber an der Uno-App orientieren. Waehrend der Umsetzung hat SEO fuer Google-Sichtbarkeit in Oberoesterreich Prioritaet.

Status: nicht abgeschlossen. Es wurde viel umgesetzt und verifiziert, aber der volle Zielumfang ist noch nicht abschliessend gegen alle Uno-Flows auditiert.

## Bisher Erledigt

### Web-App-Grundlage

- Astro-App unter `src/web/` aufgebaut.
- Starwind/Tailwind-basierte UI-Komponenten integriert.
- Gemeinsames Layout, Header, Footer und App-Shell erstellt.
- Mobile Navigation und Konto-/Listen-Einstiege an die App-Struktur angeglichen.

### SEO fuer Oberoesterreich

- Oeffentliche Immobilienseiten mit Canonicals, SEO-Metadaten und strukturierten Daten erweitert.
- `robots.txt`, `sitemap`, `llms.txt` und Noindex-Regeln fuer private/API-nahe Seiten umgesetzt.
- Regionale Landingpages fuer Oberoesterreich und Bezirke erstellt.
- 80 lokale Ortsseiten unter `/immobilien/orte/.../` angelegt.
- Globaler Orte-Hub `/immobilien/orte/` mit A-Z- und Regionsstruktur erstellt.
- Ratgeber-Seiten fuer wichtige Suchintentionen angelegt.
- Suchintent-Seiten wie Haus kaufen, Wohnung kaufen, Grundstueck kaufen, Privat/Makler und Zwangsversteigerungen in Oberoesterreich umgesetzt.
- FAQ-Bloecke und `FAQPage` JSON-LD auf relevanten SEO-Seiten ergaenzt.
- Interne Verlinkung zwischen Region, Ort, Suche, Footer und Header ausgebaut.

### Immobiliensuche

- Suchseite `/immobilien/` mit Filterung nach Typ, Anbieter, Region, Alter und Sortierung umgesetzt.
- Pagination fuer Suchergebnisse ergaenzt.
- Live-API-Angebote werden in Suchlisten integriert.
- API-Bilder werden vorgeprueft und erhalten Fallbacks.
- Gespeicherte Filterpraeferenzen wirken auf die Suche.
- Multi-Ort-Filter aus den Uno-OrtPickern umgesetzt:
  - Filtereinstellungen speichern mehrere Orte in `SelectedOrtes`.
  - Suche nutzt gespeicherte Orte.
  - Manuelle Region-Auswahl ueberschreibt gespeicherte Orte.
  - Ortsmatching wurde auf Orts-/Regionsmetadaten eingegrenzt, damit Beschreibungstreffer wie "Richtung Linz" nicht falsch matchen.

### Detailseiten

- Statische Detailseiten fuer Immobilien erstellt.
- API-Detailseiten unter `/immobilien/angebote/[id]/` ergaenzt.
- Zwangsversteigerungs-Detailseiten unter `/zwangsversteigerungen/[slug]/` erstellt.
- Kontaktbereich und Bottom-Contact-Dock fuer statische, API- und Live-Detailseiten umgesetzt.
- Kontakttexte, Preis-/Flaechenangaben und Detaildaten naeher an Uno-Paritaet gebracht.

### Favoriten, Blockieren und Benutzerlisten

- Favoriten-Seite umgesetzt.
- Blockiert-Seite umgesetzt.
- Meine-Immobilien-Seite umgesetzt.
- Kartenaktionen fuer Favorit, Blockieren und Drei-Punkt-Menue ergaenzt.
- Lokale und API-basierte Userlisten werden unterstuetzt.
- Zwangsversteigerungen werden in passenden Listen beruecksichtigt.

### Auth, Profil und Konto

- Login-Seite erstellt.
- Registrierungsseite erstellt.
- Profilseite erstellt.
- Lokale Session-Verarbeitung fuer Web-Flows integriert.
- Konto-/User-Flows sind vorhanden, aber noch nicht abschliessend gegen alle Uno-Auth- und Rollenfaelle auditiert.

### Inserieren und Bearbeiten

- Inserieren-Seite `/inserieren/` umgesetzt.
- Bearbeiten-Seite `/immobilien/bearbeiten/` umgesetzt.
- Formularvalidierung an Uno-Regeln angepasst:
  - Titel mindestens 10 Zeichen.
  - Beschreibung mindestens 50 Zeichen.
  - Preis, Adresse, Ort und PLZ erforderlich.
  - Mindestens ein Foto fuer neue Inserate erforderlich.
- Fotoauswahl mit 20-Bilder-Grenze und Vorschau umgesetzt.
- Ortsauswahl ueber Gemeinden in Oberoesterreich integriert.
- Versteigerungsfelder fuer Zwangsversteigerungen ergaenzt.
- Typabhaengige Formularfelder verbessert:
  - Haus zeigt Wohnflaeche, Zimmer und Baujahr.
  - Grundstueck blendet Wohnflaeche, Zimmer und Baujahr aus.
  - Grundstueck speichert diese Hausfelder nicht versehentlich im Payload.

### Filter- und Benachrichtigungseinstellungen

- Filtereinstellungen-Seite umgesetzt.
- Multi-Ort-Auswahl nach Uno-OrtPicker-Prinzip ergaenzt.
- Benachrichtigungsseite umgesetzt.
- Benachrichtigungsmodus verhaelt sich wie in Uno:
  - `SameAsSearch` versteckt eigene Filter.
  - `Custom` zeigt eigene Filter.
  - Deaktivierte Benachrichtigungen verstecken den Filterbereich.
- Gespeicherte Benachrichtigungsorte werden wieder geladen.

## Zuletzt Verifiziert

Die folgenden Checks wurden im Verlauf der Umsetzung wiederholt erfolgreich ausgefuehrt:

```powershell
npm run check
npm run build
```

Letzter Build-Stand:

- `astro check`: 0 Fehler, 0 Warnungen.
- `astro build`: erfolgreich.
- 314 Seiten gebaut.
- Sitemap wird erzeugt.

Browser-Verifikation mit Playwright wurde fuer die zuletzt geaenderten Bereiche durchgefuehrt:

- Filtereinstellungen: Multi-Ort-Auswahl speichert `Linz` und `Leonding`.
- Immobiliensuche: gespeicherte Orte filtern korrekt; manuelle Region-Auswahl ueberschreibt gespeicherte Orte.
- Benachrichtigungen: Filterbereiche blenden korrekt ein/aus; gespeicherte Orte werden angehakt.
- Inserieren/Bearbeiten: Grundstueck blendet Hausfelder aus; Haus zeigt sie wieder; Mobile ohne horizontalen Overflow.

## Bekannte Offene Punkte

Diese Punkte sind noch nicht als vollstaendig erledigt bewiesen:

- Vollstaendiger Requirement-by-Requirement-Audit gegen alle Uno-Presentation-Seiten.
- Rollen- und Auth-Paritaet fuer Seller/Buyer-Flows im Web.
- Remote-API-Fehlerzustaende und Offline-/Fallback-Verhalten ueber alle Userlisten und Formularflows.
- Detailtiefe bei typ-spezifischen Immobilienfeldern, besonders fuer Zwangsversteigerungen.
- Voller visueller Vergleich der wichtigsten Uno-Seiten gegen Desktop und Mobile Web.
- End-to-End-Tests fuer Web-UI-Flows, nicht nur manuelle Playwright-Pruefung.
- Finaler SEO-Audit fuer indexierbare vs. private/noindex Seiten.

## Naechster Sinnvoller Schritt

Als naechstes sollte ein systematischer Uno-vs-Astro-Paritaetscheck als Markdown-Checklist erstellt und abgearbeitet werden. Der erste konkrete Umsetzungspunkt daraus sollte wahrscheinlich die Auth-/Rollen-Paritaet sein:

- pruefen, welche Uno-Seiten Buyer, Seller oder eingeloggte Nutzer voraussetzen;
- Web-Routen entsprechend absichern oder klar in den lokalen Fallback-Modus fuehren;
- Header, Profil, Meine Immobilien, Inserieren, Favoriten und Blockiert gegen diesen Zustand testen;
- danach `npm run check`, `npm run build` und Playwright-Flows fuer eingeloggt/nicht eingeloggte Nutzer ausfuehren.

Dieser Schritt bringt den Funktionsumfang naeher an die Uno-App und schuetzt gleichzeitig SEO, weil private oder rollenbezogene Seiten sauber von indexierbaren Oberoesterreich-Landingpages getrennt bleiben.
