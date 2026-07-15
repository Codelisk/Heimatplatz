const DEFAULT_RESPONSIVE_WIDTHS = [640, 960, 1280] as const;

export type ResponsiveImage = {
  src: string;
  srcset?: string;
};

/**
 * Setzt die Zielbreite nur fuer den eigenen API-Bildproxy. Lokale Uploads,
 * Fallbacks und fremde URLs bleiben unveraendert.
 */
export function withProxyImageWidth(url: string, width?: number) {
  try {
    const parsed = new URL(url, "https://heimatplatz.invalid");
    if (!parsed.pathname.endsWith("/api/images/proxy")) return url;

    if (width) parsed.searchParams.set("w", String(width));
    else parsed.searchParams.delete("w");

    return parsed.origin === "https://heimatplatz.invalid"
      ? `${parsed.pathname}${parsed.search}${parsed.hash}`
      : parsed.toString();
  } catch {
    return url;
  }
}

/**
 * Erzeugt browserseitig waehlbare Proxy-Varianten. Der Proxy skaliert nie
 * hoch, deshalb bleiben kleine Justiz-Originale klein und werden nicht durch
 * kuenstlich erzeugte Pixel aufgeblasen.
 */
export function getResponsiveImage(
  url: string,
  widths: readonly number[] = DEFAULT_RESPONSIVE_WIDTHS,
): ResponsiveImage {
  const variants = widths
    .filter((width, index, all) => width > 0 && all.indexOf(width) === index)
    .sort((a, b) => a - b)
    .map((width) => ({ width, url: withProxyImageWidth(url, width) }));

  if (variants.length === 0 || variants.every((variant) => variant.url === url)) {
    return { src: url };
  }

  return {
    src: variants[0].url,
    srcset: variants.map((variant) => `${variant.url} ${variant.width}w`).join(", "),
  };
}

export function getOriginalProxyImage(url: string) {
  return withProxyImageWidth(url);
}
