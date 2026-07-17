export function slugifyLocation(value: string) {
  return value
    .replace(/ä/g, "ae")
    .replace(/Ä/g, "ae")
    .replace(/ö/g, "oe")
    .replace(/Ö/g, "oe")
    .replace(/ü/g, "ue")
    .replace(/Ü/g, "ue")
    .replace(/ß/g, "ss")
    .normalize("NFD")
    .replace(/[\u0300-\u036f]/g, "")
    .toLowerCase()
    .replace(/[^a-z0-9]+/g, "-")
    .replace(/^-+|-+$/g, "");
}
