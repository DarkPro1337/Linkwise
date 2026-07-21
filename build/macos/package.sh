#!/usr/bin/env bash
set -euo pipefail

root="$(cd "$(dirname "$0")/../.." && pwd)"
rid="${1:-osx-arm64}"

case "$rid" in
    osx-arm64)
        swift_target="arm64-apple-macos12.0"
        ;;
    osx-x64)
        swift_target="x86_64-apple-macos12.0"
        ;;
    *)
        echo "Unsupported runtime identifier: $rid" >&2
        echo "Expected osx-arm64 or osx-x64." >&2
        exit 2
        ;;
esac

artifacts="$root/artifacts/macos/$rid"
publish="$artifacts/publish"
bundle="$artifacts/Linkwise.app"
macos="$bundle/Contents/MacOS"

rm -rf "$artifacts"
mkdir -p "$publish" "$macos" "$bundle/Contents/Resources"

dotnet publish \
    "$root/src/Linkwise.Desktop/Linkwise.Desktop.csproj" \
    --configuration Release \
    --runtime "$rid" \
    --self-contained true \
    --output "$publish"

cp -R "$publish/." "$macos/"
cp "$root/build/macos/Info.plist" "$bundle/Contents/Info.plist"
cp "$root/src/Linkwise.Desktop/Assets/app-macos.icns" "$bundle/Contents/Resources/app-macos.icns"
cp "$root/LICENSE" "$bundle/Contents/Resources/LICENSE"
cp "$root/THIRD-PARTY-NOTICES.md" "$bundle/Contents/Resources/THIRD-PARTY-NOTICES.md"

xcrun swiftc \
    "$root/src/Linkwise.Platforms.Mac/Native/DefaultHandler.swift" \
    -parse-as-library \
    -framework AppKit \
    -target "$swift_target" \
    -o "$macos/Linkwise.DefaultHandler"

chmod +x "$macos/Linkwise.Desktop" "$macos/Linkwise.DefaultHandler"
plutil -lint "$bundle/Contents/Info.plist"

# Ad-hoc signing is sufficient for local development and keeps all nested native files consistent.
codesign --force --deep --sign - "$bundle"

echo "$bundle"
