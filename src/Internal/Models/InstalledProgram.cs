using System;
using System.IO;
using ATP.Internal.Utils;
using Prism.Mvvm;

namespace ATP.Internal.Models
{
    public class InstalledProgram : BindableBase
    {
        public string HotKey { set; get; }
        public string Id { get; set; }
        public string DisplayName { get; set; }
        public string Location { get; set; }
        public string FileName { set; get; }
        public string Icon { get; set; }

        public static InstalledProgram New(string appFilePath)
        {
            var installApp = new InstalledProgram
            {
                Id = Guid.NewGuid().ToString(),
                Location = appFilePath,
                DisplayName = Path.GetFileNameWithoutExtension(appFilePath),
                FileName = Path.GetFileName(appFilePath)
            };

            var iconFilePath = ViewHelper.GetAppIconFile(installApp.Location);
            installApp.Icon = iconFilePath;

            return installApp;
        }
    }
}