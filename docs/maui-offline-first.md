# MAUI Offline-First

Die MAUI-App speichert erfolgreiche, explizit freigegebene Leseantworten dauerhaft
in `heimatplatz-offline.db` unter `FileSystem.AppDataDirectory`. Der Store basiert auf
Shiny DocumentDb mit SQLite und wird von Shiny Mediator sowohl fuer den persistenten
Cache als auch fuer den Offline-Fallback verwendet.

## Ablauf

1. `LocalFirstRequestMiddleware` liefert einen vorhandenen lokalen Datensatz sofort.
2. Ist er laut `RefreshAfterSeconds` veraltet und Internet vorhanden, aktualisiert ein
   entkoppelter Mediator-Request den lokalen Stand im Hintergrund.
3. Pull-to-Refresh erzwingt online einen Serverabruf. Ist der Server nicht erreichbar,
   bleibt der lokale Stand sichtbar.
4. Ohne Internet beendet `OfflineNetworkGuardMiddleware` nicht lokal beantwortbare
   Serveroperationen sofort, ohne einen HTTP-Timeout abzuwarten.

Ein Request benoetigt mindestens einen erfolgreichen Online-Abruf, bevor er offline
beantwortet werden kann. Bilder verwenden weiterhin den Plattform-Image-Cache und
werden nicht separat in die SQLite-Datenbank heruntergeladen.

## Sicherheit und Trennung

Cache-Schluessel enthalten Benutzer-ID, API-Endpunkt, Request-Typ und einen stabilen
Hash der Request-Parameter. Dadurch werden Daten verschiedener Benutzer, Umgebungen
und Filter nicht vermischt. Logout und ein serverseitig abgelehnter Token-Refresh
loeschen die lokalen Eintraege des Benutzers.

## Neue Offline-Reads

Neue sichere GET-Requests werden einzeln in
`Offline/OfflineDataConfiguration.cs` eingetragen. Keine Wildcards verwenden: POST,
PUT, PATCH und DELETE sind derzeit online-only und werden nicht in eine lokale Outbox
geschrieben. Vollstaendig offline schreibbare Daten benoetigen zusaetzlich eine
fachliche Konfliktstrategie und einen passenden Server-Sync-Vertrag.

## Verwendete Shiny-Bausteine

- [Shiny Mediator Offline Availability](https://shinylib.net/mediator/middleware/offline/)
- [Shiny Mediator Caching](https://shinylib.net/mediator/middleware/caching/)
- [Shiny DocumentDb SQLite](https://shinylib.net/documentdb/sqlite/)
