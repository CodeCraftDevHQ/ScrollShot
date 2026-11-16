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
