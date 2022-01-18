using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Permissions;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Forms;
using System.Windows.Media;
using Newtonsoft.Json;
using NLog;
using Vanara.PInvoke;
using static Vanara.PInvoke.User32;
using Control = System.Windows.Forms.Control;
using MessageBox = System.Windows.MessageBox;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;
using Path = System.IO.Path;

namespace AltTabPlus
{
    public partial class MainWindow : Window
    {
        private SafeHHOOK _globalKeyboardHook;
        private HookProc _keyboardHookProc;
        private readonly ILogger _logger = LogManager.GetCurrentClassLogger();  

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
            NativeMethods.ReleaseWindowsHook(_globalKeyboardHook);
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

            for (var i = 0; i < LbApp.Items.Count; i++)
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
            //https://github.com/Code52/carnac/blob/master/src/Carnac.Logic/KeyMonitor/InterceptKeys.cs
            var alt = (Control.ModifierKeys & Keys.Alt) != 0;
            var control = (Control.ModifierKeys & Keys.Control) != 0;
            var shift = (Control.ModifierKeys & Keys.Shift) != 0;
            var keyDown = wParam == (IntPtr)WindowMessage.WM_KEYDOWN;
            var keyUp = wParam == (IntPtr)WindowMessage.WM_KEYUP;
            var vkCode = Marshal.ReadInt32(lParam);
            var key = (Keys)vkCode;

            //http://msdn.microsoft.com/en-us/library/windows/desktop/ms646286(v=vs.85).aspx
            if (key != Keys.RMenu && key != Keys.LMenu && wParam == (IntPtr)WindowMessage.WM_SYSKEYDOWN)
            {
                alt = true;
                keyDown = true;
            }
            if (key != Keys.RMenu && key != Keys.LMenu && wParam == (IntPtr)WindowMessage.WM_SYSKEYUP)
            {
                alt = true;
                keyUp = true;
            }

            var shortcut = new Shortcut(
                key,
                keyDown ?
                    KeyDirection.Down : keyUp
                        ? KeyDirection.Up : KeyDirection.Unknown,
                alt, control, shift);

            var isValid = shortcut.IsValid();
            if (isValid)
            {
                var shortcutString = shortcut.ToString();
                _logger.Info($"{shortcutString}");
                HandleShortcut(shortcutString);
            }

            
            return CallNextHookEx(_globalKeyboardHook, code, wParam, lParam);
        }

        private void SetShortcut(string shortcut)
        {
            Dispatcher.Invoke(() =>
            {
                _currentRecordButton.Content = shortcut;
                _currentSelectedAppItem.HotKey = shortcut;
            });
        }

        private void StopRecordShortcut()
        {
            SaveConfig(InstalledApplications.ToList());
            RefreshData(false);
        }

        private void HandleShortcut(string combinationKeys)
        {
            if (_isRecordingShortcut)
            {
                //录制
                RecordShortcut(combinationKeys);
            }
            else
            {
                //匹配
                MatchShortcut(combinationKeys);
            }
        }

        private void RecordShortcut(string shortcut)
        {
            SetShortcut(shortcut);
        }

        private void MatchShortcut(string shortcut)
        {
            _logger.Info($"current shortcut:{shortcut}");

            if (!_cache.ContainsKey(shortcut))
            {
                _logger.Info($"not found 【{shortcut}】 process.");
            }

            var app = _cache[shortcut];
            _logger.Info($"found 【{shortcut}】 process {app.Location}.");

            var queryResult = Process
                .GetProcesses()
                .FirstOrDefault(item => item.MainWindowHandle != IntPtr.Zero && item.MainModule?.ModuleName == app.FileName);
            if (queryResult != default)
            {
                NativeMethods.BringWindowToFront(queryResult.MainWindowHandle);
            }
            else
            {
                _logger.Info($"【{shortcut}】 handle process not running, start app ({app.Location}).");
                Task.Factory.StartNew(() =>
                {
                    if (File.Exists(app.Location))
                        Process.Start(app.Location);
                });
            }
        }
    }

    public class Shortcut
    {
        public Shortcut(Keys key, KeyDirection keyDirection, bool altPressed, bool controlPressed, bool shiftPressed)
        {
            AltPressed = altPressed;
            ControlPressed = controlPressed;
            Key = key;
            KeyDirection = keyDirection;
            ShiftPressed = shiftPressed;
        }

        public bool AltPressed { get; private set; }
        public bool ControlPressed { get; private set; }
        public bool ShiftPressed { get; private set; }
        public Keys Key { get; private set; }
        public KeyDirection KeyDirection { get; private set; }

        public bool IsValid()
        {
            return (AltPressed || ControlPressed || ShiftPressed) && (IsLetter() || IsNumber()) && KeyDirection == KeyDirection.Down;
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            if (ControlPressed)
                sb.Append("Ctrl + ");
            if (ShiftPressed)
                sb.Append("Shift + ");
            if (AltPressed)
                sb.Append("Alt + ");

            var keyString = IsNumber() ? Key.ToString().Remove(0,1) :  Key.ToString();
            sb.Append(keyString);

            return sb.ToString();
        }

        public bool IsLetter()
        {
            return Key is >= Keys.A and <= Keys.Z;
        }

        public bool IsNumber()
        {
            return Key is >= Keys.D0 and <= Keys.D9;
        }
    }

    public enum KeyDirection
    {
        Down,
        Up,
        Unknown
    }

    public class InstalledApplication
    {
        public string Id { get; set; }
        public string DisplayName { get; set; }
        public string Location { get; set; }
        public string FileName { set; get; }
        public string Icon { get; set; }
        public string HotKey { get; set; }

        public static InstalledApplication New(string appFilePath)
        {
            var installApp = new InstalledApplication
            {
                Location = appFilePath,
                DisplayName = Path.GetFileNameWithoutExtension(appFilePath),
                FileName = Path.GetFileName(appFilePath)
            };

            return installApp;
        }
    }
}
