using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using D = System.Drawing;

namespace ScrollShot
{
    public partial class RegionOverlay : Window
    {
        readonly D.Bitmap _desktop;
        readonly D.Rectangle _vb;
        readonly List<WinInfo> _wins;
        readonly AppSettings _s;
        readonly bool _windowOnly;
        double _scale = 1;
        bool _down;
        bool _dragging;
        Point _startPx;
        Point _lastPx;
        D.Rectangle? _hover;

        public D.Rectangle? Result { get; private set; }

        internal RegionOverlay(D.Bitmap desktop, D.Rectangle vb, List<WinInfo> wins, AppSettings s, bool windowOnly)
        {
            _desktop = desktop;
            _vb = vb;
            _wins = wins;
            _s = s;
            _windowOnly = windowOnly;
            InitializeComponent();
            Dim.Opacity = s.OverlayDim;
            Sel.Stroke = WinUtil.ParseBrush(s.SelectionColor, Brushes.DodgerBlue);
            Sel.StrokeThickness = s.SelectionThickness;
            Loaded += (a, b) =>
            {
                Activate();
                Focus();
                Native.SetForegroundWindow(new WindowInteropHelper(this).Handle);
            };
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            var h = new WindowInteropHelper(this).Handle;
            WinUtil.SwallowDpiChange(h);
            WinUtil.SetPhysical(h, _vb.X, _vb.Y, _vb.Width, _vb.Height, true, false);
            _scale = VisualTreeHelper.GetDpi(this).DpiScaleX;
            Root.Width = _vb.Width / _scale;
            Root.Height = _vb.Height / _scale;
            Bg.Width = Root.Width;
            Bg.Height = Root.Height;
            Bg.Source = ScreenGrab.ToSource(_desktop);
            HLine.Visibility = _s.ShowCrosshair ? Visibility.Visible : Visibility.Collapsed;
            VLine.Visibility = _s.ShowCrosshair ? Visibility.Visible : Visibility.Collapsed;
            UpdateVisual(null);
        }

        Point Phys(MouseEventArgs e)
        {
            var p = e.GetPosition(Root);
            return new Point(p.X * _scale, p.Y * _scale);
        }

        D.Point ToScreen(Point px)
        {
            return new D.Point(_vb.X + (int)px.X, _vb.Y + (int)px.Y);
        }

        D.Rectangle Norm(Point a, Point b)
        {
            int x1 = (int)Math.Min(a.X, b.X);
            int y1 = (int)Math.Min(a.Y, b.Y);
            int x2 = (int)Math.Max(a.X, b.X);
            int y2 = (int)Math.Max(a.Y, b.Y);
            var r = D.Rectangle.FromLTRB(_vb.X + x1, _vb.Y + y1, _vb.X + x2, _vb.Y + y2);
            return D.Rectangle.Intersect(r, _vb);
        }

        D.Rectangle? HitTest(D.Point sp)
        {
            if (!_s.HighlightWindows && !_windowOnly) return null;
            var w = ScreenGrab.WindowAt(_wins, sp);
            if (w == null) return null;
            D.Rectangle rect = w.Bounds;
            if ((Keyboard.Modifiers & ModifierKeys.Control) != 0)
            {
                var child = ScreenGrab.ChildRectAt(w.Handle, sp);
                if (child.HasValue) rect = child.Value;
            }
            rect = D.Rectangle.Intersect(rect, _vb);
            if (rect.Width < 4 || rect.Height < 4) return null;
            return rect;
        }

        void UpdateCross(Point px)
        {
            double x = px.X / _scale;
            double y = px.Y / _scale;
            HLine.X1 = 0;
            HLine.X2 = Root.Width;
            HLine.Y1 = y;
            HLine.Y2 = y;
            VLine.Y1 = 0;
            VLine.Y2 = Root.Height;
            VLine.X1 = x;
            VLine.X2 = x;
        }

        void UpdateVisual(D.Rectangle? sel)
        {
            var full = new Rect(0, 0, Root.Width, Root.Height);
            if (!sel.HasValue)
            {
                Dim.Data = new RectangleGeometry(full);
                Sel.Visibility = Visibility.Collapsed;
                SizeTag.Visibility = Visibility.Collapsed;
                return;
            }
            var r = sel.Value;
            var rect = new Rect((r.X - _vb.X) / _scale, (r.Y - _vb.Y) / _scale, r.Width / _scale, r.Height / _scale);
            Dim.Data = new CombinedGeometry(GeometryCombineMode.Exclude, new RectangleGeometry(full), new RectangleGeometry(rect));
            Canvas.SetLeft(Sel, rect.X);
            Canvas.SetTop(Sel, rect.Y);
            Sel.Width = rect.Width;
            Sel.Height = rect.Height;
            Sel.Visibility = Visibility.Visible;
            if (_s.ShowSizeLabel)
            {
                SizeText.Text = r.Width + " × " + r.Height;
                double ty = rect.Y - 26;
                if (ty < 0) ty = rect.Y + 4;
                Canvas.SetLeft(SizeTag, Math.Max(0, rect.X));
                Canvas.SetTop(SizeTag, ty);
                SizeTag.Visibility = Visibility.Visible;
            }
        }

        void RefreshHover()
        {
            if (_dragging) return;
            _hover = HitTest(ToScreen(_lastPx));
            UpdateVisual(_hover);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            var pp = Phys(e);
            _lastPx = pp;
            UpdateCross(pp);
            if (_down)
            {
                if (!_dragging && !_windowOnly && (Math.Abs(pp.X - _startPx.X) > 4 || Math.Abs(pp.Y - _startPx.Y) > 4))
                {
                    _dragging = true;
                }
                if (_dragging)
                {
                    UpdateVisual(Norm(_startPx, pp));
                    return;
                }
            }
            RefreshHover();
        }

        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonDown(e);
            _down = true;
            _startPx = Phys(e);
            CaptureMouse();
        }

        protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonUp(e);
            if (!_down) return;
            _down = false;
            ReleaseMouseCapture();
            var pp = Phys(e);
            if (_dragging)
            {
                var r = Norm(_startPx, pp);
                if (r.Width >= 4 && r.Height >= 4) Result = r;
                Close();
                return;
            }
            RefreshHover();
            if (_hover.HasValue)
            {
                Result = _hover;
                Close();
            }
        }

        protected override void OnMouseRightButtonUp(MouseButtonEventArgs e)
        {
            base.OnMouseRightButtonUp(e);
            Result = null;
            Close();
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (e.Key == Key.Escape)
            {
                Result = null;
                Close();
                return;
            }
            if (e.Key == Key.LeftCtrl || e.Key == Key.RightCtrl) RefreshHover();
        }

        protected override void OnKeyUp(KeyEventArgs e)
        {
            base.OnKeyUp(e);
            if (e.Key == Key.LeftCtrl || e.Key == Key.RightCtrl) RefreshHover();
        }
    }
}
