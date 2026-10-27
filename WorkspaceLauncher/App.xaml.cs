using System;
using System.Windows;
using WorkspaceLauncher.Engine;
using WorkspaceLauncher.Services;

namespace WorkspaceLauncher
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Command-line arguments: --launch <WorkflowId>
            if (e.Args.Length >= 2 && e.Args[0].Equals("--launch", StringComparison.OrdinalIgnoreCase))
            {
                string workflowIdStr = e.Args[1];
                if (Guid.TryParse(workflowIdStr, out Guid workflowId))
                {
                    var workflows = PersistenceService.Instance.LoadWorkflows();
                    var targetWorkflow = workflows.Find(w => w.Id == workflowId);

                    if (targetWorkflow != null)
                    {
                        try
                        {
                            // Run the workspace launcher in headless mode
                            await WorkflowExecutor.Instance.ExecuteWorkflowAsync(targetWorkflow);
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Error executing workflow: {ex.Message}");
                        }
                    }
                }
                
                // Shutdown immediately after running
                Shutdown();
                return;
            }

            // Standard Mode: Open the GUI editor
            var mainWindow = new MainWindow();
            mainWindow.Show();
        }
    }
}
