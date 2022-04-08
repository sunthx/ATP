using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Threading;
using ATP.Internal;
using ATP.Internal.Services;
using ATP.Internal.TrayIcon;
using ATP.Internal.Utils;
using ATP.Views;
using NLog;
using Prism.Ioc;
using MessageBox = System.Windows.MessageBox;

namespace ATP
{
    public partial class App
    {
        private ATPTrayIcon _atpTrayIcon;
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
        
            _atpTrayIcon = new ATPTrayIcon();
            _atpTrayIcon.OnOpen += AtpTrayIconOnOpen;
        }

        private void AtpTrayIconOnOpen()
        {
            if (_mainWindow != null)
            {
                var currentProcess = Process.GetCurrentProcess();
                NativeMethods.BringWindowToFront((uint)currentProcess.Id, currentProcess.MainWindowHandle);
            }
            else
            {
                CreateShell();
                _mainWindow.Show();
            }
        }

        protected override void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterSingleton<IAppService, AppService>();
        }

        protected override Window CreateShell()
        {
            _mainWindow = Container.Resolve<MainWindow>();
            _mainWindow.Closed += (sender, args) =>
            {
                _mainWindow = null;
            };

            return _mainWindow;
        }
    }
}
