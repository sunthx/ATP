using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Drawing.Printing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using NLog;

namespace ATP
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
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
    }
}
