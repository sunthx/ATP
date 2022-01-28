using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Forms;
using ATP.Themes.Controls;
using Newtonsoft.Json;
using NLog;
using static Vanara.PInvoke.User32;
using Control = System.Windows.Forms.Control;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;

namespace ATP
{
    public partial class MainWindow : TheWindow 
    {
        private readonly ILogger _logger = LogManager.GetCurrentClassLogger();

        private bool _isRecordingShortcut;
        private ToggleButton _currentRecordButton;
        private QuickApp _currentSelectedAppItem;
        private QuickApp _currentSelectedApp;

        public MainWindow(QuickOpenAppManage manage)
        {
            InitializeComponent();
            Loaded += MainWindow_Loaded;
            Closed += OnClosed;
        }

        public ObservableCollection<QuickApp> InstalledApplications { get; set; }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            InstalledApplications = new ObservableCollection<QuickApp>();
        
            RefreshData(true);

            LbApp.ItemsSource = InstalledApplications;
        }

        private void OnClosed(object sender, EventArgs e)
        {
            
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
            if (_currentRecordButton.Tag is QuickApp application)
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
        /// 将 App 加载到 GUI 上
        /// </summary>
        /// <param name="filePath"></param>
        private void AddAppInfoToList(string filePath)
        {
            if (InstalledApplications.Any(item => item.Location == filePath))
            {
                return;
            }

            var app = QuickApp.New(filePath);
            var iconFilePath = Utils.GetAppIconFile(filePath);
            app.Icon = iconFilePath;

            InstalledApplications.Add(app);
            SaveConfig(InstalledApplications.ToList());
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

        
    }
  }
