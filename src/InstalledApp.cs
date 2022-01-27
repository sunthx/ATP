using System.IO;

namespace ATP
{
    public class InstalledApp
    {
        public string DisplayName { get; set; }
        public string Location { get; set; }
        public string FileName { set; get; }
        public string Icon { get; set; }
        public string HotKey { get; set; }

        public static InstalledApp New(string appFilePath)
        {
            var installApp = new InstalledApp
            {
                Location = appFilePath,
                DisplayName = Path.GetFileNameWithoutExtension(appFilePath),
                FileName = Path.GetFileName(appFilePath)
            };

            return installApp;
        }
    }
}