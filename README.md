# Codex Usage Widget

A lightweight Windows WPF widget that displays the usage windows returned by the locally installed Codex desktop app and presents them as a draggable Jelly Card.

## Requirements

- Windows 10/11 x64
- .NET 10 SDK to build
- Codex desktop app installed and signed in

## Build

```powershell
dotnet publish .\CodexUsageWidget\CodexUsageWidget.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o .\publish
```

Run `publish\CodexUsageWidget.exe`.

## Features

- Plan-aware primary/secondary windows, credits, and unlimited accounts
- Automatic refresh every minute without sending a model request
- Jelly Card edge deformation with spring return
- Custom opacity slider and always-on-top transparent WPF window

## Important limitation

This is an unofficial community project. It reads `account/rateLimits/read` through the locally installed Codex app-server interface. That interface is not a public stability contract and may change when Codex updates. The widget does not read or store passwords, API keys, or conversation contents; it uses the local Codex process only to request the current usage snapshot.

If Codex is not installed, not signed in, or its internal protocol changes, the widget shows a refresh failure instead of fabricating usage values.

## License

MIT. See [LICENSE](LICENSE).
