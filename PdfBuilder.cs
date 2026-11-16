using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace ScrollShot
{
    internal sealed class PdfBuilder
    {
        sealed class PageData
        {
            public double W, H, X, Y, DW, DH;
            public byte[] Jpeg;
            public int PxW, PxH;
        }

        readonly List<PageData> _pages = new List<PageData>();

        public void AddPage(double pageW, double pageH, double x, double y, double dw, double dh, byte[] jpeg, int pxW, int pxH)
        {
            _pages.Add(new PageData { W = pageW, H = pageH, X = x, Y = y, DW = dw, DH = dh, Jpeg = jpeg, PxW = pxW, PxH = pxH });
        }

        static string F(double d)
        {
            return d.ToString("0.###", CultureInfo.InvariantCulture);
        }

        public void Save(string path)
        {
            using (var fs = new FileStream(path, FileMode.Create, FileAccess.Write))
            {
                var offsets = new List<long>();
                Action<string> w = s =>
                {
                    var b = Encoding.ASCII.GetBytes(s);
                    fs.Write(b, 0, b.Length);
                };
                w("%PDF-1.4\n");
                fs.Write(new byte[] { 0x25, 0xE2, 0xE3, 0xCF, 0xD3, 0x0A }, 0, 6);
                int n = _pages.Count;

                offsets.Add(fs.Position);
                w("1 0 obj\n<< /Type /Catalog /Pages 2 0 R >>\nendobj\n");

                var kids = new StringBuilder();
                for (int i = 0; i < n; i++) kids.Append(3 + 3 * i).Append(" 0 R ");
                offsets.Add(fs.Position);
                w("2 0 obj\n<< /Type /Pages /Kids [" + kids + "] /Count " + n + " >>\nendobj\n");

                for (int i = 0; i < n; i++)
                {
                    var p = _pages[i];
                    int po = 3 + 3 * i;
                    int co = po + 1;
                    int io = po + 2;

                    offsets.Add(fs.Position);
                    w(po + " 0 obj\n<< /Type /Page /Parent 2 0 R /MediaBox [0 0 " + F(p.W) + " " + F(p.H) + "] /Resources << /XObject << /Im0 " + io + " 0 R >> >> /Contents " + co + " 0 R >>\nendobj\n");

                    string content = "q " + F(p.DW) + " 0 0 " + F(p.DH) + " " + F(p.X) + " " + F(p.Y) + " cm /Im0 Do Q";
                    offsets.Add(fs.Position);
                    w(co + " 0 obj\n<< /Length " + content.Length + " >>\nstream\n" + content + "\nendstream\nendobj\n");

                    offsets.Add(fs.Position);
                    w(io + " 0 obj\n<< /Type /XObject /Subtype /Image /Width " + p.PxW + " /Height " + p.PxH + " /ColorSpace /DeviceRGB /BitsPerComponent 8 /Filter /DCTDecode /Length " + p.Jpeg.Length + " >>\nstream\n");
                    fs.Write(p.Jpeg, 0, p.Jpeg.Length);
                    w("\nendstream\nendobj\n");
                }

                long xref = fs.Position;
                int total = 3 + 3 * n;
                w("xref\n0 " + total + "\n");
                w("0000000000 65535 f \n");
                foreach (long off in offsets) w(off.ToString("D10") + " 00000 n \n");
                w("trailer\n<< /Size " + total + " /Root 1 0 R >>\nstartxref\n" + xref + "\n%%EOF\n");
            }
        }
    }
}
