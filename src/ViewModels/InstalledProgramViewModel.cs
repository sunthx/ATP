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

        public InstalledProgram ProgramInfo { get; set; }

        private bool _isRecordHotKey;
        public bool IsRecordHotKey
        {
            get => _isRecordHotKey;
            set => SetProperty(ref _isRecordHotKey, value);
        }

        private string _currentHotKey;
        public string CurrentHotKey
        {
            get => _currentHotKey;
            set => SetProperty(ref _currentHotKey, value);
        }

        public void SetHotKey(string key)
        {
            CurrentHotKey = key;
            ProgramInfo.HotKey = key;
        }
    }
}
