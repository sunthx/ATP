using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Win32;

namespace AltTabPlus
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            InstalledApplications = new ObservableCollection<InstalledApplication>();
            LbApp.ItemsSource = InstalledApplications;
        }

        public ObservableCollection<InstalledApplication> InstalledApplications { get; set; }

        private void BtnAddApp_OnClick(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "应用程序|*.exe",
                Multiselect = false
            };

            var dialogResult = openFileDialog.ShowDialog(this);
            if (!dialogResult.HasValue || !dialogResult.Value) 
                return;

            var filePath = openFileDialog.FileName;
            AddAppInfoToList(filePath);
        }

        private void AddAppInfoToList(string filePath)
        {
            
        }
    }
}
