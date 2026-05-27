export type GuidePage = {
  slug: string;
  title: string;
  h1: string;
  description: string;
  canonicalPath: string;
  dateModified: string;
  readingTime: string;
  keywords: string[];
  intro: string;
  sections: Array<{
    heading: string;
    body: string[];
    checklist?: string[];
  }>;
  faqs: Array<{
    question: string;
    answer: string;
  }>;
  relatedIntentSlugs: string[];
  relatedRegionSlugs: string[];
};

export const guidePages: GuidePage[] = [
  {
    slug: "haus-kaufen-oberoesterreich",
    title: "Haus kaufen in Oberösterreich: Ratgeber",
    h1: "Haus kaufen in Oberösterreich",
    description:
      "Ratgeber fuer den Hauskauf in Oberösterreich: Lage, Preis, Sanierung, Grundstueck, Pendeln und wichtige Regionen richtig vergleichen.",
    canonicalPath: "/ratgeber/haus-kaufen-oberoesterreich/",
    dateModified: "2026-05-27",
    readingTime: "6 Minuten",
    keywords: [
      "Haus kaufen Oberösterreich",
      "Einfamilienhaus kaufen OÖ",
      "Hauskauf Linz-Land",
      "Haus kaufen Salzkammergut",
    ],
    intro:
      "Beim Hauskauf in Oberösterreich entscheidet selten nur der Kaufpreis. Lage, Grundstueck, Pendelstrecke, Sanierungsbedarf und laufende Kosten muessen gemeinsam betrachtet werden.",
    sections: [
      {
        heading: "Region und Alltag zuerst klaeren",
        body: [
          "Der Zentralraum rund um Linz, Wels und Steyr ist fuer viele Kaeufer attraktiv, weil Arbeitswege, Schulen und Infrastruktur dicht beieinander liegen.",
          "In Regionen wie Salzkammergut, Innviertel und Muehlviertel spielen Ruhe, Natur, Grundstuecksgroesse und Erreichbarkeit eine groessere Rolle.",
        ],
        checklist: [
          "Fahrzeit zu Arbeit, Schule und Nahversorgung pruefen",
          "Gemeindeabgaben und laufende Betriebskosten einplanen",
          "Grundstueck, Zufahrt und Nachbarschaft vor Ort vergleichen",
        ],
      },
      {
        heading: "Sanierung realistisch einpreisen",
        body: [
          "Aeltere Haeuser koennen in guten Lagen attraktiv sein, aber Dach, Fenster, Heizung, Elektrik und Feuchtigkeit entscheiden ueber den tatsaechlichen Aufwand.",
          "Ein niedriger Angebotspreis ist nur dann ein Vorteil, wenn die noetigen Arbeiten, Fristen und Finanzierungsreserven klar sind.",
        ],
      },
      {
        heading: "Angebote vergleichbar machen",
        body: [
          "Vergleiche Haeuser nicht nur nach Preis, sondern nach Preis pro nutzbarer Flaeche, Grundstueck, Zustand, Lagequalitaet und verfuegbaren Unterlagen.",
          "Heimatplatz buendelt klassische Inserate, private Angebote und gerichtliche Immobilienseiten, damit Suchende den Markt in Oberösterreich schneller ueberblicken.",
        ],
      },
    ],
    faqs: [
      {
        question: "Welche Regionen sind fuer den Hauskauf in Oberösterreich besonders gesucht?",
        answer:
          "Haussuchen konzentrieren sich haeufig auf Linz, Linz-Land, Wels, Wels-Land, Voecklabruck, Gmunden und gut erreichbare Gemeinden im Muehlviertel und Innviertel.",
      },
      {
        question: "Was sollte ich vor einer Besichtigung pruefen?",
        answer:
          "Wichtig sind Lage, Baujahr, Sanierungsstand, Energie- und Heizsystem, Grundstuecksgroesse, Zufahrt, rechtliche Unterlagen und die Plausibilitaet des Kaufpreises.",
      },
      {
        question: "Sind private Hausangebote in Oberösterreich sinnvoll?",
        answer:
          "Ja, wenn Preis, Kontaktweg, Unterlagen und Objektzustand transparent sind. Private Angebote sollten genauso sorgfaeltig wie Maklerangebote geprueft werden.",
      },
    ],
    relatedIntentSlugs: ["haus-kaufen-oberoesterreich", "immobilien-privat-oberoesterreich"],
    relatedRegionSlugs: ["linz", "linz-land", "wels", "wels-land", "voecklabruck", "gmunden"],
  },
  {
    slug: "wohnung-kaufen-oberoesterreich",
    title: "Wohnung kaufen in Oberösterreich: Ratgeber",
    h1: "Wohnung kaufen in Oberösterreich",
    description:
      "Ratgeber fuer Eigentumswohnungen in Oberösterreich: Stadtlagen, Betriebskosten, Ruecklage, Sanierung und regionale Suche einordnen.",
    canonicalPath: "/ratgeber/wohnung-kaufen-oberoesterreich/",
    dateModified: "2026-05-27",
    readingTime: "5 Minuten",
    keywords: [
      "Wohnung kaufen Oberösterreich",
      "Eigentumswohnung Linz",
      "Wohnung kaufen Wels",
      "Wohnung kaufen Steyr",
    ],
    intro:
      "Beim Wohnungskauf zaehlen neben Lage und Kaufpreis besonders Betriebskosten, Ruecklage, Zustand des Hauses und die Nutzbarkeit im Alltag.",
    sections: [
      {
        heading: "Lage nach Lebensmodell bewerten",
        body: [
          "In Linz, Wels und Steyr sind kurze Wege, Oeffis, Nahversorgung und Arbeitsplatznaehe oft wichtiger als reine Quadratmeter.",
          "In Bezirksstaedten und Pendlergemeinden koennen groessere Wohnungen oder ruhigere Lagen attraktiver sein, wenn Verkehrsanbindung und Infrastruktur passen.",
        ],
        checklist: [
          "Betriebskosten und Ruecklage ansehen",
          "Sanierungen im Haus und Protokolle der Eigentuemergemeinschaft pruefen",
          "Laerm, Parken, Keller, Lift und Freiflaechen realistisch bewerten",
        ],
      },
      {
        heading: "Gebaeudezustand nicht unterschaetzen",
        body: [
          "Eine schoene Wohnung kann teuer werden, wenn Dach, Fassade, Heizung oder Allgemeinflaechen bald erneuert werden muessen.",
          "Vor dem Kauf lohnt ein Blick auf Energieausweis, Ruecklagenstand und geplante Beschluesse der Eigentuemergemeinschaft.",
        ],
      },
      {
        heading: "Stadt und Umland gemeinsam vergleichen",
        body: [
          "Viele Suchende vergleichen Linz mit Linz-Land, Wels mit Wels-Land oder Steyr mit umliegenden Gemeinden.",
          "Regionale Suchseiten helfen, Angebote aus Stadt und Umgebung schnell gegeneinander zu stellen.",
        ],
      },
    ],
    faqs: [
      {
        question: "Welche Kosten sind beim Wohnungskauf neben dem Kaufpreis wichtig?",
        answer:
          "Zu beachten sind Betriebskosten, Ruecklage, Finanzierungskosten, Erwerbsnebenkosten, moegliche Sanierungsumlagen und laufende Energiekosten.",
      },
      {
        question: "Ist Linz die wichtigste Region fuer Eigentumswohnungen in Oberösterreich?",
        answer:
          "Linz ist ein grosser Wohnungsschwerpunkt, aber auch Wels, Steyr, Voecklabruck, Gmunden und gut angebundene Umlandgemeinden sind relevant.",
      },
      {
        question: "Wie finde ich leistbare Wohnungen in Oberösterreich?",
        answer:
          "Sinnvoll ist ein Vergleich nach Region, Wohnflaeche, Zustand, Betriebskosten und Pendelstrecke statt nur nach dem niedrigsten Kaufpreis.",
      },
    ],
    relatedIntentSlugs: ["wohnung-kaufen-oberoesterreich", "immobilien-makler-oberoesterreich"],
    relatedRegionSlugs: ["linz", "wels", "steyr", "voecklabruck", "gmunden", "perg"],
  },
  {
    slug: "grundstueck-kaufen-oberoesterreich",
    title: "Grundstück kaufen in Oberösterreich: Ratgeber",
    h1: "Grundstueck kaufen in Oberösterreich",
    description:
      "Ratgeber fuer Grundstuecke und Baugrund in Oberösterreich: Widmung, Erschliessung, Zufahrt, Hanglage und Gemeinde richtig pruefen.",
    canonicalPath: "/ratgeber/grundstueck-kaufen-oberoesterreich/",
    dateModified: "2026-05-27",
    readingTime: "6 Minuten",
    keywords: [
      "Grundstueck kaufen Oberösterreich",
      "Baugrund OÖ",
      "Grundstueck Linz-Land",
      "Baugrund Salzkammergut",
    ],
    intro:
      "Bei Grundstuecken entscheidet die rechtliche und technische Nutzbarkeit. Flaeche allein sagt wenig aus, wenn Widmung, Erschliessung oder Zufahrt unklar sind.",
    sections: [
      {
        heading: "Widmung und Bebauung pruefen",
        body: [
          "Vor einer Kaufentscheidung sollte klar sein, ob das Grundstueck als Bauland gewidmet ist und welche Bebauungsregeln in der Gemeinde gelten.",
          "Bebauungsplan, Baufluchtlinien, Hanglage, Abstaende und moegliche Aufschliessungskosten koennen die tatsaechliche Nutzbarkeit stark beeinflussen.",
        ],
        checklist: [
          "Flaechenwidmung und Bebauungsplan anfragen",
          "Zufahrt, Kanal, Wasser, Strom und Internet klaeren",
          "Hanglage, Boden, Altlasten und Nachbarbebauung besichtigen",
        ],
      },
      {
        heading: "Gemeindeumfeld mitdenken",
        body: [
          "Ein Baugrund ist auch eine Standortentscheidung. Kindergarten, Schule, Nahversorgung, Verkehr und Gemeindeentwicklung sollten zur Lebensplanung passen.",
          "Im Zentralraum sind Grundstuecke oft knapper, waehrend laendlichere Bezirke mehr Flaeche bieten koennen, aber andere Pendelwege haben.",
        ],
      },
      {
        heading: "Preis pro nutzbarer Flaeche vergleichen",
        body: [
          "Der Quadratmeterpreis ist nur aussagekraeftig, wenn die tatsaechlich bebaubare und nutzbare Flaeche beruecksichtigt wird.",
          "Erschliessung, Abbruch, Gelaendeanpassung und Auflagen koennen ein vermeintlich guenstiges Grundstueck deutlich verteuern.",
        ],
      },
    ],
    faqs: [
      {
        question: "Was ist beim Baugrund in Oberösterreich besonders wichtig?",
        answer:
          "Entscheidend sind Widmung, Bebauungsplan, Erschliessung, Zufahrt, Bodenbeschaffenheit, Hanglage, Gemeindeabgaben und die Lage im Alltag.",
      },
      {
        question: "Wo werden Grundstuecke in Oberösterreich haeufig gesucht?",
        answer:
          "Haefige Suchregionen sind Linz-Land, Wels-Land, Perg, Gmunden, Voecklabruck, Grieskirchen und Gemeinden mit guter Pendelanbindung.",
      },
      {
        question: "Reicht ein niedriger Quadratmeterpreis als Kaufargument?",
        answer:
          "Nein. Entscheidend ist, welche Flaeche wirklich bebaubar ist und welche Kosten fuer Erschliessung, Gelaende, Auflagen oder Abbruch entstehen.",
      },
    ],
    relatedIntentSlugs: ["grundstueck-kaufen-oberoesterreich"],
    relatedRegionSlugs: ["linz-land", "wels-land", "perg", "gmunden", "voecklabruck", "grieskirchen"],
  },
  {
    slug: "zwangsversteigerung-oberoesterreich",
    title: "Zwangsversteigerungen in Oberösterreich verstehen",
    h1: "Zwangsversteigerungen in Oberösterreich verstehen",
    description:
      "Ratgeber zu gerichtlichen Immobilienversteigerungen in Oberösterreich: Termin, Schätzwert, Mindestgebot, Edikt und Risiken einordnen.",
    canonicalPath: "/ratgeber/zwangsversteigerung-oberoesterreich/",
    dateModified: "2026-05-27",
    readingTime: "7 Minuten",
    keywords: [
      "Zwangsversteigerung Oberösterreich",
      "Immobilien Versteigerung OÖ",
      "Edikt Oberösterreich",
      "Haus Versteigerung Oberösterreich",
    ],
    intro:
      "Zwangsversteigerungen koennen interessante Immobilien sichtbar machen, erfordern aber eine besonders sorgfaeltige Pruefung von Unterlagen, Fristen und Risiken.",
    sections: [
      {
        heading: "Edikt und Termine genau lesen",
        body: [
          "Das Edikt enthaelt zentrale Informationen wie Gericht, Aktenzeichen, Termin, Schaetzwert, Mindestgebot und Objektbeschreibung.",
          "Fristen, Besichtigungsmoeglichkeiten und Zahlungsbedingungen koennen je Verfahren unterschiedlich sein.",
        ],
        checklist: [
          "Gericht, Aktenzeichen und Termin notieren",
          "Schaetzwert, Mindestgebot und Objektbeschreibung vergleichen",
          "Besichtigung, Rechte, Lasten und Unterlagen vorab klaeren",
        ],
      },
      {
        heading: "Schaetzwert ist kein Marktpreis",
        body: [
          "Der Schaetzwert ist eine wichtige Orientierung, ersetzt aber keine eigene Pruefung von Lage, Zustand, Renovierungsbedarf und Finanzierung.",
          "Bei Versteigerungen koennen rechtliche und praktische Fragen wichtiger sein als bei klassischen Inseraten.",
        ],
      },
      {
        heading: "Regionale Suche hilft beim Ueberblick",
        body: [
          "Gerichtliche Angebote sind ueber viele Orte und Bezirke verteilt. Regionale Landingpages machen Zwangsversteigerungen in Linz, Wels, Steyr und den Bezirken besser auffindbar.",
          "Heimatplatz verlinkt Detailseiten mit Termin-, Wert- und Quellenangaben, soweit sie in den Daten verfuegbar sind.",
        ],
      },
    ],
    faqs: [
      {
        question: "Was bedeutet Mindestgebot bei einer Zwangsversteigerung?",
        answer:
          "Das Mindestgebot ist der Betrag, ab dem Gebote im Verfahren beruecksichtigt werden koennen. Details ergeben sich aus den jeweiligen Verfahrensunterlagen.",
      },
      {
        question: "Sind Zwangsversteigerungen guenstiger als normale Immobilienangebote?",
        answer:
          "Nicht automatisch. Entscheidend sind Zustand, Rechte und Lasten, Nachfrage im Termin, Finanzierung, Nebenkosten und die tatsaechliche Nutzbarkeit.",
      },
      {
        question: "Wo finde ich Zwangsversteigerungen in Oberösterreich?",
        answer:
          "Heimatplatz fuehrt gerichtliche Immobilienseiten nach Region und verlinkt verfuegbare Edikt- oder Dokumentquellen fuer weitere Pruefung.",
      },
    ],
    relatedIntentSlugs: ["haus-kaufen-oberoesterreich", "grundstueck-kaufen-oberoesterreich"],
    relatedRegionSlugs: ["linz", "wels", "steyr", "perg", "braunau-am-inn", "gmunden"],
  },
  {
    slug: "immobilie-privat-verkaufen-oberoesterreich",
    title: "Immobilie privat verkaufen in Oberösterreich",
    h1: "Immobilie privat verkaufen in Oberösterreich",
    description:
      "Ratgeber fuer private Verkaeufer in Oberösterreich: Inserat, Preis, Fotos, Unterlagen und Kontakt strukturiert vorbereiten.",
    canonicalPath: "/ratgeber/immobilie-privat-verkaufen-oberoesterreich/",
    dateModified: "2026-05-27",
    readingTime: "6 Minuten",
    keywords: [
      "Immobilie privat verkaufen Oberösterreich",
      "Haus privat verkaufen OÖ",
      "Wohnung privat verkaufen Linz",
      "Immobilieninserat Oberösterreich",
    ],
    intro:
      "Private Verkaeufer brauchen ein klares Inserat, realistische Preisargumente und vollstaendige Informationen, damit Interessenten schnell Vertrauen fassen.",
    sections: [
      {
        heading: "Unterlagen vor dem Inserat sammeln",
        body: [
          "Ein gutes Inserat beginnt vor dem Fotografieren. Flaechen, Baujahr, Energieausweis, Grundbuch, Plaene, Betriebskosten und Sanierungsangaben sollten griffbereit sein.",
          "Je klarer die Daten sind, desto leichter koennen Kaeufer ein Objekt mit anderen Angeboten in Oberösterreich vergleichen.",
        ],
        checklist: [
          "Titel, Beschreibung und Lagevorteile konkret formulieren",
          "Preis, Flaechen, Zimmer und Zustand nachvollziehbar angeben",
          "Aussagekraeftige Fotos und Kontaktwege bereitstellen",
        ],
      },
      {
        heading: "Preis mit regionalem Vergleich begruenden",
        body: [
          "Ein Verkaufspreis wirkt glaubwuerdiger, wenn Lage, Zustand, Flaeche, Grundstueck und regionale Vergleichsangebote zusammenpassen.",
          "Besonders in Linz, Wels, Steyr und Umlandgemeinden vergleichen Kaeufer private Angebote sehr direkt mit Makler- und Portalangeboten.",
        ],
      },
      {
        heading: "Kontaktprozess einfach halten",
        body: [
          "Interessenten reagieren besser, wenn E-Mail, Telefon, Besichtigungsfenster und die wichtigsten Antworten klar sind.",
          "Heimatplatz bildet private Angebote gemeinsam mit Suchfiltern und regionalen Seiten ab, damit sie nicht nur in der App, sondern auch ueber Google auffindbar werden.",
        ],
      },
    ],
    faqs: [
      {
        question: "Was braucht ein gutes privates Immobilieninserat?",
        answer:
          "Wichtig sind aussagekraeftige Fotos, vollstaendige Flaechen- und Preisdaten, eine ehrliche Beschreibung, klare Kontaktangaben und relevante Unterlagen.",
      },
      {
        question: "Soll ich als privater Verkaeufer einen Fixpreis nennen?",
        answer:
          "Ein klarer Preis hilft bei der Vergleichbarkeit. Er sollte aber durch Lage, Zustand, Flaechen und regionale Marktdaten plausibel wirken.",
      },
      {
        question: "Wie wird ein privates Angebot in Oberösterreich besser gefunden?",
        answer:
          "Hilfreich sind konkrete Orts- und Bezirksangaben, saubere Objektdaten, gute Detailseiten, regionale interne Links und indexierbare SEO-Seiten.",
      },
    ],
    relatedIntentSlugs: ["immobilien-privat-oberoesterreich", "haus-kaufen-oberoesterreich"],
    relatedRegionSlugs: ["linz", "wels", "steyr", "linz-land", "wels-land", "voecklabruck"],
  },
];

export function getGuidePage(slug: string) {
  const guide = guidePages.find((item) => item.slug === slug);
  if (!guide) throw new Error(`Unknown guide page: ${slug}`);
  return guide;
}
