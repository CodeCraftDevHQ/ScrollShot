using System;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace ScrollShot
{
    internal static class ShareService
    {
        static readonly HttpClient Http = new HttpClient { Timeout = TimeSpan.FromSeconds(120) };

        static void SetClipboard(Action action)
        {
            for (int i = 0; i < 5; i++)
            {
                try
                {
                    action();
                    return;
                }
                catch
                {
                    Thread.Sleep(80);
                }
            }
        }

        public static void CopyText(string text)
        {
            SetClipboard(() => System.Windows.Clipboard.SetText(text));
        }

        public static void CopyFile(string path)
        {
            var sc = new StringCollection();
            sc.Add(path);
            SetClipboard(() => System.Windows.Clipboard.SetFileDropList(sc));
        }

        public static void OpenWith(string path, AppSettings s)
        {
            string exe = string.IsNullOrWhiteSpace(s.EditorPath) ? "mspaint.exe" : s.EditorPath;
            Process.Start(new ProcessStartInfo(exe, "\"" + path + "\"") { UseShellExecute = true });
        }

        static string Mime(string path)
        {
            switch (Path.GetExtension(path).ToLowerInvariant())
            {
                case ".png": return "image/png";
                case ".jpg":
                case ".jpeg": return "image/jpeg";
                case ".bmp": return "image/bmp";
                case ".gif": return "image/gif";
                case ".pdf": return "application/pdf";
                case ".mp4": return "video/mp4";
                default: return "application/octet-stream";
            }
        }

        public static async Task<string> UploadAsync(string path, AppSettings s)
        {
            using (var form = new MultipartFormDataContent())
            using (var fs = File.OpenRead(path))
            {
                var fc = new StreamContent(fs);
                fc.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(Mime(path));
                form.Add(fc, string.IsNullOrWhiteSpace(s.UploadFileField) ? "file" : s.UploadFileField, Path.GetFileName(path));
                using (var req = new HttpRequestMessage(HttpMethod.Post, s.UploadUrl) { Content = form })
                {
                    if (!string.IsNullOrWhiteSpace(s.UploadHeader))
                    {
                        int i = s.UploadHeader.IndexOf(':');
                        if (i > 0) req.Headers.TryAddWithoutValidation(s.UploadHeader.Substring(0, i).Trim(), s.UploadHeader.Substring(i + 1).Trim());
                    }
                    using (var resp = await Http.SendAsync(req))
                    {
                        string body = (await resp.Content.ReadAsStringAsync()).Replace("\\/", "/");
                        if (!resp.IsSuccessStatusCode) throw new Exception((int)resp.StatusCode + " " + resp.ReasonPhrase);
                        string pattern = string.IsNullOrWhiteSpace(s.UploadResponseRegex) ? @"https?://[^\s""'<>\\]+" : s.UploadResponseRegex;
                        var m = Regex.Match(body, pattern);
                        if (!m.Success) throw new Exception(body.Length > 120 ? body.Substring(0, 120) : body);
                        return m.Groups.Count > 1 && m.Groups[1].Success ? m.Groups[1].Value : m.Value;
                    }
                }
            }
        }
    }
}
