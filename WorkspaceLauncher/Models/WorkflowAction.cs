using System;

namespace WorkspaceLauncher.Models
{
    public enum ActionType
    {
        App,
        Url,
        File,
        Folder
    }

    public enum LaunchState
    {
        Normal,
        Minimized,
        Maximized
    }

    public class WorkflowAction
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = string.Empty;
        public ActionType Type { get; set; }
        public string PathOrUrl { get; set; } = string.Empty;
        public string Arguments { get; set; } = string.Empty;
        public string WorkingDirectory { get; set; } = string.Empty;
        public int DelayMs { get; set; } = 0;
        public int Order { get; set; } = 0;
        public LaunchState State { get; set; } = LaunchState.Normal;
        public bool IsEnabled { get; set; } = true;

        public WorkflowAction Clone()
        {
            return new WorkflowAction
            {
                Id = Guid.NewGuid(), // Generate new ID for duplicate
                Name = Name,
                Type = Type,
                PathOrUrl = PathOrUrl,
                Arguments = Arguments,
                WorkingDirectory = WorkingDirectory,
                DelayMs = DelayMs,
                Order = Order,
                State = State,
                IsEnabled = IsEnabled
            };
        }
    }
}
