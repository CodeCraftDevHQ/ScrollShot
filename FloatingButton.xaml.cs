using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Interop;

namespace ScrollShot
{
    public partial class FloatingButton : Window
    {
        readonly CaptureController _ctl;
        bool _down;
        Point _downPt;

        internal FloatingButton(CaptureController ctl)
        {
            _ctl = ctl;
            InitializeComponent();
            double size = ctl.Settings.FloatingSize;
            Width = size;
            Height = size;
            Opacity = ctl.Settings.FloatingOpacity;
            Glyph.FontSize = size * 0.45;
            Loaded += OnLoaded;
        }

        void OnLoaded(object sender, RoutedEventArgs e)
        {
            var h = new WindowInteropHelper(this).Handle;
            WinUtil.ExcludeFromCapture(h);
            var st = _ctl.Settings;
            var r = WinUtil.GetRect(h);
            bool placed = false;
            if (st.FloatingX.HasValue && st.FloatingY.HasValue)
            {
                var want = new System.Drawing.Rectangle(st.FloatingX.Value, st.FloatingY.Value, r.Width, r.Height);
                if (System.Windows.Forms.Screen.AllScreens.Any(s => s.Bounds.IntersectsWith(want)))
                {
                    WinUtil.MoveTo(h, want.X, want.Y);
                    placed = true;
                }
            }
            if (!placed)
            {
                var wa = System.Windows.Forms.Screen.PrimaryScreen.WorkingArea;
                WinUtil.MoveTo(h, wa.Right - r.Width - 20, wa.Y + wa.Height / 2);
            }
        }

        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonDown(e);
            _down = true;
            _downPt = e.GetPosition(this);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (_down && e.LeftButton == MouseButtonState.Pressed)
            {
                var p = e.GetPosition(this);
                if (Math.Abs(p.X - _downPt.X) > 4 || Math.Abs(p.Y - _downPt.Y) > 4)
                {
                    _down = false;
                    DragMove();
                    SavePosition();
                }
            }
        }

        protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonUp(e);
            if (_down)
            {
                _down = false;
                ShowMenu();
            }
        }

        void SavePosition()
        {
            var r = WinUtil.GetRect(new WindowInteropHelper(this).Handle);
            _ctl.Settings.FloatingX = r.X;
            _ctl.Settings.FloatingY = r.Y;
            _ctl.SaveSettings();
        }

        void ShowMenu()
        {
            var m = new ContextMenu
            {
                FlowDirection = FlowDirection.RightToLeft,
                PlacementTarget = this,
                Placement = PlacementMode.Top
            };
            AddItem(m, "گرفتن از ناحیه", _ctl.Region);
            AddItem(m, "گرفتن از پنجره", _ctl.WindowCap);
            AddItem(m, "تمام صفحه", _ctl.Full);
            AddItem(m, "اسکرین‌شات اسکرولی", _ctl.Scroll);
            AddItem(m, "تنظیمات", _ctl.OpenSettings);
            m.IsOpen = true;
        }

        static void AddItem(ContextMenu m, string header, Action action)
        {
            var item = new MenuItem { Header = header };
            item.Click += (a, b) => action();
            m.Items.Add(item);
        }
    }
}
