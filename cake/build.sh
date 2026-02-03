#!/usr/bin/env bash
# Bootstrap script for Cake Frosting build (macOS/Linux)

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
TARGET="${1:-Default}"
CONFIGURATION="${2:-Release}"

show_help() {
    cat << EOF
Heimatplatz Build Script (Cake Frosting)

Usage:
    ./build.sh [target] [configuration]

Arguments:
    target          The Cake task to run (default: Default)
    configuration   Build configuration (default: Release)

Available Targets:
    Default              Run default task (VersionBump)
    VersionBump          Increment minor version
    BuildAndroid         Build Android APK
    BuildIos             Build iOS IPA (macOS only)
    BuildWasm            Build WebAssembly
    DeployAndroid        Build and deploy to Play Store internal
    DeployIos            Build and deploy to TestFlight (macOS only)
    DeployWasm           Build and deploy to Azure Static Web Apps
    DeployAll            Deploy to all platforms
    ComplianceCheck      Check store agreements

Examples:
    ./build.sh                           # Run default task
    ./build.sh VersionBump               # Bump version only
    ./build.sh DeployAndroid             # Build and deploy Android
    ./build.sh DeployWasm                # Build and deploy WASM
    ./build.sh DeployAll                 # Deploy to all platforms

Environment Variables:
    ANDROID_KEYSTORE_PATH              Path to Android keystore
    ANDROID_KEYSTORE_PASSWORD          Keystore password
    ANDROID_KEY_ALIAS                  Key alias
    ANDROID_KEY_PASSWORD               Key password
    PLAY_STORE_JSON_KEY_PATH           Path to Play Store service account JSON
    MATCH_GIT_URL                      Git URL for iOS certificates
    MATCH_PASSWORD                     Password for Match encryption
    ASC_KEY_ID                         App Store Connect API Key ID
    ASC_ISSUER_ID                      App Store Connect Issuer ID
    ASC_KEY_PATH                       Path to App Store Connect API key file
    AZURE_STATIC_WEB_APPS_API_TOKEN    Azure Static Web Apps deployment token
    API_BASE_URL                       API base URL for WASM build
EOF
    exit 0
}

if [[ "$1" == "-h" || "$1" == "--help" || "$1" == "help" ]]; then
    show_help
fi

echo "=== Heimatplatz Build Script ==="
echo "Target: $TARGET"
echo "Configuration: $CONFIGURATION"
echo ""

cd "$SCRIPT_DIR"

echo "Restoring dependencies..."
dotnet restore Build.csproj

echo "Running Cake task: $TARGET"
dotnet run --project Build.csproj --no-restore -- --target "$TARGET" --configuration "$CONFIGURATION"

echo "Build completed successfully!"
