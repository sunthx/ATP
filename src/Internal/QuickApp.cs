using System;
using System.IO;

namespace ATP.Internal
{
    public class QuickApp
    {
        public string Id { get; set; }
        public string DisplayName { get; set; }
        public string Location { get; set; }
        public string FileName { set; get; }
        public string Icon { get; set; }
        public string HotKey { get; set; }

        public static QuickApp New(string appFilePath)
        {
            var installApp = new QuickApp
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