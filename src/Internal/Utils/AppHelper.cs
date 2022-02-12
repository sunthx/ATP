using System;
using System.Collections.Generic;
using System.Linq;
using ATP.Internal.Models;
using Microsoft.Win32;

namespace ATP.Internal.Utils
{
    internal class AppHelper
    {
        private static readonly string[] RegistryKeys = {
            @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall",
            @"SOFTWARE\Wow6432Node\Microsoft\Windows\CurrentVersion\Uninstall" };

        public static List<InstalledProgram> GetInstalledPrograms()
        {
            var result = new Dictionary<string,InstalledProgram>();
            var view = Environment.Is64BitOperatingSystem ? RegistryView.Registry64 : RegistryView.Registry32;
            AddInstalledProgramToResultView(RegistryHive.LocalMachine, view, RegistryKeys[0],result);
            AddInstalledProgramToResultView(RegistryHive.CurrentUser, view, RegistryKeys[0],result);
            AddInstalledProgramToResultView(RegistryHive.LocalMachine, RegistryView.Registry64, RegistryKeys[1],result);

            return result.Values.ToList();
        }

        private static void AddInstalledProgramToResultView(RegistryHive hive, RegistryView view, string registryKey,Dictionary<string,InstalledProgram> resultView)
        {
            using (var currentKey = RegistryKey.OpenBaseKey(hive, view).OpenSubKey(registryKey))
            {
                foreach (var subKeyName in currentKey.GetSubKeyNames())
                {
                    using (var subKey = currentKey.OpenSubKey(subKeyName))
                    {
                        var displayName = subKey.GetValue("DisplayName");
                        var displayIcon = subKey.GetValue("DisplayIcon");
                        if (displayName == null || displayIcon == null)
                            continue;

                        var app = new InstalledProgram
                        {
                            DisplayName = (string)displayName,
                            Location = (string)displayIcon
                        };
				
                        if(!resultView.ContainsKey(app.DisplayName))
                        {
                            resultView.Add(app.DisplayName,app);
                        }
                    }
                }
            }
        }
    }
}
