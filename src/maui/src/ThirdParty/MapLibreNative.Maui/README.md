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

- `win-x64`/`win-arm64` (mln-cabi.dll), `ios` (XCFramework), `maccatalyst` (.a):
  **unveraendert aus den 4.5.0-NuGet-Paketen** uebernommen.
- `android-arm64`/`android-x64` (libmln-cabi.so): **selbst gebaut am 28.07.2026**
  mit 16-KB-Page-Size (LOAD p_align 0x4000, behebt XA0141/Play-Blocker) und
  per llvm-strip gestrippt (6-7 MB statt 112 MB unstripped wie upstream).

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

## Update-Prozess

1. Upstream-Tag auschecken, `bindings/` ueber dieses Verzeichnis kopieren
   (Abweichungen 1-2 oben erneut anwenden).
2. Passende native Binaries aus den zugehoerigen NuGet-Paketen nach `native/` legen
   (Managed-Stand und Native-ABI MUESSEN vom selben Release stammen!).
3. `native-src/` mitziehen, diese README-Standzeile aktualisieren, App bauen + Karte verifizieren.
