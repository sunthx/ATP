using ATP.Internal.Models;
using Prism.Mvvm;

namespace ATP.ViewModels
{
    public class InstalledProgramViewModel : BindableBase
    {
        public InstalledProgramViewModel(InstalledProgram programInfo)
        {
            ProgramInfo = programInfo;
            CurrentHotKey = programInfo.HotKey;
        }

        /// <summary>
        /// 当前的 APP 的信息
        /// </summary>
        public InstalledProgram ProgramInfo { get; set; }

        private bool _isRecordHotKey;
        /// <summary>
        /// 正在录制快捷键
        /// </summary>
        public bool IsRecordHotKey
        {
            get => _isRecordHotKey;
            set => SetProperty(ref _isRecordHotKey, value);
        }

        private string _currentHotKey;
        /// <summary>
        /// 当前的快捷键
        /// </summary>
        public string CurrentHotKey
        {
            get => _currentHotKey;
            set => SetProperty(ref _currentHotKey, value);
        }

        /// <summary>
        /// 设置快捷键
        /// </summary>
        /// <param name="key"></param>
        public void SetHotKey(string key)
        {
            CurrentHotKey = key;
            ProgramInfo.HotKey = key;
        }
    }
}
