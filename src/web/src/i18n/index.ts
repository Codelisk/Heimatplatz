/**
 * Zentrale Lokalisierung der Web-App — aktuell rein deutsch.
 *
 * Alle user-sichtbaren UI-Texte liegen in den Domain-Modulen unter ./de/*
 * (ein Modul pro fachlichem Bereich, Keys mit Modul-Präfix wie "home.emptyTitle").
 * Zugriff über t("key") — typsicher, Autocomplete über TranslationKey.
 *
 * Platzhalter im Wert per {name}-Syntax, Ersetzung über den params-Parameter:
 *   t("card.priceFrom", { price: "250.000 €" })
 */
import { account } from "./de/account";
import { auth } from "./de/auth";
import { common } from "./de/common";
import { editor } from "./de/editor";
import { foreclosures } from "./de/foreclosures";
import { home } from "./de/home";
import { misc } from "./de/misc";
import { property } from "./de/property";

export const de = {
  ...common,
  ...home,
  ...property,
  ...editor,
  ...auth,
  ...account,
  ...foreclosures,
  ...misc,
} as const;

export type TranslationKey = keyof typeof de;

export function t(
  key: TranslationKey,
  params?: Record<string, string | number>,
): string {
  let value: string = de[key];
  if (params) {
    for (const [name, replacement] of Object.entries(params)) {
      value = value.replaceAll(`{${name}}`, String(replacement));
    }
  }
  return value;
}
