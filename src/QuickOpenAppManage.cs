using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;
using Newtonsoft.Json;
using NLog;
using Vanara.PInvoke;

namespace ATP
{
    public class QuickOpenAppManage 
    {
        private readonly ILogger _logger = LogManager.GetCurrentClassLogger();
        private readonly User32.HookProc _keyboardHookProc;
        private User32.SafeHHOOK _globalKeyboardHook;
        private int _currentMainProcessId;

        //cache
        private Dictionary<string, QuickApp> _cache;

        //config
        private bool _isContinueWhenHandled = true;

        public QuickOpenAppManage(int mainProcessId)
        {
            _cache = new Dictionary<string, QuickApp>();
            _currentMainProcessId = mainProcessId;
            _keyboardHookProc = KeyboardHookProc;
        }

        public void StartListening()
        {
            _globalKeyboardHook = NativeMethods.RegisterKeyboardHook(_keyboardHookProc); 
        }

        public void StopListening()
        {
            NativeMethods.ReleaseWindowsHook(_globalKeyboardHook);
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
        /// 从配置文件中加载
        /// </summary>  
        private List<QuickApp> LoadDataFromConfigFile()
        {
            if (!File.Exists(Constants.ConfigFilePath))
                return new List<QuickApp>();


            var data = File.ReadAllText(Constants.ConfigFilePath);
            return JsonConvert.DeserializeObject<List<QuickApp>>(data) ?? new List<QuickApp>();
        }

        /// <summary>
        /// 保存当前配置
        /// </summary>                     
        private void SaveConfig(List<QuickApp> applications)
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

        /// <summary>
        /// 键盘钩子处理函数
        /// </summary>
        private IntPtr KeyboardHookProc(int code, IntPtr wParam, IntPtr lParam)
        {
            var alt = (Control.ModifierKeys & Keys.Alt) != 0;
            var control = (Control.ModifierKeys & Keys.Control) != 0;
            var shift = (Control.ModifierKeys & Keys.Shift) != 0;
            var keyDown = wParam == (IntPtr)User32.WindowMessage.WM_KEYDOWN;
            var keyUp = wParam == (IntPtr)User32.WindowMessage.WM_KEYUP;
            var vkCode = Marshal.ReadInt32(lParam);
            var key = (Keys)vkCode;

            //http://msdn.microsoft.com/en-us/library/windows/desktop/ms646286(v=vs.85).aspx
            if (key != Keys.RMenu && key != Keys.LMenu && wParam == (IntPtr)User32.WindowMessage.WM_SYSKEYDOWN)
            {
                alt = true;
                keyDown = true;
            }
            if (key != Keys.RMenu && key != Keys.LMenu && wParam == (IntPtr)User32.WindowMessage.WM_SYSKEYUP)
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


            return User32.CallNextHookEx(_globalKeyboardHook, code, wParam, lParam);
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
    }
}