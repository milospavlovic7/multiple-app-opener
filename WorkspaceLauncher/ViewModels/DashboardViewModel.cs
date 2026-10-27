using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using WorkspaceLauncher.Engine;
using WorkspaceLauncher.Models;
using WorkspaceLauncher.Services;

namespace WorkspaceLauncher.ViewModels
{
    public class DashboardViewModel : ViewModelBase
    {
        private readonly ObservableCollection<Workflow> _workflows;
        private readonly Action<Workflow> _editWorkflowAction;
        private readonly Action<Workflow> _launchHeadlessCallback;
        private string _launchStatusMessage = string.Empty;
        private bool _isLaunching = false;

        public ObservableCollection<Workflow> Workflows => _workflows;

        public IEnumerable<Workflow> PinnedWorkflows => _workflows.Where(w => w.IsPinned);

        public IEnumerable<Workflow> RecentWorkflows => _workflows
            .OrderByDescending(w => w.LastLaunched ?? DateTime.MinValue)
            .Take(4);

        public int TotalLaunches => _workflows.Sum(w => w.LaunchCount);

        public int TotalWorkflowsCount => _workflows.Count;

        public string LaunchStatusMessage
        {
            get => _launchStatusMessage;
            set => SetProperty(ref _launchStatusMessage, value);
        }

        public bool IsLaunching
        {
            get => _isLaunching;
            set => SetProperty(ref _isLaunching, value);
        }

        // Commands
        public RelayCommand<Workflow> LaunchCommand { get; }
        public RelayCommand<Workflow> EditCommand { get; }
        public RelayCommand<Workflow> PinCommand { get; }
        public RelayCommand<Workflow> DuplicateCommand { get; }
        public RelayCommand<Workflow> DeleteCommand { get; }
        public RelayCommand<Workflow> GenerateLauncherCommand { get; }
        public RelayCommand CreateNewCommand { get; }

        public DashboardViewModel(
            ObservableCollection<Workflow> workflows, 
            Action<Workflow> editWorkflowAction, 
            Action<Workflow> launchHeadlessCallback,
            RelayCommand createNewCommand)
        {
            _workflows = workflows;
            _editWorkflowAction = editWorkflowAction;
            _launchHeadlessCallback = launchHeadlessCallback;
            CreateNewCommand = createNewCommand;

            LaunchCommand = new RelayCommand<Workflow>(async (w) => await LaunchWorkflowAsync(w));
            EditCommand = new RelayCommand<Workflow>(w => _editWorkflowAction(w));
            PinCommand = new RelayCommand<Workflow>(TogglePin);
            DuplicateCommand = new RelayCommand<Workflow>(DuplicateWorkflow);
            DeleteCommand = new RelayCommand<Workflow>(DeleteWorkflow);
            GenerateLauncherCommand = new RelayCommand<Workflow>(GenerateLauncher);

            // Hook collection changes to refresh filtered lists and stats
            _workflows.CollectionChanged += (s, e) => RefreshDashboardData();
        }

        public void RefreshDashboardData()
        {
            OnPropertyChanged(nameof(PinnedWorkflows));
            OnPropertyChanged(nameof(RecentWorkflows));
            OnPropertyChanged(nameof(TotalLaunches));
            OnPropertyChanged(nameof(TotalWorkflowsCount));
        }

        private async Task LaunchWorkflowAsync(Workflow workflow)
        {
            if (workflow == null || IsLaunching) return;

            IsLaunching = true;
            LaunchStatusMessage = $"Launching '{workflow.Name}' workspace...";

            var warnings = new List<string>();
            await WorkflowExecutor.Instance.ExecuteWorkflowAsync(workflow, warning =>
            {
                warnings.Add(warning);
            });

            // Refresh recent lists and stats
            RefreshDashboardData();
            PersistenceService.Instance.SaveWorkflows(_workflows.ToList());

            IsLaunching = false;

            if (warnings.Any())
            {
                string warningSummary = string.Join("\n", warnings);
                MessageBox.Show(
                    $"Workspace loaded with warnings:\n\n{warningSummary}", 
                    "Workspace Launch", 
                    MessageBoxButton.OK, 
                    MessageBoxImage.Warning
                );
            }
            else
            {
                LaunchStatusMessage = "Launched successfully!";
                await Task.Delay(2000);
                if (!IsLaunching) LaunchStatusMessage = string.Empty;
            }
        }

        private void TogglePin(Workflow workflow)
        {
            if (workflow == null) return;
            workflow.IsPinned = !workflow.IsPinned;
            RefreshDashboardData();
            PersistenceService.Instance.SaveWorkflows(_workflows.ToList());
        }

        private void DuplicateWorkflow(Workflow workflow)
        {
            if (workflow == null) return;
            var duplicate = workflow.Clone();
            _workflows.Add(duplicate);
            PersistenceService.Instance.SaveWorkflows(_workflows.ToList());
        }

        private void DeleteWorkflow(Workflow workflow)
        {
            if (workflow == null) return;
            var result = MessageBox.Show(
                $"Are you sure you want to delete the workflow '{workflow.Name}'?", 
                "Confirm Delete", 
                MessageBoxButton.YesNo, 
                MessageBoxImage.Question
            );

            if (result == MessageBoxResult.Yes)
            {
                _workflows.Remove(workflow);
                PersistenceService.Instance.SaveWorkflows(_workflows.ToList());
            }
        }

        private void GenerateLauncher(Workflow workflow)
        {
            if (workflow == null) return;
            try
            {
                var settings = PersistenceService.Instance.LoadSettings();
                string path = LauncherGeneratorService.Instance.GenerateShortcut(workflow, settings.DefaultLauncherLocation);
                MessageBox.Show(
                    $"Launcher generated successfully!\nPath: {path}\n\nYou can now double-click this shortcut from your Desktop to open the workspace.", 
                    "Launcher Generated", 
                    MessageBoxButton.OK, 
                    MessageBoxImage.Information
                );
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Failed to generate launcher:\n{ex.Message}", 
                    "Error", 
                    MessageBoxButton.OK, 
                    MessageBoxImage.Error
                );
            }
        }
    }
}
