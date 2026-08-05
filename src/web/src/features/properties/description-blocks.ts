/**
 * Segmentiert Plaintext-Beschreibungen (z.B. aus dem OpenImmo-Import) in
 * Absaetze und Aufzaehlungsbloecke. Die Feeds liefern kein HTML - Listen
 * kommen als ASCII-Zeilen ("* Doppelgarage", "• 3 Zimmer") und wuerden als
 * Fliesstext mit sichtbaren Markern gerendert.
 */
export type DescriptionBlock =
  | { type: "paragraph"; text: string }
  | { type: "list"; items: string[] };

const BULLET_LINE = /^[*•]\s+(.+)$/;

export function splitDescriptionBlocks(text: string): DescriptionBlock[] {
  const blocks: DescriptionBlock[] = [];
  let paragraphLines: string[] = [];
  let listItems: string[] = [];

  const flushParagraph = () => {
    if (paragraphLines.length) {
      blocks.push({ type: "paragraph", text: paragraphLines.join("\n") });
      paragraphLines = [];
    }
  };
  const flushList = () => {
    if (listItems.length) {
      blocks.push({ type: "list", items: listItems });
      listItems = [];
    }
  };

  for (const rawLine of text.split(/\r?\n/)) {
    const line = rawLine.trim();
    if (!line) {
      // Leerzeile trennt Bloecke; Abstand kommt aus dem Block-Layout
      flushParagraph();
      flushList();
      continue;
    }

    const bullet = line.match(BULLET_LINE);
    if (bullet) {
      flushParagraph();
      listItems.push(bullet[1].trim());
      continue;
    }

    flushList();
    paragraphLines.push(line);
  }

  flushParagraph();
  flushList();
  return blocks;
}

/**
 * Leporello-Falz: teilt eine lange Beschreibung in sichtbaren Vorspann (lead)
 * und zusammengefalteten Rest (rest). Kurze Texte bleiben ungefaltet
 * (rest = []). Der Schnitt faellt immer auf eine Block-, Zeilen- oder
 * Satzgrenze - nie mitten in einen Satz, damit der Vorspann wie ein
 * bewusst gesetzter Anriss liest und kein Fade-Out noetig ist.
 */
export interface DescriptionFoldPlan {
  lead: DescriptionBlock[];
  rest: DescriptionBlock[];
}

const FOLD_MIN_TOTAL = 900; // darunter lohnt sich kein Falz
const LEAD_BUDGET = 500; // Ziellaenge des sichtbaren Vorspanns in Zeichen
const MIN_REST = 300; // kleinere Reste einfach mit anzeigen
const SPLIT_SLACK = 350; // max. Abstand hinter dem Budget fuer eine Trennstelle

function blockLength(block: DescriptionBlock): number {
  return block.type === "paragraph"
    ? block.text.length
    : block.items.reduce((sum, item) => sum + item.length + 2, 0);
}

// Trennt einen langen Absatz an einer Zeilen- oder Satzgrenze nahe dem Budget
function findParagraphCut(text: string, budget: number): [string, string] | null {
  for (const marker of ["\n", ". "]) {
    const idx = text.indexOf(marker, Math.max(0, budget));
    if (idx !== -1 && idx <= budget + SPLIT_SLACK) {
      const cutAt = marker === ". " ? idx + 1 : idx; // Satzpunkt bleibt beim Vorspann
      const head = text.slice(0, cutAt).trimEnd();
      const tail = text.slice(cutAt).trimStart();
      if (head && tail) return [head, tail];
    }
  }
  return null;
}

// Durchschnittliches Lesetempo fuer Deutsch, in Zeichen pro Minute
const READING_CHARS_PER_MINUTE = 1250;

/** Geschaetzte Lesezeit der Bloecke in Minuten (mindestens 1) */
export function descriptionReadingMinutes(blocks: DescriptionBlock[]): number {
  const total = blocks.reduce((sum, block) => sum + blockLength(block), 0);
  return Math.max(1, Math.round(total / READING_CHARS_PER_MINUTE));
}

export function planDescriptionFold(blocks: DescriptionBlock[]): DescriptionFoldPlan {
  const total = blocks.reduce((sum, block) => sum + blockLength(block), 0);
  if (total < FOLD_MIN_TOTAL) return { lead: blocks, rest: [] };

  const lead: DescriptionBlock[] = [];
  const rest: DescriptionBlock[] = [];
  let seen = 0;

  for (const block of blocks) {
    if (seen >= LEAD_BUDGET) {
      rest.push(block);
      continue;
    }
    const length = blockLength(block);
    // Ein Absatz, der das Budget weit ueberschiesst, wird an einer
    // natuerlichen Grenze geteilt statt komplett sichtbar zu bleiben
    if (block.type === "paragraph" && seen + length > LEAD_BUDGET + SPLIT_SLACK) {
      const cut = findParagraphCut(block.text, LEAD_BUDGET - seen);
      if (cut) {
        lead.push({ type: "paragraph", text: cut[0] });
        rest.push({ type: "paragraph", text: cut[1] });
        seen = LEAD_BUDGET;
        continue;
      }
    }
    lead.push(block);
    seen += length;
  }

  const restTotal = rest.reduce((sum, block) => sum + blockLength(block), 0);
  if (restTotal < MIN_REST) return { lead: blocks, rest: [] };
  return { lead, rest };
}
