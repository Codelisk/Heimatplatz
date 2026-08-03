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
