/**
 * JSON.stringify fuer Inhalte, die per set:html in ein <script>-Tag eingebettet werden
 * (JSON-LD, eingebettete Datenbloecke).
 *
 * JSON.stringify escaped "</script>" NICHT - flieszt nutzergenerierter Text ein
 * (z.B. ein Inseratstitel "</script><script>..."), bricht er aus dem Script-Block
 * aus und wird als Markup interpretiert (stored XSS). "<" wird deshalb als
 * Unicode-Escape (Backslash-u003c) ausgegeben: fuer JSON.parse identisch,
 * im HTML aber inert.
 */
export function jsonForScriptTag(value: unknown): string {
  return JSON.stringify(value).replace(/</g, "\\u003c");
}
