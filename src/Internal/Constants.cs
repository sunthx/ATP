using System;
using System.IO;

namespace ATP.Internal
{
    internal class Constants
    {
        public static string DataDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),"AltTabPlus");
        public static string IconCacheDirectory = Path.Combine(DataDirectory, "icons");
        public static string ConfigFilePath = Path.Combine(DataDirectory, "config.json");
    }
}
