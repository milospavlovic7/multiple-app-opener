using System;
using System.Collections.Generic;
using System.Linq;
using WorkspaceLauncher.Models;
using Xunit;

namespace WorkspaceLauncher.Tests
{
    public class WorkflowExecutorTests
    {
        [Fact]
        public void TestWorkflowActionOrdering()
        {
            // Arrange
            var actions = new List<WorkflowAction>
            {
                new WorkflowAction { Name = "Third", Order = 3 },
                new WorkflowAction { Name = "First", Order = 1 },
                new WorkflowAction { Name = "Fourth", Order = 4 },
                new WorkflowAction { Name = "Second", Order = 2 }
            };

            var workflow = new Workflow
            {
                Name = "Test Order Workflow",
                Actions = actions
            };

            // Act
            var ordered = workflow.Actions.OrderBy(a => a.Order).ToList();

            // Assert
            Assert.Equal("First", ordered[0].Name);
            Assert.Equal("Second", ordered[1].Name);
            Assert.Equal("Third", ordered[2].Name);
            Assert.Equal("Fourth", ordered[3].Name);
        }

        [Fact]
        public void TestWorkflowActionFiltering()
        {
            // Arrange
            var actions = new List<WorkflowAction>
            {
                new WorkflowAction { Name = "Enabled 1", IsEnabled = true, Order = 1 },
                new WorkflowAction { Name = "Disabled", IsEnabled = false, Order = 2 },
                new WorkflowAction { Name = "Enabled 2", IsEnabled = true, Order = 3 }
            };

            var workflow = new Workflow
            {
                Name = "Test Filter Workflow",
                Actions = actions
            };

            // Act
            var activeActions = workflow.Actions.Where(a => a.IsEnabled).OrderBy(a => a.Order).ToList();

            // Assert
            Assert.Equal(2, activeActions.Count);
            Assert.Equal("Enabled 1", activeActions[0].Name);
            Assert.Equal("Enabled 2", activeActions[1].Name);
            Assert.DoesNotContain(activeActions, a => a.Name == "Disabled");
        }

        [Fact]
        public void TestWorkflowCloning()
        {
            // Arrange
            var workflow = new Workflow
            {
                Id = Guid.NewGuid(),
                Name = "Original",
                Icon = "Music",
                Color = "#FF00FF",
                Description = "Hello Original",
                IsPinned = true,
                Actions = new List<WorkflowAction>
                {
                    new WorkflowAction { Name = "Original Action 1", Order = 0 }
                }
            };

            // Act
            var clone = workflow.Clone();

            // Assert
            Assert.NotEqual(workflow.Id, clone.Id);
            Assert.Equal("Original (Copy)", clone.Name);
            Assert.Equal(workflow.Icon, clone.Icon);
            Assert.Equal(workflow.Color, clone.Color);
            Assert.Equal(workflow.Description, clone.Description);
            Assert.True(clone.IsPinned); // Cloned items preserve pins but reset stats
            Assert.Equal(0, clone.LaunchCount);
            Assert.Null(clone.LastLaunched);

            Assert.Single(clone.Actions);
            Assert.NotEqual(workflow.Actions[0].Id, clone.Actions[0].Id);
            Assert.Equal(workflow.Actions[0].Name, clone.Actions[0].Name);
            Assert.Equal(workflow.Actions[0].Order, clone.Actions[0].Order);
        }
    }
}
