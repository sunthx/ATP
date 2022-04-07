using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using ATP.Internal.Models;
using ATP.Internal.Services;
using Microsoft.Win32;
using Prism.Commands;
using Prism.Mvvm;

namespace ATP.ViewModels
{
    public class MainWindowViewModel : BindableBase
    {
        private readonly IAppService _appService;
        private bool _isRecordingHotKey;
        private InstalledProgramViewModel _currentInstalledProgramViewModel;

        public MainWindowViewModel(IAppService appService)
        {
            _appService = appService;
            _appService.OnHotKeyReceived += AppServiceOnOnHotKeyReceived;

            AddAppCommand = new DelegateCommand(AddAppCommandExecute);
            RecordHotKeyCommand = new DelegateCommand<InstalledProgramViewModel>(RecordHotKeyCommandExecute);
            DeleteAppCommand = new DelegateCommand<InstalledProgramViewModel>(DeleteAppCommandExecute);
            OpenAppFolderCommand = new DelegateCommand<InstalledProgramViewModel>(OpenAppFolderCommandExecute);

            InstalledApplications = new ObservableCollection<InstalledProgramViewModel>();
            _appService.GetAll().ForEach(item =>
            {
                InstalledApplications.Add(new InstalledProgramViewModel(item));
            });
        }

        public DelegateCommand AddAppCommand { set; get; }
        public DelegateCommand<InstalledProgramViewModel> RecordHotKeyCommand { set; get; }
        public ObservableCollection<InstalledProgramViewModel> InstalledApplications { get; set; }
        public DelegateCommand<InstalledProgramViewModel> DeleteAppCommand { set; get; }
        public DelegateCommand<InstalledProgramViewModel> OpenAppFolderCommand{ set; get; }


        private void AddAppCommandExecute()
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "Application|*.exe",
                Multiselect = false
            };

            var dialogResult = openFileDialog.ShowDialog();
            if (!dialogResult.HasValue || !dialogResult.Value)
                return;


            var result = _appService.Add(openFileDialog.FileName);
            if (result != null)
            {
                InstalledApplications.Add(new InstalledProgramViewModel(result));
            }
        }

        private void DeleteAppCommandExecute(InstalledProgramViewModel installedProgramViewModel)
        {
            var appId = installedProgramViewModel.ProgramInfo.Id;
            if(_appService.Delete(appId))
                InstalledApplications.Remove(installedProgramViewModel);
        }

        private void OpenAppFolderCommandExecute(InstalledProgramViewModel installedProgramViewModel)
        {
            var folder =Path.GetDirectoryName(installedProgramViewModel.ProgramInfo.Location);
            System.Diagnostics.Process.Start("Explorer", folder);
        }

        private void RecordHotKeyCommandExecute(InstalledProgramViewModel installedProgram)
        {
            if (installedProgram.IsRecordHotKey)
            {
                InstalledApplications.ToList().ForEach(item => { item.IsRecordHotKey = false; });
                installedProgram.IsRecordHotKey = true;
                _isRecordingHotKey = installedProgram.IsRecordHotKey;
                _currentInstalledProgramViewModel = installedProgram;
            }
            else
            {
                _isRecordingHotKey = false;
                _currentInstalledProgramViewModel = default;
            }
        }

        private void AppServiceOnOnHotKeyReceived(CombinationKeys combinationKeys)
        {
            if (_isRecordingHotKey)
            {
                var result = _appService.SetHotKey(_currentInstalledProgramViewModel.ProgramInfo.Id, combinationKeys);
                if (result)
                {
                    _currentInstalledProgramViewModel.SetHotKey(combinationKeys.ToString());
                    _currentInstalledProgramViewModel.IsRecordHotKey = false;
                }
            
                combinationKeys.IsHandled = true;
            }
        }
    }
}
