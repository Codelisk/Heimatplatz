import { defineCollection } from "astro:content";
import { file } from "astro/loaders";
import { z } from "astro/zod";

const properties = defineCollection({
  loader: file("src/data/properties.json"),
  schema: z.object({
    id: z.string(),
    slug: z.string(),
    title: z.string(),
    description: z.string(),
    type: z.enum(["apartment", "house", "land", "foreclosure"]),
    typeLabel: z.string(),
    location: z.string(),
    region: z.string(),
    regionSlug: z.string(),
    address: z.string(),
    postalCode: z.string(),
    price: z.number().int().positive(),
    livingArea: z.number().positive().optional(),
    plotArea: z.number().positive().optional(),
    rooms: z.number().positive().optional(),
    imageUrl: z.url(),
    imageAlt: z.string(),
    sellerType: z.enum(["private", "agent", "bank", "court"]),
    sellerLabel: z.string(),
    sellerName: z.string(),
    source: z.string(),
    originalUrl: z.url().optional(),
    updatedAt: z.coerce.date(),
    highlights: z.array(z.string()).default([]),
    features: z.array(z.string()).default([]),
    contacts: z
      .array(
        z.object({
          label: z.string(),
          email: z.email().optional(),
          phone: z.string().optional(),
        }),
      )
      .default([]),
  }),
});

export const collections = { properties };
