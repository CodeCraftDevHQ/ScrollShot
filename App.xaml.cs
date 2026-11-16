using System.Threading;
using System.Windows;

namespace ScrollShot
{
    public partial class App : Application
    {
        Mutex _mutex;
        CaptureController _controller;
        TrayController _tray;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            bool created;
            _mutex = new Mutex(true, "ScrollShot.SingleInstance", out created);
            if (!created)
            {
                Shutdown();
                return;
            }
            DispatcherUnhandledException += (s, a) =>
            {
                MessageBox.Show(a.Exception.Message, "ScrollShot");
                a.Handled = true;
            };
            var settings = SettingsStore.Load();
            Loc.SetLanguage(settings.Language);
            ThemeService.Apply(settings);
            _controller = new CaptureController(settings);
            _tray = new TrayController(_controller);
            _controller.Start(_tray);
        }

        protected override void OnExit(ExitEventArgs e)
        {
            if (_tray != null) _tray.Dispose();
            base.OnExit(e);
        }
    }
}
