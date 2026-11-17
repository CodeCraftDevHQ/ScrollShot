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

    public sealed class NamedProfile
    {
        public string Name { get; set; } = "";
        public Dictionary<string, string> Values { get; set; } = new Dictionary<string, string>();
    }

    public sealed class AppSettings
    {
        static readonly string[] ProfileKeys =
        {
            "SaveFolder", "FileNamePattern", "Format", "JpgQuality", "SaveToFile", "CopyToClipboard",
            "ScalePercent", "MaxWidthPx", "TargetSizeKB", "SplitEnabled", "SplitHeightPx", "SmartCut",
            "AlsoSavePdf", "PdfPageMode", "PdfPageSize", "PdfMarginMm", "PdfJpegQuality",
            "ScrollDirection", "TrimHeader", "TrimFooter", "ScrollMode", "ScrollFromTop", "ScrollWithoutMouse", "ScrollNotches", "MaxHeightPx"
        };

        public List<NamedProfile> Profiles { get; set; }
        public string ActiveProfile { get; set; } = "";
        public HotkeyDef ProfileHotkey { get; set; } = new HotkeyDef();
        public HotkeyDef RegionDelayHotkey { get; set; } = new HotkeyDef();
        public HotkeyDef RecordHotkey { get; set; } = new HotkeyDef(Native.MOD_CONTROL | Native.MOD_SHIFT, 0x35);
        public string RecordFormat { get; set; } = "gif";
        public int RecordFps { get; set; } = 10;
        public int RecordMaxSeconds { get; set; } = 60;
        public int RecordScalePercent { get; set; } = 100;
        public bool RecordCursor { get; set; } = true;
        public int RecordCountdownSec { get; set; } = 2;
        public string FfmpegPath { get; set; } = "";

        public NamedProfile CaptureProfile(string name)
        {
            var p = new NamedProfile { Name = name };
            foreach (var key in ProfileKeys)
            {
                var pi = typeof(AppSettings).GetProperty(key);
                if (pi == null) continue;
                p.Values[key] = JsonSerializer.Serialize(pi.GetValue(this), pi.PropertyType);
            }
            return p;
        }

        public bool ApplyProfile(string name)
        {
            if (Profiles == null) return false;
            var p = Profiles.Find(x => string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase));
            if (p == null) return false;
            foreach (var kv in p.Values)
            {
                var pi = typeof(AppSettings).GetProperty(kv.Key);
                if (pi == null || !pi.CanWrite) continue;
                try
                {
                    pi.SetValue(this, JsonSerializer.Deserialize(kv.Value, pi.PropertyType));
                }
                catch
                {
                }
            }
            ActiveProfile = p.Name;
            Normalize();
            return true;
        }

        void SeedProfiles()
        {
            Profiles = new List<NamedProfile>();
            var docs = new AppSettings { Format = "pdf", PdfPageMode = "a4", PdfJpegQuality = 75 };
            Profiles.Add(docs.CaptureProfile("Documents (PDF)"));
            var hq = new AppSettings { Format = "png", ScalePercent = 100 };
            Profiles.Add(hq.CaptureProfile("High quality (PNG)"));
            var small = new AppSettings { Format = "jpg", JpgQuality = 70, ScalePercent = 80, SplitEnabled = true };
            Profiles.Add(small.CaptureProfile("Small & shareable (JPG)"));
        }

        public string SaveFolder { get; set; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures), "ScrollShot");
        public string Language { get; set; } = "en";
        public string Theme { get; set; } = "dark";
        public string AccentColor { get; set; } = "#FF3B82F6";
        public string EditorPath { get; set; } = "";
        public string UploadUrl { get; set; } = "";
        public string UploadFileField { get; set; } = "file";
        public string UploadHeader { get; set; } = "";
        public string UploadResponseRegex { get; set; } = "";
        public bool UploadAuto { get; set; } = false;
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

        public bool TrimHeader { get; set; } = false;
        public bool TrimFooter { get; set; } = false;
        public string ScrollDirection { get; set; } = "vertical";
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
            if (Profiles == null) SeedProfiles();
            if (ActiveProfile == null) ActiveProfile = "";
            if (EditorPath == null) EditorPath = "";
            if (UploadUrl == null) UploadUrl = "";
            if (string.IsNullOrWhiteSpace(UploadFileField)) UploadFileField = "file";
            if (UploadHeader == null) UploadHeader = "";
            if (UploadResponseRegex == null) UploadResponseRegex = "";
            if (ProfileHotkey == null) ProfileHotkey = new HotkeyDef();
            if (RegionDelayHotkey == null) RegionDelayHotkey = new HotkeyDef();
            if (RecordHotkey == null) RecordHotkey = new HotkeyDef();
            if (RecordFormat != "gif" && RecordFormat != "mp4") RecordFormat = "gif";
            if (FfmpegPath == null) FfmpegPath = "";
            RecordFps = Math.Clamp(RecordFps, 5, 30);
            RecordMaxSeconds = Math.Clamp(RecordMaxSeconds, 3, 600);
            RecordScalePercent = Math.Clamp(RecordScalePercent, 25, 100);
            RecordCountdownSec = Math.Clamp(RecordCountdownSec, 0, 10);
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
            if (ScrollDirection != "vertical" && ScrollDirection != "horizontal") ScrollDirection = "vertical";
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
