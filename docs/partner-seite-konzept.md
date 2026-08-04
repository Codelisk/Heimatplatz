# Konzept: Partner-Seite `/partner/`

Stand: 03.08.2026 · Status: Entwurf (nicht umgesetzt)

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

**Design-Richtung: ruhig und hochwertig ("klassisch statt clever").** Die Seite
übernimmt das Niveau der Inserats-Karten (Zettel-Schatten, gerade, klare Typografie),
NICHT die verspielten Extras der Makler-Seite (keine schiefen Zettel, keine
Stempel-Optik, keine Scroll-Reveal-Animationen). Partner sollen seriös präsentiert
werden — die Seite ist Teil des Verkaufsarguments gegenüber Maklern.

### 2.1 Hero (Markenpanel-Look, ruhig)

Gleicher Rot-Verlauf (`linear-gradient(160deg,#c9161c,#ee6a50)`, Dark-Variante wie
Makler-Seite) und dezente Haus-Linienzeichnung als Wasserzeichen — aber ohne
Einstiegs-Animationen und ohne Siegel-Stempel. Inhalte:

- Kicker: "GEMEINSAM FÜR DIE REGION" (o. ä.)
- Titel: "Unsere Partner"
- Intro: 1–2 Sätze, warum Heimatplatz mit regionalen Maklern zusammenarbeitet
- **Live-Zahlen als schlichte Zeile:** "N Partner · M aktive Inserate" — kommt aus
  der API, nicht hartcodiert

### 2.2 Partner-Karten (Kernstück)

Partner als gerade Karten mit dem etablierten Zettel-Schatten der Inserats-Karten
(`--zettel-shadow`, Hover vertieft nur den Schatten). Pro Karte:

| Element | Inhalt | Quelle |
|---|---|---|
| Logo | helles Logo-Feld als Kartenkopf (auch im Dark Mode hell — Logos nie invertieren) | Media-Pipeline, selbst gehostet |
| Meta-Zeile | "MAKLER-PARTNER · Partner seit 2026" (dezent, uppercase, Markenrot + muted) | Category-Enum + PartnerSince |
| Name + Ort | "Immobär Immobilien" / "Innviertel, Oberösterreich" | Partner-Datensatz |
| Kurzbeschreibung | 2–3 Sätze | Partner-Datensatz |
| Live-Zeile | roter Punkt + "45 aktive Inserate" | API-Count über `Property.SourceName` |
| Aktionen (durch Trennlinie abgesetzt) | Primär-Button "Inserate ansehen" (interne Suche) + Textlink "Website" (extern, `target=_blank rel=noopener`, bewusst **follow** — der Backlink ist Teil des Partner-Gegenwerts) | Partner-Datensatz |

"Inserate ansehen" verlinkt auf die Startsuche mit `SearchText=<SellerName>` (der
`SearchText`-Parameter existiert bereits in `search-query.ts`). Kein neuer Filter nötig.

Solange nur ein Partner gelistet ist, füllt eine dezente Platzhalter-Karte
(gestrichelter Rahmen, "Ihre Objekte auf Heimatplatz?" + Button zu `/makler/`) die
Lücke im Grid — Akquise ohne leere Seite.

### 2.3 Datenquellen-Abschnitt (optional, klein)

Schlichter Transparenz-Hinweis unterhalb der Partner (linke Randlinie, muted):
"Zwangsversteigerungen stammen aus den öffentlichen Edikten der Justiz
(edikte.justiz.gv.at)." Bewusst als *Datenquelle* etikettiert, nicht als
"Partner" — die Justiz ist kein Kooperationspartner.

### 2.4 Abschluss-CTA

Breites Karten-Panel: "Ihre Immobilien auf Heimatplatz? Werden Sie Partner." →
Button zu `/makler/`. Kein zweites Lead-Formular — das Formular bleibt einzig auf
`/makler/`.

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
