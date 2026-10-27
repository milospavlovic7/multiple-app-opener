using System;
using System.Collections.ObjectModel;
using System.Linq;
using WorkspaceLauncher.Models;
using WorkspaceLauncher.Services;

namespace WorkspaceLauncher.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        private ViewModelBase _currentViewModel;
        private readonly ObservableCollection<Workflow> _workflows;
        private string _activeSection = "Dashboard"; // Dashboard, Capture, Settings, Editor

        public ViewModelBase CurrentViewModel
        {
            get => _currentViewModel;
            set => SetProperty(ref _currentViewModel, value);
        }

        public string ActiveSection
        {
            get => _activeSection;
            set => SetProperty(ref _activeSection, value);
        }

        public ObservableCollection<Workflow> Workflows => _workflows;

        // Sidebar Commands
        public RelayCommand NavigateDashboardCommand { get; }
        public RelayCommand NavigateCaptureCommand { get; }
        public RelayCommand NavigateSettingsCommand { get; }
        public RelayCommand CreateNewWorkflowCommand { get; }

        public MainWindowViewModel()
        {
            // Load workflows
            var loadedWorkflows = PersistenceService.Instance.LoadWorkflows();
            _workflows = new ObservableCollection<Workflow>(loadedWorkflows);

            // Initialize commands
            NavigateDashboardCommand = new RelayCommand(NavigateToDashboard);
            NavigateCaptureCommand = new RelayCommand(NavigateToCapture);
            NavigateSettingsCommand = new RelayCommand(NavigateToSettings);
            CreateNewWorkflowCommand = new RelayCommand(CreateNewWorkflow);

            // Set default view
            _currentViewModel = CreateDashboardViewModel();
        }

        public void NavigateToDashboard()
        {
            ActiveSection = "Dashboard";
            CurrentViewModel = CreateDashboardViewModel();
        }

        public void NavigateToCapture()
        {
            ActiveSection = "Capture";
            CurrentViewModel = new SessionCaptureViewModel(_workflows, NavigateToDashboard);
        }

        public void NavigateToSettings()
        {
            ActiveSection = "Settings";
            CurrentViewModel = new SettingsViewModel();
        }

        public void NavigateToEditor(Workflow workflow)
        {
            ActiveSection = "Editor";
            CurrentViewModel = new WorkflowEditorViewModel(workflow, () =>
            {
                // Save changes when editor is closed
                PersistenceService.Instance.SaveWorkflows(_workflows.ToList());
                NavigateToDashboard();
            });
        }

        private void CreateNewWorkflow()
        {
            var newWorkflow = new Workflow
            {
                Name = string.Empty,
                Description = string.Empty,
                Icon = "Briefcase",
                Color = "#9333EA"
            };

            _workflows.Add(newWorkflow);
            PersistenceService.Instance.SaveWorkflows(_workflows.ToList());
            NavigateToEditor(newWorkflow);
        }

        private DashboardViewModel CreateDashboardViewModel()
        {
            return new DashboardViewModel(
                _workflows,
                NavigateToEditor,
                LaunchWorkflowHeadless,
                CreateNewWorkflowCommand
            );
        }

        private void LaunchWorkflowHeadless(Workflow workflow)
        {
            // Background headless launching is handled directly inside the DashboardViewModel
        }
    }
}
