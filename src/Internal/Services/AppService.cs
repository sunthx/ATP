using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ATP.Internal.Models;
using ATP.Internal.Utils;
using Newtonsoft.Json;
using NLog;

namespace ATP.Internal.Services
{
    public class AppService : IDisposable, IAppService
    {
        private readonly int _mainProcessId;
        private readonly ILogger _logger = LogManager.GetCurrentClassLogger();
        private KeyboardService _keyboardService;

        //cache
        private readonly Dictionary<string, InstalledProgram> _hotKeyCache;
        private readonly Dictionary<string, InstalledProgram> _appList;

        public AppService()
        {
            _mainProcessId = Process.GetCurrentProcess().Id;
            _hotKeyCache = new Dictionary<string, InstalledProgram>();
            _appList = new Dictionary<string, InstalledProgram>();
            _keyboardService = new KeyboardService();
            _keyboardService.OnReceived += KeyboardServiceOnOnReceived;
            _keyboardService.Start();
        }

        public event Action<CombinationKeys> OnHotKeyReceived;

        public List<InstalledProgram> GetAll()
        {
            Load();
            return _appList.Values.ToList();
        }

        public InstalledProgram Add(string appPath)
        {
            if (Exist(appPath))
                return default;

            var app = InstalledProgram.New(appPath);
            _appList.Add(app.Id, app);

            Save();
            return app;
        }

        public bool SetHotKey(string appId, CombinationKeys key)
        {
            var hotKey = key.ToString();
            if (_hotKeyCache.ContainsKey(hotKey))
                return false;

            var app = _appList[appId];
            app.HotKey = hotKey;
            _hotKeyCache.Add(app.HotKey, app);

            Save();
            return true;
        }

        private bool Handle(CombinationKeys combinationKeys)
        {
            if (combinationKeys.IsHandled)
                return true;

            var shortcut = combinationKeys.ToString();
            _logger.Info($"current shortcut:{shortcut}");

            if (!_hotKeyCache.ContainsKey(shortcut))
            {
                _logger.Info($"not found 【{shortcut}】 process.");
                return false;
            }

            var app = _hotKeyCache[shortcut];
            _logger.Info($"found 【{shortcut}】 process {app.Location}.");

            var queryResult = Process
                .GetProcesses()
                .FirstOrDefault(item =>
                    item.MainWindowHandle != IntPtr.Zero && item.MainModule?.ModuleName == app.FileName);

            if (queryResult != default)
            {
                NativeMethods.BringWindowToFront((uint)_mainProcessId, queryResult.MainWindowHandle);
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

        private bool Exist(string appPath)
        {
            return _appList.Values.Any(item => item.Location == appPath);
        }

        private void Load()
        {
            if (_appList.Any())
                return;

            if (!File.Exists(Constants.ConfigFilePath))
                return;

            var fileContent = File.ReadAllText(Constants.ConfigFilePath);
            var data = JsonConvert.DeserializeObject<List<InstalledProgram>>(fileContent) ?? new List<InstalledProgram>();
            if (data.Any())
            {
                data.ForEach(item =>
                {
                    _appList.Add(item.Id, item);

                    if (string.IsNullOrEmpty(item.HotKey))
                        return;

                    if (!_hotKeyCache.ContainsKey(item.HotKey))
                        _hotKeyCache.Add(item.HotKey, item);
                });
            }
        }

        private void Save()
        {
            var data = JsonConvert.SerializeObject(_appList.Values.ToList());
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

        private void KeyboardServiceOnOnReceived(CombinationKeys combinationKeys)
        {
            OnHotKeyReceived?.Invoke(combinationKeys);
            if (!combinationKeys.IsHandled)
            {
                Handle(combinationKeys);
            }
        }

        public void Dispose()
        {
            _keyboardService.Stop();
            _keyboardService = null;
        }
    }
}