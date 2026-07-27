---
name: funktionstest
description: Vollstaendiger manueller Funktionstest der Heimatplatz-App (Astro-Web in src/web und/oder .NET-MAUI-App in src/maui) — erst das komplette Funktionsinventar aus dem Code erheben, dann jede einzelne Funktion wie ein akribischer menschlicher Tester in der laufenden App durchklicken, Bugs/UI-/UX-Auffaelligkeiten in einem Markdown-Bericht protokollieren und das Ergebnis melden. Use for functional test, QA run, Testdurchlauf, Funktionstest, Regressionstest, "teste die App", "geh alles durch", "such Bugs".
---

# Funktionstest Heimatplatz

Ein kompletter Testdurchlauf wie ihn ein Mensch machen wuerde: nicht Code lesen und daraus
schliessen, sondern die **laufende App bedienen** und jede Funktion einzeln ausprobieren.
Code wird nur benutzt, um zu wissen **was es alles gibt** und um einen Befund einzuordnen.

**Grundhaltung:** akribisch, misstrauisch, vollstaendig. Lieber ein Befund zu viel als einer zu
wenig. Nichts als "passt schon" abhaken, was nicht wirklich angesehen wurde.

---

## Ablauf

### Phase 0 — Auftrag klaeren (kurz)

Aus dem Auftrag ableiten, nur bei echter Mehrdeutigkeit nachfragen:

- **Plattform:** Web, MAUI oder beides? (ohne Angabe: beides, Web zuerst)
- **Ziel-Umgebung:** Standard ist **Test** (`test.heimatplatz.at` / `test-api.heimatplatz.at`).
  Prod nur lesend testen — keine Registrierungen, Inserate, Feedbacks oder Loeschungen auf Prod.
- **Umfang:** kompletter Durchlauf (Default) oder nur ein Bereich.

Danach **sofort** pruefen, welcher Stand ueberhaupt getestet wird:

```
git status --short && git log --oneline -3 && git worktree list
```

Bei MAUI zusaetzlich: laeuft evtl. ein Build aus einem fremden Worktree? (siehe `references/maui.md`)
Ein Befund gegen einen fremden Branch ist ein Falschbefund.

### Phase 1 — Funktionsinventar erheben

**Vor dem ersten Klick** eine vollstaendige Liste aller Funktionen erstellen. Quellen:

**Web (`src/web`)**
- `src/web/src/pages/**` — jede Route, auch `[id]`/`[slug]`, `/intern/**` und die `.ts`-Endpoints
- `src/web/src/features/**` — fachliche Slices (Suche, Properties, Auth, Feedback, Marketing …)
- `src/web/src/components/**` — interaktive Komponenten (Formulare, Dialoge, Picker, Karte)
- Navigation/Layout (`layouts/`, Header/Footer/Flyout) — jeder Link ist ein Testpunkt

**MAUI (`src/maui/src/Heimatplatz.Maui`)**
- `Features/*/Presentation/*Page.xaml` — jede Seite
- die zugehoerigen `*ViewModel.cs` — jedes `[RelayCommand]` ist eine Funktion
- `AppShell`/Flyout-Eintraege, `FlyoutMenuEntry.cs` — jeder Menuepunkt
- `Core/DeepLink*`, Push-Handler — Einstiege von aussen

Das Inventar in eine Tabelle bringen (Bereich → Funktion → Vorbedingung/Rolle). Dabei bewusst
mitnehmen, was leicht vergessen wird:

- **Rollen/Zustaende:** Gast, eingeloggter Kaeufer, Verkaeufer (privat), Makler, Verwaltung, Admin
- **Datenzustaende:** Liste mit Treffern / leere Liste / genau 1 Treffer / sehr viele Treffer
- **Objekt-Typen:** Haus, Grundstueck, Zwangsversteigerung (jeweils eigene Feld-Sichtbarkeit,
  siehe `usertests/PropertyDetailPage_Testfaelle.md`)
- **Fehlerzustaende:** ungueltige Eingabe, abgelaufene Session, offline, 404-Route, fremdes Objekt
- **Darstellung:** Light + Dark, schmal + breit (Web: Mobile 390px / Desktop 1440px)

Das Inventar ist die Checkliste — am Ende muss jede Zeile ein Ergebnis haben.

### Phase 2 — Umgebung aufsetzen

Jetzt die passende Referenz lesen und die dort beschriebene Umgebung herstellen:

- Web → `references/web.md`
- MAUI → `references/maui.md`

Beide Dateien enthalten Setup, Testbenutzer und die bekannten Fallen, die schon mehrfach zu
Falschbefunden gefuehrt haben. **Diese Fallen vor dem Testen abhaken**, nicht danach.

### Phase 3 — Testen wie ein Mensch

Das Inventar von oben nach unten durchgehen. Pro Funktion:

1. **Hinschauen, bevor geklickt wird.** Was steht da? Stimmen Beschriftung, Format, Abstand,
   Ausrichtung, Zustand der Buttons?
2. **Den normalen Weg gehen** — so wie ein Nutzer es taete, mit realistischen Eingaben.
3. **Danebengreifen** — leeres Formular abschicken, Unsinn eintippen (Text im Zahlenfeld,
   Preis 0, Preis 99999999999, E-Mail ohne @, sehr langer Text, Sonderzeichen/Umlaute),
   doppelt klicken, waehrend des Ladens nochmal klicken.
4. **Zurueck und wieder hin** — Browser-Zurueck bzw. Geraete-Zurueck, Seite neu laden,
   Scrollposition und Formularinhalte pruefen, Deep-Link direkt aufrufen.
5. **Zustand pruefen** — hat die Aktion wirklich gewirkt? An anderer Stelle nachsehen
   (Liste, Detail, Favoriten, andere Plattform), nicht nur dem Toast glauben.

Verbindliche Regeln:

- **Nie aus dem Code schliessen, dass etwas funktioniert.** Nur was gesehen wurde, gilt als
  geprueft. Code dient zur Erklaerung eines Befunds, nicht als Beweis.
- **Jeden Befund einmal reproduzieren**, bevor er in den Bericht kommt. Nicht reproduzierbar →
  trotzdem notieren, aber als "einmalig beobachtet" markieren.
- **Nicht reparieren waehrend des Tests.** Erst alles finden, dann berichten. Fixes nur, wenn
  ausdruecklich beauftragt — und dann in einer eigenen Runde danach.
- **Screenshot bei jedem visuellen Befund** und bei jedem wichtigen Zustand, auch wenn er ok ist.
- **Konsole und Netzwerk mitlaufen lassen** (Web: Console + Network + CSP-Check; MAUI: `adb logcat`).
  Ein sauberer Screen mit roter Konsole ist ein Befund.
- Bei blockierenden Problemen (App startet nicht, Login unmoeglich) nicht endlos probieren:
  maximal 2–3 Anlaeufe, dann als Befund notieren, Bereich als "nicht testbar" markieren und
  **mit dem Rest weitermachen**.

**Zusaetzlich immer auf UI/UX achten** (das ist Teil des Auftrags, kein Extra):

| Bereich | Worauf achten |
|---|---|
| Layout | Ueberlauf, abgeschnittener Text, gequetschte Buttons, Umbrueche bei langen Namen/Orten, ungleiche Abstaende, verrutschte Ausrichtung |
| Zustaende | Ladezustand sichtbar? Button waehrend Laden gesperrt? Leerzustand mit Text statt weisser Flaeche? Fehlerzustand mit Wiederholen-Moeglichkeit? |
| Rueckmeldung | Passiert nach jedem Klick sichtbar etwas? Toast/Meldung verstaendlich, auf Deutsch, in Sie-Form? |
| Formate | Preis `€ 520.000` (mit schmalem NBSP), Flaechen `m²`, Datum deutsch, keine `NaN`/`undefined`/`0`-Platzhalter |
| Texte | Deutsch, keine Platzhalter/Resource-Keys, keine englischen Restwoerter, Tippfehler |
| Theme | Light **und** Dark: Kontrast, unsichtbarer Text, falsche Flaechenfarben, Bounce-Bereiche |
| Groessen | **Web: jede Seite in Phone- UND Desktop-Breite — Pflicht, siehe `references/web.md`.** MAUI: nur Hochformat (kein Querformat), ggf. zusaetzlich ein kleines Geraet |
| Bedienbarkeit | Tastatur/Tab-Reihenfolge und Fokus (Web), Touch-Ziele gross genug (MAUI), Dialoge schliessbar |
| Konsistenz | Gleiche Funktion auf Web und MAUI gleich benannt und gleich formatiert? |

### Phase 4 — Bericht schreiben

Waehrend des Testens **laufend** mitschreiben, nicht erst am Ende aus dem Gedaechtnis.

Ablage:

```
usertests/berichte/<YYYY-MM-DD>-<plattform>/
├── bericht.md
└── shots/<nn>-<kurzname>.png
```

`<plattform>` = `web`, `maui` oder `web-maui`. Bei zweitem Lauf am selben Tag `-2` anhaengen.

Aufbau von `bericht.md`:

```markdown
# Funktionstest <Plattform> — <Datum>

## Rahmen
- Getesteter Stand: <branch> @ <commit-sha>
- Umgebung: <Test/Prod/lokal>, API: <url>
- Geraet/Browser: <...>
- Getestet als: <Rollen>
- Dauer / Umfang: <...>

## Zusammenfassung
<3-6 Saetze: Gesamteindruck, was funktioniert, wo es hakt.>

| Schwere | Anzahl |
|---|---|
| S1 Blocker | n |
| S2 Schwer | n |
| S3 Mittel | n |
| S4 Kosmetik | n |
| Hinweis/Frage | n |

## Befunde

### B-01 · S2 · <Bereich> — <Kurztitel>
- **Schritte:** 1. … 2. … 3. …
- **Erwartet:** …
- **Tatsaechlich:** …
- **Screenshot:** shots/01-….png
- **Konsole/Log:** <relevante Zeile oder "unauffaellig">
- **Vermutete Stelle:** `pfad/datei.ext:zeile` *(nur wenn belastbar)*
- **Reproduzierbar:** ja / einmalig beobachtet

## Geprueft und in Ordnung
<Inventar-Tabelle mit Haken — macht die Abdeckung sichtbar.>

## Nicht getestet / nicht testbar
<Was ausgelassen wurde und warum.>
```

Schweregrade:

- **S1 Blocker** — Funktion unbenutzbar, Datenverlust, App-/Seiten-Absturz, Login unmoeglich
- **S2 Schwer** — Funktion liefert falsches Ergebnis oder bricht ab, Umweg noetig
- **S3 Mittel** — stoerend, aber Funktion erfuellt ihren Zweck (falsches Format, fehlende Rueckmeldung)
- **S4 Kosmetik** — Optik/Text/Abstand
- **Hinweis/Frage** — Design-Meinung oder unklare Soll-Vorgabe. **Klar von Bugs trennen** —
  nicht jede Abweichung vom eigenen Geschmack ist ein Fehler.

### Phase 5 — Ergebnis melden

Im Chat kurz und ehrlich zusammenfassen — nicht den ganzen Bericht ausschuetten:

1. Was getestet wurde (Plattform, Umgebung, Stand, Umfang)
2. Zahlen je Schweregrad
3. Die S1/S2-Befunde als Einzeiler
4. Was **nicht** getestet werden konnte
5. Pfad zum Bericht
6. Angebot: Befunde abarbeiten? (nicht ungefragt loslegen)

Ehrlich bleiben: wenn ein Bereich uebersprungen wurde, steht das drin. Kein "alles gruen",
wenn nicht alles angesehen wurde.

---

## Referenzen

- `references/web.md` — Astro-Web: Setup, Routen, Testbenutzer, CSP-/Override-Fallen
- `references/maui.md` — MAUI: Emulator, Build, DevFlow, API-Umschalter, Fallen
- `usertests/PropertyDetailPage_Testfaelle.md` — vorhandene Feld-Sichtbarkeits-Testfaelle je Objekttyp
- `usertests/` — Playwright-Setup (Achtung: `baseURL` zeigt auf die **API** `:5292`, nicht auf das Web)
