# Konzept: Partner-Seite `/partner/`

Stand: 04.08.2026 · Status: Umgesetzt (Phasen 1–3; Immobär-Eintrag über /intern/partner/ offen)

## 1. Ziel und Einordnung

Eine öffentliche, indexierbare Seite, die alle Partner von Heimatplatz präsentiert —
aktuell Immobär Immobilien (OpenImmo-Feed live, ~45 Inserate auf Prod), absehbar weitere
(findmyhome, derStandard, Justimmo-Makler sind in Gesprächen).

Die Seite erfüllt drei Zwecke:

1. **Vertrauen für Besucher:** "Hinter den Inseraten stehen echte regionale Makler."
2. **Gegenwert für Partner:** Sichtbarkeit + Backlink — das konkrete Versprechen, das
   die Marketing-Akquise (`/intern/marketing`) Maklern machen kann. Die Partner-Seite
   ist damit direkt ein Verkaufsargument im Onboarding.
3. **SEO/AEO:** Eine weitere kuratierte, verlinkbare Inhaltsseite mit strukturierten
   Organization-Daten.

**Abgrenzung zu `/makler/`:** `/makler/` ist die Akquise-Seite ("Werden Sie Partner",
Lead-Formular). `/partner/` ist das Schaufenster ("Das sind unsere Partner"). Beide
verlinken aufeinander: Die Partner-Seite endet mit einem CTA-Zettel Richtung `/makler/`,
die Makler-Seite bekommt einen Vertrauens-Verweis "Diese Partner sind schon dabei".

## 2. Seitenaufbau

**Design-Richtung (final, 4.8.): Partnerverzeichnis im Regionalblatt-Stil —
Blue-Ocean-Ansatz.** Nach zwei verworfenen Fassungen (ruhige Karten, dann
Wow-Effekte) gilt: nicht auf den Faktoren konkurrieren, die jede Portal-Partnerseite
bedient, sondern eigene schaffen. ERRC-Schema:

| | |
|---|---|
| **Eliminieren** | Karten-Grid, Logo-Wand, Effekt-Gimmicks (Stempel-Animationen, Countups, Schräglagen), rote Hero-Fläche |
| **Reduzieren** | Farbeinsatz (Rot nur als Signal), Bewegung (nur Link-Hover), Marketing-Sprache |
| **Steigern** | Nachprüfbare Fakten (Live-Zahl + "Stand"-Datum, OpenImmo-Automatik, Region, seit-Jahr), Typografie/Lesbarkeit (max-w-prose, klare Hierarchie) |
| **Kreieren** | Verzeichnis-Metapher (Regionalblatt), Partnerschafts-Erklärung mit überprüfbaren Zusagen, CTA als "Anzeige in eigener Sache", Partner-Nachweis auf Objekt-Detailseiten |

Begründung aus der UX-Recherche (NN/g "Communicating Trustworthiness", Trust-Signal-
Guides): Vertrauen entsteht durch spezifische, nachprüfbare Angaben statt Selbstlob,
Logos im Kontext statt als Wand, sichtbar gepflegte Aktualität (Stand-Datum).

### 2.1 Masthead (Zeitungs-Titelkopf)

Keine Hero-Fläche. Dicke + dünne Haarlinie, Wappen als Krone, zentrierter Kicker
in Markenrot, großer Titel, darunter eine **Datumszeile** zwischen Haarlinien:
"Stand: {Datum} · Oberösterreich · N Makler · M aktive Inserate" (Zahlen live aus
der API). Lead-Absatz mit roter Initiale (einziges Schmuck-Element).

**Benennung (4.8. abends, Kundenwunsch):** Öffentlich heißt die Seite
**"Maklerverzeichnis"** — bewusst KEIN "Partner"-Begriff (klingt nach offiziellem
Partnerprogramm); es sind schlicht die regionalen Maklerbüros, deren Objekte auf
Heimatplatz erscheinen. Ebenso keine technischen Details (OpenImmo o. ä.) in
öffentlichen Texten. Route `/partner/`, Feature-/Intern-Namen bleiben unverändert.

### 2.2 Verzeichnis-Einträge (Kernstück)

Partner als nummerierte redaktionelle Einträge (01, 02, …) mit Haarlinien statt
Karten. Pro Eintrag:

| Element | Inhalt | Quelle |
|---|---|---|
| Nummer | rote Mono-Ziffer | Reihenfolge |
| Name + Meta | Name groß, darunter "MAKLER-PARTNER · Region" klein-kapitalig | Partner-Datensatz |
| Logo | klein rechts im Eintrag (Kontext statt Logo-Wand; auch im Dark Mode helles Feld) | Media-Pipeline, selbst gehostet |
| Kurzbeschreibung | max-w-prose | Partner-Datensatz |
| Ticker-Zahl | Live-Inseratszahl als größter Wert des Eintrags (große rote Ziffer + Label "AKTIVE INSERATE") rechts oben | Live-Count über SourceName |
| Links | "Inserate ansehen →" (Suche mit SellerName via SearchText-Parameter) + "Website ↗" (extern, `rel=noopener`, bewusst **follow**) | Partner-Datensatz |

Nummern groß und rot (Verzeichnis-Charakter), Rubrik-Label als schwarzer Balken,
Meta-Zeile "MAKLER-PARTNER · Region · Seit Jahr". Eine frühere
Partnerschafts-Erklärung (§1–3) und die "Datenübernahme"-Zeile wurden am 4.8.
auf Kundenwunsch entfernt — die Edikte-Transparenz steht seither als Fußnote
über der Schlusslinie.

### 2.4 Abschluss-CTA: "Anzeige in eigener Sache"

Klassische Zeitungsanzeige mit Doppelrahmen: Kicker "ANZEIGE IN EIGENER SACHE",
"Ihre Objekte auf Heimatplatz?" + Button zu `/makler/`. Trägt bei leerem Verzeichnis
die Seite. Kein zweites Lead-Formular — das Formular bleibt einzig auf `/makler/`.

### 2.5 Partner-Nachweis auf Objekt-Detailseiten

Im Anbieter-Kasten der Detailseite (`/immobilien/angebote/[id]`): stammt das Objekt
aus dem Feed eines gelisteten Partners (exaktes `SourceName`-Match), erscheint ein
Siegel-Block "Heimatplatz-Partnerbetrieb · Partner seit {Jahr} · Objektdaten kommen
automatisch vom Makler" mit Link ins Verzeichnis. Vertrauen am Ort der
Kaufentscheidung — dort, wo die Herkunftsfrage tatsächlich entsteht.

## 3. Datenhaltung — Empfehlung: eigenes Backend-Feature `Partners`

### Warum nicht statische Web-Config?

Eine `partners.ts` im Web wäre schneller, aber: jeder neue Partner = Deploy (die
Pipeline ist konkret: findmyhome, derStandard, Justimmo), MAUI könnte nichts anzeigen,
und die Live-Inseratszahl braucht ohnehin die API. Das Kontakt-Stammdaten-Muster
(Pflege unter `/intern/` ohne Deploy) hat sich bewährt — Partner sind derselbe Fall.

### API-Feature (Struktur nach CLAUDE.md-Konvention)

```
src/api/src/Features/Partners/
├── Heimatplatz.Api.Features.Partners/
│   ├── README.md
│   ├── Configuration/ServiceCollectionExtensions.cs   # AddPartnersFeature()
│   ├── Data/Entities/Partner.cs                       # : BaseEntity
│   ├── Data/Configurations/PartnerConfiguration.cs
│   └── Handlers/
│       ├── GetPartnersHandler.cs                      # public
│       └── Admin-CRUD-Handler                         # X-Admin-Key
└── Heimatplatz.Api.Features.Partners.Contracts/
    ├── README.md
    └── Mediator/Requests/...
```

**Entity `Partner : BaseEntity`:**

| Feld | Typ | Zweck |
|---|---|---|
| `Name` | string | Anzeigename ("Immobär Immobilien") |
| `Category` | enum | `Broker`, `DataSource` (erweiterbar: `Portal`, `Technology`) |
| `Description` | string | Kurzbeschreibung für den Zettel |
| `WebsiteUrl` | string | externer Link |
| `LogoUrl` | string? | über bestehende Media-Pipeline hochgeladen (Original + Display-Variante), **selbst gehostet** — kein Hotlink auf Partner-Domains (CSP ist ENFORCING, `img-src` würde brechen) |
| `City` / `Region` | string? | "Bezirk Ried" |
| `PartnerSince` | DateOnly? | für den Stempel |
| `SourceName` | string? | Verknüpfung zu `Property.SourceName` (`immobaer.at`) → Live-Count |
| `SellerName` | string? | für den "Inserate ansehen"-Suchlink |
| `DisplayOrder` | int | Sortierung |
| `IsVisible` | bool | Moderation ohne Löschen |

**Endpoints:**

- `GET /api/partners` (public): sichtbare Partner sortiert, je Partner
  `ActiveListingCount` (Count über `SourceName`, `IsHidden`-Inserate ausgenommen).
  Später ETag-fähig für MAUI (Stammdaten-Conditional-GET-Muster).
- `POST/PUT/DELETE /api/admin/partners` (X-Admin-Key, fail-closed wie
  Nutzer-/Immobilienverwaltung).

**Kein Prod-Seeder** — Partner sind echte Vertragsdaten, keine Referenzdaten. Für die
Test-Umgebung ein Demo-Seeder (`Database:EnableSeeding`) mit 2–3 fiktiven Partnern.

### Pflege-UI `/intern/partner/`

Gleiche Muster wie `/intern/kontakt`: Admin-API-Client aus `lib/server/admin-api.ts`,
CSRF-Schutz, 303-Redirect nach Form-Post, Logo-Upload. Liste + Formular reichen —
kein eigenes Dashboard.

### Web-Feature `src/web/src/features/partners/`

`api.ts` nach dem Muster von `features/legal/api.ts`: `fetchPartners()` mit
`cached()`-TTL (10 min), PascalCase/camelCase-tolerante Normalisierung, Fallback =
leere Liste. **Leerzustand ist Pflicht-Design:** API down oder noch kein Partner
gepflegt → Seite zeigt Hero + Datenquellen + Werde-Partner-CTA, keine leere Tafel.
Nach Admin-Mutationen betroffene Cache-Keys invalidieren.

## 4. SEO / AEO

- `BaseLayout` mit `canonicalPath="/partner/"`, Meta-Titel/-Description aus i18n
  (`src/i18n/de/partner.ts`, alles über `t()`)
- `sitemap.xml.ts`: `staticRoutes` um `"/partner/"` ergänzen
- JSON-LD: `breadcrumbSchema` + `ItemList` mit `Organization`-Einträgen je Partner
  (name, url, logo, address/addressLocality wenn gepflegt)
- `llms.txt` um die Partner-Seite ergänzen
- Footer-Link "Partner" neben "Für Makler" (`SiteFooter.astro`)

## 5. Rechtliches / Inhaltliches

- **Logo-Freigabe je Partner schriftlich einholen.** Immobär hat der Partnerschaft
  zugestimmt — die Logo-Nutzung für die Partner-Seite explizit bestätigen lassen
  (eine Zeile per Mail reicht).
- **Keine Partner in Verhandlung listen** (findmyhome, derStandard, Justimmo) —
  erst nach Zusage. Bis dahin trägt der Werde-Partner-CTA die Seite.
- Justiz-Edikte nur als Datenquelle, nicht als Partner (siehe 2.3).

## 6. Umsetzungsphasen

1. **API:** Feature `Partners` + Contracts + Admin-CRUD + Migration + Test-Seeder,
   Registrierung in `Core.Startup`, READMEs.
2. **Intern:** `/intern/partner/` Pflege-UI mit Logo-Upload.
3. **Web:** `/partner/`-Seite (Hero, Zettel-Grid, Datenquellen, CTA), Footer-Link,
   Sitemap, JSON-LD, i18n. Verweis-Abschnitt auf `/makler/` ("schon dabei").
4. **Inhalt:** Immobär über `/intern/partner/` eintragen (Logo, Beschreibung,
   `SourceName=immobaer.at`, `SellerName=Immobär Immobilien`) — danach ohne Deploy
   erweiterbar.
5. **Später/optional:** MAUI-Ansicht (Conditional-GET-Stammdaten-Muster),
   Logo-Leiste auf der Startseite.

MAUI ist bewusst NICHT Teil des ersten Wurfs — die API macht den Weg frei, ohne dass
jetzt App-Aufwand entsteht.

## 7. Mockup

Ein visueller Entwurf (Hero, Partner-Karten, Platzhalter, CTA, Dark Mode) liegt als
statisches HTML-Mockup vor — Tokens 1:1 aus `starwind.css` (Papier-Palette,
Zettel-Schatten), bewusst ruhige Ausführung ohne Stempel und Animationen.
