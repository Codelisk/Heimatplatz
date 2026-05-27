export const PROPERTY_TYPE_LABELS = {
  apartment: "Wohnung",
  house: "Haus",
  land: "Grund",
  foreclosure: "Zwangsversteigerung",
} as const;

export function formatPrice(value: number) {
  if (value >= 1000000) {
    return `${Math.round(value / 100000) / 10} Mio. EUR`;
  }

  return `${Math.round(value / 1000)} Tsd. EUR`;
}

export function formatPriceLong(value: number) {
  return new Intl.NumberFormat("de-AT", {
    style: "currency",
    currency: "EUR",
    maximumFractionDigits: 0,
  }).format(value);
}

export function formatDate(date: Date) {
  return new Intl.DateTimeFormat("de-AT", {
    day: "2-digit",
    month: "2-digit",
    year: "numeric",
  }).format(date);
}
