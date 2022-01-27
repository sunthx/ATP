using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Interop;
using ATP.Themes.Controls;
using Newtonsoft.Json;
using NLog;
using Vanara.PInvoke;
using static Vanara.PInvoke.User32;
using Control = System.Windows.Forms.Control;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;

namespace ATP
{
    public partial class MainWindow : TheWindow 
    {
        private readonly ILogger _logger = LogManager.GetCurrentClassLogger();

        private SafeHHOOK _globalKeyboardHook;
        private HookProc _keyboardHookProc;

        private bool _isContinueWhenHandled = false;
        private bool _isRecordingShortcut;

        private ToggleButton _currentRecordButton;
        private InstalledApp _currentSelectedAppItem;

        private Dictionary<string, InstalledApp> _cache;
        private readonly uint _currentProcessId;

        public MainWindow()
        {
            InitializeComponent();
            Loaded += MainWindow_Loaded;
            Closed += OnClosed;

            // TbVersion.Text = $"version {Assembly.GetExecutingAssembly().GetName().Version}";
            _currentProcessId = (uint)Process.GetCurrentProcess().Id;
        }

       
        public ObservableCollection<InstalledApp> InstalledApplications { get; set; }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            if (!Directory.Exists(Constants.DataDirectory))
                Directory.CreateDirectory(Constants.DataDirectory);

            if (!Directory.Exists(Constants.IconCacheDirectory))
                Directory.CreateDirectory(Constants.IconCacheDirectory);

            _cache = new Dictionary<string, InstalledApp>();
            InstalledApplications = new ObservableCollection<InstalledApp>();
        
            RefreshData(true);

            LbApp.ItemsSource = InstalledApplications;

            _keyboardHookProc = KeyboardHookProc;
            _globalKeyboardHook = NativeMethods.RegisterKeyboardHook(_keyboardHookProc);
        }

        private void OnClosed(object sender, EventArgs e)
        {
            NativeMethods.ReleaseWindowsHook(_globalKeyboardHook);
        }

        private void BtnAddApp_OnClick(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "Application|*.exe",
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
            if (!(sender is ToggleButton toggleButton))
                return;

            _currentRecordButton = toggleButton;
            if (_currentRecordButton.Tag is InstalledApp application)
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
                var contentPresenter = Utils.FindVisualChild<ContentPresenter>((ListBoxItem)LbApp.ItemContainerGenerator.ContainerFromItem(LbApp.Items[i]));
                var dataTemplate = contentPresenter.ContentTemplate;
                var target = (ToggleButton)dataTemplate.FindName("TgbRecord", contentPresenter);
                result.Add(target);
            }

            return result;
        }

        /// <summary>
        /// 重加载数据
        /// </summary>
        /// <param name="isUpdateView">是否刷新GUI</param>
        private void RefreshData(bool isUpdateView = false)
        {
            var data = LoadDataFromConfigFile();
            if (data.Any())
            {
                data.ForEach(item =>
                {
                    if (isUpdateView)
                        InstalledApplications.Add(item);

                    if (string.IsNullOrEmpty(item.HotKey))
                        return;

                    if (!_cache.ContainsKey(item.HotKey))
                        _cache.Add(item.HotKey, item);
                });
            }
        }

        /// <summary>
        /// 将 App 加载到 GUI 上
        /// </summary>
        /// <param name="filePath"></param>
        private void AddAppInfoToList(string filePath)
        {
            if (InstalledApplications.Any(item => item.Location == filePath))
            {
                return;
            }

            var app = InstalledApp.New(filePath);
            var iconFilePath = Utils.GetAppIconFile(filePath);
            app.Icon = iconFilePath;

            InstalledApplications.Add(app);
            SaveConfig(InstalledApplications.ToList());
        }

        /// <summary>
        /// 从配置文件中加载
        /// </summary>  
        private List<InstalledApp> LoadDataFromConfigFile()
        {
            if (!File.Exists(Constants.ConfigFilePath))
                return new List<InstalledApp>();


            var data = File.ReadAllText(Constants.ConfigFilePath);
            return JsonConvert.DeserializeObject<List<InstalledApp>>(data) ?? new List<InstalledApp>();
        }

        /// <summary>
        /// 键盘钩子处理函数
        /// </summary>
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

            var combinationKeys = new CombinationKeys(
                key,
                keyDown ?
                    KeyDirection.Down : keyUp
                        ? KeyDirection.Up : KeyDirection.Unknown,
                alt, control, shift);

            var isValid = combinationKeys.IsValid();
            if (isValid)
            {
                var shortcutString = combinationKeys.ToString();
                _logger.Info($"{shortcutString}");
                var isHandled = HandleCombinationKeys(shortcutString);
                if (isHandled && !_isContinueWhenHandled)
                    return (IntPtr)1;
            }
            else if (!combinationKeys.IsAnyKeyPressed() && _isRecordingShortcut)
            {
                StopRecordShortcut();
            }


            return CallNextHookEx(_globalKeyboardHook, code, wParam, lParam);
        }

        /// <summary>
        /// 处理组合键
        /// </summary>     
        private bool HandleCombinationKeys(string combinationKeys)
        {
            if (_isRecordingShortcut)
            {
                //录制
                ShowShortcut(combinationKeys);
                return true;
            }
            else
            {
                //匹配
                return MatchShortcut(combinationKeys);
            }
        }

        /// <summary>
        /// 显示当前的快捷键 
        /// </summary>                
        private void ShowShortcut(string shortcut)
        {
            Dispatcher.Invoke(() =>
            {
                _currentRecordButton.Content = shortcut;
                _currentSelectedAppItem.HotKey = shortcut;
            });
        }

        /// <summary>
        /// 匹配快捷键
        /// </summary>
        /// <remarks>
        /// 如果匹配成功，则执行对应的操作
        /// </remarks>
        private bool MatchShortcut(string shortcut)
        {
            _logger.Info($"current shortcut:{shortcut}");

            if (!_cache.ContainsKey(shortcut))
            {
                _logger.Info($"not found 【{shortcut}】 process.");
                return false;
            }

            var app = _cache[shortcut];
            _logger.Info($"found 【{shortcut}】 process {app.Location}.");

            var queryResult = Process
                .GetProcesses()
                .FirstOrDefault(item => item.MainWindowHandle != IntPtr.Zero && item.MainModule?.ModuleName == app.FileName);
            
            if (queryResult != default)
            {
                NativeMethods.BringWindowToFront(_currentProcessId,queryResult.MainWindowHandle);
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

            return true;
        }

        /// <summary>
        /// 停止录制快捷键
        /// </summary>
        private void StopRecordShortcut()
        {
            SaveConfig(InstalledApplications.ToList());
            RefreshData(false);

            Dispatcher.Invoke(() =>
            {
                _currentRecordButton.IsChecked = false;
            });

            _isRecordingShortcut = false;
        }

        /// <summary>
        /// 保存当前配置
        /// </summary>                     
        private void SaveConfig(List<InstalledApp> applications)
        {
            var data = JsonConvert.SerializeObject(applications);
            if (File.Exists(Constants.ConfigFilePath))
                File.Delete(Constants.ConfigFilePath);

            using (var fs = File.Create(Constants.ConfigFilePath))
            {
                using (var streamWriter = new StreamWriter(fs))
                {
                    streamWriter.Write(data);
                    streamWriter.Flush();
                }
            }
        }
    }
}
