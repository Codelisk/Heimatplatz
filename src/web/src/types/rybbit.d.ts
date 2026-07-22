// Globale window.rybbit-API des Rybbit-Tracking-Scripts (siehe BaseLayout.astro).
// Nur im Prod-Build vorhanden (PUBLIC_RYBBIT_SITE_ID) - daher optional, Aufrufe
// immer mit window.rybbit?.event(...) absichern.
export {};

declare global {
  interface Window {
    rybbit?: {
      event: (name: string, properties?: Record<string, string | number>) => void;
    };
  }
}
