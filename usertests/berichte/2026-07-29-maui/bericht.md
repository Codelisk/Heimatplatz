# Funktionstest MAUI/Android — 29.07.2026

## Rahmen

- Getesteter Stand: `master` @ `b7f7acda4422d8998cdf851b176367425d27ba5b`
- Test-Worktree: `/tmp/heimatplatz-qa-b7f7acda` (detached, sauber)
- APK: selbst aus diesem Commit gebaut und frisch installiert
  (`Heimatplatz.Maui/bin/Debug/net10.0-android/at.heimatplatz.app-Signed.apk`)
- App/Automation: Heimatplatz `1.84.0` (Build 84), .NET MAUI `10.0.10`,
  Microsoft MAUI DevFlow `0.1.0-preview.12.26368.2`
- Umgebung: Android 16 / API 36, x86_64-Emulator, Test-API
  `https://test-api.heimatplatz.at`
- Anzeigegrößen: 720×1600 @ 320 dpi (360×800 dp) und
  1080×2400 @ 420 dpi (411×914 dp), jeweils Hochformat
- Getestet als: Gast und Verkäufer (`test.seller@heimatplatz.dev`)
- Umfang: gezielter Intensivtest der neuen Detailkarten und der Lage-Vorschau im Immobilien-Editor
- Testdaten nach dem Lauf: `Ringstraße 42`, `LocationDisplay=Approximate`,
  Karten-Pin `IsApproximate=true` — Ausgangszustand vollständig wiederhergestellt

## Zusammenfassung

Die Grundgestaltung ist auf Android in Hell und Dunkel sowie auf beiden
Telefonformaten sauber. Kreis, Punkt-Pin, OÖ-Maskierung, Edikt-Text, Wahlkarten,
Leer- und Verbergen-Overlay funktionieren. Der Lauf hat vier eigenständige Befunde
ergeben:

- Der Kernzustand **„Genau“ ist bei einem bestehenden Inserat nicht end-to-end
  wirksam**: Der Editor zeigt einen exakten Treffer und das Update speichert
  `Exact`, die Detailseite bleibt trotzdem beim Umgebungskreis (S2).
- Die native Karte blockiert den Seiten-Scroll, obwohl ihr Ein-Finger-Pan
  deaktiviert ist (S3).
- Eine fehlgeschlagene Geocode-Vorschau lässt unbemerkt den alten Pin zur neuen
  Adresse stehen und versucht nach Netzrückkehr nicht erneut (S3).
- Beim kalten Kartenstart erscheint mehrere Sekunden lang nur eine schwarze Fläche
  ohne Ladeindikator (S4).

| Schwere | Anzahl |
|---|---:|
| S1 Blocker | 0 |
| S2 Schwer | 1 |
| S3 Mittel | 2 |
| S4 Kosmetik | 1 |
| Hinweis/Frage | 0 |

## Testinventar

| Bereich | Testfall | Vorbedingung | Ergebnis |
|---|---|---|---|
| Immobilie · Detail | Ungefähre Lage: Sektion nach Datentabelle, 300-m-Kreis, Hinweis | öffentliches Inserat mit `Approximate` | bestanden |
| Immobilie · Detail | Genaue Lage: roter Punkt-Pin, höherer Zoom, kein Ungefähr-Hinweis | Inserat nach Speichern von `Exact` | **fehlgeschlagen: B-03** |
| Immobilie · Detail | Nicht anzeigen: Sektion fehlt vollständig | Inserat mit `Hidden` | bestanden |
| Immobilie · Detail | Fehlende Koordinaten: Sektion fehlt vollständig | Inserat ohne Pin | nicht separat testbar; leere Pin-Antwort über `Hidden` abgedeckt |
| ZV · Detail | Kreis, OÖ-Maskierung und Edikt-Hinweis | ZV Enns | bestanden |
| Detailkarten | Light/Dark-Wechsel baut Stil und Layer korrekt neu auf | sichtbare Karte | bestanden |
| Detailkarten | Zurück, erneut öffnen, Warm- und Kaltstart | sichtbare Karte | bestanden |
| Detailkarten | Seiten-Scroll über der Karte bleibt flüssig; Ein-Finger-Pan aus | sichtbare Karte | **fehlgeschlagen: B-01** |
| Detailkarten | Zoomtasten und Pinch funktionieren; Rotate/Tilt bleiben aus | sichtbare Karte | teilweise: `+` bestanden; Pinch nicht automatisierbar |
| Detailkarten | Offline/Netzfehler lässt Detailseite benutzbar und blendet Komfortkarte aus | Netzwerkwechsel | bestanden |
| Editor · Aufbau | Reihenfolge entspricht Anzeige: Fotos bis Quelle | Verkäufer, eigenes Inserat | bestanden |
| Editor · Vorbelegung | Edit-Modus lädt Adresse, Ort, Modus und geocodiert automatisch | eigenes Inserat | bestanden |
| Editor · Ungefähr | Wahlkarte markiert, Vorschau zeigt Kreis | auflösbare Adresse | bestanden |
| Editor · Genau | Wahlkarte markiert, Vorschau zeigt Punkt-Pin | exakt auflösbare Adresse | bestanden |
| Editor · Genau-Fallback | Kreis plus verständlicher Hinweis | nur ungefähr auflösbare Adresse | bestanden |
| Editor · Nicht anzeigen | Auge-aus-Overlay; Rückschalten zeigt bisherigen Kartenstand | vorhandene Vorschau | bestanden |
| Editor · Leerzustand | verständliches Overlay ohne Ort bzw. bei unauflösbarer Eingabe | Ort/Adresse leer | bestanden |
| Editor · Debounce | schnelle Adresseingabe übernimmt nur den letzten Stand | mehrere Änderungen < 1,2 s | bestanden; genau ein finaler Geocode-Request |
| Editor · Fehlerzustand | Geocode-Ausfall zeigt keinen fremden/veralteten Stand | Netz während Adresseingabe getrennt | **fehlgeschlagen: B-02** |
| Editor · Kaltstart | Kartenfläche hat einen verständlichen Ladezustand | neue Karteninstanz | **fehlgeschlagen: B-04** |
| Editor · Theme | Kartenstil, Overlays, Wahlrahmen und Texte in Light/Dark sauber | alle drei Modi | bestanden |
| Editor · Bedienung | Scroll über Karte, Tastatur, Zurück und erneutes Öffnen | Editor weit nach unten gescrollt | Tastatur/Zurück bestanden; **Scroll B-01** |
| Editor · Persistenz | Modus speichern, Detailansicht prüfen und Ausgangszustand wiederherstellen | eigenes Inserat | `Hidden`/Restore bestanden; **`Exact` B-03** |
| Responsive | normales Telefon und kleines 720×1600-Gerät im Hochformat | zwei Fensterkonfigurationen | bestanden |
| Diagnose | DevFlow-Baum, Android-UI-Baum und `adb logcat` ohne relevante Feature-Exception | kompletter Lauf | bestanden |

## Befunde

### B-01 · S3 · Detailkarte/Editor — Karte verschluckt den Ein-Finger-Scroll der Seite

- **Schritte:** 1. Ein Inserat mit sichtbarer Lage-Karte öffnen. 2. Bis zur Sektion
  „Lage & Umgebung“ scrollen. 3. Innerhalb der Karte mit einem Finger nach oben oder
  unten wischen. 4. Den gleichen Swipe direkt oberhalb der Karte wiederholen.
- **Erwartet:** Da das Ein-Finger-Pan der Karte deaktiviert ist, scrollt ein Swipe auf
  der Karte die umgebende Detailseite bzw. den Editor flüssig weiter.
- **Tatsächlich:** Innerhalb der Karte passiert gar nichts; die native Kartenansicht
  konsumiert den Touch weiterhin. Direkt oberhalb der Karte scrollt die Seite sofort.
  Auf 360×800 dp belegt die Karte fast den gesamten freien Viewport, wodurch das
  Weiter- bzw. Zurückscrollen unnötig schwierig wird.
- **Screenshots:** `shots/06-detail-scroll-down-over-map-small.png` (keine Bewegung)
  und [`shots/07-detail-scroll-down-outside-map-small.png`](shots/07-detail-scroll-down-outside-map-small.png)
  (gleicher Swipe außerhalb scrollt sofort)
- **Konsole/Log:** unauffällig
- **Vermutete Stelle:** `src/maui/src/Heimatplatz.Maui/Features/Properties/Controls/PropertyLocationMapView.cs`
  (`ScrollGesturesEnabled = false` deaktiviert Karten-Pan, reicht Android-Touches aber
  nicht an den Eltern-`ScrollView` weiter)
- **Reproduzierbar:** ja, mehrfach auf Immobilien-, ZV-Detailseite und Editor

### B-02 · S3 · Editor — alter Pin bleibt nach Geocode-Ausfall unbemerkt zur neuen Adresse stehen

- **Schritte:** 1. Inserat im Editor öffnen und die exakte Vorschau für
  `Ringstraße 42` vollständig laden. 2. Netzwerk trennen. 3. Adresse über die echte
  Android-Tastatur auf `Hauptplatz 99` ändern. 4. Debounce und Request-Timeout
  abwarten. 5. Netzwerk wieder aktivieren und ohne weitere Eingabe warten.
- **Erwartet:** Die alte Position wird sofort als veraltet entfernt oder mit einem
  klaren Offline-/Fehlerzustand überlagert. Nach Netzrückkehr erfolgt ein Retry.
- **Tatsächlich:** Der alte Punkt-Pin bleibt unverändert sichtbar; darunter steht
  weiterhin „So sehen Besucher die Lage Ihres Inserats.“ Es gibt keinen Hinweis und
  nach Netzrückkehr keinen neuen Geocode-Request. Die Karten-Screenshots vor der
  Änderung, offline danach und zehn Sekunden nach Netzrückkehr sind sogar bytegleich
  (gleicher SHA-256).
- **Screenshots:** geänderte Eingabe
  [`shots/24a-editor-offline-changed-address-small-dark.png`](shots/24a-editor-offline-changed-address-small-dark.png),
  unverändert alter Pin
  [`shots/24-editor-offline-stale-preview-small-dark.png`](shots/24-editor-offline-stale-preview-small-dark.png)
  und nach Netzrückkehr
  [`shots/25-editor-after-network-return-still-stale-small-dark.png`](shots/25-editor-after-network-return-still-stale-small-dark.png)
- **Konsole/Netzwerk:** Der fehlgeschlagene Request wird vom DevFlow-Netzwerkpuffer
  nicht als abgeschlossene Anfrage geführt; nach Netzrückkehr bleibt die Liste leer.
- **Vermutete Stelle:** `PropertyWizardViewModel.LocationPreview.cs:115-118`; der
  `catch` loggt nur auf Debug-Level und lässt `_previewCoords`, `PreviewLocation`,
  Hinweistext und Retry-Zustand unverändert.
- **Reproduzierbar:** ja

### B-03 · S2 · Editor/Detail/API — „Genau“ wird gespeichert, bleibt öffentlich aber „Ungefähr“

- **Schritte:** 1. Bestehendes Inserat `Modernes Reihenhaus in Wels` öffnen. 2. Die
  automatische Vorschau abwarten (`geocode-preview`: `IsExact=true`). 3. „Genau“
  wählen; die Vorschau zeigt korrekt den Punkt-Pin. 4. Änderungen speichern
  (`PUT /api/properties`: 200). 5. Inserat neu als öffentliche Detailseite öffnen.
- **Erwartet:** `map-pins?PropertyId=…` liefert `IsApproximate=false`; die
  Detailseite zeigt bei Zoom 15 den Punkt-Pin ohne Ungefähr-Hinweis.
- **Tatsächlich:** Das Detail-DTO enthält danach zwar
  `LocationDisplay="Exact"`, `map-pins` liefert aber weiterhin
  `IsApproximate=true`. Die neu geladene Android-Detailseite zeigt wieder
  300-m-Kreis und den Text „Ungefähre Lage …“.
- **Screenshots:** korrekte Editor-Vorschau
  [`shots/26-editor-exact-before-save-small-dark.png`](shots/26-editor-exact-before-save-small-dark.png)
  und falscher Zustand nach erfolgreichem Speichern
  [`shots/27-detail-after-exact-save-still-approx-small-dark.png`](shots/27-detail-after-exact-save-still-approx-small-dark.png)
- **Konsole/Netzwerk:** Preview 200/`IsExact=true`, Update 200,
  Detail-DTO `Exact`, Karten-Pin dennoch `IsApproximate=true`.
- **Vermutete Stelle:** `UpdatePropertyHandler.cs:127-153` geocodiert nur bei
  geänderter Adresse oder fehlenden Koordinaten. Ein bestehender Datensatz mit
  Koordinaten, aber `IsLocationExact=false`, wird beim alleinigen Opt-in auf
  `LocationDisplay=Exact` nicht mit der bereits erfolgreichen Preview-Qualität
  nachgezogen.
- **Reproduzierbar:** ja; dreifach über API, App-Neuladen und sichtbaren Kreis
- **Aufräumen:** Anschließend `Hidden` erfolgreich geprüft und das Inserat wieder
  auf `Approximate` gespeichert; Adresse und Karten-Pin entsprechen dem Zustand vor
  dem Test.

### B-04 · S4 · Karte — schwarzer Leerzustand ohne Fortschritt beim Kaltstart

- **Schritte:** App kalt starten, eigenes Inserat öffnen und die Lage-Sektion sofort
  in den Viewport scrollen.
- **Erwartet:** Bis Stil und Layer bereit sind, erscheint wenigstens Papierhintergrund,
  Skeleton oder Spinner.
- **Tatsächlich:** Rund fünf Sekunden nach dem Öffnen ist nur eine schwarze Fläche
  mit Kartensteuerung sichtbar; weder Karte noch Kreis noch Ladehinweis. Im Messlauf
  war der Map-View nach 1,3 s vorhanden, der Screenshot entstand nach 4,9 s. Erst
  nach 10,8 s waren Stil und Kreis vollständig gerendert. Der erste PMTiles-Request
  selbst dauerte 1,83 s.
- **Screenshots:** [`shots/35-editor-cold-map-immediate-small-dark.png`](shots/35-editor-cold-map-immediate-small-dark.png)
  und der selbstheilende Endzustand
  [`shots/35-editor-cold-map-plus5s-small-dark.png`](shots/35-editor-cold-map-plus5s-small-dark.png)
- **Reproduzierbar:** ja, bei kalter Karteninstanz auf beiden Anzeigegrößen; warm
  danach deutlich schneller

## Geprüft und in Ordnung

- Die Lage-Sektion sitzt auf Immobilien- und ZV-Detailseiten nach den Datentabellen.
- Ungefähre Lage: Kreis, Zoom, Privacy-Jitter und Hinweistext stimmen.
- ZV Enns: Edikt-Variante des Hinweises und OÖ-Maskierung an der Landesgrenze stimmen.
- Heller und dunkler Kartenstil bauen Kreis/Pin nach Theme-Wechsel wieder korrekt auf.
- Ein-Finger-Pan der Karte selbst ist aus; `+` zoomt und skaliert den Kreis korrekt.
- Editor-Reihenfolge und Vorbelegung entsprechen der Anzeige; automatische
  Geocodierung startet beim Öffnen.
- Alle drei Wahlkarten sind visuell konsistent. Exakt-Pin, Ungefähr-Kreis,
  Exact-Fallback, Auge-aus-Overlay und Ort-Leerzustand sind verständlich.
- Drei schnelle Adresseingaben erzeugen nach 1,2-s-Debounce nur einen Request für
  den letzten Wert.
- „Nicht anzeigen“ persistiert vollständig: `LocationDisplay=Hidden`,
  `Pins=[]`, keine Lage-Sektion in der Detailseite.
- Offline: Eine bereits geladene Karte übersteht Netzwerkverlust und Theme-Rebuild
  samt Layer; beim kalten Offline-Start bleibt die Detailseite aus Cache benutzbar
  und lässt die Komfortsektion weg.
- Verwerfen-Dialog: „Weiter bearbeiten“ und „Verwerfen“ funktionieren; ungespeicherte
  Adresse/Modus wurden nicht übernommen.
- Beide Telefonformate zeigen keine Überläufe, abgeschnittenen Wahltexte oder
  problematischen Kontrast.
- Android-Build aus dem exakten Commit: erfolgreich, 0 Fehler.
- Kein Feature-Crash, keine ANR und keine relevante Exception im PID-gefilterten
  `adb logcat`.

## Nicht getestet / nicht testbar

- Pinch-Zoom und echte Zwei-Finger-Rotate/Tilt-Gesten sind über DevFlow/ADB nicht
  zuverlässig injizierbar. Zoomtaste und das Fehlen sichtbarer Rotate/Tilt-Controls
  wurden geprüft.
- Es gab keinen ungefähr sichtbaren Testdatensatz ohne Koordinaten. Das Verhalten
  bei leerer Pin-Antwort wurde über `Hidden` und einen kalten Offline-Start geprüft,
  aber nicht mit einem separaten `Approximate`-ohne-Koordinaten-Fixture.
- Kein physisches Android-Gerät und kein iOS-Gerät; dieser Lauf war bewusst
  Android-Emulator-QA.

## Diagnosen außerhalb des Features

- Der sideloaded Emulator-Build protokolliert beim Play-In-App-Update-Check
  `Failed to bind to the service`. Das ist auf diesem AVD ohne Store-Installation
  erwartbar und nicht kartenbezogen.
- MAUI meldete nach vielen Navigationen/Theme-Wechseln mehrfach
  `RoundRectangle RealParent ... Garbage Collected`. Es gab keinen sichtbaren Fehler,
  Crash oder Leak-Nachweis; deshalb kein Feature-Befund.
- Der anfängliche Systemdialog „System UI isn't responding“ gehörte zu
  `com.android.systemui`, nicht zu Heimatplatz. Nach „Wait“ blieb die App stabil;
  keine App-ANR im Lauf.
