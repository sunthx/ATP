using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Drawing;
using System.Drawing.Printing;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Threading;
using NLog;
using Application = System.Windows.Application;
using MessageBox = System.Windows.MessageBox;

namespace ATP
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private ATPTrayIcon _atpTrayIcon;

        public App()
        {
            AppDomain.CurrentDomain.UnhandledException += CurrentDomainOnUnhandledException;
            Application.Current.DispatcherUnhandledException += CurrentOnDispatcherUnhandledException;
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

            _atpTrayIcon = new ATPTrayIcon();

            var mainWindow = new MainWindow();
            mainWindow.Show();

            Application.Current.MainWindow = mainWindow;
            Application.Current.MainWindow.Show();
        }
    }

    public class ATPTrayIcon : IDisposable
    {
        readonly NotifyIcon trayIcon;

        public ATPTrayIcon()
        {
            var exitMenuItem = new MenuItem
            {
                Text = "退出" 
            };

            var iconStream = Assembly.GetExecutingAssembly().GetManifestResourceStream("cool.embedded.ico");

            trayIcon = new NotifyIcon
            {
                Icon = new Icon(iconStream),
                ContextMenu = new ContextMenu(new[] { exitMenuItem })
            };

            exitMenuItem.Click += (sender, args) =>
            {
                trayIcon.Visible = false;
                Application.Current.Shutdown();
            };

            trayIcon.MouseClick += NotifyIconClick;
            trayIcon.Visible = true;
        }

        public event Action OpenPreferences = () => { }; 

        void NotifyIconClick(object sender, MouseEventArgs mouseEventArgs)
        {
            if (mouseEventArgs.Button == MouseButtons.Left)
            {
                var preferencesWindow = Application.Current.Windows.Cast<Window>().FirstOrDefault(x => x.Name == "PreferencesViewWindow");
                if (preferencesWindow != null)
                {
                    preferencesWindow.Activate();
                }
                else
                {
                    OpenPreferences();
                }
            }
        }

        public void Dispose()
        {
            trayIcon.Dispose();
        }
    }
}
