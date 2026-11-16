using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace ScrollShot
{
    public sealed class HotkeyDef
    {
        public uint Modifiers { get; set; }
        public uint Key { get; set; }

        public HotkeyDef() { }

        public HotkeyDef(uint modifiers, uint key)
        {
            Modifiers = modifiers;
            Key = key;
        }

        public override string ToString()
        {
            if (Key == 0) return "";
            var parts = new List<string>();
            if ((Modifiers & Native.MOD_CONTROL) != 0) parts.Add("Ctrl");
            if ((Modifiers & Native.MOD_ALT) != 0) parts.Add("Alt");
            if ((Modifiers & Native.MOD_SHIFT) != 0) parts.Add("Shift");
            if ((Modifiers & Native.MOD_WIN) != 0) parts.Add("Win");
            parts.Add(KeyName());
            return string.Join("+", parts);
        }

        string KeyName()
        {
            if (Key == 0x2C) return "PrtSc";
            string n = System.Windows.Input.KeyInterop.KeyFromVirtualKey((int)Key).ToString();
            if (n.Length == 2 && n[0] == 'D' && char.IsDigit(n[1])) return n.Substring(1);
            return n;
        }
    }

    public sealed class AppSettings
    {
        public string SaveFolder { get; set; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures), "ScrollShot");
        public string Language { get; set; } = "en";
        public string Theme { get; set; } = "dark";
        public string AccentColor { get; set; } = "#FF3B82F6";
        public string FileNamePattern { get; set; } = "Screenshot_{date}_{time}";
        public string Format { get; set; } = "png";
        public int JpgQuality { get; set; } = 92;
        public bool SaveToFile { get; set; } = true;
        public bool CopyToClipboard { get; set; } = true;
        public bool ShowToast { get; set; } = true;
        public int ToastSeconds { get; set; } = 6;
        public bool PlaySound { get; set; } = false;
        public bool StartWithWindows { get; set; } = false;

        public bool ShowFloatingButton { get; set; } = true;
        public int FloatingSize { get; set; } = 48;
        public double FloatingOpacity { get; set; } = 0.85;
        public int? FloatingX { get; set; }
        public int? FloatingY { get; set; }

        public HotkeyDef RegionHotkey { get; set; } = new HotkeyDef(Native.MOD_CONTROL | Native.MOD_SHIFT, 0x31);
        public HotkeyDef WindowHotkey { get; set; } = new HotkeyDef(Native.MOD_CONTROL | Native.MOD_SHIFT, 0x32);
        public HotkeyDef FullScreenHotkey { get; set; } = new HotkeyDef(Native.MOD_CONTROL | Native.MOD_SHIFT, 0x33);
        public HotkeyDef ScrollHotkey { get; set; } = new HotkeyDef(Native.MOD_CONTROL | Native.MOD_SHIFT, 0x34);
        public string TrayDoubleClick { get; set; } = "region";

        public string FullScreenScope { get; set; } = "cursor";
        public int DelaySeconds { get; set; } = 3;

        public double OverlayDim { get; set; } = 0.45;
        public string SelectionColor { get; set; } = "#FF3B82F6";
        public int SelectionThickness { get; set; } = 2;
        public bool ShowSizeLabel { get; set; } = true;
        public bool ShowCrosshair { get; set; } = true;
        public bool HighlightWindows { get; set; } = true;
        public string FrameColor { get; set; } = "#FFEF4444";

        public int ScalePercent { get; set; } = 100;
        public int MaxWidthPx { get; set; } = 0;
        public int TargetSizeKB { get; set; } = 0;
        public bool SplitEnabled { get; set; } = false;
        public int SplitHeightPx { get; set; } = 8000;
        public bool SmartCut { get; set; } = true;
        public bool AlsoSavePdf { get; set; } = false;
        public string PdfPageMode { get; set; } = "a4";
        public string PdfPageSize { get; set; } = "a4";
        public int PdfMarginMm { get; set; } = 10;
        public int PdfJpegQuality { get; set; } = 85;

        public string ScrollMode { get; set; } = "auto";
        public int ScrollCountdownSec { get; set; } = 2;
        public bool ScrollFromTop { get; set; } = false;
        public bool ScrollWithoutMouse { get; set; } = true;
        public int ScrollNotches { get; set; } = 3;
        public int ScrollDelayMs { get; set; } = 500;
        public int ScrollStableTimeoutMs { get; set; } = 2500;
        public int MaxHeightPx { get; set; } = 30000;
        public int MaxFrames { get; set; } = 300;
        public int IgnoreLeftPx { get; set; } = 0;
        public int IgnoreRightPx { get; set; } = 20;
        public int MatchTolerance { get; set; } = 6;

        public AppSettings Clone()
        {
            return JsonSerializer.Deserialize<AppSettings>(JsonSerializer.Serialize(this)).Normalize();
        }

        public AppSettings Normalize()
        {
            if (string.IsNullOrWhiteSpace(SaveFolder)) SaveFolder = new AppSettings().SaveFolder;
            if (string.IsNullOrWhiteSpace(FileNamePattern)) FileNamePattern = "Screenshot_{date}_{time}";
            if (Language != "en" && Language != "fa") Language = "en";
            if (Theme != "dark" && Theme != "light" && Theme != "system") Theme = "dark";
            if (string.IsNullOrWhiteSpace(AccentColor)) AccentColor = "#FF3B82F6";
            if (Format != "png" && Format != "jpg" && Format != "bmp" && Format != "pdf") Format = "png";
            ScalePercent = Math.Clamp(ScalePercent, 10, 100);
            MaxWidthPx = Math.Clamp(MaxWidthPx, 0, 20000);
            TargetSizeKB = Math.Clamp(TargetSizeKB, 0, 200000);
            SplitHeightPx = Math.Clamp(SplitHeightPx, 1000, 60000);
            PdfMarginMm = Math.Clamp(PdfMarginMm, 0, 40);
            PdfJpegQuality = Math.Clamp(PdfJpegQuality, 30, 100);
            if (PdfPageMode != "a4" && PdfPageMode != "single") PdfPageMode = "a4";
            if (PdfPageSize != "a4" && PdfPageSize != "letter") PdfPageSize = "a4";
            JpgQuality = Math.Clamp(JpgQuality, 40, 100);
            ToastSeconds = Math.Clamp(ToastSeconds, 1, 60);
            FloatingSize = Math.Clamp(FloatingSize, 32, 96);
            FloatingOpacity = Math.Clamp(FloatingOpacity, 0.2, 1.0);
            DelaySeconds = Math.Clamp(DelaySeconds, 0, 30);
            OverlayDim = Math.Clamp(OverlayDim, 0.05, 0.9);
            SelectionThickness = Math.Clamp(SelectionThickness, 1, 8);
            ScrollNotches = Math.Clamp(ScrollNotches, 1, 20);
            ScrollCountdownSec = Math.Clamp(ScrollCountdownSec, 0, 10);
            ScrollDelayMs = Math.Clamp(ScrollDelayMs, 100, 5000);
            ScrollStableTimeoutMs = Math.Clamp(ScrollStableTimeoutMs, 500, 10000);
            MaxHeightPx = Math.Clamp(MaxHeightPx, 2000, 100000);
            MaxFrames = Math.Clamp(MaxFrames, 2, 1000);
            IgnoreLeftPx = Math.Clamp(IgnoreLeftPx, 0, 200);
            IgnoreRightPx = Math.Clamp(IgnoreRightPx, 0, 200);
            MatchTolerance = Math.Clamp(MatchTolerance, 1, 30);
            if (ScrollMode != "auto" && ScrollMode != "manual") ScrollMode = "auto";
            if (FullScreenScope != "cursor" && FullScreenScope != "all") FullScreenScope = "cursor";
            if (RegionHotkey == null) RegionHotkey = new HotkeyDef();
            if (WindowHotkey == null) WindowHotkey = new HotkeyDef();
            if (FullScreenHotkey == null) FullScreenHotkey = new HotkeyDef();
            if (ScrollHotkey == null) ScrollHotkey = new HotkeyDef();
            return this;
        }
    }

    internal static class SettingsStore
    {
        static string FilePath
        {
            get
            {
                return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ScrollShot", "settings.json");
            }
        }

        public static AppSettings Load()
        {
            try
            {
                if (File.Exists(FilePath))
                {
                    var s = JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(FilePath));
                    if (s != null) return s.Normalize();
                }
            }
            catch
            {
            }
            return new AppSettings().Normalize();
        }

        public static void Save(AppSettings s)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(FilePath));
                File.WriteAllText(FilePath, JsonSerializer.Serialize(s, new JsonSerializerOptions { WriteIndented = true }));
            }
            catch
            {
            }
        }
    }
}
