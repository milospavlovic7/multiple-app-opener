using System;
using System.Collections.Generic;

namespace WorkspaceLauncher.Models
{
    public class Workflow
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = string.Empty;
        public string Icon { get; set; } = "Briefcase"; // Predefined key
        public string Color { get; set; } = "#8A2BE2"; // Accent Hex color
        public string Description { get; set; } = string.Empty;
        public List<WorkflowAction> Actions { get; set; } = new List<WorkflowAction>();
        public bool IsPinned { get; set; } = false;
        public int LaunchCount { get; set; } = 0;
        public DateTime? LastLaunched { get; set; }

        public Workflow Clone()
        {
            var clonedActions = new List<WorkflowAction>();
            foreach (var action in Actions)
            {
                clonedActions.Add(action.Clone());
            }

            return new Workflow
            {
                Id = Guid.NewGuid(), // Generate new ID for duplicate
                Name = Name + " (Copy)",
                Icon = Icon,
                Color = Color,
                Description = Description,
                Actions = clonedActions,
                IsPinned = IsPinned,
                LaunchCount = 0,
                LastLaunched = null
            };
        }
    }
}
