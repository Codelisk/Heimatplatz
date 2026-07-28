# Funktionstest MAUI — 27.07.2026

## Rahmen

- **Getesteter Stand:** `master` @ `cff3665` (Arbeitsverzeichnis sauber bis auf `usertests/berichte/`)
- **Build:** frisch aus *diesem* Worktree gebaut (`dotnet build -f net10.0-android -c Debug -p:EmbedAssembliesIntoApk=true`, 27.07. 14:58, 0 Warnungen/0 Fehler), danach `adb uninstall` + Neuinstallation → Erststart-Zustand
- **Geraet:** Android-Emulator `Phone_API35` (`emulator-5554`), 720×1600 @ dpi 320 (= 360×800 dp), nur Hochformat
- **Umgebung:** Test — `https://test-api.heimatplatz.at`, in der App bestaetigt durch die Flyout-Pille **„Entwicklung · Test-API"**, Version **1.83.0**
- **Getestet als:** Gast, Kaeufer (`test.buyer@heimatplatz.dev`), Verkaeufer privat (`test.seller@heimatplatz.dev`)
- **Datenlage Test-DB:** 13 Objekte (8 Haus, 3 Grundstueck, 2 ZV — je ein Haus-ZV und ein Grund-ZV), damit alle vier Detailseiten-Testfaelle abgedeckt
- **Umfang:** kompletter Durchlauf ueber Suche, Filter, Detailseiten, Karte, Sammlungen, Benachrichtigungen, Profil, Inserieren (anlegen + veroeffentlichen + loeschen), Offline, Theme, Deep-Links, Start-/Zurueck-Verhalten

### Vorsorge gegen Falschbefunde

- Es liefen zwei fremde Emulatoren (`Phone_API35`, `Tablet_API35`) mit einem aelteren Build. Ein eigener Emulator liess sich nicht starten (die AVD `pixel_5_-_api_35` wird von einem seit 25.07. haengenden qemu-Prozess blockiert, auch `-read-only` schlaegt fehl). Stattdessen wurde auf `Phone_API35` der **eigene frische Build** installiert — damit ist die Herkunft des getesteten Codes eindeutig.
- Keine laufende Release-/Screenshot-Pipeline (Uhr nicht 09:41, Debug-Eintrag im Flyout vorhanden).
- Emulator-Netz validiert (`captive_portal_detection_enabled=0`, `dumpsys connectivity` meldet `VALIDATED`).
- `adb logcat` ueber den gesamten Lauf mitgeschnitten: **keine einzige App-Exception, kein FATAL, kein AndroidRuntime-Crash**.

## Zusammenfassung

Die App macht einen sehr soliden Eindruck. Alle Kernfluesse funktionieren end-to-end: Anmelden, Suchen, Filtern, Sortieren, Detailseiten je Objekttyp, Favoriten und Blockieren inklusive sofortiger Listen-Synchronisation, Benachrichtigungs-Einstellungen samt Android-Berechtigung, und der komplette Inserats-Weg vom leeren Editor bis zum oeffentlich sichtbaren Inserat und wieder zurueck zum Loeschen. Die Formularvalidierung ist durchgehend deutsch, in Sie-Form und praezise; die Preisformatierung `€ 349.000` stimmt ueberall; Light und Dark sind beide sauber. Die vier Feld-Sichtbarkeits-Testfaelle aus `PropertyDetailPage_Testfaelle.md` sind alle erfuellt — der dort als „bekanntes Issue" vermerkte Stand ist ueberholt.

Der gewichtigste Befund betrifft die **Karte**: sie laedt ihre Pins ohne Benutzerkontext und zeigt deshalb auch Objekte an, die der Nutzer blockiert hat — inklusive Mini-Zettel und Sprung auf die Detailseite. Sichtbar wird das auch an der Zaehler-Differenz „11 Inserate auf der Karte" gegenueber „10 Objekte" in der Liste.

Daneben faellt eine wiederkehrende Layout-Schwaeche auf: in den Bottom-Sheets ist die primaere Aktion beim Oeffnen angeschnitten und erst nach Scrollen bedienbar. Der Rest sind Kosmetik- und Textbefunde. Zwei Auffaelligkeiten liessen sich trotz gezielter Versuche nicht reproduzieren und sind entsprechend markiert.

| Schwere | Anzahl |
|---|---|
| S1 Blocker | 0 |
| S2 Schwer | 1 |
| S3 Mittel | 3 |
| S4 Kosmetik | 6 |
| Hinweis/Frage | 7 |
| Einmalig beobachtet | 2 |

---

## Nachtrag 27.07. — behobene Befunde

Alle Befunde sind abgearbeitet. In einer ersten Runde die beiden kartenunabhaengigen S3-Befunde,
danach die Kartenumstellung (eigene Session) und zuletzt die S4-Runde. Ein Befund (B-06) hat sich
bei genauerem Hinsehen als Fehldeutung herausgestellt und wurde zurueckgezogen statt „repariert".

| Befund | Status | Aenderung |
|---|---|---|
| **B-01** blockierte Objekte auf der Karte | ✅ behoben | Die Kartenansicht wurde auf ein natives MapLibre-Steuerelement umgestellt (`70cd6ac`). Die Pins laufen jetzt ueber `mediator.Request` — dieselbe authentifizierte Pipeline wie die Liste, damit greift der Blockier-Filter serverseitig. |
| **B-02** Sheets schneiden die Primaeraktion ab | ✅ behoben + verifiziert | `HomePage.xaml.cs`: Sortier-, Typ- und Zeitraum-Sheet auf `FitContent = true` statt fester Detent-Anteile |
| **B-03** Karte ohne Ladeanzeige | ⚪ hinfaellig | Beschrieb das Verhalten des Karten-WebViews, den es nicht mehr gibt. Am nativen Steuerelement nicht nachgeprueft. |
| **B-04** widerspruechliche Offline-Meldungen | ✅ behoben + verifiziert | `HomeViewModel.cs` + `HomePage.xaml`: neues `ShowCachedDataNotice`, `UpdateResultCount()` auch im Fehlerpfad |
| **B-05** „Kopiert!" erscheint doppelt | ✅ behoben + verifiziert | `PropertyDetailViewModel.cs` + `.xaml`: eigener Kanal `ShareFeedback`/`HasShareFeedback` fuer den Teilen-Knopf, `CopyFeedback` bleibt der Kontaktleiste |
| **B-06** linker Pfeil im Bildbetrachter angeschnitten | ❌ kein Befund | Fehldeutung im Testlauf: der Knopf lag ueber einer sehr dunklen Bildpartie und verschmolz optisch mit dem schwarzen Hintergrund. Die Raender sind im Code symmetrisch (je 14), auf hellen Bildern sind beide Pfeile vollstaendige Kreise. |
| **B-07** Bildbetrachter deckt die Titelleiste nicht ab | ✅ behoben + verifiziert | `Shell.NavBarIsVisible` an das neue `IsNavBarVisible` gebunden — die Leiste verschwindet, solange die Lightbox offen ist |
| **B-08** fehlender Umlaut in „pruefen" | ✅ behoben + verifiziert | `PropertyMapStrings.resx:25` |
| **B-09** Trefferzahl hinter der Karten-Pille | ✅ behoben + verifiziert | `HomePage.xaml`: unterer Footer-Freiraum von 20 auf 72 (Pille ist 40 hoch + 20 Abstand) |
| **B-10** irrefuehrende Meldung bei leerem Titel | ✅ behoben + verifiziert | Neue Meldung `ValidationTitleRequired`, Leer- und Zu-kurz-Fall getrennt geprueft |

Damit sind **alle Befunde erledigt** — 8 behoben, 1 als Fehldeutung zurueckgezogen, 1 hinfaellig.
Offen bleiben nur die 7 Hinweise/Fragen (Design-Entscheidungen) und die 2 einmaligen Beobachtungen.

### Nachverifikation der S4-Fixes am Geraet

Die zweite Fix-Runde wurde auf dem **physischen Geraet** nachgeprueft (Galaxy S24 Ultra,
SM-S928B, 1080×2340 @ dpi 450 = 384×832 dp) — also auf einer anderen Groesse als der
Emulator der ersten Runde, was besonders fuer die layoutabhaengigen Fixes aussagekraeftig ist:

| Pruefpunkt | Ergebnis |
|---|---|
| Typ-Sheet „Fertig" | vollstaendig, `T=1952 B=2104` bei 2340 Bildschirmhoehe |
| Sortier-Sheet, alle 8 Optionen | vollstaendig, letzte endet bei `2104` |
| Trefferzahl vs. Karten-Pille | „11 Objekte" `1949–2003`, Pille `2036–2149` — kein Ueberlapp |
| „Kopiert!" nach Kopieren in der Kontaktleiste | nur **einmal**, in der Kontaktleiste; unter der Adresse nichts mehr |
| Bildbetrachter | Titelleiste ausgeblendet, kehrt nach Zurueck-Taste **und** X-Knopf zurueck |
| Karte offline | „Bitte **prüfen** Sie Ihre Verbindung" |
| Startseite offline mit Cache | Liste **und** Hinweis, kein Fehlerzustand |
| Startseite wieder online | kein Hinweis, Liste laedt |
| Wizard: leerer Titel | „Bitte geben Sie einen Titel für Ihr Inserat ein." |
| Wizard: Titel „Haus" | „Titel muss mindestens 10 Zeichen lang sein" |

Screenshots: `shots/dev-01-*` bis `shots/dev-10-*` (Geraet), `shots/s4-01-*` bis `shots/s4-08-*` (Emulator).
Der beim Validierungstest entstandene Entwurf wurde wieder geloescht — die Test-Daten bleiben sauber.

### B-02 — Nachweis

Feste Detent-Anteile waren auf 800-dp-Geraeten schlicht zu knapp bemessen: Das Typ-Sheet braucht
rund 556 px, bekam bei `0.36` aber nur ~455 px. `FitContent` misst den Inhalt und rechnet den Detent
selbst aus — unabhaengig von Geraetegroesse und Schriftskalierung.

| Element | vorher | nachher |
|---|---|---|
| Typ-Sheet „Fertig" | `T=1461 B=1504` → **43 px, angeschnitten** | `T=1324 B=1432` → **108 px, vollstaendig** |
| Sortier-Sheet, letzte Option „PLZ absteigend" | `T=1481 B=1504` → **23 px, angeschnitten** | `T=1340 B=1432` → **92 px, vollstaendig** |
| Zeitraum-Sheet, letzte Option „Letztes Jahr" | (war ebenfalls zu knapp bemessen) | `T=1340 B=1432` → vollstaendig |

Alle drei enden jetzt deutlich oberhalb der Navigationsleiste (~1500 px).
Screenshots: `shots/fix-01-typsheet.png`, `shots/fix-02-sortiersheet.png`, `shots/fix-03-zeitraumsheet.png`

Das **Ort-Panel** bleibt bewusst bei einem festen Anteil (`0.75`): seine Liste ist beliebig lang und
scrollt selbst — am Inhalt bemessen waere es immer bildschirmfuellend.

### B-04 — Nachweis

`IsShowingCachedData` meldet nur „Backend nicht erreichbar", nicht „es sind Daten da". Der Hinweis
liegt im `CollectionView.Header` und wurde deshalb auch ueber dem leeren Fehlerzustand gerendert.
Zusaetzlich wurde `IsEmpty` im Fehlerpfad gar nicht nachgefuehrt (`UpdateResultCount()` lief nur im
Erfolgsfall), blieb beim ersten Laden also auf `false`.

Beide Zustaende nachverifiziert:

| Fall | Erwartet | Ergebnis |
|---|---|---|
| Offline **ohne** Cache (Neuinstallation, Kaltstart ohne Netz) | nur Fehlerzustand mit „Erneut versuchen" | ✅ Banner ist weg, nur der Fehlerzustand — `shots/fix-05-offline-ohne-cache.png` |
| Offline **mit** Cache (vorher online geladen, dann Netz aus + Neustart) | Liste **und** Banner | ✅ beides da — `shots/fix-06-offline-mit-cache.png` |
| Online | kein Banner | ✅ |

Der Banner haengt bewusst **nicht** an `HasNoLoadError`: bleibt bei einem Backend-Ausfall ein zuvor
geladener Stand sichtbar, ist zwar eine Fehlermeldung gesetzt, der Hinweis ueber der Liste ist dann
aber genau richtig — die Fehlermeldung selbst steckt in der `EmptyView` und bleibt in dem Fall
unsichtbar.

### Randnotiz zum ANR

Beim Verifizieren trat einmalig ein ANR auf (Neuinstallation + Kaltstart ohne Netz, 11,7 s
blockierter Main-Thread). Der Emulator brach kurz darauf komplett weg — er war zu dem Zeitpunkt
bereits instabil (kurz zuvor `adb root`). Nach Neustart des Emulators liess sich derselbe Ablauf mit
demselben Build **nicht** reproduzieren: Kaltstart ohne Netz laeuft sauber in den Fehlerzustand.
Wird als Umgebungsproblem eingestuft, nicht als App-Befund.

---

## Befunde

### B-01 · S2 · Karte — blockierte Immobilien erscheinen weiterhin auf der Karte

- **Schritte:**
  1. Als `test.buyer` anmelden (hat „Großes Baugrundstück Mühlviertel" blockiert).
  2. Startseite, Typ-Filter „Haus · Grund" → Liste meldet am Ende **„10 Objekte"**.
  3. Karten-Pille antippen → Kopfzeile meldet **„11 Inserate auf der Karte"**.
  4. Auf den Marker-Cluster im Muehlviertel zoomen → Preisschild **„€ 95.000"** antippen.
- **Erwartet:** Die Karte zeigt dieselbe Treffermenge wie die Liste; blockierte Objekte sind ausgeblendet.
- **Tatsaechlich:** Der Mini-Zettel oeffnet „Großes Baugrundstück Mühlviertel, 4240 Freistadt, € 95.000" — genau das blockierte Objekt. Ueber den Pfeil-Button laesst sich die native Detailseite oeffnen; dort ist das Blockier-Symbol korrekt rot/aktiv. Die App kennt den Zustand also, nur die Karte ignoriert ihn.
- **Screenshots:** `shots/67-karte-nach-warten.png` (11 Inserate), `shots/64-villa-bild.png` (10 Objekte), `shots/69-karte-minizettel.png`, `shots/70-karte-detail-intercept.png`
- **Serverseitig gegengeprueft:**
  - `GET /api/properties/map-pins?PropertyTypesJson=[1,2]` **mit** Buyer-Token → 10 Pins, ohne Muehlviertel
  - dieselbe Anfrage **ohne** Token → 11 Pins, **mit** Muehlviertel
  - `GET /api/properties?...` mit Buyer-Token → `Total: 10`
  Die API verhaelt sich also korrekt; die Karte fragt ohne Anmeldung ab.
- **Vermutete Stelle:** `src/maui/src/Heimatplatz.Maui/Features/Properties/Presentation/PropertyMapViewModel.cs:70-76` — die Karte ist ein WebView auf `/karte-embed`; `MapEmbedLink.BuildQuery(...)` uebergibt nur Filter und Theme, keinerlei Benutzerkontext/Token.
- **Reproduzierbar:** ja

---

### B-02 · S3 · Bottom-Sheets — primaere Aktion beim Oeffnen abgeschnitten

- **Schritte:** Startseite → Chip „Typ" antippen.
- **Erwartet:** Der Bestaetigen-Button „Fertig" ist vollstaendig sichtbar.
- **Tatsaechlich:** Beim Oeffnen ist „Fertig" auf **43 px Hoehe** beschnitten (Bounds `[40,1461]–[680,1504]`, Bildschirm 1600 px, Navigationsleiste ab ~1500). Erst nach einem Wisch im Sheet ist der Button vollstaendig da (**108 px**, `[40,1364]–[680,1472]`). Dasselbe Muster im Sortier-Sheet: die letzte Option „PLZ absteigend" ist beim Oeffnen abgeschnitten (`T=1481, B=1504`).
- **Screenshots:** `shots/25-typ-sheet.png` (angeschnitten), `shots/26-typ-sheet-gescrollt.png` (nach Scrollen vollstaendig), `shots/61-sortier-sheet.png`
- **Vermutete Stelle:** Detent-Hoehe der Sheets vs. Inhaltshoehe auf 800-dp-Geraeten — passt zur bekannten Falle „XAML-Detents addieren zu Defaults" (Shiny-FloatingPanel).
- **Reproduzierbar:** ja

---

### B-03 · S3 · Karte — rund 10–15 s leere Flaeche ohne Ladeanzeige

- **Schritte:** Startseite → Karten-Pille antippen.
- **Erwartet:** Sichtbarer Ladezustand, bis Kacheln und Marker da sind.
- **Tatsaechlich:** Nach ~5 s zeigt die Seite nur eine leere beige Rasterflaeche mit dem Stempel „OBERÖSTERREICH" — keine Kacheln, keine Marker, kein Spinner. Erst nach insgesamt ~15 s erscheint die vollstaendige Karte. Der Zustand ist von einem Fehler nicht zu unterscheiden.
- **Screenshots:** `shots/66-karte.png` (nach ~5 s), `shots/67-karte-nach-warten.png` (fertig)
- **Vermutete Stelle:** `PropertyMapViewModel.OnWebNavigated` setzt `IsLoading = false`, sobald die WebView-Navigation abgeschlossen ist; Kacheln und Pins laden danach noch asynchron weiter.
- **Reproduzierbar:** ja

---

### B-04 · S3 · Offline-Startseite — zwei sich widersprechende Meldungen gleichzeitig

- **Schritte:** Als Verkaeufer angemeldet, `svc wifi disable` + `svc data disable`, Startseite oeffnen.
- **Erwartet:** Eine eindeutige Aussage.
- **Tatsaechlich:** Oben steht das Banner **„Offline – angezeigt wird der zuletzt gespeicherte Stand."**, darunter gleichzeitig **„Laden fehlgeschlagen … Diese Inhalte wurden noch nicht lokal gespeichert – sobald Sie wieder online sind, klappt es."** Es wird ueberhaupt keine Liste angezeigt, das Banner ist also sachlich falsch.
- **Screenshot:** `shots/103-offline-home.png`
- **Reproduzierbar:** ja
- **Positiv daneben:** Favoriten und Karte zeigen offline jeweils einen sauberen Einzelzustand mit „Erneut versuchen"; kein Endlos-Laden, kein Dialog.

---

### B-05 · S4 · Detailseite — „Kopiert!" erscheint doppelt, einmal an falscher Stelle

- **Schritte:** Detailseite → „Kontakt" ausklappen → bei Telefon oder E-Mail auf „Kopieren" tippen.
- **Erwartet:** Eine Rueckmeldung, an der Stelle der Aktion.
- **Tatsaechlich:** „Kopiert!" erscheint **zweimal gleichzeitig** — korrekt in der Kontaktleiste (y≈1460) und zusaetzlich im Hauptinhalt direkt **unter der Adresse** (y≈1063), wo gar nichts kopiert wurde.
- **Screenshots:** `shots/51-kopieren-sofort.png`, `shots/52-kopiert-doppelt-email.png`
- **Reproduzierbar:** ja (mit Telefon- und E-Mail-Zeile)

---

### B-06 · S4 · Bildbetrachter — linker Navigationspfeil am Bildschirmrand angeschnitten

- **Schritte:** Detailseite mit mehreren Bildern → Bild antippen (Großansicht).
- **Erwartet:** Beide Pfeile symmetrisch und vollstaendig sichtbar.
- **Tatsaechlich:** Der linke Pfeil „‹" klebt am linken Rand und ist sichtbar angeschnitten; der rechte Pfeil „›" hat Abstand und ist vollstaendig. Funktionieren tun beide.
- **Screenshots:** `shots/46-bildbetrachter.png`, `shots/46b-bildbetrachter-erneut.png`
- **Reproduzierbar:** ja

---

### B-07 · S4 · Bildbetrachter — Overlay deckt die Titelleiste nicht ab

- **Tatsaechlich:** In der Großansicht bleiben Titelleiste mit Zurueck-Pfeil und Inseratstitel sichtbar, ebenso ein heller Streifen unterhalb der schwarzen Flaeche. Die Ansicht wirkt dadurch nicht wie eine Vollbild-Lightbox.
- **Screenshot:** `shots/46b-bildbetrachter-erneut.png`
- **Reproduzierbar:** ja

---

### B-08 · S4 · Karte offline — fehlender Umlaut in „pruefen"

- **Tatsaechlich:** „Die Kartenansicht braucht eine Internetverbindung. Bitte **pruefen** Sie Ihre Verbindung und versuchen Sie es erneut." — im UI sichtbar, kein Dump-Artefakt.
- **Screenshot:** `shots/104-offline-karte.png`
- **Stelle:** `src/maui/src/Heimatplatz.Maui.Localization/Properties/PropertyMapStrings.resx:25`
- **Reproduzierbar:** ja
- **Hinweis:** Ein Suchlauf ueber alle `*.resx` zeigt, dass dies die **einzige** betroffene Stelle ist; alle anderen Texte verwenden korrekte Umlaute.

---

### B-09 · S4 · Startseite — Trefferzahl am Listenende wird von der Karten-Pille verdeckt

- **Tatsaechlich:** Am Ende der Liste steht „10 Objekte" genau hinter der schwebenden „Karte"-Pille und ist dadurch nur teilweise lesbar.
- **Screenshot:** `shots/64-villa-bild.png`
- **Reproduzierbar:** ja

---

### B-10 · S4 · Inserats-Editor — irrefuehrende Meldung bei leerem Titel

- **Schritte:** „Neu" → sofort „Inserat veröffentlichen".
- **Tatsaechlich:** Meldung „Titel muss mindestens 10 Zeichen lang sein", obwohl das Feld komplett leer ist. Passender waere „Bitte geben Sie einen Titel ein". Zusaetzlich ueberlagert das Meldungs-Banner die Oberkante des Foto-Bereichs.
- **Screenshot:** `shots/87-wizard-leer-veroeffentlichen.png`
- **Reproduzierbar:** ja

---

## Hinweise und Fragen (keine Bugs)

> **Nachtrag 28.07. — Entscheidungen getroffen und umgesetzt** (verifiziert am Galaxy S24 Ultra):
>
> | Hinweis | Entscheidung | Umsetzung |
> |---|---|---|
> | **H-01** Rueckfrage beim Favoriten-Entfernen | Rueckfrage entfernen | `FavoritesViewModel.ConfirmBeforeRemove => false` — entfernt jetzt wie die Blockiert-Seite ohne Dialog (Server-Gegenprobe 4→3, Testdaten danach wiederhergestellt) |
> | **H-02** Lasten auf der ZV-Detailseite | einbauen | Eigene „Lasten"-Karte zwischen Beschreibung und Datenblatt: Bezeichnung mit Glaeubiger-Zweitzeile (entfaellt, wenn schon im Namen enthalten), Betraege rechtsbuendig, Summe unter Haarlinie ab zwei bezifferten Posten. `shots/h-03-lasten-karte.png` |
> | **H-03** „GEBÄUDE" bei ZV-Grundstueck | passt so | keine Aenderung |
> | **H-04** Flyout-Eintrag unter der Falz | nicht machen | keine Aenderung |
> | **H-05** Theme-Umschalter ohne sichtbare Wirkung | Modusnamen einblenden | Glass-Pille mit Icon + „Design: hell/dunkel/System" schwebt nach jedem Tipp kurz ueber dem Hero (Fade-in 140 ms, 1,2 s stehen, Fade-out 320 ms); nur bei echtem Tipp, nicht beim Seitenaufbau. `shots/h-09-…` (hell) / `h-10-…` (dunkel) |
> | **H-06** Gericht doppelt | ist okay so | keine Aenderung |
> | **H-07** AutomationIds auf Auth-Seiten | machen | 16 IDs auf Anmelden (`Login_*`), Registrieren (`Register_*`) und Passwort vergessen (`ForgotPassword_*`); am Geraet per UI-Dump verifiziert |

- **H-01 — Favorit entfernen fragt nach, Blockierung aufheben nicht.** Auf der Favoriten-Seite erscheint „Favorit entfernen? / Möchten Sie ‚…' wirklich aus Ihren Favoriten entfernen?" mit Nein/Ja; auf der Blockiert-Seite wird ohne Rueckfrage entfernt. Im Code ist das bewusst so (`PropertyCollectionViewModelBase.ConfirmBeforeRemove => true`, nur `BlockedViewModel` ueberschreibt auf `false`). Angesichts der Vorgabe „keine Rueckfragen bei umkehrbaren Aktionen" waere Gleichbehandlung konsequenter — beides ist mit einem Tipp wiederherstellbar. Screenshot: `shots/37-favorit-entfernen.png`
- **H-02 — ZV-Detail zeigt die Lasten nicht.** Die API liefert zu „Grundstück Enns" `Encumbrances` (Hypothek Bank Austria € 34.000, Grundsteuer € 2.500). Auf der Detailseite kommen sie an keiner Stelle vor. Fuer ZV-Interessenten ist das eine wesentliche Information — bewusst dem Edikt ueberlassen oder Luecke?
- **H-03 — ZV-Grundstueck zeigt einen Abschnitt „GEBÄUDE".** Bei „Grundstück Enns" erscheint „GEBÄUDE / Zustand: Renovierungsbedürftig", obwohl es ein Grundstueck ohne Bebauung ist. Ursache sind die **Seed-Daten** (`BuildingArea: 0`, aber `BuildingCondition: "Renovierungsbedürftig"`); die App gibt nur wieder, was sie bekommt. Fix entweder in den Seed-Daten oder durch Ausblenden des Abschnitts bei `BuildingArea = 0`.
- **H-04 — „Immobilie hinzufügen" liegt im Flyout unter der Falz.** Auf 800 dp Hoehe ist der Eintrag erst nach Scrollen sichtbar, ohne Scroll-Hinweis. Betrifft nur Builds mit Debug-Eintrag (9 Eintraege); in der Store-Version duerfte es sich ausgehen. Screenshots: `shots/03-flyout.png`, `shots/04-flyout-scrolled.png`
- **H-05 — Theme-Umschalter: erster Tipp ohne sichtbare Wirkung.** `CycleMode()` geht System → Hell → Dunkel. Steht das System auf Hell, aendert der erste Tipp nur das Icon, nicht die Darstellung. Screenshots: `shots/76-profil-kaeufer.png`, `shots/77-profil-dark.png`, `shots/78-theme-zweiter-tap.png`
- **H-06 — Gericht doppelt auf der ZV-Detailseite.** „Bezirksgericht Steyr" steht sowohl in der Tabelle unter „RECHTLICHES" als auch dauerhaft in der Fussleiste „ZUSTÄNDIGES GERICHT".
- **H-07 — Anmelde- und Registrierungsseite haben keine AutomationIds.** Alle uebrigen getesteten Seiten sind gut adressierbar (`Home_*`, `Flyout_*`); auf den Auth-Seiten musste ueber Koordinaten getestet werden. Reine Testbarkeit, fuer Nutzer ohne Wirkung.

---

## Einmalig beobachtet (nicht reproduzierbar)

- **E-01 — Nach erfolgreicher Anmeldung blieb die App auf der Anmeldeseite.** Beim ersten Login (Weg: Meine Immobilien → „Neu" → Anmeldeseite, davor drei Fehlversuche: leer, ungueltige Adresse, falsches Passwort) wurde das Formular geleert und keine Fehlermeldung gezeigt — die Debug-Seite bestaetigte aber „Aktuell: Käufer — test.buyer@heimatplatz.dev". Die Navigation auf die Startseite (`LoginViewModel.cs:129`) unterblieb also. **Drei gezielte Reproduktionsversuche** ueber denselben Weg inklusive vorherigem Fehlversuch schlugen fehl — dort erschien jedes Mal korrekt der Ladehinweis „Anmeldung wird durchgeführt…" und danach die Startseite. Screenshots: `shots/15-login-erfolgreich.png` (Befund), `shots/19-…`/`shots/20-…` (korrekter Ablauf)
- **E-02 — Startseite zeigte einmal den ZV-Filter statt „Haus · Grund".** Unmittelbar nach einem Theme-Wechsel stand der Typ-Chip auf „ZV" und die Liste zeigte nur ZV-Objekte, obwohl zuvor und danach „Haus · Grund" aktiv war. Gezielte Reproduktion ueber den Theme-Wechsel und ueber den Besuch der Benachrichtigungs-Seite (beides Verdachtskandidaten) blieb je ohne Befund — der Filter ueberlebte beide Wege korrekt. Screenshot: `shots/79-home-dark.png`

---

## Geprueft und in Ordnung

### Start, Navigation, Querschnitt

| Funktion | Ergebnis |
|---|---|
| Kaltstart aus dem Launcher (Neuinstallation) | ✅ Splash → Startseite mit Daten |
| Warmstart aus dem Hintergrund | ✅ stellt die Startseite wieder her |
| Zurueck-Taste auf der Startseite | ✅ App geht in den Hintergrund, Prozess lebt weiter — kein unerwartetes Beenden |
| Zurueck-Taste auf jeder Detailebene | ✅ jeweils eine Ebene zurueck |
| Flyout: alle 9 Eintraege + Impressum-/Datenschutz-Links vorhanden | ✅ |
| Umgebungs-Pille „Entwicklung · Test-API" + Version 1.83.0 | ✅ |
| Light-Theme | ✅ |
| Dark-Theme (Startseite, Favoriten, Profil, Statusleiste, Navigationsleiste) | ✅ Kontraste durchgehend gut |
| Deep-Link `heimatplatz://property/{guid}` | ✅ oeffnet die Detailseite |
| Deep-Link `heimatplatz://foreclosure/{guid}` | ✅ oeffnet die ZV-Detailseite |
| Logcat ueber den gesamten Lauf | ✅ keine App-Exception, kein Crash |

> Anmerkung zu den Deep-Links: `property/{guid}` mit einer ZV-GUID landet auf der allgemeinen Detailseite statt auf der ZV-Seite. Das ist **kein Fehler** — es sind zwei getrennte Hosts, und die Teilen-Funktion erzeugt ohnehin Web-Links (`WebLinks.ListingUrl`), keine Deep-Links. Es gibt also keinen Pfad, auf dem die App selbst einen falschen Link erzeugt.

### Gast (nicht angemeldet)

| Funktion | Ergebnis |
|---|---|
| Startseite, Liste, Karten | ✅ |
| Favoriten / Blockiert / Meine Immobilien / Benachrichtigungen / Feedback | ✅ je „Nicht angemeldet" + „Anmelden", Text passend zur Seite, Sie-Form |
| „Neu" auf Meine Immobilien als Gast | ✅ leitet auf die Anmeldeseite, kein Fehlerzustand |
| Profil als Gast | ✅ leitet auf die Anmeldeseite |
| Anmeldung leer abschicken | ✅ „Bitte geben Sie Ihre E-Mail-Adresse ein." |
| Anmeldung mit `kaputte-adresse` | ✅ „Bitte geben Sie eine gültige E-Mail-Adresse ein." |
| Anmeldung mit falschem Passwort | ✅ „E-Mail-Adresse oder Passwort ist falsch." — verraet nicht, ob das Konto existiert |
| Anmeldung korrekt | ✅ Ladehinweis → Startseite |
| Registrierung: Seite, Felder, Verkaeufer-Schalter | ✅ |
| Registrierung leer abschicken | ✅ „Bitte geben Sie Ihren Vornamen ein." |
| Passwort vergessen | ✅ Seite mit neutraler Formulierung („Wenn ein Konto existiert …") |

### Suche und Filter

| Funktion | Ergebnis |
|---|---|
| Typ-Filter Haus / Grundstueck / Zwangsversteigerung | ✅ wirkt sofort, Chip-Beschriftung folgt („Haus · Grund", „ZV", „Grund · ZV") |
| ZV standardmaessig **aus** | ✅ entspricht der Vorgabe |
| Sortierung: alle 8 Optionen vorhanden, aktive mit Haken | ✅ |
| Sortierung „Preis aufsteigend" | ✅ 189.000 → 245.000 → 275.000 → 289.000 → 298.000 → 315.000 → 349.000 → 365.000 |
| Trefferzahl „10 Objekte" | ✅ stimmt mit der API ueberein (11 Haus+Grund minus 1 blockiertes) |
| Sticky-Kopf klappt beim Scrollen auf ein Filter-Symbol zusammen | ✅ |
| Bilder laden nach (Platzhalter-Logo waehrend des Ladens) | ✅ kein Fehler, Bild erscheint |

### Detailseiten (Soll: `usertests/PropertyDetailPage_Testfaelle.md`)

| Testfall | Ergebnis |
|---|---|
| **TC-PD-001 Grundstueck** — „Sonniges Baugrundstück Linz-Land" | ✅ nur Kachel „720 m² GRUNDSTÜCK"; **keine** Wohnflaeche, **keine** Zimmer, **kein** Baujahr |
| **TC-PD-002 Haus** — „Einfamilienhaus in Linz-Urfahr" | ✅ Wohnflaeche 145 m², Grundstueck 520 m², Zimmer 5; in der Tabelle Baujahr 2018, Zustand, Schlafzimmer 3, Badezimmer 2, Stockwerke 1, Ausstattung; **Preis/m² € 2.407** (349.000 / 145 = 2.407 ✓) |
| **TC-PD-003 ZV Grundstueck** — „Grundstück Enns" | ✅ keine Zimmer, kein Baujahr (Abschnitt „GEBÄUDE/Zustand" siehe H-03) |
| **TC-PD-004 ZV Haus** — „Haus in Traun" | ✅ Zimmer 4, Baujahr 1985, bebaute Flaeche 110 m² |
| ZV: „Mindestgebot", Termin, Schaetzwert, **Zuständiges Gericht**, Aktenzeichen, Edikt-Link | ✅ alle vorhanden |
| Preisformat `€ 349.000` / `€ 47.600` / `1.200 m²` | ✅ durchgehend korrekt |
| Bildergalerie: 3× vorwaerts, 3× rueckwaerts, Anschlag an beiden Enden | ✅ Zaehler 1/3 → 3/3 → 1/3, kein Verspringen (bekannter Wackelkandidat behoben) |
| Großansicht: oeffnen, vor/zurueck, Zaehler, Zurueck-Taste schliesst nur das Overlay | ✅ Galerie uebernimmt danach die Position (2/3) |
| Kontaktleiste aus-/einklappen, E-Mail und Telefon inkl. Symbolen | ✅ |
| „Kopieren" fuer Telefon und E-Mail | ✅ Funktion arbeitet (Rueckmeldungs-Darstellung siehe B-05) |
| Favorit setzen auf der Detailseite | ✅ Herz wird rot |
| Blockier-Zustand auf der Detailseite | ✅ korrekt aktiv dargestellt |

### Sammlungen

| Funktion | Ergebnis |
|---|---|
| Favorit auf Detailseite setzen → Favoritenliste zieht sofort nach | ✅ (LocalFirst-Cache greift korrekt) |
| Favorit aus der Liste entfernen (mit Bestaetigung) | ✅ verschwindet sofort, naechster Eintrag rueckt nach |
| Blockierung aufheben (ohne Bestaetigung) | ✅ verschwindet aus „Blockiert" |
| … und Objekt taucht wieder in der Startseiten-Liste auf | ✅ „Einfamilienhaus in Linz-Urfahr" war zurueck |
| Profil-Zaehler „4 Favoriten / 1 Blockiert" | ✅ stimmt mit den durchgefuehrten Aktionen ueberein |

### Benachrichtigungen

| Funktion | Ergebnis |
|---|---|
| Hauptschalter einschalten | ✅ loest die Android-Berechtigungsabfrage aus |
| Berechtigung erteilen | ✅ Einstellungen klappen auf |
| Filtermodus (Alle / Wie Suchfilter / Benutzerdefiniert) | ✅ Auswahl sichtbar |
| Immobilientyp- und Anbietertyp-Auswahl, Ortssuche | ✅ vorhanden, „Makler & Verwaltung" korrekt dargestellt |
| Hinweis „Ihre Einstellungen werden automatisch gespeichert" | ✅ folgerichtig kein Speichern-Knopf |

### Verkaeufer und Inserieren

| Funktion | Ergebnis |
|---|---|
| Meine Immobilien listet die 3 eigenen Objekte | ✅ deckt sich mit `GET /api/properties/user` |
| Editor oeffnet im Detailseiten-Look mit Platzhaltern | ✅ |
| Live-Checkliste „Noch offen: …" | ✅ aktualisiert sich nach jeder Eingabe |
| Veroeffentlichen mit leerem Formular | ✅ wird blockiert |
| Foto aus der Galerie | ✅ Android-Fotoauswahl, Bild uebernommen, Zaehler 1/1 |
| Preis, Titel, Strasse | ✅ |
| Ortssuche „Wels" | ✅ Vorschlaege „Wels (4600)", „Pichl bei Wels (4632)", „Thalheim bei Wels (4600)", Auswahl uebernommen |
| Beschreibung „Selbst schreiben" mit < 50 Zeichen | ✅ „Die Beschreibung muss mindestens 50 Zeichen lang sein." |
| Vollstaendige Beschreibung | ✅ Checkliste wird zu „Alles komplett – Ihr Inserat kann online gehen" |
| Veroeffentlichen | ✅ Inserat erscheint in Meine Immobilien (€ 389.000, 4600 Wels, „Fläche k.A." als Fallback) |
| Oeffentliche Sichtbarkeit gegengeprueft | ✅ `GET /api/properties` zeigt „Testhaus Funktionstest 27.07", 1 Bild, PLZ 4600 |
| Inserat loeschen (mit Bestaetigung „Immobilie löschen?") | ✅ verschwindet aus der Liste |
| Loeschung serverseitig gegengeprueft | ✅ oeffentliche Anzahl zurueck von 12 auf 11, Objekt weg |
| Weitere Editor-Bausteine sichtbar: Lage-Anzeige (Ungefaehr/Genau/Nicht anzeigen), Anbieter aus dem Profil, Ansprechpartner, Baujahr, Merkmale, Link zum Originalinserat | ✅ vorhanden |

> **Testdaten sauber hinterlassen:** Das angelegte Inserat wurde wieder geloescht. Veraendert bleibt lediglich, dass `test.buyer` „Grundstück Enns" nun als Favorit hat, „Baugrundstück in Wels" nicht mehr, und „Einfamilienhaus in Linz-Urfahr" nicht mehr blockiert ist — alles Folge der oben protokollierten Tests.

---

## Nicht getestet / nicht testbar

- **Feedback-Bereich** (neue Anfrage mit Bild und Sprachnachricht, Thread lesen, Umbenennen) — der Gast-Zustand wurde geprueft, der angemeldete Ablauf aus Zeitgruenden nicht.
- **KI-Beschreibung („Erstellen lassen") und Diktat-Feld** im Inserats-Editor — Modus-Auswahl gesehen, Generierung nicht ausgeloest.
- **Kamera-Aufnahme** („Aufnehmen") — nur die Galerie-Auswahl getestet.
- **Bearbeiten eines bestehenden Inserats** und **Entwuerfe fortsetzen** — die Test-DB enthielt keine Entwuerfe (`/api/property-drafts/` leer), angelegt wurde nur ein neues Inserat.
- **Filtereinstellungen-Seite**, **Ort-Panel** und **Zeitraum-Filter** — auf der Startseite nur die Chips selbst geprueft, nicht die vollen Panels.
- **Impressum / Datenschutz** — Links im Flyout-Fuss vorhanden, Inhalte nicht geoeffnet.
- **Push-Zustellung und Push-Deep-Link** — Einstellungen und Berechtigung geprueft, echter Versand nicht (braucht Serverseite).
- **Rollen Makler und Hausverwaltung** — nur Kaeufer und privater Verkaeufer durchlaufen.
- **Querformat** — laut Vorgabe ausdruecklich kein Testfall.
- **Eigener Emulator auf eigenem Port** — nicht moeglich, siehe „Vorsorge gegen Falschbefunde"; ersatzweise eigener frischer Build auf `Phone_API35`.
