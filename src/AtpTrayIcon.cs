using System;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Forms;
using Application = System.Windows.Application;

namespace ATP
{
    public class AtpTrayIcon : IDisposable
    {
        private readonly NotifyIcon _trayIcon;

        public AtpTrayIcon()
        {
            var exitMenuItem = new MenuItem
            {
                Text = "退出" 
            };

            var iconStream = Assembly.GetExecutingAssembly().GetManifestResourceStream("ATP.cool.embedded.ico");
            if (iconStream == null)
                throw new ApplicationException("load tray icon failed.");

            _trayIcon = new NotifyIcon
            {
                Icon = new Icon(iconStream),
                ContextMenu = new ContextMenu(new[] { exitMenuItem })
            };

            exitMenuItem.Click += (sender, args) =>
            {
                _trayIcon.Visible = false;
                Application.Current.Shutdown();
            };

            _trayIcon.MouseClick += NotifyIconClick;
            _trayIcon.Visible = true;
        }

        public event Action OnOpen = () => { }; 

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
                    OnOpen();
                }
            }
        }

        public void Dispose()
        {
            _trayIcon.Dispose();
        }
    }
}