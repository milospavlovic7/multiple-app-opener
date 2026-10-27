using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Automation;
using WorkspaceLauncher.Models;

namespace WorkspaceLauncher.Services
{
    public class SessionCaptureService
    {
        private static readonly Lazy<SessionCaptureService> _instance = 
            new Lazy<SessionCaptureService>(() => new SessionCaptureService());

        public static SessionCaptureService Instance => _instance.Value;

        private SessionCaptureService() { }

        public List<CapturedItem> CaptureCurrentSession()
        {
            var capturedItems = new List<CapturedItem>();

            // 1. Capture Open Folders (Explorer COM)
            CaptureExplorerFolders(capturedItems);

            // 2. Capture Running Applications & Browser Tabs
            CaptureRunningProcesses(capturedItems);

            // Filter out self if captured
            string currentProcessName = Process.GetCurrentProcess().ProcessName;
            capturedItems.RemoveAll(item => 
                item.Type == ActionType.App && 
                Path.GetFileNameWithoutExtension(item.PathOrUrl).Equals(currentProcessName, StringComparison.OrdinalIgnoreCase)
            );

            return capturedItems;
        }

        private void CaptureExplorerFolders(List<CapturedItem> list)
        {
            try
            {
                Type shellType = Type.GetTypeFromProgID("Shell.Application");
                if (shellType == null) return;

                dynamic shell = Activator.CreateInstance(shellType);
                dynamic windows = shell.Windows();
                int count = windows.Count;

                for (int i = 0; i < count; i++)
                {
                    dynamic window = windows.Item(i);
                    if (window == null) continue;

                    string fullName = window.FullName;
                    if (fullName != null && fullName.EndsWith("explorer.exe", StringComparison.OrdinalIgnoreCase))
                    {
                        string locationUrl = window.LocationURL;
                        if (!string.IsNullOrEmpty(locationUrl))
                        {
                            try
                            {
                                var uri = new Uri(locationUrl);
                                if (uri.IsFile)
                                {
                                    string localPath = uri.LocalPath;
                                    if (Directory.Exists(localPath) && !list.Any(x => x.Type == ActionType.Folder && x.PathOrUrl.Equals(localPath, StringComparison.OrdinalIgnoreCase)))
                                    {
                                        list.Add(new CapturedItem
                                        {
                                            Name = Path.GetFileName(localPath) ?? "Folder",
                                            Type = ActionType.Folder,
                                            PathOrUrl = localPath,
                                            IsSelected = true,
                                            IsUrlValid = true
                                        });
                                    }
                                }
                            }
                            catch { }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error capturing explorer folders: {ex.Message}");
            }
        }

        private void CaptureRunningProcesses(List<CapturedItem> list)
        {
            try
            {
                var processes = Process.GetProcesses();
                foreach (var proc in processes)
                {
                    try
                    {
                        if (proc.Id == Process.GetCurrentProcess().Id) continue;
                        if (proc.MainWindowHandle == IntPtr.Zero) continue;
                        if (string.IsNullOrEmpty(proc.MainWindowTitle)) continue;

                        string processName = proc.ProcessName.ToLower();

                        // Skip explorer folders (already handled)
                        if (processName == "explorer") continue;

                        // Check if browser
                        if (processName == "chrome" || processName == "msedge" || processName == "firefox")
                        {
                            // Try to extract active tab URL
                            string url = GetActiveBrowserUrl(proc);
                            bool isUrlValid = !string.IsNullOrEmpty(url);

                            // We'll create a unique browser entry for this window
                            string browserName = processName == "chrome" ? "Chrome Tab" : (processName == "msedge" ? "Edge Tab" : "Firefox Tab");
                            
                            string finalName = proc.MainWindowTitle;
                            // Clean browser suffix from title
                            if (finalName.EndsWith(" - Google Chrome")) finalName = finalName.Substring(0, finalName.Length - 16);
                            else if (finalName.EndsWith(" - Microsoft Edge")) finalName = finalName.Substring(0, finalName.Length - 17);
                            else if (finalName.EndsWith(" - Mozilla Firefox")) finalName = finalName.Substring(0, finalName.Length - 18);

                            // Skip blank pages/new tabs
                            if (finalName.Equals("New Tab", StringComparison.OrdinalIgnoreCase) || 
                                finalName.Equals("New tab", StringComparison.OrdinalIgnoreCase))
                            {
                                continue;
                            }

                            if (!list.Any(x => x.Type == ActionType.Url && x.PathOrUrl.Equals(url, StringComparison.OrdinalIgnoreCase) && isUrlValid))
                            {
                                list.Add(new CapturedItem
                                    {
                                        Name = string.IsNullOrEmpty(finalName) ? browserName : finalName,
                                        Type = ActionType.Url,
                                        PathOrUrl = isUrlValid ? url : string.Empty,
                                        IsUrlValid = isUrlValid,
                                        RawProcessName = proc.ProcessName,
                                        RawWindowTitle = proc.MainWindowTitle,
                                        IsSelected = true
                                    });
                            }
                        }
                        else
                        {
                            // Regular Desktop App
                            string appPath = string.Empty;
                            try
                            {
                                appPath = proc.MainModule?.FileName ?? string.Empty;
                            }
                            catch
                            {
                                // MainModule access can be denied for system/admin apps
                            }

                            if (string.IsNullOrEmpty(appPath))
                            {
                                // Try query path via WMI or skip if not found
                                continue;
                            }

                            // Clean application name
                            string appName = Path.GetFileNameWithoutExtension(appPath);

                            // Filter out system shims, background apps
                            if (appName.Equals("ApplicationFrameHost", StringComparison.OrdinalIgnoreCase) ||
                                appName.Equals("SystemSettings", StringComparison.OrdinalIgnoreCase) ||
                                appName.Equals("TextInputHost", StringComparison.OrdinalIgnoreCase))
                            {
                                continue;
                            }

                            if (!list.Any(x => x.Type == ActionType.App && x.PathOrUrl.Equals(appPath, StringComparison.OrdinalIgnoreCase)))
                            {
                                list.Add(new CapturedItem
                                {
                                    Name = proc.MainWindowTitle,
                                    Type = ActionType.App,
                                    PathOrUrl = appPath,
                                    IsSelected = true,
                                    IsUrlValid = true,
                                    RawProcessName = proc.ProcessName,
                                    RawWindowTitle = proc.MainWindowTitle
                                });
                            }
                        }
                    }
                    catch (Exception)
                    {
                        // Ignore individual process access errors (very common in Windows)
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error capturing running processes: {ex.Message}");
            }
        }

        private string GetActiveBrowserUrl(Process process)
        {
            try
            {
                if (process.MainWindowHandle == IntPtr.Zero) return string.Empty;

                var root = AutomationElement.FromHandle(process.MainWindowHandle);
                if (root == null) return string.Empty;

                // Browsers store the address bar differently. Let's find Edit controls.
                var editCondition = new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Edit);
                var editElements = root.FindAll(TreeScope.Descendants, editCondition);

                foreach (AutomationElement edit in editElements)
                {
                    try
                    {
                        // Check if automation name/ID matches URL bar
                        string autoId = edit.Current.AutomationId ?? string.Empty;
                        string name = edit.Current.Name ?? string.Empty;

                        // UI Automation pattern for getting value
                        if (edit.TryGetCurrentPattern(ValuePattern.Pattern, out object patternObj))
                        {
                            var valPattern = (ValuePattern)patternObj;
                            string url = valPattern.Current.Value;

                            if (!string.IsNullOrEmpty(url))
                            {
                                // Validate if it looks like a URL or domain
                                if (url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || 
                                    url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                                {
                                    return url;
                                }
                                else if (url.Contains(".") && !url.Contains(" ") && !url.Contains("\\"))
                                {
                                    return "https://" + url;
                                }
                            }
                        }
                    }
                    catch { }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"UI Automation Error for {process.ProcessName}: {ex.Message}");
            }
            return string.Empty;
        }
    }
}
