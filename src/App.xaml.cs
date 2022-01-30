using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Threading;
using ATP.Internal;
using NLog;
using Application = System.Windows.Application;
using MessageBox = System.Windows.MessageBox;

namespace ATP
{
    public partial class App
    {
        private AtpTrayIcon _atpTrayIcon;
        private QuickAppService _quickAppService;
        private MainWindow _mainWindow;

        public App()
        {
            AppDomain.CurrentDomain.UnhandledException += CurrentDomainOnUnhandledException;
            Current.DispatcherUnhandledException += CurrentOnDispatcherUnhandledException;
        }

        private void CurrentOnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            LogManager.GetCurrentClassLogger().Error(e.Exception.Message);
            MessageBox.Show("CRASH!!!!");
        }

        private void CurrentDomainOnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            LogManager.GetCurrentClassLogger().Error(e.ExceptionObject);
            MessageBox.Show("CRASH!!!!");
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            if (!Directory.Exists(Constants.DataDirectory))
                Directory.CreateDirectory(Constants.DataDirectory);

            if (!Directory.Exists(Constants.IconCacheDirectory))
                Directory.CreateDirectory(Constants.IconCacheDirectory);

            _quickAppService = new QuickAppService();

            _atpTrayIcon = new AtpTrayIcon();
            _atpTrayIcon.OnOpen += AtpTrayIconOnOpen;

            ShowMainWindow();
        }

        private void AtpTrayIconOnOpen()
        {
            ShowMainWindow();
        }

        private void ShowMainWindow()
        {
            if (_mainWindow != null)
            {
                NativeMethods.BringWindowToFront(
                    (uint)Process.GetCurrentProcess().Id,
                    Process.GetCurrentProcess().Handle);
                return;
            }

            _mainWindow = new MainWindow(_quickAppService);
            _mainWindow.Closed += (sender, args) =>
            {
                _mainWindow = null;
            };

            Application.Current.MainWindow = _mainWindow;
            Application.Current.MainWindow.Show();
        }
    }
}
