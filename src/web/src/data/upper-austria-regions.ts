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
      "Haeuser, Wohnungen und Grundstuecke in Linz und Linz-Urfahr mit Fokus auf zentrale Lagen, Pendlerwege, Hochschulen und Familieninfrastruktur.",
    districts: ["Innenstadt", "Urfahr", "Pöstlingberg", "Kleinmuenchen", "Ebelsberg"],
    searchIntents: ["Haus kaufen Linz", "Wohnung kaufen Linz", "Immobilien Linz Umgebung"],
    marketNotes: ["Starke Nachfrage nach Wohnungen und Reihenhaeusern", "Viele Suchanfragen nach Urfahr, Pöstlingberg und Zentrum"],
  },
  {
    name: "Steyr",
    slug: "steyr",
    title: "Immobilien in Steyr kaufen",
    description:
      "Regionale Immobilienseiten fuer Steyr mit Altstadtlagen, Familienhaeusern, Wohnungen und Anlageobjekten nahe Enns und Steyr-Fluss.",
    districts: ["Altstadt", "Steyrdorf", "Muenichholz", "Tabor"],
    searchIntents: ["Immobilien Steyr", "Haus kaufen Steyr", "Wohnung Steyr"],
    marketNotes: ["Altstadtwohnungen und sanierte Bestandsobjekte sind wichtige Suchthemen", "Steyr ist ein eigener lokaler Immobilienmarkt neben Steyr-Land"],
  },
  {
    name: "Wels",
    slug: "wels",
    title: "Immobilien in Wels kaufen",
    description:
      "Immobiliensuche fuer Wels im oberoesterreichischen Zentralraum mit Wohnungen, Reihenhaeusern und Pendlerlagen Richtung Linz.",
    districts: ["Innenstadt", "Lichtenegg", "Pernau", "Vogelweide"],
    searchIntents: ["Haus kaufen Wels", "Wohnung Wels kaufen", "Immobilien Wels Zentrum"],
    marketNotes: ["Wels ist fuer Wohnungssuchen und Zentralraum-Pendler relevant", "Die Stadt sollte getrennt von Wels-Land auffindbar bleiben"],
  },
  {
    name: "Braunau am Inn",
    slug: "braunau-am-inn",
    title: "Immobilien im Bezirk Braunau am Inn",
    description:
      "Haeuser, Wohnungen und Grundstuecke im Innviertel: Braunau, Mattighofen, Altheim und Gemeinden nahe Bayern.",
    districts: ["Braunau am Inn", "Mattighofen", "Altheim", "Mauerkirchen"],
    searchIntents: ["Immobilien Braunau", "Haus kaufen Braunau am Inn", "Grundstueck Innviertel"],
    marketNotes: ["Grenznahe Lagen zu Bayern erzeugen eigene Suchintentionen", "Familienhaeuser und Baugruende sind zentrale Themen"],
  },
  {
    name: "Eferding",
    slug: "eferding",
    title: "Immobilien im Bezirk Eferding",
    description:
      "Immobilien im Bezirk Eferding zwischen Donau, Linz und Wels mit Fokus auf Einfamilienhaeuser, Baugrund und ruhige Wohnlagen.",
    districts: ["Eferding", "Hartkirchen", "Aschach an der Donau", "Alkoven"],
    searchIntents: ["Immobilien Eferding", "Haus kaufen Eferding", "Grundstueck Eferding"],
    marketNotes: ["Eferding ist fuer Suchende im Zentralraum eine Alternative zu Linz und Wels", "Donau- und Pendlerlagen sollten getrennt beschrieben werden"],
  },
  {
    name: "Freistadt",
    slug: "freistadt",
    title: "Immobilien im Bezirk Freistadt",
    description:
      "Haeuser, Wohnungen, Bauernhaeuser und Grundstuecke im Muehlviertel: Freistadt, Pregarten, Unterweitersdorf und Grenzlagen.",
    districts: ["Freistadt", "Pregarten", "Unterweitersdorf", "Kefermarkt"],
    searchIntents: ["Immobilien Freistadt", "Haus kaufen Freistadt", "Grundstueck Muehlviertel"],
    marketNotes: ["Muehlviertler Wohnlagen und Pendeln nach Linz sind zentrale Entscheidungsthemen", "Freistadt braucht eigene regionale Informationen jenseits allgemeiner OÖ-Seiten"],
  },
  {
    name: "Gmunden",
    slug: "gmunden",
    title: "Immobilien im Bezirk Gmunden",
    description:
      "Haeuser, Wohnungen und Baugrund im Bezirk Gmunden, im Salzkammergut und rund um Traunsee, Bad Ischl und Altmuenster.",
    districts: ["Gmunden", "Altmuenster", "Laakirchen", "Bad Ischl", "Ebensee"],
    searchIntents: ["Immobilien Gmunden", "Grundstueck Traunsee", "Haus Salzkammergut"],
    marketNotes: ["See- und Salzkammergutlagen haben stark eigene Suchbegriffe", "Wohn- und Ferienimmobilien sollten sauber unterschieden werden"],
  },
  {
    name: "Grieskirchen",
    slug: "grieskirchen",
    title: "Immobilien im Bezirk Grieskirchen",
    description:
      "Immobilien im Hausruckviertel: Grieskirchen, Gallspach, Peuerbach und ländliche Gemeinden mit Familienhaeusern und Baugrund.",
    districts: ["Grieskirchen", "Gallspach", "Peuerbach", "Waizenkirchen"],
    searchIntents: ["Immobilien Grieskirchen", "Haus kaufen Grieskirchen", "Baugrund Hausruckviertel"],
    marketNotes: ["Grieskirchen ist relevant fuer Suchende zwischen Wels, Eferding und Ried", "Baugrund und Einfamilienhaeuser dominieren viele Suchmuster"],
  },
  {
    name: "Kirchdorf an der Krems",
    slug: "kirchdorf-an-der-krems",
    title: "Immobilien im Bezirk Kirchdorf an der Krems",
    description:
      "Immobilien im Bezirk Kirchdorf mit Krems-, Pyhrn- und Almtal-Lagen: Haeuser, Wohnungen und Grundstuecke in Oberösterreichs Sueden.",
    districts: ["Kirchdorf an der Krems", "Micheldorf", "Kremsmuenster", "Windischgarsten"],
    searchIntents: ["Immobilien Kirchdorf", "Haus kaufen Kirchdorf an der Krems", "Wohnung Kremsmuenster"],
    marketNotes: ["Der Bezirk verbindet Pendlerlagen und alpine Wohnlagen", "Regionale Begriffe wie Pyhrn-Priel und Almtal helfen bei Long-Tail-Suchen"],
  },
  {
    name: "Linz-Land",
    slug: "linz-land",
    title: "Immobilien im Bezirk Linz-Land",
    description:
      "Suchseiten fuer Linz-Land, Leonding, Traun, Ansfelden und Enns als zentrale Pendler- und Familienlagen in Oberösterreich.",
    districts: ["Leonding", "Traun", "Ansfelden", "Enns", "Pasching"],
    searchIntents: ["Haus kaufen Linz-Land", "Immobilien Leonding", "Wohnung Traun"],
    marketNotes: ["Linz-Land ist fuer Pendler und Familien einer der wichtigsten Bezirke", "Leonding, Traun und Ansfelden sollten intern stark verlinkt werden"],
  },
  {
    name: "Perg",
    slug: "perg",
    title: "Immobilien im Bezirk Perg",
    description:
      "Immobilien im unteren Muehlviertel: Perg, Mauthausen, Schwertberg und Donau-nahe Wohnlagen mit guter Linz-Anbindung.",
    districts: ["Perg", "Mauthausen", "Schwertberg", "Grein"],
    searchIntents: ["Immobilien Perg", "Haus kaufen Perg", "Wohnung Mauthausen"],
    marketNotes: ["Perg ist fuer Linz-Pendler und Donau-Lagen relevant", "Suchbegriffe verbinden oft Muehlviertel, Donau und Familienhaus"],
  },
  {
    name: "Ried im Innkreis",
    slug: "ried-im-innkreis",
    title: "Immobilien im Bezirk Ried im Innkreis",
    description:
      "Haeuser, Wohnungen und Baugrund im Bezirk Ried im Innkreis mit Fokus auf Ried, Geinberg, Obernberg und Innviertler Gemeinden.",
    districts: ["Ried im Innkreis", "Geinberg", "Obernberg am Inn", "Eberschwang"],
    searchIntents: ["Immobilien Ried im Innkreis", "Haus kaufen Ried", "Grundstueck Innviertel"],
    marketNotes: ["Ried ist ein eigener Immobilienmarkt im Innviertel", "Thermen- und Grenzregionen erzeugen zusaetzliche lokale Suchintentionen"],
  },
  {
    name: "Rohrbach",
    slug: "rohrbach",
    title: "Immobilien im Bezirk Rohrbach",
    description:
      "Immobilien im oberen Muehlviertel: Rohrbach-Berg, Aigen-Schlaegl, Haslach und Gemeinden nahe Boehmerwald.",
    districts: ["Rohrbach-Berg", "Aigen-Schlaegl", "Haslach an der Muehl", "Neufelden"],
    searchIntents: ["Immobilien Rohrbach", "Haus kaufen Rohrbach-Berg", "Grundstueck Boehmerwald"],
    marketNotes: ["Rohrbach braucht eigene Muehlviertel- und Boehmerwald-Begriffe", "Haeuser mit Grund sind fuer viele regionale Suchen zentral"],
  },
  {
    name: "Schärding",
    slug: "schaerding",
    title: "Immobilien im Bezirk Schärding",
    description:
      "Immobilien im Bezirk Schärding mit Innlagen, Grenznaehe zu Bayern, Familienhaeusern, Wohnungen und Baugrund.",
    districts: ["Schärding", "Andorf", "Raab", "Taufkirchen an der Pram"],
    searchIntents: ["Immobilien Schärding", "Haus kaufen Schärding", "Wohnung Innviertel"],
    marketNotes: ["Grenznaehe und Innlage sind wichtige Long-Tail-Signale", "Schärding sollte fuer Haus- und Wohnungssuchen getrennt auffindbar sein"],
  },
  {
    name: "Steyr-Land",
    slug: "steyr-land",
    title: "Immobilien im Bezirk Steyr-Land",
    description:
      "Immobilien in Steyr-Land mit Sierning, Garsten, Bad Hall und ländlichen Familienlagen rund um Steyr.",
    districts: ["Sierning", "Garsten", "Bad Hall", "Wolfern"],
    searchIntents: ["Immobilien Steyr-Land", "Haus kaufen Sierning", "Grundstueck Bad Hall"],
    marketNotes: ["Steyr-Land ergaenzt die Stadt Steyr mit staerkerem Familienhaus-Fokus", "Bad Hall und Sierning sind relevante regionale Suchorte"],
  },
  {
    name: "Urfahr-Umgebung",
    slug: "urfahr-umgebung",
    title: "Immobilien im Bezirk Urfahr-Umgebung",
    description:
      "Immobilien noerdlich von Linz: Urfahr-Umgebung mit Gallneukirchen, Ottensheim, Gramastetten und Muehlviertler Pendlerlagen.",
    districts: ["Gallneukirchen", "Ottensheim", "Gramastetten", "Engerwitzdorf"],
    searchIntents: ["Immobilien Urfahr-Umgebung", "Haus kaufen Gallneukirchen", "Grundstueck Ottensheim"],
    marketNotes: ["Viele Suchende vergleichen Urfahr-Umgebung direkt mit Linz", "Pendlerlagen und Muehlviertel-Nahe sind starke Standortkriterien"],
  },
  {
    name: "Voecklabruck",
    slug: "voecklabruck",
    title: "Immobilien im Bezirk Voecklabruck",
    description:
      "Immobilien in Voecklabruck, Attersee-Region, Mondsee, Schwanenstadt und Pendlerlagen Richtung Linz und Salzburg.",
    districts: ["Voecklabruck", "Attnang-Puchheim", "Schwanenstadt", "Attersee", "Mondsee"],
    searchIntents: ["Immobilien Voecklabruck", "Wohnung Voecklabruck", "Haus Attersee"],
    marketNotes: ["Attersee und Mondsee bringen eigene, starke Suchbegriffe", "Voecklabruck verbindet leistbare Pendlerlagen und Seenregion"],
  },
  {
    name: "Wels-Land",
    slug: "wels-land",
    title: "Immobilien im Bezirk Wels-Land",
    description:
      "Immobilien im Bezirk Wels-Land rund um Marchtrenk, Thalheim, Gunskirchen und ländliche Zentralraum-Gemeinden.",
    districts: ["Marchtrenk", "Thalheim bei Wels", "Gunskirchen", "Bad Wimsbach-Neydharting"],
    searchIntents: ["Immobilien Wels-Land", "Haus kaufen Marchtrenk", "Grundstueck Thalheim bei Wels"],
    marketNotes: ["Wels-Land ist fuer Zentralraum-Suchen neben Wels Stadt wichtig", "Marchtrenk und Thalheim sollten in internen Links sichtbar bleiben"],
  },
];
