using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Forms;
using System.Windows.Media;
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
        private readonly Dictionary<int, Keys> _pressedKeys = new();     
        
        private ToggleButton _currentRecordButton;
        private InstalledApplication _currentSelectedAppItem;

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

            RefreshData(true);

            LbApp.ItemsSource = InstalledApplications;

            _keyboardHookProc = KeyboardHookProc;
            _globalKeyboardHook = NativeMethods.RegisterKeyboardHook(_keyboardHookProc);
        }

        private void RefreshData(bool isUpdateView = false)
        {
            var data = LoadDataFromConfigFile();
            if (data.Any())
            {
                data.ForEach(item =>
                {
                    if(isUpdateView)
                        InstalledApplications.Add(item);

                    if (string.IsNullOrEmpty(item.HotKey)) 
                        return;

                    if(!_cache.ContainsKey(item.HotKey))
                        _cache.Add(item.HotKey, item);
                });
            }
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
            if (sender is not ToggleButton toggleButton)
                return;

            _currentRecordButton = toggleButton;
            if (_currentRecordButton.Tag is InstalledApplication application)
            {
                _currentSelectedAppItem = application;
            }

            _isRecordingShortcut = _currentRecordButton.IsChecked ?? false;
            if (!_isRecordingShortcut)
            {
                StopRecordShortcut();
                return;
            }

            GetRecordButtonFromListBoxItemDataTemplate().ForEach(item => item.IsChecked = false);
            _currentRecordButton.IsChecked = true;
        }

        private List<ToggleButton> GetRecordButtonFromListBoxItemDataTemplate()
        {
            var result = new List<ToggleButton>();

            for (int i = 0; i < LbApp.Items.Count; i++)
            {
                var contentPresenter = FindVisualChild<ContentPresenter>((ListBoxItem)LbApp.ItemContainerGenerator.ContainerFromItem(LbApp.Items[i]));
                var dataTemplate = contentPresenter.ContentTemplate;
                var target = (ToggleButton)dataTemplate.FindName("TgbRecord", contentPresenter);
                result.Add(target);
            }

            return result;
        }

        private TChildItem FindVisualChild<TChildItem>(DependencyObject obj)
            where TChildItem : DependencyObject
        {
            for (var i = 0; i < VisualTreeHelper.GetChildrenCount(obj); i++)
            {
                var child = VisualTreeHelper.GetChild(obj, i);
                if (child != null && child is TChildItem)
                    return (TChildItem)child;
                else
                {
                    var childOfChild = FindVisualChild<TChildItem>(child);
                    if (childOfChild != null)
                        return childOfChild;
                }
            }
            return null;
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
            if (File.Exists(Constants.ConfigFilePath))
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
                OnKeyPressed(keyData);
            }

            if (keyboardMessageType is WindowMessage.WM_KEYUP or WindowMessage.WM_SYSKEYUP)
            {
                OnKeyUp(keyData);
            }

            return CallNextHookEx(_globalKeyboardHook.Value, code, wParam, lParam);
        }

        private void OnKeyPressed(Keys key)
        {
            var keyIndex = GetKeyIndex(key);
            if (_pressedKeys.Values.Count >= 4 || _pressedKeys.ContainsKey(keyIndex))
                return;

            _pressedKeys.Add(keyIndex, key);
            var keys = _pressedKeys.Values.OrderByDescending(item => item).ToList();
            var combinationKeys = string.Join('+', keys);

            HandleCombinationKeyPressed(combinationKeys);
        }

        private void OnKeyUp(Keys key)
        {
            var keyIndex = GetKeyIndex(key);
            if (_pressedKeys.ContainsKey(keyIndex))
            {
                _pressedKeys.Remove(keyIndex);
            }

            HandleCombinationKeyPressedUp();
        }

        private int GetKeyIndex(Keys key)
        {
            // return key switch
            // {
            //     Keys.ControlKey or Keys.LControlKey or Keys.RControlKey => (int)Keys.Control,
            //     Keys.Menu or Keys.LMenu or Keys.RMenu => (int)Keys.Alt,
            //     Keys.ShiftKey or Keys.LShiftKey or Keys.RShiftKey => (int)Keys.Shift,
            //     _ => (int)key
            // };

            return (int)key;
        }

        private void HandleCombinationKeyPressedUp()
        {
            if(!_isRecordingShortcut)
                return;

            if (_pressedKeys.Count != 0)
                return;

            if (_isRecordingShortcut)
            {
                StopRecordShortcut();
            }
        }

        private void HandleCombinationKeyPressed(string ck)
        {
            if (_isRecordingShortcut)
            {
                SetShortcut(ck);
                return;
            }

            if (!_cache.ContainsKey(ck)) 
                return;

            var app = _cache[ck];
            var queryResult = GetTargetWindowInPtr(app.Location);

            if (queryResult != null)
            {
                NativeMethods.BringWindowToFront(queryResult);
            }
            else
            {
                if(File.Exists(app.Location))
                     Process.Start(app.Location);
            }
        }

        private void SetShortcut(string shortcut)
        {
            _currentRecordButton.Content = shortcut;
            _currentSelectedAppItem.HotKey = shortcut;
        }

        private void StopRecordShortcut()
        {
            SaveConfig(InstalledApplications.ToList());
            RefreshData(false);
        }

        private IntPtr GetTargetWindowInPtr(string filePath)
        {
            return Process.GetProcesses()
                .Where(item => item.MainWindowHandle != IntPtr.Zero && item.MainModule?.FileName == filePath)
                .Select(item => item.MainWindowHandle)
                .FirstOrDefault();
        }
    }
}