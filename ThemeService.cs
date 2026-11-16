using System;
using System.Windows.Media;
using Microsoft.Win32;
using ModernWpf;

namespace ScrollShot
{
    internal static class ThemeService
    {
        public static event Action Changed;

        public static bool IsDark { get; private set; } = true;

        public static void Apply(AppSettings s)
        {
            Apply(s.Theme, s.AccentColor);
        }

        public static void Apply(string theme, string accent)
        {
            ApplicationTheme? t = null;
            if (theme == "dark") t = ApplicationTheme.Dark;
            else if (theme == "light") t = ApplicationTheme.Light;
            ThemeManager.Current.ApplicationTheme = t;
            try
            {
                ThemeManager.Current.AccentColor = (Color)ColorConverter.ConvertFromString(accent);
            }
            catch
            {
            }
            IsDark = theme == "dark" || (theme == "system" && SystemIsDark());
            var h = Changed;
            if (h != null) h();
        }

        static bool SystemIsDark()
        {
            try
            {
                using (var k = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize"))
                {
                    object v = k == null ? null : k.GetValue("AppsUseLightTheme");
                    return v is int && (int)v == 0;
                }
            }
            catch
            {
                return false;
            }
        }
    }
}
