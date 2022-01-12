using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Forms;
using Newtonsoft.Json;
using static Vanara.PInvoke.User32;
using MessageBox = System.Windows.MessageBox;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;
using Path = System.IO.Path;

namespace AltTabPlus
{
    public partial class MainWindow : Window
    {
        private HHOOK? _globalKeyboardHook;
        private HookProc _keyboardHookProc;

        private bool _isRecordingShortcut;
        private ToggleButton _currentRecordButton;

        private Dictionary<string, InstalledApplication> _cache;

        public MainWindow()
        {
            InitializeComponent();
            Loaded += MainWindow_Loaded;
            Closed += OnClosed;
        }

        public ObservableCollection<InstalledApplication> InstalledApplications { get; set; }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            if (!Directory.Exists(Constants.DataDirectory))
                Directory.CreateDirectory(Constants.DataDirectory);

            if (!Directory.Exists(Constants.IconCacheDirectory))
                Directory.CreateDirectory(Constants.IconCacheDirectory);

            _cache = new Dictionary<string, InstalledApplication>();
            InstalledApplications = new ObservableCollection<InstalledApplication>();

            var data = LoadDataFromConfigFile();
            if (data.Any())
            {
                data.ForEach(item =>
                {
                    InstalledApplications.Add(item);
                    if (!string.IsNullOrEmpty(item.HotKey))
                    {
                        _cache.Add(item.HotKey,item);
                    }
                });
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

        private void SetShortcutButton_Click(object sender, RoutedEventArgs e)
        {
            _currentRecordButton = (ToggleButton)sender;
            if (!_currentRecordButton.IsChecked.Value)
            {
                _isRecordingShortcut = true;
            }
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
            var keyboardMessageType = (WindowMessage)wParam.ToInt32();
            var keyboardInput = (KEYBDINPUT)Marshal.PtrToStructure(lParam, typeof(KEYBDINPUT));
            var keyData = (Keys)keyboardInput.wVk;

            if (keyboardMessageType is WindowMessage.WM_KEYDOWN or WindowMessage.WM_SYSKEYDOWN)
            {
                switch (keyData)
                {
                    case Keys.ControlKey or Keys.LControlKey or Keys.RControlKey:
                        MessageBox.Show("ControlKey d");
                        break;
                    case Keys.LShiftKey or Keys.RShiftKey:
                        MessageBox.Show("ShiftKey");
                        break;
                    case Keys.LMenu or Keys.RMenu:
                        MessageBox.Show("Alt d");
                        break;
                    default:
                        MessageBox.Show(keyData.ToString());
                        break;
                }
            }

            if (keyboardMessageType is WindowMessage.WM_KEYUP or WindowMessage.WM_SYSKEYUP)
            {
                switch (keyData)
                {
                    case Keys.ControlKey or Keys.LControlKey or Keys.RControlKey:
                        MessageBox.Show("ControlKey u");
                        break;
                    case Keys.LShiftKey or Keys.RShiftKey:
                        MessageBox.Show("ShiftKey");
                        break;
                    case Keys.LMenu or Keys.RMenu:
                        MessageBox.Show("Alt p");
                        break;
                    default:
                        MessageBox.Show(keyData.ToString());
                        break;
                }
            }

            return CallNextHookEx(_globalKeyboardHook.Value,code, wParam, lParam);
        }
    }

    public class Shortcut
    {
        public List<Modifiers> Modifiers { get; set; }
        public string Key { get; set; }
    }

    public enum Modifiers
    {
        Control = 1,
        Shift,
        Alt
    }
}
