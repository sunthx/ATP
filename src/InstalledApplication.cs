using System.IO;

namespace AltTabPlus;

public class InstalledApplication
{
    public string Id { get; set; }
    public string DisplayName { get; set; }
    public string Location { get; set; }
    public string Icon { get; set; }
    public string HotKey { get; set; }

    public static InstalledApplication New(string appFilePath)
    {
        var installApp = new InstalledApplication();
        installApp.Location = appFilePath;
        installApp.DisplayName = Path.GetFileNameWithoutExtension(appFilePath);

        return installApp;
    }
}