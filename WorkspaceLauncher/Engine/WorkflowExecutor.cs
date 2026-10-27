using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Win32;
using WorkspaceLauncher.Models;
using WorkspaceLauncher.Services;

namespace WorkspaceLauncher.Engine
{
    public class WorkflowExecutor
    {
        private static readonly Lazy<WorkflowExecutor> _instance = 
            new Lazy<WorkflowExecutor>(() => new WorkflowExecutor());

        public static WorkflowExecutor Instance => _instance.Value;

        private WorkflowExecutor() { }

        public async Task ExecuteWorkflowAsync(Workflow workflow, Action<string>? onWarning = null)
        {
            var settings = PersistenceService.Instance.LoadSettings();

            // Order enabled actions by their Order index
            var enabledActions = workflow.Actions
                .Where(a => a.IsEnabled)
                .OrderBy(a => a.Order)
                .ToList();

            foreach (var action in enabledActions)
            {
                // Respect launch delay
                if (action.DelayMs > 0)
                {
                    await Task.Delay(action.DelayMs);
                }

                try
                {
                    switch (action.Type)
                    {
                        case ActionType.App:
                            LaunchApp(action, onWarning);
                            break;
                        case ActionType.Url:
                            LaunchUrl(action, settings.DefaultBrowser, onWarning);
                            break;
                        case ActionType.Folder:
                            LaunchFolder(action, onWarning);
                            break;
                        case ActionType.File:
                            LaunchFile(action, onWarning);
                            break;
                    }
                }
                catch (Exception ex)
                {
                    string errMsg = $"Failed to execute action '{action.Name}': {ex.Message}";
                    Debug.WriteLine(errMsg);
                    onWarning?.Invoke(errMsg);
                }
            }

            // Update stats
            workflow.LaunchCount++;
            workflow.LastLaunched = DateTime.Now;
        }

        private void LaunchApp(WorkflowAction action, Action<string>? onWarning)
        {
            if (string.IsNullOrEmpty(action.PathOrUrl))
            {
                onWarning?.Invoke($"Application '{action.Name}' has an empty path.");
                return;
            }

            if (!File.Exists(action.PathOrUrl))
            {
                onWarning?.Invoke($"Application file not found at: '{action.PathOrUrl}'");
                return;
            }

            var psi = new ProcessStartInfo
            {
                FileName = action.PathOrUrl,
                Arguments = action.Arguments,
                WorkingDirectory = string.IsNullOrEmpty(action.WorkingDirectory) 
                    ? Path.GetDirectoryName(action.PathOrUrl) 
                    : action.WorkingDirectory,
                UseShellExecute = true,
                WindowStyle = MapWindowStyle(action.State)
            };

            Process.Start(psi);
        }

        private void LaunchUrl(WorkflowAction action, string browserSetting, Action<string>? onWarning)
        {
            if (string.IsNullOrEmpty(action.PathOrUrl))
            {
                onWarning?.Invoke($"Website URL action '{action.Name}' is empty.");
                return;
            }

            string url = action.PathOrUrl;
            // Basic prefixing
            if (!url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) && 
                !url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                url = "https://" + url;
            }

            var psi = new ProcessStartInfo
            {
                UseShellExecute = true,
                WindowStyle = MapWindowStyle(action.State)
            };

            if (browserSetting.Equals("System Default", StringComparison.OrdinalIgnoreCase))
            {
                psi.FileName = url;
            }
            else
            {
                // Route to a specific browser
                string browserExe = GetBrowserExecutable(browserSetting);
                if (!string.IsNullOrEmpty(browserExe))
                {
                    psi.FileName = browserExe;
                    psi.Arguments = $"\"{url}\"";
                }
                else
                {
                    // Fallback to system default and warn
                    onWarning?.Invoke($"Browser '{browserSetting}' not found. Falling back to system default.");
                    psi.FileName = url;
                }
            }

            Process.Start(psi);
        }

        private void LaunchFolder(WorkflowAction action, Action<string>? onWarning)
        {
            if (string.IsNullOrEmpty(action.PathOrUrl))
            {
                onWarning?.Invoke($"Folder path is empty.");
                return;
            }

            if (!Directory.Exists(action.PathOrUrl))
            {
                onWarning?.Invoke($"Folder not found: '{action.PathOrUrl}'");
                return;
            }

            var psi = new ProcessStartInfo
            {
                FileName = action.PathOrUrl,
                UseShellExecute = true,
                WindowStyle = MapWindowStyle(action.State)
            };

            Process.Start(psi);
        }

        private void LaunchFile(WorkflowAction action, Action<string>? onWarning)
        {
            if (string.IsNullOrEmpty(action.PathOrUrl))
            {
                onWarning?.Invoke($"File path is empty.");
                return;
            }

            if (!File.Exists(action.PathOrUrl))
            {
                onWarning?.Invoke($"File not found: '{action.PathOrUrl}'");
                return;
            }

            var psi = new ProcessStartInfo
            {
                FileName = action.PathOrUrl,
                UseShellExecute = true,
                WindowStyle = MapWindowStyle(action.State)
            };

            Process.Start(psi);
        }

        private ProcessWindowStyle MapWindowStyle(LaunchState state)
        {
            return state switch
            {
                LaunchState.Minimized => ProcessWindowStyle.Minimized,
                LaunchState.Maximized => ProcessWindowStyle.Maximized,
                _ => ProcessWindowStyle.Normal
            };
        }

        private string GetBrowserExecutable(string browserName)
        {
            string keyName = browserName.ToLower() switch
            {
                "chrome" => "chrome.exe",
                "edge" => "msedge.exe",
                "firefox" => "firefox.exe",
                _ => string.Empty
            };

            if (string.IsNullOrEmpty(keyName)) return string.Empty;

            // Try to find the browser via Windows App Paths registry
            try
            {
                using (var key = Registry.LocalMachine.OpenSubKey($@"SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\{keyName}"))
                {
                    if (key != null)
                    {
                        object? value = key.GetValue(null); // default value
                        if (value != null)
                        {
                            return value.ToString() ?? keyName;
                        }
                    }
                }
            }
            catch { }

            // Fallback to PATH resolving (windows will find chrome.exe etc. if registered)
            return keyName;
        }
    }
}
