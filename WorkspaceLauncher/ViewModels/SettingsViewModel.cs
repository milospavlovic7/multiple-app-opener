using System;
using System.Collections.Generic;
using WorkspaceLauncher.Models;
using WorkspaceLauncher.Services;

namespace WorkspaceLauncher.ViewModels
{
    public class SettingsViewModel : ViewModelBase
    {
        private readonly Settings _settings;

        public List<string> Browsers { get; } = new List<string> { "System Default", "Chrome", "Edge", "Firefox" };

        public string DefaultBrowser
        {
            get => _settings.DefaultBrowser;
            set
            {
                if (_settings.DefaultBrowser != value)
                {
                    _settings.DefaultBrowser = value;
                    OnPropertyChanged();
                    SaveSettings();
                }
            }
        }

        public int DefaultDelayMs
        {
            get => _settings.DefaultDelayMs;
            set
            {
                if (_settings.DefaultDelayMs != value)
                {
                    _settings.DefaultDelayMs = value;
                    OnPropertyChanged();
                    SaveSettings();
                }
            }
        }

        public bool LaunchAtStartup
        {
            get => _settings.LaunchAtStartup;
            set
            {
                if (_settings.LaunchAtStartup != value)
                {
                    _settings.LaunchAtStartup = value;
                    OnPropertyChanged();
                    SaveSettings();
                    ToggleStartupShortcut(value);
                }
            }
        }

        public string DefaultLauncherLocation
        {
            get => _settings.DefaultLauncherLocation;
            set
            {
                if (_settings.DefaultLauncherLocation != value)
                {
                    _settings.DefaultLauncherLocation = value;
                    OnPropertyChanged();
                    SaveSettings();
                }
            }
        }

        public SettingsViewModel()
        {
            _settings = PersistenceService.Instance.LoadSettings();
        }

        private void SaveSettings()
        {
            PersistenceService.Instance.SaveSettings(_settings);
        }

        private void ToggleStartupShortcut(bool enable)
        {
            try
            {
                string startupFolder = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
                string shortcutPath = System.IO.Path.Combine(startupFolder, "WorkspaceLauncher.lnk");

                if (enable)
                {
                    string exePath = Environment.ProcessPath ?? string.Empty;
                    if (!string.IsNullOrEmpty(exePath))
                    {
                        Type shellType = Type.GetTypeFromProgID("WScript.Shell")!;
                        dynamic shell = Activator.CreateInstance(shellType);
                        dynamic shortcut = shell.CreateShortcut(shortcutPath);
                        shortcut.TargetPath = exePath;
                        shortcut.WorkingDirectory = System.IO.Path.GetDirectoryName(exePath);
                        shortcut.Description = "Start Workspace Launcher Editor on Windows Startup";
                        shortcut.Save();
                    }
                }
                else
                {
                    if (System.IO.File.Exists(shortcutPath))
                    {
                        System.IO.File.Delete(shortcutPath);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Startup shortcut toggle failed: {ex.Message}");
            }
        }
    }
}
