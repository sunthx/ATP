using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Newtonsoft.Json;
using Vanara.PInvoke;
using static Vanara.PInvoke.User32;
using MessageBox = System.Windows.MessageBox;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;
using Path = System.IO.Path;

namespace AltTabPlus
{
    public partial class MainWindow : Window
    {
        private User32.HHOOK? _globalKeyboardHook;
        private User32.HookProc _keyboardHookProc;

        public MainWindow()
        {
            InitializeComponent();
            Loaded += MainWindow_Loaded;
            Closed += OnClosed;
        }

        public ObservableCollection<InstalledApplication> InstalledApplications { get; set; }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            InstalledApplications = new ObservableCollection<InstalledApplication>();

            var data = LoadDataFromConfigFile();
            if (data.Any())
            {
                data.ForEach(item => InstalledApplications.Add(item));
            }

            LbApp.ItemsSource = InstalledApplications;

            _keyboardHookProc = KeyboardHookProc;
            _globalKeyboardHook = NativeMethods.RegisterKeyboardHook(_keyboardHookProc);
        }

        private void OnClosed(object? sender, EventArgs e)
        {
            if (_globalKeyboardHook.HasValue)
            {
                NativeMethods.ReleaseWindowsHook(_globalKeyboardHook.Value);
            }
        }

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
            if (InstalledApplications.Any(item => item.Location == filePath))
            {
                return;
            }

            var app = InstalledApplication.New(filePath);
            var iconFilePath = GetAppIconFile(filePath);
            app.Icon = iconFilePath;

            InstalledApplications.Add(app);
            SaveConfig(InstalledApplications.ToList());
        }

        private string GetAppIconFile(string filePath)
        {
            var destinationFile = Path.Combine(Constants.IconCacheDirectory,
                $"{Path.GetFileNameWithoutExtension(filePath)}.png");
            if (File.Exists(destinationFile))
                return destinationFile;

            NativeMethods.ExtractAndSaveAppIconFile(filePath, destinationFile);
            return destinationFile;
        }

        private List<InstalledApplication> LoadDataFromConfigFile()
        {
            if (!File.Exists(Constants.ConfigFilePath))
                return new();


            var data = File.ReadAllText(Constants.ConfigFilePath);
            return JsonConvert.DeserializeObject<List<InstalledApplication>>(data) ?? new();
        }

        private void SaveConfig(List<InstalledApplication> applications)
        {
            var data = JsonConvert.SerializeObject(applications);
            if(File.Exists(Constants.ConfigFilePath))
                File.Delete(Constants.ConfigFilePath);

            using var fs = File.Create(Constants.ConfigFilePath);
            using var streamWriter = new StreamWriter(fs);
            streamWriter.Write(data);
            streamWriter.Flush();
        }

        private IntPtr KeyboardHookProc(int code, IntPtr wParam, IntPtr lParam)
        {
            // var messageCode = wParam.ToInt32();
            // if (messageCode == (int)WindowMessage.WM_KEYUP)
            // {
            //     Dispatcher.Invoke(() => { MessageBox.Show("123"); });
            // }
            //
            // KEYBDINPUT input = (KEYBDINPUT)Marshal.PtrToStructure(lParam,typeof(KEYBDINPUT));
            //
            // //Keys key = (Keys)wParam.ToInt32();
            
            return User32.CallNextHookEx(_globalKeyboardHook.Value,code, wParam, lParam);
        }
    }
}
