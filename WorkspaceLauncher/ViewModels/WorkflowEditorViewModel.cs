using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using Microsoft.Win32;
using WorkspaceLauncher.Helpers;
using WorkspaceLauncher.Models;
using WorkspaceLauncher.Services;

namespace WorkspaceLauncher.ViewModels
{
    public class WorkflowEditorViewModel : ViewModelBase
    {
        private readonly Workflow _workflow;
        private readonly Action _backAction;
        private string _searchText = string.Empty;
        private ObservableCollection<WorkflowAction> _filteredActions = new ObservableCollection<WorkflowAction>();

        public Workflow Workflow => _workflow;

        public string WorkflowName
        {
            get => _workflow.Name;
            set
            {
                if (_workflow.Name != value)
                {
                    _workflow.Name = value;
                    OnPropertyChanged();
                }
            }
        }

        public string WorkflowDescription
        {
            get => _workflow.Description;
            set
            {
                if (_workflow.Description != value)
                {
                    _workflow.Description = value;
                    OnPropertyChanged();
                }
            }
        }

        public string WorkflowColor
        {
            get => _workflow.Color;
            set
            {
                if (_workflow.Color != value)
                {
                    _workflow.Color = value;
                    OnPropertyChanged();
                }
            }
        }

        public string WorkflowIcon
        {
            get => _workflow.Icon;
            set
            {
                if (_workflow.Icon != value)
                {
                    _workflow.Icon = value;
                    OnPropertyChanged();
                }
            }
        }

        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetProperty(ref _searchText, value))
                {
                    ApplyFilter();
                }
            }
        }

        public ObservableCollection<WorkflowAction> FilteredActions
        {
            get => _filteredActions;
            set => SetProperty(ref _filteredActions, value);
        }

        public System.Collections.Generic.IEnumerable<string> AvailableIcons => IconHelper.GetAvailableIconKeys();

        public System.Collections.Generic.IEnumerable<string> Colors { get; } = new List<string>
        {
            "#9333EA", // Purple
            "#3B82F6", // Blue
            "#10B981", // Green
            "#EF4444", // Red
            "#F59E0B", // Orange
            "#06B6D4", // Cyan
            "#EC4899", // Pink
            "#64748B"  // Slate Gray
        };

        public System.Collections.Generic.IEnumerable<ActionType> ActionTypes { get; } = 
            Enum.GetValues(typeof(ActionType)).Cast<ActionType>();

        public System.Collections.Generic.IEnumerable<LaunchState> LaunchStates { get; } = 
            Enum.GetValues(typeof(LaunchState)).Cast<LaunchState>();

        // Commands
        public RelayCommand AddAppActionCommand { get; }
        public RelayCommand AddUrlActionCommand { get; }
        public RelayCommand AddFolderActionCommand { get; }
        public RelayCommand AddFileActionCommand { get; }
        public RelayCommand<WorkflowAction> BrowsePathCommand { get; }
        public RelayCommand<WorkflowAction> DeleteActionCommand { get; }
        public RelayCommand<WorkflowAction> DuplicateActionCommand { get; }
        public RelayCommand<WorkflowAction> MoveUpCommand { get; }
        public RelayCommand<WorkflowAction> MoveDownCommand { get; }
        public RelayCommand<WorkflowAction> ToggleEnableCommand { get; }
        public RelayCommand SaveAndBackCommand { get; }
        public RelayCommand GenerateLauncherCommand { get; }
        public RelayCommand CaptureSessionCommand { get; }

        public WorkflowEditorViewModel(Workflow workflow, Action backAction)
        {
            _workflow = workflow;
            _backAction = backAction;

            AddAppActionCommand = new RelayCommand(() => AddAction(ActionType.App));
            AddUrlActionCommand = new RelayCommand(() => AddAction(ActionType.Url));
            AddFolderActionCommand = new RelayCommand(() => AddAction(ActionType.Folder));
            AddFileActionCommand = new RelayCommand(() => AddAction(ActionType.File));

            BrowsePathCommand = new RelayCommand<WorkflowAction>(BrowsePath);
            DeleteActionCommand = new RelayCommand<WorkflowAction>(DeleteAction);
            DuplicateActionCommand = new RelayCommand<WorkflowAction>(DuplicateAction);
            MoveUpCommand = new RelayCommand<WorkflowAction>(MoveActionUp);
            MoveDownCommand = new RelayCommand<WorkflowAction>(MoveActionDown);
            ToggleEnableCommand = new RelayCommand<WorkflowAction>(ToggleActionEnable);
            
            SaveAndBackCommand = new RelayCommand(SaveAndBack);
            GenerateLauncherCommand = new RelayCommand(GenerateLauncher);
            CaptureSessionCommand = new RelayCommand(CaptureCurrentSession);

            ApplyFilter();
        }

        private void ApplyFilter()
        {
            var actions = _workflow.Actions.OrderBy(a => a.Order).ToList();
            if (!string.IsNullOrEmpty(SearchText))
            {
                actions = actions.Where(a => 
                    a.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase) || 
                    a.PathOrUrl.Contains(SearchText, StringComparison.OrdinalIgnoreCase)
                ).ToList();
            }

            FilteredActions = new ObservableCollection<WorkflowAction>(actions);
        }

        private void AddAction(ActionType type)
        {
            string defaultName = type switch
            {
                ActionType.App => "Launch Application",
                ActionType.Url => "Open Website",
                ActionType.Folder => "Open Folder",
                ActionType.File => "Open File",
                _ => "New Action"
            };

            var newAction = new WorkflowAction
            {
                Name = defaultName,
                Type = type,
                Order = _workflow.Actions.Count > 0 ? _workflow.Actions.Max(a => a.Order) + 1 : 0
            };

            _workflow.Actions.Add(newAction);
            ApplyFilter();
        }

        private void BrowsePath(WorkflowAction action)
        {
            if (action == null) return;

            if (action.Type == ActionType.Folder)
            {
                var dialog = new OpenFolderDialog
                {
                    Title = "Select Folder to Open"
                };

                if (dialog.ShowDialog() == true)
                {
                    action.PathOrUrl = dialog.FolderName;
                    action.Name = Path.GetFileName(dialog.FolderName) ?? "Open Folder";
                    ApplyFilter();
                }
            }
            else if (action.Type == ActionType.App)
            {
                var dialog = new OpenFileDialog
                {
                    Title = "Select Executable File",
                    Filter = "Executable Files (*.exe)|*.exe|All Files (*.*)|*.*"
                };

                if (dialog.ShowDialog() == true)
                {
                    action.PathOrUrl = dialog.FileName;
                    action.Name = Path.GetFileNameWithoutExtension(dialog.FileName);
                    action.WorkingDirectory = Path.GetDirectoryName(dialog.FileName) ?? string.Empty;
                    ApplyFilter();
                }
            }
            else if (action.Type == ActionType.File)
            {
                var dialog = new OpenFileDialog
                {
                    Title = "Select File to Open",
                    Filter = "Supported Files (*.txt;*.md;*.pdf;*.docx;*.jpg;*.png)|*.txt;*.md;*.pdf;*.docx;*.jpg;*.png|All Files (*.*)|*.*"
                };

                if (dialog.ShowDialog() == true)
                {
                    action.PathOrUrl = dialog.FileName;
                    action.Name = Path.GetFileName(dialog.FileName);
                    ApplyFilter();
                }
            }
        }

        private void DeleteAction(WorkflowAction action)
        {
            if (action == null) return;
            _workflow.Actions.Remove(action);
            ReindexOrders();
            ApplyFilter();
        }

        private void DuplicateAction(WorkflowAction action)
        {
            if (action == null) return;
            var duplicate = action.Clone();
            duplicate.Order = action.Order + 1;
            
            // Shift subsequent actions
            foreach (var a in _workflow.Actions.Where(a => a.Order >= duplicate.Order))
            {
                a.Order++;
            }

            _workflow.Actions.Add(duplicate);
            ReindexOrders();
            ApplyFilter();
        }

        private void MoveActionUp(WorkflowAction action)
        {
            if (action == null) return;
            var actions = _workflow.Actions.OrderBy(a => a.Order).ToList();
            int index = actions.IndexOf(action);
            if (index > 0)
            {
                var swapWith = actions[index - 1];
                int temp = action.Order;
                action.Order = swapWith.Order;
                swapWith.Order = temp;
                ApplyFilter();
            }
        }

        private void MoveActionDown(WorkflowAction action)
        {
            if (action == null) return;
            var actions = _workflow.Actions.OrderBy(a => a.Order).ToList();
            int index = actions.IndexOf(action);
            if (index >= 0 && index < actions.Count - 1)
            {
                var swapWith = actions[index + 1];
                int temp = action.Order;
                action.Order = swapWith.Order;
                swapWith.Order = temp;
                ApplyFilter();
            }
        }

        private void ToggleActionEnable(WorkflowAction action)
        {
            if (action == null) return;
            action.IsEnabled = !action.IsEnabled;
            ApplyFilter();
        }

        private void ReindexOrders()
        {
            var ordered = _workflow.Actions.OrderBy(a => a.Order).ToList();
            for (int i = 0; i < ordered.Count; i++)
            {
                ordered[i].Order = i;
            }
        }

        private void SaveAndBack()
        {
            // Sync orders in the original actions
            ReindexOrders();
            _backAction();
        }

        private void GenerateLauncher()
        {
            try
            {
                ReindexOrders();
                var settings = PersistenceService.Instance.LoadSettings();
                string path = LauncherGeneratorService.Instance.GenerateShortcut(_workflow, settings.DefaultLauncherLocation);
                MessageBox.Show(
                    $"Launcher generated successfully!\nPath: {path}", 
                    "Success", 
                    MessageBoxButton.OK, 
                    MessageBoxImage.Information
                );
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Failed to generate launcher: {ex.Message}", 
                    "Error", 
                    MessageBoxButton.OK, 
                    MessageBoxImage.Error
                );
            }
        }

        private void CaptureCurrentSession()
        {
            try
            {
                var items = SessionCaptureService.Instance.CaptureCurrentSession();
                int currentMaxOrder = _workflow.Actions.Count > 0 ? _workflow.Actions.Max(a => a.Order) : -1;
                
                int importedCount = 0;
                var missingUrls = new System.Collections.Generic.List<string>();

                foreach (var item in items)
                {
                    // Map captured items directly as workflow actions
                    var newAction = new WorkflowAction
                    {
                        Name = item.Name,
                        Type = item.Type,
                        PathOrUrl = item.PathOrUrl,
                        DelayMs = 0,
                        Order = ++currentMaxOrder,
                        IsEnabled = true
                    };
                    
                    if (item.Type == ActionType.Url && !item.IsUrlValid)
                    {
                        missingUrls.Add(item.Name);
                    }
                    
                    _workflow.Actions.Add(newAction);
                    importedCount++;
                }
                
                ApplyFilter();
                
                if (importedCount > 0)
                {
                    string msg = $"Successfully imported {importedCount} action items from your current session!";
                    if (missingUrls.Any())
                    {
                        msg += $"\n\nNote: {missingUrls.Count} captured browser tab(s) did not expose their URLs. They have been added with empty targets. Please enter them manually:\n- " + string.Join("\n- ", missingUrls);
                    }
                    MessageBox.Show(msg, "Session Capture Import", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show("No active applications, browser tabs, or folders were detected to capture.", "Session Capture Import", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to capture current session: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
