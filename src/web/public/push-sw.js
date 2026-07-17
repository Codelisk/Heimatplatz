// Heimatplatz Web-Push Service Worker.
// Payload-Format von Shiny.Extensions.Push (WebPushPayloadBuilder, flaches JSON):
// { "title": "...", "body": "...", "deeplink": "...", "icon": "...", "data": { ... } }
// "deeplink" traegt die Android-Intent-Action und ist im Web nutzlos - die Ziel-URL
// wird hier aus data.propertyId gebaut (gleiche Route wie die Detailseite).

self.addEventListener("install", () => {
  self.skipWaiting();
});

self.addEventListener("activate", (event) => {
  event.waitUntil(self.clients.claim());
});

function parsePayload(event) {
  if (!event.data) return {};
  try {
    return event.data.json();
  } catch {
    return { body: event.data.text() };
  }
}

self.addEventListener("push", (event) => {
  const payload = parsePayload(event);
  const data = payload.data ?? {};
  const title = payload.title || "Heimatplatz";
  const options = {
    body: payload.body || "",
    icon: payload.icon || "/apple-touch-icon.png",
    data,
    // Gleiche Immobilie ersetzt eine noch sichtbare Benachrichtigung statt zu stapeln
    tag: data.propertyId ? `property-${data.propertyId}` : undefined,
  };
  event.waitUntil(self.registration.showNotification(title, options));
});

self.addEventListener("notificationclick", (event) => {
  event.notification.close();
  const data = event.notification.data ?? {};
  const targetPath = data.propertyId
    ? `/immobilien/angebote/${encodeURIComponent(data.propertyId)}/`
    : "/";

  event.waitUntil(
    (async () => {
      const targetUrl = new URL(targetPath, self.location.origin).href;
      const windows = await self.clients.matchAll({ type: "window", includeUncontrolled: true });
      for (const client of windows) {
        if (client.url === targetUrl && "focus" in client) {
          return client.focus();
        }
      }
      return self.clients.openWindow(targetPath);
    })(),
  );
});
