using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Threading;
using NLog;
using Application = System.Windows.Application;
using MessageBox = System.Windows.MessageBox;

namespace ATP
{
    public partial class App
    {
        private AtpTrayIcon _atpTrayIcon;
        private QuickOpenAppManage _quickOpenAppManage;

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

            var processId = Process.GetCurrentProcess().Id;
            _quickOpenAppManage = new QuickOpenAppManage(processId);
            _quickOpenAppManage.StartListening();

            _atpTrayIcon = new AtpTrayIcon();
            _atpTrayIcon.OpenPreferences += _atpTrayIcon_OpenPreferences;
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _quickOpenAppManage.StopListening();
            base.OnExit(e);
        }

        private void _atpTrayIcon_OpenPreferences()
        {
        }
    }
}
