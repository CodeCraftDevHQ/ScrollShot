<div align="center">

# 📸 ScrollShot

**A fast, modern, and customizable screenshot utility for Windows featuring Android-style scrolling screenshots, GIF/MP4 screen recording, and extensive output controls.**

*Built with WPF on .NET 10*

![Platform](https://img.shields.io/badge/Platform-Windows%2010%2F11-0078D6?style=flat-square&logo=windows)
![Framework](https://img.shields.io/badge/.NET-10.0-512BD4?style=flat-square&logo=dotnet)
![UI](https://img.shields.io/badge/UI-WPF%20%7C%20ModernWpfUI-blue?style=flat-square)
![License](https://img.shields.io/badge/License-MIT-green?style=flat-square)

[Features](#-features) • [Default Shortcuts](#-default-shortcuts) • [Scrolling Guide](#-using-scrolling-screenshots) • [Build and Install](#-build-and-installation) • [Architecture](#-project-layout)

</div>

---

## ✨ Features

### 🎯 Capture
* **Versatile Modes:** Capture a custom region, a specific window, or full screen (the monitor under the cursor, or all connected monitors).
* **Delayed Capture:** Countdown timer support—ideal for capturing dropdown menus, context menus, and tooltips.
* **Precision Magnifier:** Displays live screen coordinates and Hex color codes, supports 1 px arrow-key nudging (hold `Shift` for 10 px), and copies the color under the cursor with `C`.
* **Smart Window Selection:** Hover over any window to select it, or hold `Ctrl` to target inner panes and child controls.
* **Multi-Monitor & DPI Aware:** Per-monitor DPI awareness ensures crisp captures across mixed-scaling setups.

### 📜 Scrolling Screenshots
* **Vertical & Horizontal Scrolling:** Supports both fully automatic and manual scrolling modes.
* **Broad Compatibility:** Works seamlessly in web browsers, File Explorer, Visual Studio, and most applications with scrollable content.
* **Cursor-Free Scrolling:** Dispatches window messages directly to scroll without moving your mouse (with an input-based fallback when needed).
* **Smart Stitching Engine:** Adaptive scroll stepping, multi-band image matching, and automatic retries on failed matches.
* **Sticky Element Removal:** Optionally detects and strips sticky headers and footers from the final stitched image.
* **Configurable Limits:** Set maximum output height and frame count thresholds.

### 💾 Output & Optimization
* **Multiple Formats:** Export to `PNG`, `JPG`, `BMP`, or `PDF`.
* **Smart Size Reduction:** Scale by percentage, enforce a maximum width, adjust JPEG quality, or target a specific file size.
* **Auto-Splitting:** Split ultra-long screenshots into multiple files with smart cuts at low-activity rows (avoiding awkward cuts through text).
* **Built-in PDF Writer:** Export to paginated A4 documents or a single continuous tall page with customizable margins and JPEG compression.
* **Flexible Destinations:** Save directly to disk, copy to clipboard, or do both simultaneously.

### 🎥 Screen Recording
* **GIF & MP4 Support:** Record any screen region to `GIF` (built-in encoder, zero external dependencies) or `MP4` (powered by `ffmpeg`).
* **Full Recording Control:** Customize FPS, maximum duration, output scale, cursor visibility, and start countdown.

### 🚀 Quick Sharing
* **Interactive Preview Toast:** Instantly open, reveal in folder, delete, or share captures right after taking them.
* **Custom HTTP Uploader:** Upload captures manually or automatically to any HTTP endpoint (supports multipart form fields, custom headers like `Authorization: Bearer ...`, and Regex link extraction to clipboard).
* **Workflow Integration:** Launch captures directly in your preferred image editor, or copy the file/path to clipboard.

### 🎨 Customization & Localization
* **Modern Themes:** Dark, Light, or System-synced themes with customizable accent colors (powered by `ModernWpfUI`).
* **Bilingual UI:** Full support for English and Persian, including complete right-to-left (RTL) layout adaptation.
* **Named Profiles:** Bundle output and scrolling settings into switchable profiles accessible via the tray menu or global hotkeys.
* **System Integration:** Global hotkeys, system tray icon, optional floating capture button, and launch at Windows startup.

---

## 🔑 Default Shortcuts

All shortcuts can be customized in **Settings > Shortcuts & button**.

| Shortcut | Action |
| :--- | :--- |
| `Ctrl + Shift + 1` | Capture Region |
| `Ctrl + Shift + 2` | Capture Window |
| `Ctrl + Shift + 3` | Capture Full Screen |
| `Ctrl + Shift + 4` | Scrolling Screenshot *(press again to finish)* |
| `Ctrl + Shift + 5` | Record Region *(press again to stop)* |
| `Esc` | Cancel selection, scroll capture, or recording |

> **Note:** *Region delay* and *Next profile* shortcuts are unassigned by default and can be configured in Settings.

---

## 📖 Using Scrolling Screenshots

1. Press `Ctrl + Shift + 4` and drag to select a region, or click to select a window.
2. Once the countdown finishes, scrolling and capturing start automatically *(or choose Manual mode to scroll yourself)*.
3. Press the shortcut again or click **Finish & save** to stitch and export. Press `Esc` at any time to cancel.

> **💡 Troubleshooting Animated Pages:** If frame matching fails on pages with animations or dynamic content, open **Settings > Scrolling** and increase the **Scroll delay**, reduce the **Notches per step**, or raise the **Match tolerance**. You can also enable *Save failed frames* to inspect frames for debugging.

---

## ⚙️ Requirements

* **OS:** Windows 10 version 1903 or later (Windows 11 recommended). *Excluding ScrollShot's own overlays from captures relies on `WDA_EXCLUDEFROMCAPTURE`.*
* **Runtime:** [.NET 10 Desktop Runtime](https://dotnet.microsoft.com/) *(not required if using the self-contained installer build)*.
* **Optional:** `ffmpeg.exe` for MP4 recording (place next to the executable, add to `PATH`, or configure in **Settings > Recording**).

---

## 🔧 Build and Installation

### Building from Source
1. Install **Visual Studio** with the **.NET desktop development** workload and the **.NET 10 SDK**.
2. Open `ScrollShot.csproj` (the `ModernWpfUI` NuGet package restores automatically).
3. Build and run the solution.

To run the unit test suite:
```bash
dotnet test ScrollShot.Tests
```

### Creating the Installer
`build-installer.ps1` publishes a self-contained single-file executable and, if **Inno Setup 6** is installed, compiles `installer-output\ScrollShot-Setup-<version>.exe`:

```powershell
.\build-installer.ps1
# Or specify a custom release version:
.\build-installer.ps1 -Version 1.1.0
```
*(Remember to bump `<Version>` in `ScrollShot.csproj` before each release.)*

---

## 🔄 Updates & Upload Endpoint

### Automatic Updates
In **Settings > About & updates**, set the update URL to a JSON manifest formatted as:

```json
{
  "version": "1.1.0",
  "url": "https://example.com/download",
  "notes": "What's new in this release"
}
```
> **GitHub Releases Support:** A GitHub latest-release API URL (`https://api.github.com/repos/<owner>/<repo>/releases/latest`) is also natively supported using `tag_name` and `html_url`. ScrollShot checks automatically once a day or manually via **Tray Menu > Check for updates**.

### HTTP Upload Configuration
In **Settings > Sharing**, configure your custom upload endpoint:
* **Upload URL & Field Name:** Target endpoint and multipart form file field name.
* **Custom Header (Optional):** e.g., `Authorization: Bearer <your-token>`.
* **Response Regex (Optional):** Regular expression to extract the resulting URL from the response body (the first capture group, or the full match, is copied to the clipboard).

---

## 📂 Files & Data Storage

| Data Type | Path |
| :--- | :--- |
| **Settings** | `%AppData%\ScrollShot\settings.json` |
| **Rotating Log** | `%AppData%\ScrollShot\log.txt` *(Tray Menu > Open log file)* |
| **Failed Stitch Frames** | `%AppData%\ScrollShot\debug` *(when enabled)* |
| **Default Save Folder** | Configurable in **Settings > General** |

---

## 🧱 Project Layout

| Area | Files |
| :--- | :--- |
| **Capture & Overlay** | `ScreenGrab.cs`, `RegionOverlay.xaml(.cs)`, `CaptureController.cs` |
| **Scrolling Engine** | `ScrollStitcher.cs`, `ScrollControlWindow.xaml(.cs)`, `ScrollFrameWindow.cs` |
| **Screen Recording** | `RecordWindow.xaml(.cs)`, `GifWriter.cs` |
| **Output & Processing** | `OutputService.cs`, `ImageTools.cs`, `PdfBuilder.cs` |
| **UI & Localization** | `SettingsWindow.xaml(.cs)`, `ToastWindow.xaml(.cs)`, `FloatingButton.xaml(.cs)`, `TrayController.cs`, `ThemeService.cs`, `Localization.cs` |
| **Core Services** | `ShareService.cs`, `UpdateService.cs`, `Log.cs`, `AppSettings.cs` |

---

## 📄 License

Released under the [**MIT License**](LICENSE).