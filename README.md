# Linkwise

Cross-platform desktop URL router and browser profile picker built with .NET and AvaloniaUI.

This project is a proof-of-concept for a cross-platform URL router and browser profile picker.

- `Linkwise.Core`: configuration models, JSON config store, URL rule engine, incoming URL parser, and process-based browser launcher.
- `Linkwise.Desktop`: Avalonia settings UI, tray icon/menu, config editing, Chrome profile import, route preview, and test URL launch.
- `Linkwise.Platforms.Mac`: macOS default-handler registration through a small native Swift helper.
- `Linkwise.Platforms.Windows`: per-user Windows URL-handler registration and Default Apps integration.

The first run creates an empty config file at the platform application data path:

on macOS: `~/Library/Application Support/Linkwise/config.json`  
on Windows: `%APPDATA%/Linkwise`

Use `Import Chrome` in the Browser Targets tab to add detected local Google Chrome profiles. If Chrome is not installed or no Chrome user data directory exists, no targets are created automatically.

Other browsers support will be added in the future. 

## Run

Open the settings UI:

```bash
dotnet run --project src/Linkwise.Desktop/Linkwise.Desktop.csproj
```

Route a URL as a default-handler style invocation:

```bash
dotnet run --project src/Linkwise.Desktop/Linkwise.Desktop.csproj -- https://gitlab.company.local/project
```

## macOS default-handler notes

Default-handler registration requires macOS 12 or later and a packaged `.app` bundle. Build a local bundle for the current Apple Silicon Mac with:

```bash
./build/macos/package.sh
```

For an Intel Mac, use:

```bash
./build/macos/package.sh osx-x64
```

The bundle is written to `artifacts/macos/<rid>/Linkwise.app`. Open that bundle, configure and save a valid fallback browser target, then use **Use Linkwise for Web Links** on the Fallback tab. macOS may ask for confirmation before changing the HTTP and HTTPS handlers.

The packaging script publishes the Avalonia application, compiles the native `Linkwise.DefaultHandler` helper with `swiftc`, adds both URL schemes to `Info.plist`, and ad-hoc signs the result for local development. The normal `dotnet build` flow remains cross-platform and does not invoke Swift.

Incoming macOS open-URL events are handled through Avalonia's activatable lifetime. The command-line URL flow remains available for development without packaging.

## Windows default-handler notes

Build a self-contained Windows x64 directory from PowerShell with:

```powershell
.\build\windows\package.ps1
```

For Windows on ARM, use:

```powershell
.\build\windows\package.ps1 -RuntimeIdentifier win-arm64
```

The application is written to `artifacts\windows\<rid>\Linkwise`. Keep that directory at a stable path, run `Linkwise.Desktop.exe`, configure and save a valid fallback browser target, and use **Use Linkwise for Web Links** on the Fallback tab.

Linkwise registers itself per user under `HKEY_CURRENT_USER`, so administrator rights are not required. Windows then opens the Default Apps page, where Linkwise must be selected for both HTTP and HTTPS. Windows does not allow desktop applications to silently replace those user choices.
