# ScrollShot

Windows screenshot app with scrolling screenshots (WPF, C#, .NET 10).

## Build
Open `ScrollShot.csproj` in Visual Studio. The `ModernWpfUI` NuGet package is restored automatically (internet needed on first build).

## Default shortcuts (changeable in Settings)
- Ctrl+Shift+1: region
- Ctrl+Shift+2: window
- Ctrl+Shift+3: full screen
- Ctrl+Shift+4: scrolling screenshot

## Scrolling screenshot
1. Press the shortcut and select an area (or click a window; hold Ctrl to pick inner panes).
2. After a short countdown it starts automatically; the scroll step adjusts itself.
3. Esc cancels; press the shortcut again or click "Finish & save" to end.

## Settings
Language (English / Persian), theme (dark / light / follow system), accent color, output format (PNG/JPG/BMP/PDF), size reduction, splitting long images, PDF options, shortcuts, floating button, and scrolling tuning.

## Installer and updates
- Run `build-installer.ps1` (needs the .NET SDK and Inno Setup 6) to publish a self-contained single-file build and create `installer-output\ScrollShot-Setup-<version>.exe`. Without Inno Setup it only publishes to `publish\`.
- Bump `<Version>` in `ScrollShot.csproj` for each release.
- Settings > About: set an update URL pointing to a JSON like `{"version":"1.1.0","url":"https://example.com/download","notes":"What's new"}` (or a GitHub latest-release API URL). The app checks once a day and via the tray item "Check for updates".
- The icon is `app.ico` (also used for the tray).

## Diagnostics and tests
- Log file: `%AppData%\ScrollShot\log.txt` (tray menu > Open log file). Enable "Save failed frames" in Settings > Scrolling to keep frames of failed stitches in `%AppData%\ScrollShot\debug`.
- Unit tests: `dotnet test ScrollShot.Tests` (stitcher on synthetic pages with sticky header/footer, GIF/LZW round trip, PDF structure, splitting, JPEG target size, settings, profiles, version parsing).
