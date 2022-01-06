using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AltTabPlus
{
    internal class Constants
    {
        public static string DataDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),"AltTabPlus");
        public static string IconCacheDirectory = Path.Combine(DataDirectory, "icons");
        public static string ConfigFilePath = Path.Combine(DataDirectory, "config.json");
    }
}
