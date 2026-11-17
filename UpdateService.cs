using System;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;

namespace ScrollShot
{
    internal sealed class UpdateInfo
    {
        public Version Latest;
        public string DownloadUrl;
        public string Notes;
        public bool IsNewer;
    }

    internal static class UpdateService
    {
        static readonly HttpClient Http = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };

        public static Version Current
        {
            get
            {
                var v = Assembly.GetExecutingAssembly().GetName().Version;
                return v ?? new Version(1, 0, 0, 0);
            }
        }

        public static string CurrentText
        {
            get
            {
                var v = Current;
                return v.Major + "." + v.Minor + "." + Math.Max(0, v.Build);
            }
        }

        public static bool TryParse(string text, out Version v)
        {
            v = null;
            if (string.IsNullOrWhiteSpace(text)) return false;
            text = text.Trim().TrimStart('v', 'V');
            int dash = text.IndexOfAny(new[] { '-', '+' });
            if (dash > 0) text = text.Substring(0, dash);
            if (!Version.TryParse(text, out v)) return false;
            if (v.Build < 0) v = new Version(v.Major, v.Minor, 0);
            return true;
        }

        public static async Task<UpdateInfo> CheckAsync(string url)
        {
            string json = await Http.GetStringAsync(url);
            using (var doc = JsonDocument.Parse(json))
            {
                var root = doc.RootElement;
                string ver = Read(root, "version");
                if (ver == null) ver = Read(root, "tag_name");
                Version latest;
                if (!TryParse(ver, out latest)) throw new FormatException("version");
                string dl = Read(root, "url");
                if (dl == null) dl = Read(root, "html_url");
                var info = new UpdateInfo
                {
                    Latest = latest,
                    DownloadUrl = dl,
                    Notes = Read(root, "notes") ?? Read(root, "body") ?? ""
                };
                var cur = Current;
                var curCmp = new Version(cur.Major, cur.Minor, Math.Max(0, cur.Build));
                var latCmp = new Version(latest.Major, latest.Minor, Math.Max(0, latest.Build));
                info.IsNewer = latCmp > curCmp;
                return info;
            }
        }

        static string Read(JsonElement e, string name)
        {
            JsonElement v;
            if (e.ValueKind == JsonValueKind.Object && e.TryGetProperty(name, out v) && v.ValueKind == JsonValueKind.String) return v.GetString();
            return null;
        }
    }
}
