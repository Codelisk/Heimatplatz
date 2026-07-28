# Erkenntnisse aus Testlaeufen — VOR jedem Lauf lesen, NACH jedem Lauf pflegen

Lebendes Gedaechtnis des Funktionstests ueber alle Laeufe und Sessions hinweg. Die
Plattform-Referenzen (`web.md`, `maui.md`) beschreiben das Setup — diese Datei sammelt,
was Laeufe an Fallen, Fehlentscheidungen und Soll-Zustaenden gelehrt haben.

**Pflege-Regeln:**

- Nur Wiederverwendbares: Falschbefund-Fallen, Werkzeug-Fallen, bewusste Produktentscheidungen,
  Datenlage-Ueberraschungen. **Keine Befunde** — die stehen im Bericht des Laufs.
- Bestehende Eintraege schaerfen statt Duplikate anhaengen; Ueberholtes loeschen.
- Jeder Eintrag mit Datum. Kurz und konkret — ein Eintrag, der nicht vor einem Fehler
  bewahrt oder eine Stunde spart, gehoert hier nicht hinein.

---

## Bewusst so — NICHT erneut als Befund melden

Produktentscheidungen aus der Abarbeitung frueherer Berichte. Wer eines dieser Verhalten
"findet", meldet einen Wiederholbefund und verbrennt Zeit:

- **Favoriten entfernen fragt nicht nach** (28.7.). Mit einem Tipp umkehrbar, wie das
  Aufheben einer Blockierung. Rueckfrage-Dialoge sind Destruktivem vorbehalten
  (Inserat loeschen in Meine Immobilien fragt weiterhin — das ist korrekt so).
- **ZV-Grundstueck zeigt einen Abschnitt GEBAEUDE/Zustand** (28.7.). Stammt aus den
  Seed-Daten (`BuildingArea: 0`, aber `BuildingCondition` gesetzt); Entscheidung: so lassen.
- **"Immobilie hinzufuegen" liegt im Dev-Flyout unter der Falz** (28.7.). Betrifft nur
  Builds mit Debug-Eintrag (9 Eintraege); Entscheidung: nicht machen.
- **Gericht steht doppelt auf der ZV-Detailseite** (Tabelle RECHTLICHES + Fussleiste;
  28.7.). Entscheidung: okay so.
- **Theme-Umschalter blendet "Design: hell/dunkel/System" als Glass-Pille ueber dem
  Hero ein** (28.7.). Das ist Soll-Verhalten, kein streunender Toast. Zyklus:
  System → Hell → Dunkel; der erste Tipp kann darstellungsgleich sein — dafuer gibt
  es die Pille ja gerade.
- **Die MAUI-Karte ist seit 28.7. nativ (MapLibre), kein WebView mehr.** Befunde aus
  Berichten vor dem 28.7., die sich auf das `/karte-embed`-WebView beziehen, sind obsolet.
- **ZV-Detailseite zeigt eine LASTEN-Karte** (28.7., zwischen Beschreibung und Datenblatt,
  mit Summenzeile ab zwei bezifferten Posten). Soll-Verhalten.
- **"Filter zuruecksetzen" gibt es NUR mobil** (28.7., Web). Der Button lebt bewusst nur
  im Mobile-Filter-Akkordeon (`MobileFilterPanel.astro`); am Desktop liegen alle Filter
  einzeln sichtbar in der Suchleiste, ein Sammel-Reset ist dort nicht gewollt.
  Entscheidung Daniel 28.7. — nicht erneut als fehlend melden.

## Falschbefund-Fallen — pruefen, BEVOR ein Befund in den Bericht geht

- **uiautomator-Dump ist XML:** `&` erscheint als `&amp;`, Nicht-BMP-Zeichen (Emoji) als
  `&#128222;`. Beides sind Dump-Artefakte. Jeden Text-"Befund" aus dem Dump im
  Screenshot gegenpruefen. (27.7. — haette zwei Falschbefunde erzeugt)
- **Dunkle Bildpartien taeuschen Clipping vor:** Ein "angeschnittener" Button auf einem
  Foto kann schlicht mit einer dunklen Motivpartie verschmelzen. Bei Rand-/Clipping-
  Verdacht ein helles Motiv gegenpruefen und die Bounds im Dump messen, bevor
  "repariert" wird. (27.7., B-06 — wurde als Falschbefund zurueckgezogen)
- **Deep-Link-Hosts sind getrennt:** `heimatplatz://property/{guid}` und
  `heimatplatz://foreclosure/{guid}`. Eine ZV-GUID ueber den property-Host landet auf
  der allgemeinen Detailseite — das ist der falsche Testaufruf, kein Bug. Die App
  selbst erzeugt nur Web-Links (Teilen), nie Deep-Links. (27.7.)
- **`adb shell service call clipboard 2` ist ein Set ohne Daten** und erzeugt SELBST den
  logcat-Fehler `IllegalArgumentException: No items` — nicht der App anlasten. Es gibt
  keinen sauberen adb-Weg, die Zwischenablage zu lesen; die In-App-Rueckmeldung zaehlt. (27.7.)
- **"Fehlt auf der Seite" erst nach Scrollen behaupten:** Der Dump zeigt nur den
  Viewport. Eine "leere" Beschreibung war schlicht unter der Falz. (27.7.)
- **Transiente UI (Toasts, "Kopiert!", Pillen) sofort screenshotten** (≤500 ms nach der
  Aktion). Ein Dump 1–2 s spaeter verpasst sie und "beweist" faelschlich ihr Fehlen.
  Umgekehrt: Samsungs System-Toast ("Copied.") nicht mit App-Feedback verwechseln. (28.7.)
- **ANR/Crash zuerst der Umgebung verdaechtigen, dann der App:** Ein Start-ANR trat nur
  auf einem instabilen Emulator auf (vorher `adb root`; Instanz brach kurz danach weg)
  und war nach Emulator-Neustart mit demselben Build nicht reproduzierbar. Vor einem
  S1 immer auf frischer Instanz reproduzieren. (27.7.)
- **Zaehler-Differenzen (Liste vs. Karte etc.) serverseitig zerlegen:** Dieselbe API
  einmal MIT und einmal OHNE Token abfragen. So wurde "11 vs. 10" eindeutig der
  anonymen Pin-Abfrage zugeordnet — API korrekt, App-Anteil isoliert. (27.7.)
- **Der App-Zustand luegt, die API nicht:** Jede schreibende Aktion (Favorit, Blockieren,
  Anlegen, Loeschen) per API-Gegenprobe verifizieren, bevor "funktioniert" oder
  "funktioniert nicht" im Bericht steht. (27./28.7.)
- **ZV-Kanon-Canonical ist auf Test NICHT pruefbar** (28.7.): Die Seed-ZV-Properties
  (Haus in Traun, Grundstueck Enns) haben keine Edikt-URL, der Join in
  `property-mirror.ts` liefert null → canonical bleibt auf `/immobilien/angebote/…`.
  Kein Bug — auf Prod setzt der Scraper die Edikt-URL. Gehoert zur Falle-8-Familie.
- **`textContent` enthaelt VERSTECKTE AuthGate-/Empty-State-Bloecke** (28.7.): Nach
  erfolgreichem Feedback-Senden "zeigte" main.textContent das Login-Gate — das war der
  per CSS versteckte Gast-Block. Sichtbarkeit immer ueber `offsetParent !== null`
  pruefen, sonst Phantom-Befunde.
- **fullPage-Screenshots zeigen Scroll-Reveal-Sektionen als Leerflaeche** (28.7.):
  /makler sah nach "Ihre Vorteile" leer aus; nach echtem Scrollen war alles da.
  Erst scrollen (Reveal ausloesen), dann urteilen.

## Werkzeug- und Bedien-Fallen

### Android (Emulator und Geraet)

- **Dump IMMER unmittelbar vor dem Tap, im selben Schritt.** Koordinaten aus einem
  aelteren Dump sind wertlos: der Flyout behaelt seine Scrollposition, Listen laden nach,
  und auf gepushten Detailseiten sitzt bei der Hamburger-Position der Zurueck-Pfeil.
  Vor dem Tap zusaetzlich verifizieren, dass die erwartete Seite ueberhaupt vorne ist. (27./28.7.)
- **Angestecktes Geraet kann PARALLEL bedient werden** (Daniel selbst oder eine andere
  Session). Zwischen Dump und Tap kann die Ansicht wechseln — ein Fehl-Tap landete so
  in einer fremden App. Niemals blind tippen, Aktionen serverseitig gegenpruefen,
  bei unerklaerlichem Seitenwechsel abbrechen statt weiterklicken. (28.7.)
- **Testdaten hinterlassen wie vorgefunden:** Alles, was der Lauf anlegt oder aendert,
  per API zurueckbauen (Test-Inserat loeschen, Entwurf loeschen, Favorit wiederherstellen,
  Theme-Zyklus zu Ende schalten) und im Bericht dokumentieren. (27./28.7.)
- **`emulator.exe ... | Select-Object -First N` killt den Emulator** — das Schliessen der
  Pipeline beendet den Prozess. Emulatoren nur per `Start-Process` mit
  `-RedirectStandardOutput` in eine Logdatei starten. (27.7.)
- **Eine AVD kann von einem haengenden qemu-Prozess gesperrt sein** — dann scheitert auch
  `-read-only`. Gleichwertige Alternative: eigenen frischen Build auf einen bereits
  laufenden Emulator installieren (`adb uninstall` + install); das sichert die
  Code-Herkunft genauso wie ein eigener Emulator. (27.7.)
- **AutomationIds erscheinen im Dump als `resource-id`** (`at.heimatplatz.app:id/...`).
  Seit 28.7. haben auch Anmelden/Registrieren/Passwort-vergessen IDs (`Login_*`,
  `Register_*`, `ForgotPassword_*`) — Koordinaten-Tapperei ist dort nicht mehr noetig.
- **Shiny-Dialoge sind eigene Windows** (`dumpsys window | grep mCurrentFocus` zeigt z.B.
  `Favorit entfernen?`) — der Fokus-Check erkennt sie zuverlaessig. (27.7.)

### Web

- **Frischer Worktree: `src/web/public/tiles/` ist gitignored** — ohne Kopie aus dem
  Haupt-Checkout 404t `oberoesterreich.pmtiles` und ALLE Karten bleiben leer
  (Lage-Karte, Split-Karte). Vor dem Karten-Test kopieren. (28.7.)
- **/karte-embed rendert ohne `?ansicht=karte` nur die Legende** — das ist der Vertrag
  mit der MAUI-App (Param oeffnet die Karte), kein Bug. `theme=light|dark` uebersteuert
  das gespeicherte Theme. (28.7.)
- **/intern ist lokal nicht testbar** — die Seiten brauchen serverseitig
  `ADMIN_API_KEY` (nur am Hetzner-Server gesetzt, IP-Schranke statt Login). Fuer
  /intern gegen `test.heimatplatz.at` testen (Achtung: deployter Build ≠ Worktree-Stand). (28.7.)
- **Playwright-MCP: `navigator.clipboard.readText()` haengt endlos** (Permission-Prompt,
  Call musste gekillt werden) — nie aufrufen; Teilen-/Kopieren-Feedback stattdessen im
  Code verifizieren. Datei-Uploads gehen nur aus den erlaubten Roots (Worktree bzw.
  `.playwright-mcp/`) — Testdateien vorher dorthin kopieren. (28.7.)
- **Umlaut-Titel in Bash/Python-Pipes vergleichen sich unzuverlaessig** (Windows-
  Encoding): "Baugrundstueck in Wels" war angeblich nicht in der API-Liste, war es
  aber — bei Gegenproben Titel ausdrucken statt boolesche in-Checks. (28.7.)
- **Sortierung persistiert als Filter-Preference** (`heimatplatz:filter-preferences`)
  und wird beim Laden von `/` angewandt, die URL bekommt `?sort=` aber erst bei der
  naechsten Interaktion nachgeschrieben — kein Bug, nur URL/Zustand-Divergenz
  (als Hinweis gemeldet 28.7.).

## Datenlage auf Test (Stand 28.7.)

- **13 Objekte: 8 Haus, 3 Grundstueck, 2 ZV** — je genau ein Haus-ZV ("Haus in Traun")
  und ein Grund-ZV ("Grundstueck Enns"). Damit sind alle vier Feld-Sichtbarkeits-
  Testfaelle aus `PropertyDetailPage_Testfaelle.md` abgedeckt, aber ohne Redundanz:
  wird eines der ZV-Objekte verändert, kippt ein Testfall.
- **"Grundstueck Enns" hat als einziges Objekt Encumbrances** (Hypothek € 34.000 +
  Grundsteuer € 2.500) — das Referenzobjekt fuer die LASTEN-Karte inkl. Summenzeile
  und Redundanz-Unterdrueckung ("Hypothek Bank Austria" ohne Glaeubiger-Zweitzeile).
- **test.buyer:** 4 Favoriten (ZV Enns, Bad Ischl, Braunau, Voecklabruck), 1 Blockierung
  ("Grosses Baugrundstueck Muehlviertel"). Die Blockierung ist Voraussetzung fuer den
  Blockiert-Filter-Test — nicht dauerhaft aufheben.
- **test.seller:** 3 Inserate, normalerweise keine Entwuerfe (`/api/property-drafts/` leer).
- **Feedback-Threads von QA-Laeufen bleiben stehen** (Nutzer koennen nicht loeschen):
  26.7. "Wunsch / Idee 1" und 28.7. "Problem-Meldung 1" (mit Intern-Testantwort,
  Status Offen) — beide als "bitte ignorieren/loeschen" gekennzeichnet, nicht als
  Datenmuell melden. (28.7.)
- **test.buyer-Filtereinstellungen** wurden am 28.7. explizit auf die App-Defaults
  gespeichert (Neueste, Haus+Grund, Privat+Makler, Zeitraum Alle) — gespeicherte
  Prefs vorhanden, aber inhaltlich Default.
- **"Haus in Traun" (ZV-Spiegel) hat ebenfalls Encumbrances** (Hypothek € 92.500 +
  Grundsteuer € 2.500) — die Aussage "Grundstueck Enns als einziges" oben stimmt so
  nicht mehr bzw. bezog sich auf die Auktions-Seite. (28.7.)
