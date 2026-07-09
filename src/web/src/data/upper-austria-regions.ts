export interface UpperAustriaRegion {
  name: string;
  slug: string;
  title: string;
  description: string;
  districts: string[];
  searchIntents: string[];
  marketNotes: string[];
}

export const upperAustriaRegions: UpperAustriaRegion[] = [
  {
    name: "Linz",
    slug: "linz",
    title: "Immobilien in Linz kaufen",
    description:
      "Häuser, Wohnungen und Grundstücke in Linz und Linz-Urfahr mit Fokus auf zentrale Lagen, Pendlerwege, Hochschulen und Familieninfrastruktur.",
    districts: ["Innenstadt", "Urfahr", "Pöstlingberg", "Kleinmünchen", "Ebelsberg"],
    searchIntents: ["Haus kaufen Linz", "Wohnung kaufen Linz", "Immobilien Linz Umgebung"],
    marketNotes: ["Starke Nachfrage nach Wohnungen und Reihenhäusern", "Viele Suchanfragen nach Urfahr, Pöstlingberg und Zentrum"],
  },
  {
    name: "Steyr",
    slug: "steyr",
    title: "Immobilien in Steyr kaufen",
    description:
      "Regionale Immobilienseiten für Steyr mit Altstadtlagen, Familienhäusern, Wohnungen und Anlageobjekten nahe Enns und Steyr-Fluss.",
    districts: ["Altstadt", "Steyrdorf", "Münichholz", "Tabor"],
    searchIntents: ["Immobilien Steyr", "Haus kaufen Steyr", "Wohnung Steyr"],
    marketNotes: ["Altstadtwohnungen und sanierte Bestandsobjekte sind wichtige Suchthemen", "Steyr ist ein eigener lokaler Immobilienmarkt neben Steyr-Land"],
  },
  {
    name: "Wels",
    slug: "wels",
    title: "Immobilien in Wels kaufen",
    description:
      "Immobiliensuche für Wels im oberösterreichischen Zentralraum mit Wohnungen, Reihenhäusern und Pendlerlagen Richtung Linz.",
    districts: ["Innenstadt", "Lichtenegg", "Pernau", "Vogelweide"],
    searchIntents: ["Haus kaufen Wels", "Wohnung Wels kaufen", "Immobilien Wels Zentrum"],
    marketNotes: ["Wels ist für Wohnungssuchen und Zentralraum-Pendler relevant", "Die Stadt sollte getrennt von Wels-Land auffindbar bleiben"],
  },
  {
    name: "Braunau am Inn",
    slug: "braunau-am-inn",
    title: "Immobilien im Bezirk Braunau am Inn",
    description:
      "Häuser, Wohnungen und Grundstücke im Innviertel: Braunau, Mattighofen, Altheim und Gemeinden nahe Bayern.",
    districts: ["Braunau am Inn", "Mattighofen", "Altheim", "Mauerkirchen"],
    searchIntents: ["Immobilien Braunau", "Haus kaufen Braunau am Inn", "Grundstück Innviertel"],
    marketNotes: ["Grenznahe Lagen zu Bayern erzeugen eigene Suchintentionen", "Familienhäuser und Baugründe sind zentrale Themen"],
  },
  {
    name: "Eferding",
    slug: "eferding",
    title: "Immobilien im Bezirk Eferding",
    description:
      "Immobilien im Bezirk Eferding zwischen Donau, Linz und Wels mit Fokus auf Einfamilienhäuser, Baugrund und ruhige Wohnlagen.",
    districts: ["Eferding", "Hartkirchen", "Aschach an der Donau", "Alkoven"],
    searchIntents: ["Immobilien Eferding", "Haus kaufen Eferding", "Grundstück Eferding"],
    marketNotes: ["Eferding ist für Suchende im Zentralraum eine Alternative zu Linz und Wels", "Donau- und Pendlerlagen sollten getrennt beschrieben werden"],
  },
  {
    name: "Freistadt",
    slug: "freistadt",
    title: "Immobilien im Bezirk Freistadt",
    description:
      "Häuser, Wohnungen, Bauernhäuser und Grundstücke im Mühlviertel: Freistadt, Pregarten, Unterweitersdorf und Grenzlagen.",
    districts: ["Freistadt", "Pregarten", "Unterweitersdorf", "Kefermarkt"],
    searchIntents: ["Immobilien Freistadt", "Haus kaufen Freistadt", "Grundstück Mühlviertel"],
    marketNotes: ["Mühlviertler Wohnlagen und Pendeln nach Linz sind zentrale Entscheidungsthemen", "Freistadt braucht eigene regionale Informationen jenseits allgemeiner OÖ-Seiten"],
  },
  {
    name: "Gmunden",
    slug: "gmunden",
    title: "Immobilien im Bezirk Gmunden",
    description:
      "Häuser, Wohnungen und Baugrund im Bezirk Gmunden, im Salzkammergut und rund um Traunsee, Bad Ischl und Altmünster.",
    districts: ["Gmunden", "Altmünster", "Laakirchen", "Bad Ischl", "Ebensee"],
    searchIntents: ["Immobilien Gmunden", "Grundstück Traunsee", "Haus Salzkammergut"],
    marketNotes: ["See- und Salzkammergutlagen haben stark eigene Suchbegriffe", "Wohn- und Ferienimmobilien sollten sauber unterschieden werden"],
  },
  {
    name: "Grieskirchen",
    slug: "grieskirchen",
    title: "Immobilien im Bezirk Grieskirchen",
    description:
      "Immobilien im Hausruckviertel: Grieskirchen, Gallspach, Peuerbach und ländliche Gemeinden mit Familienhäusern und Baugrund.",
    districts: ["Grieskirchen", "Gallspach", "Peuerbach", "Waizenkirchen"],
    searchIntents: ["Immobilien Grieskirchen", "Haus kaufen Grieskirchen", "Baugrund Hausruckviertel"],
    marketNotes: ["Grieskirchen ist relevant für Suchende zwischen Wels, Eferding und Ried", "Baugrund und Einfamilienhäuser dominieren viele Suchmuster"],
  },
  {
    name: "Kirchdorf an der Krems",
    slug: "kirchdorf-an-der-krems",
    title: "Immobilien im Bezirk Kirchdorf an der Krems",
    description:
      "Immobilien im Bezirk Kirchdorf mit Krems-, Pyhrn- und Almtal-Lagen: Häuser, Wohnungen und Grundstücke in Oberösterreichs Süden.",
    districts: ["Kirchdorf an der Krems", "Micheldorf", "Kremsmünster", "Windischgarsten"],
    searchIntents: ["Immobilien Kirchdorf", "Haus kaufen Kirchdorf an der Krems", "Wohnung Kremsmünster"],
    marketNotes: ["Der Bezirk verbindet Pendlerlagen und alpine Wohnlagen", "Regionale Begriffe wie Pyhrn-Priel und Almtal helfen bei Long-Tail-Suchen"],
  },
  {
    name: "Linz-Land",
    slug: "linz-land",
    title: "Immobilien im Bezirk Linz-Land",
    description:
      "Suchseiten für Linz-Land, Leonding, Traun, Ansfelden und Enns als zentrale Pendler- und Familienlagen in Oberösterreich.",
    districts: ["Leonding", "Traun", "Ansfelden", "Enns", "Pasching"],
    searchIntents: ["Haus kaufen Linz-Land", "Immobilien Leonding", "Wohnung Traun"],
    marketNotes: ["Linz-Land ist für Pendler und Familien einer der wichtigsten Bezirke", "Leonding, Traun und Ansfelden sollten intern stark verlinkt werden"],
  },
  {
    name: "Perg",
    slug: "perg",
    title: "Immobilien im Bezirk Perg",
    description:
      "Immobilien im unteren Mühlviertel: Perg, Mauthausen, Schwertberg und Donau-nahe Wohnlagen mit guter Linz-Anbindung.",
    districts: ["Perg", "Mauthausen", "Schwertberg", "Grein"],
    searchIntents: ["Immobilien Perg", "Haus kaufen Perg", "Wohnung Mauthausen"],
    marketNotes: ["Perg ist für Linz-Pendler und Donau-Lagen relevant", "Suchbegriffe verbinden oft Mühlviertel, Donau und Familienhaus"],
  },
  {
    name: "Ried im Innkreis",
    slug: "ried-im-innkreis",
    title: "Immobilien im Bezirk Ried im Innkreis",
    description:
      "Häuser, Wohnungen und Baugrund im Bezirk Ried im Innkreis mit Fokus auf Ried, Geinberg, Obernberg und Innviertler Gemeinden.",
    districts: ["Ried im Innkreis", "Geinberg", "Obernberg am Inn", "Eberschwang"],
    searchIntents: ["Immobilien Ried im Innkreis", "Haus kaufen Ried", "Grundstück Innviertel"],
    marketNotes: ["Ried ist ein eigener Immobilienmarkt im Innviertel", "Thermen- und Grenzregionen erzeugen zusätzliche lokale Suchintentionen"],
  },
  {
    name: "Rohrbach",
    slug: "rohrbach",
    title: "Immobilien im Bezirk Rohrbach",
    description:
      "Immobilien im oberen Mühlviertel: Rohrbach-Berg, Aigen-Schlägl, Haslach und Gemeinden nahe Böhmerwald.",
    districts: ["Rohrbach-Berg", "Aigen-Schlägl", "Haslach an der Mühl", "Neufelden"],
    searchIntents: ["Immobilien Rohrbach", "Haus kaufen Rohrbach-Berg", "Grundstück Böhmerwald"],
    marketNotes: ["Rohrbach braucht eigene Mühlviertel- und Böhmerwald-Begriffe", "Häuser mit Grund sind für viele regionale Suchen zentral"],
  },
  {
    name: "Schärding",
    slug: "schaerding",
    title: "Immobilien im Bezirk Schärding",
    description:
      "Immobilien im Bezirk Schärding mit Innlagen, Grenznähe zu Bayern, Familienhäusern, Wohnungen und Baugrund.",
    districts: ["Schärding", "Andorf", "Raab", "Taufkirchen an der Pram"],
    searchIntents: ["Immobilien Schärding", "Haus kaufen Schärding", "Wohnung Innviertel"],
    marketNotes: ["Grenznähe und Innlage sind wichtige Long-Tail-Signale", "Schärding sollte für Haus- und Wohnungssuchen getrennt auffindbar sein"],
  },
  {
    name: "Steyr-Land",
    slug: "steyr-land",
    title: "Immobilien im Bezirk Steyr-Land",
    description:
      "Immobilien in Steyr-Land mit Sierning, Garsten, Bad Hall und ländlichen Familienlagen rund um Steyr.",
    districts: ["Sierning", "Garsten", "Bad Hall", "Wolfern"],
    searchIntents: ["Immobilien Steyr-Land", "Haus kaufen Sierning", "Grundstück Bad Hall"],
    marketNotes: ["Steyr-Land ergänzt die Stadt Steyr mit stärkerem Familienhaus-Fokus", "Bad Hall und Sierning sind relevante regionale Suchorte"],
  },
  {
    name: "Urfahr-Umgebung",
    slug: "urfahr-umgebung",
    title: "Immobilien im Bezirk Urfahr-Umgebung",
    description:
      "Immobilien nördlich von Linz: Urfahr-Umgebung mit Gallneukirchen, Ottensheim, Gramastetten und Mühlviertler Pendlerlagen.",
    districts: ["Gallneukirchen", "Ottensheim", "Gramastetten", "Engerwitzdorf"],
    searchIntents: ["Immobilien Urfahr-Umgebung", "Haus kaufen Gallneukirchen", "Grundstück Ottensheim"],
    marketNotes: ["Viele Suchende vergleichen Urfahr-Umgebung direkt mit Linz", "Pendlerlagen und Mühlviertel-Nähe sind starke Standortkriterien"],
  },
  {
    name: "Vöcklabruck",
    slug: "voecklabruck",
    title: "Immobilien im Bezirk Vöcklabruck",
    description:
      "Immobilien in Vöcklabruck, Attersee-Region, Mondsee, Schwanenstadt und Pendlerlagen Richtung Linz und Salzburg.",
    districts: ["Vöcklabruck", "Attnang-Puchheim", "Schwanenstadt", "Attersee", "Mondsee"],
    searchIntents: ["Immobilien Vöcklabruck", "Wohnung Vöcklabruck", "Haus Attersee"],
    marketNotes: ["Attersee und Mondsee bringen eigene, starke Suchbegriffe", "Vöcklabruck verbindet leistbare Pendlerlagen und Seenregion"],
  },
  {
    name: "Wels-Land",
    slug: "wels-land",
    title: "Immobilien im Bezirk Wels-Land",
    description:
      "Immobilien im Bezirk Wels-Land rund um Marchtrenk, Thalheim, Gunskirchen und ländliche Zentralraum-Gemeinden.",
    districts: ["Marchtrenk", "Thalheim bei Wels", "Gunskirchen", "Bad Wimsbach-Neydharting"],
    searchIntents: ["Immobilien Wels-Land", "Haus kaufen Marchtrenk", "Grundstück Thalheim bei Wels"],
    marketNotes: ["Wels-Land ist für Zentralraum-Suchen neben Wels Stadt wichtig", "Marchtrenk und Thalheim sollten in internen Links sichtbar bleiben"],
  },
];
