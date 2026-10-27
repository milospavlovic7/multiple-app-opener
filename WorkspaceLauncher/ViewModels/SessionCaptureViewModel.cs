using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using WorkspaceLauncher.Models;
using WorkspaceLauncher.Services;

namespace WorkspaceLauncher.ViewModels
{
    public class SessionCaptureViewModel : ViewModelBase
    {
        private readonly ObservableCollection<Workflow> _workflows;
        private readonly Action _onSavedCallback;
        private string _newWorkflowName = "Captured Workspace";
        private ObservableCollection<CapturedItem> _capturedItems = new ObservableCollection<CapturedItem>();
        private bool _isCapturing = false;

        public string NewWorkflowName
        {
            get => _newWorkflowName;
            set => SetProperty(ref _newWorkflowName, value);
        }

        public ObservableCollection<CapturedItem> CapturedItems
        {
            get => _capturedItems;
            set => SetProperty(ref _capturedItems, value);
        }

        public bool IsCapturing
        {
            get => _isCapturing;
            set => SetProperty(ref _isCapturing, value);
        }

        // Commands
        public RelayCommand CaptureCommand { get; }
        public RelayCommand SaveCommand { get; }
        public RelayCommand CancelCommand { get; }

        public SessionCaptureViewModel(ObservableCollection<Workflow> workflows, Action onSavedCallback)
        {
            _workflows = workflows;
            _onSavedCallback = onSavedCallback;

            CaptureCommand = new RelayCommand(RunCapture);
            SaveCommand = new RelayCommand(SaveWorkflow, CanSave);
            CancelCommand = new RelayCommand(onSavedCallback);

            // Run initial capture
            RunCapture();
        }

        private void RunCapture()
        {
            IsCapturing = true;
            NewWorkflowName = $"Captured Workspace ({DateTime.Now:yyyy-MM-dd HH:mm})";
            
            var items = SessionCaptureService.Instance.CaptureCurrentSession();
            CapturedItems = new ObservableCollection<CapturedItem>(items);
            
            IsCapturing = false;
        }

        private bool CanSave()
        {
            // Allow saving if:
            // 1. Name is not empty
            // 2. There's at least one selected item
            // 3. All selected URLs have a valid non-empty PathOrUrl
            if (string.IsNullOrWhiteSpace(NewWorkflowName)) return false;
            
            var selected = CapturedItems.Where(i => i.IsSelected).ToList();
            if (!selected.Any()) return false;

            // Check if any selected URL item has a blank address
            foreach (var item in selected)
            {
                if (item.Type == ActionType.Url && string.IsNullOrWhiteSpace(item.PathOrUrl))
                {
                    return false; // Validation fails: user must provide a URL manually
                }
            }

            return true;
        }

        private void SaveWorkflow()
        {
            if (!CanSave()) return;

            var newWorkflow = new Workflow
            {
                Name = NewWorkflowName.Trim(),
                Description = "Captured session layout",
                Icon = "GlobeAlt",
                Color = "#06B6D4" // Cyan accent for captured workspaces
            };

            int order = 0;
            foreach (var item in CapturedItems.Where(i => i.IsSelected))
            {
                newWorkflow.Actions.Add(new WorkflowAction
                {
                    Name = item.Name,
                    Type = item.Type,
                    PathOrUrl = item.PathOrUrl.Trim(),
                    Order = order++,
                    State = LaunchState.Normal
                });
            }

            _workflows.Add(newWorkflow);
            PersistenceService.Instance.SaveWorkflows(_workflows.ToList());

            MessageBox.Show(
                $"Workflow '{newWorkflow.Name}' created successfully with {newWorkflow.Actions.Count} actions!", 
                "Workspace Saved", 
                MessageBoxButton.OK, 
                MessageBoxImage.Information
            );

            _onSavedCallback();
        }
    }
}
