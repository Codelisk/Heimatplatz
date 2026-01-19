# Push Notifications Setup Guide

Diese Anleitung beschreibt die manuelle Konfiguration für Firebase Cloud Messaging (Android) und Apple Push Notification Service (iOS/macOS).

## Voraussetzungen

- Apple Developer Account (für APNs)
- Google/Firebase Account (für FCM)

---

## 1. Firebase Setup (Android)

### 1.1 Firebase-Projekt erstellen

1. Öffne [Firebase Console](https://console.firebase.google.com)
2. Klicke "Projekt hinzufügen"
3. Projektname eingeben (z.B. "Heimatplatz")
4. Google Analytics optional aktivieren
5. Projekt erstellen

### 1.2 Android-App hinzufügen

1. Im Firebase-Projekt: "App hinzufügen" → Android-Symbol
2. Android-Paketname: `com.heimatplatz.app` (muss mit App übereinstimmen)
3. App-Nickname: "Heimatplatz Android"
4. `google-services.json` herunterladen
5. Datei nach `src/uno/src/Heimatplatz.App/Platforms/Android/` kopieren

### 1.3 Service Account für Server erstellen

1. Firebase Console → Zahnrad → Projekteinstellungen
2. Tab "Dienstkonten"
3. "Neuen privaten Schlüssel generieren"
4. JSON-Datei als `firebase-service-account.json` im API-Root speichern (`src/api/src/Heimatplatz.Api/`)

---

## 2. APNs Setup (iOS/macOS)

### 2.1 APNs Auth Key erstellen

1. Öffne [Apple Developer Portal](https://developer.apple.com/account)
2. "Certificates, Identifiers & Profiles"
3. "Keys" → "+" Button
4. Name eingeben (z.B. "Heimatplatz Push")
5. "Apple Push Notifications service (APNs)" aktivieren
6. "Continue" → "Register"
7. **Key ID notieren** (z.B. `ABC123DEFG`)
8. `.p8` Datei herunterladen (nur einmal möglich!)
9. Datei als `apns-auth-key.p8` im API-Root speichern

### 2.2 Team ID finden

1. Apple Developer Portal → Account
2. "Membership" oder oben rechts im Portal
3. **Team ID notieren** (z.B. `9876543210`)

### 2.3 iOS-App in Firebase registrieren (optional, für unified tokens)

Falls du Firebase auch für iOS nutzen willst:
1. Firebase Console → Projekt → "App hinzufügen" → iOS
2. Bundle ID: `com.heimatplatz.app`
3. `GoogleService-Info.plist` herunterladen
4. In Firebase: Project Settings → Cloud Messaging → APNs Auth Key hochladen

---

## 3. Server-Konfiguration

### 3.1 appsettings.json aktualisieren

Bearbeite `src/api/src/Heimatplatz.Api/appsettings.json`:

```json
{
  "PushNotifications": {
    "Firebase": {
      "ServiceAccountPath": "firebase-service-account.json"
    },
    "Apns": {
      "TeamId": "DEIN_TEAM_ID",
      "KeyId": "DEIN_KEY_ID",
      "PrivateKeyPath": "apns-auth-key.p8",
      "BundleId": "com.heimatplatz.app",
      "UseProduction": false
    }
  }
}
```

### 3.2 Für Production (Aspire Secrets)

Für Production sollten Secrets über Aspire/Azure Key Vault konfiguriert werden:

```json
{
  "PushNotifications": {
    "Firebase": {
      "ServiceAccountPath": "/secrets/firebase-service-account.json"
    },
    "Apns": {
      "TeamId": "${APNS_TEAM_ID}",
      "KeyId": "${APNS_KEY_ID}",
      "PrivateKeyPath": "/secrets/apns-auth-key.p8",
      "BundleId": "com.heimatplatz.app",
      "UseProduction": true
    }
  }
}
```

---

## 4. Client-Konfiguration (Uno Platform)

### 4.1 Android: google-services.json

Stelle sicher, dass in `Heimatplatz.App.csproj`:

```xml
<ItemGroup Condition="$(TargetFramework.Contains('android'))">
  <GoogleServicesJson Include="Platforms\Android\google-services.json" />
</ItemGroup>
```

### 4.2 iOS: Capabilities

In Xcode oder via Entitlements:
- Push Notifications Capability aktivieren
- Background Modes → Remote notifications aktivieren

---

## 5. Testen

### 5.1 Lokales Testen

1. API starten
2. App auf echtem Gerät starten (Emulator unterstützt oft kein Push)
3. In App einloggen und Notification-Präferenzen setzen
4. Neue Property in der gewünschten Stadt erstellen
5. Push sollte ankommen

### 5.2 APNs Sandbox vs Production

- `UseProduction: false` → APNs Sandbox (Development Builds)
- `UseProduction: true` → APNs Production (App Store Builds)

---

## Dateien Checkliste

| Datei | Speicherort | Quelle |
|-------|-------------|--------|
| `firebase-service-account.json` | `src/api/src/Heimatplatz.Api/` | Firebase Console |
| `apns-auth-key.p8` | `src/api/src/Heimatplatz.Api/` | Apple Developer Portal |
| `google-services.json` | `src/uno/src/Heimatplatz.App/Platforms/Android/` | Firebase Console |
| `GoogleService-Info.plist` | `src/uno/src/Heimatplatz.App/Platforms/iOS/` | Firebase Console (optional) |

**Wichtig:** Alle Secret-Dateien sind in `.gitignore` eingetragen und werden NICHT committed!

---

## Troubleshooting

### Push kommt nicht an

1. **Token registriert?** - Prüfe DB `PushSubscriptions` Tabelle
2. **Correct Platform?** - Android-Token nur für FCM, iOS-Token nur für APNs
3. **APNs Sandbox?** - Development Build braucht `UseProduction: false`
4. **Firebase konfiguriert?** - Prüfe Logs auf "Firebase is not configured"

### Invalid Token Errors

Tokens werden automatisch aus der DB entfernt wenn:
- FCM: `Unregistered` oder `InvalidArgument`
- APNs: `BadDeviceToken`, `Unregistered`, `ExpiredToken`

### Logs prüfen

```bash
# API Logs für Push-Status
dotnet run | grep -i "push\|firebase\|apns"
```
