# Linkwise

Cross-platform desktop URL router and browser profile picker built with .NET and AvaloniaUI.

This project is a proof-of-concept for a cross-platform URL router and browser profile picker.

- `Linkwise.Core`: configuration models, JSON config store, URL rule engine, incoming URL parser, and process-based browser launcher.
- `Linkwise.Desktop`: Avalonia settings UI, tray icon/menu, config editing, Chrome profile import, route preview, and test URL launch.
- `Linkwise.Platforms.Mac` / `Linkwise.Platforms.Windows`: placeholders for OS default-handler integration.

The first run creates an empty config file at the platform application data path:

```text
~/Library/Application Support/Linkwise/config.json
```

on macOS

```text
.../AppData/Local/Linkwise/config.json
```

on Windows

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

The command-line URL flow works for the proof-of-concept. Full macOS integration still needs an app bundle with `CFBundleURLTypes` for `http` and `https`, plus Launch Services/open-url event handling. That should be implemented in the macOS adapter after the core route flow is stable.

