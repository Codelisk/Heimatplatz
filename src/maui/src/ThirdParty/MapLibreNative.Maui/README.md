# MapLibreNative.Maui (Vendor)

Gevendorter Quellcode des MapLibre-Native-Bindings — P/Invoke-Schicht (`Mbgl*`,
`NativeMethods`), Geometrie-Typen und die vorgebauten nativen `mln-cabi`-Binaries.
Wird von `MapLibreNative.Maui.Handlers` konsumiert; die App referenziert nur das
Handlers-Projekt.

## Herkunft

- **Upstream:** [TechIdiots-LLC/MaplibreNativeMAUI](https://github.com/TechIdiots-LLC/MaplibreNativeMAUI)
- **Stand:** Tag `v4.5.0`, Commit `21fb356` (uebernommen am 28.07.2026, Ordner `bindings/`)
- **Lizenz:** BSD 2-Clause (siehe `LICENSE`, Copyright Andrew Calcutt) — Hinweis muss erhalten bleiben
- **Warum gevendort:** Zwei-Sterne-Projekt mit einem Maintainer; wir wollen Fixes
  (Query-Bug, StyleUrl-Verhalten) selbst einspielen koennen und nicht am NuGet-Feed haengen.

## Native Binaries (`native/`)

- `win-x64`/`win-arm64` (mln-cabi.dll), `maccatalyst` (.a):
  **unveraendert aus den 4.5.0-NuGet-Paketen** uebernommen.
- `ios` (XCFramework, Device+Simulator): **selbst gebaut am 29.07.2026** via
  Codemagic-Workflow `ios-native-maplibre` (codemagic.yaml) - maplibre-native
  @ 08579ca7 + native-src-Wrapper, Metal, inkl. **pixelRatio-Fix** (Abweichung 4);
  damit rendert iOS identisch zu Android und braucht keine Kompensation mehr.
- `android-arm64`/`android-x64` (libmln-cabi.so): **selbst gebaut am 28.07.2026**
  mit 16-KB-Page-Size (LOAD p_align 0x4000, behebt XA0141/Play-Blocker) und
  per llvm-strip gestrippt (6-7 MB statt 112 MB unstripped wie upstream).
  Enthaelt zusaetzlich den **pixelRatio-Fix** (siehe Abweichung 4) — die
  Android-Binaries sind damit die einzigen mit korrekter Textskalierung auf
  High-DPI-Geraeten; win/ios/maccatalyst-Binaries haben weiterhin das
  Upstream-Verhalten (dort faellt es kaum auf, weil der Faktor ~1 ist bzw.
  iOS ungenutzt).

Das csproj bindet die Binaries ueber Existenz-Bedingungen ein — genau dafuer ist
der `native/`-Ordner upstream vorgesehen.

### Android-Eigenbau-Rezept (Upstream-CI-Rezept + 16-KB-Flag)

Quellen: Binding-Repo Tag `v4.5.0` mit Submodul `dependencies/maplibre-native`
@ `08579ca7` (rekursiv). Toolchain: NDK 27.0.12077973, CMake >= 3.25, Ninja.

```
cmake -B build-<abi> -G Ninja \
  -DCMAKE_TOOLCHAIN_FILE=<ndk>/build/cmake/android.toolchain.cmake \
  -DANDROID_ABI=<arm64-v8a|x86_64> -DANDROID_PLATFORM=android-23 \
  -DCMAKE_BUILD_TYPE=Release -DMLN_WITH_OPENGL=ON \
  -DANDROID_SUPPORT_FLEXIBLE_PAGE_SIZES=ON        # <- der 16-KB-Fix
cmake --build build-<abi> --target mln-cabi --parallel 8
llvm-strip --strip-unneeded build-<abi>/native/libmln-cabi.so -o libmln-cabi.so
```

Der C++-Wrapper-Quellcode liegt als Referenz unter `native-src/` (der
maplibre-native-Core ist dort NICHT enthalten, upstream ein Git-Submodul).
Das Flag gehoert upstream in `.github/workflows/native-android.yml` gemeldet —
dann kann der Eigenbau beim naechsten Update wieder entfallen.

## Lokale Abweichungen vom Upstream-Stand

1. `TargetFrameworks` auf net10 reduziert (net9-Workloads lokal nicht installiert).
2. `MapLibreNative.Maui.Vulkan.csproj` nicht uebernommen (kein Drop-in: eigene
   Assembly-Identitaet, die Handlers binden nur an diese GL-Basis).
3. `../../ThirdParty/Directory.Packages.props` schaltet die zentrale
   Paketversionsverwaltung fuer den Vendor-Code ab (Original-Versionen bleiben).
4. **pixelRatio-Fix in `native-src/src/mln_cabi.cpp` (28.07.2026):** Upstream
   uebergibt die PHYSISCHE Framebuffer-Groesse als mbgl-Map-Groesse. mbgl
   erwartet dort aber LOGISCHE px (physisch ÷ pixelRatio) — die Verletzung
   liess Text/Icons mit Faktor 1 rendern (auf einem 450-dpi-Handy unlesbar
   winzig), waehrend Kreise/Linien mit dem pixelRatio skalierten, und verschob
   die Zoom-Skala um +log2(pixelRatio) (~+1.5) gegenueber dem Web. Der Fix:
   `CabiMap` merkt sich den pixelRatio, Map-Groessen werden geteilt
   (Frontend/Viewport bleibt physisch), und ALLE Screen-Koordinaten der C-API
   (Gesten-Anker, moveBy, latLngForPixel, pixelForLatLng, Query-Punkt/-Box,
   Kamera-Paddings) konvertieren an der ABI-Grenze — die Managed-Seite spricht
   weiterhin physische px. Gehoert upstream gemeldet; bis dahin bei jedem
   Update erneut anwenden und die Android- UND iOS-Binaries neu bauen
   (iOS: Codemagic-Workflow `ios-native-maplibre`, s. codemagic.yaml).
5. **iOS-AOT-sichere Callbacks (29.07.2026):** Upstream uebergibt den Render-
   Callback (`MbglFrontend`) und den Map-Observer (`MbglMap`) als Delegates —
   Delegate-Marshalling native→managed erzeugt den Reverse-Wrapper erst zur
   Laufzeit (JIT) und crasht auf iOS (AOT-only) mit `ExecutionEngineException`
   beim Oeffnen der Karte. Umgebaut auf statische
   `[UnmanagedCallersOnly]`-Trampoline + Funktionspointer (`IntPtr` in
   `NativeMethods.FrontendCreateGl`/`MapCreate`/`MapCreate2`) mit
   GCHandle-Userdata; die Controller (MaciOS/Android) rufen jetzt
   `_frontend?.Dispose()` fuer die GCHandle-Freigabe. Die uebrigen
   Delegate-Callbacks (LogFn, HttpProvider, Offline*) sind auf iOS derzeit
   unbenutzt — vor einer Nutzung dort genauso umbauen! Bei Updates erneut
   anwenden.
6. **iOS-Gesten + pixelRatio-Kompensation (29.07.2026):** Upstream hat auf iOS
   KEINE Touch-Gesten implementiert (nur Overlay-Buttons; die Click-Events
   feuerten nie). Der MaciOS-Controller hat jetzt Pan/Pinch/Tap/DoubleTap/
   LongPress-Recognizer, die dieselben C-ABI-Primitiven fuettern wie Windows.
   Die `compatPixelRatio`-Umrechnung in `MbglMap` (fuer Binaries OHNE den
   Abweichung-4-Fix, die LOGISCHE px erwarten) ist seit dem iOS-Eigenbau
   (29.07.) NUR NOCH fuer MacCatalyst aktiv - dessen `.a` ist weiter der
   Upstream-Stand. Wird auch MacCatalyst neu gebaut, dort ebenfalls auf 1.0
   stellen (MapLibreMapController.MaciOS.cs) und die MacCatalyst-Sonderwerte
   des Preis-Chips (PropertyMapPage) entfernen — sonst wird doppelt
   konvertiert!

## Update-Prozess

1. Upstream-Tag auschecken, `bindings/` ueber dieses Verzeichnis kopieren
   (Abweichungen 1-2 oben erneut anwenden).
2. Passende native Binaries aus den zugehoerigen NuGet-Paketen nach `native/` legen
   (Managed-Stand und Native-ABI MUESSEN vom selben Release stammen!).
3. `native-src/` mitziehen, diese README-Standzeile aktualisieren, App bauen + Karte verifizieren.
