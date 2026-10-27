using System;
using System.IO;

namespace WorkspaceLauncher.Models
{
    public class Settings
    {
        public string DefaultBrowser { get; set; } = "System Default"; // System Default, Chrome, Edge, Firefox
        public int DefaultDelayMs { get; set; } = 500; // Default gap between actions
        public bool LaunchAtStartup { get; set; } = false;
        public string Theme { get; set; } = "Dark";
        public string DefaultLauncherLocation { get; set; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop));
    }
}
