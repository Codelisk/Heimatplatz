import type { APIRoute } from "astro";
import {
  API_PROPERTY_BUILD_LIMIT,
  fetchApiProperties,
  getApiPropertyImage,
} from "@/features/properties/live-api";

export const GET: APIRoute = async () => {
  const properties = await fetchApiProperties({ pageSize: API_PROPERTY_BUILD_LIMIT });
  const images = Object.fromEntries(
    properties.map((property) => [property.Id, getApiPropertyImage(property)]),
  );

  return new Response(JSON.stringify(images), {
    headers: {
      "content-type": "application/json; charset=utf-8",
      "cache-control": "public, max-age=300",
    },
  });
};
