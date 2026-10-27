using System;
using System.Collections.Generic;
using System.Text.Json;
using WorkspaceLauncher.Models;
using Xunit;

namespace WorkspaceLauncher.Tests
{
    public class SerializationTests
    {
        [Fact]
        public void TestWorkflowSerializationRoundTrip()
        {
            // Arrange
            var workflow = new Workflow
            {
                Id = Guid.NewGuid(),
                Name = "Test Workflow",
                Description = "A workflow to test serialization",
                Color = "#FF0000",
                Icon = "Music",
                IsPinned = true,
                LaunchCount = 5,
                LastLaunched = DateTime.Now,
                Actions = new List<WorkflowAction>
                {
                    new WorkflowAction
                    {
                        Id = Guid.NewGuid(),
                        Name = "Launch Chrome",
                        Type = ActionType.Url,
                        PathOrUrl = "https://google.com",
                        DelayMs = 200,
                        Order = 0,
                        IsEnabled = true
                    },
                    new WorkflowAction
                    {
                        Id = Guid.NewGuid(),
                        Name = "Open Folder",
                        Type = ActionType.Folder,
                        PathOrUrl = @"C:\Users\Milos Pavlovic\Documents",
                        DelayMs = 500,
                        Order = 1,
                        IsEnabled = false
                    }
                }
            };

            var options = new JsonSerializerOptions { WriteIndented = true };

            // Act
            string json = JsonSerializer.Serialize(workflow, options);
            var deserialized = JsonSerializer.Deserialize<Workflow>(json);

            // Assert
            Assert.NotNull(deserialized);
            Assert.Equal(workflow.Id, deserialized.Id);
            Assert.Equal(workflow.Name, deserialized.Name);
            Assert.Equal(workflow.Description, deserialized.Description);
            Assert.Equal(workflow.Color, deserialized.Color);
            Assert.Equal(workflow.Icon, deserialized.Icon);
            Assert.Equal(workflow.IsPinned, deserialized.IsPinned);
            Assert.Equal(workflow.LaunchCount, deserialized.LaunchCount);
            Assert.NotNull(deserialized.Actions);
            Assert.Equal(2, deserialized.Actions.Count);
            
            // Check first action
            Assert.Equal(workflow.Actions[0].Id, deserialized.Actions[0].Id);
            Assert.Equal(workflow.Actions[0].Name, deserialized.Actions[0].Name);
            Assert.Equal(workflow.Actions[0].Type, deserialized.Actions[0].Type);
            Assert.Equal(workflow.Actions[0].PathOrUrl, deserialized.Actions[0].PathOrUrl);
            Assert.Equal(workflow.Actions[0].DelayMs, deserialized.Actions[0].DelayMs);
            Assert.Equal(workflow.Actions[0].Order, deserialized.Actions[0].Order);
            Assert.True(deserialized.Actions[0].IsEnabled);

            // Check second action
            Assert.Equal(workflow.Actions[1].Id, deserialized.Actions[1].Id);
            Assert.False(deserialized.Actions[1].IsEnabled);
        }

        [Fact]
        public void TestSettingsSerializationRoundTrip()
        {
            // Arrange
            var settings = new Settings
            {
                DefaultBrowser = "Chrome",
                DefaultDelayMs = 1000,
                LaunchAtStartup = true,
                Theme = "Dark",
                DefaultLauncherLocation = @"C:\Desktop"
            };

            // Act
            string json = JsonSerializer.Serialize(settings);
            var deserialized = JsonSerializer.Deserialize<Settings>(json);

            // Assert
            Assert.NotNull(deserialized);
            Assert.Equal("Chrome", deserialized.DefaultBrowser);
            Assert.Equal(1000, deserialized.DefaultDelayMs);
            Assert.True(deserialized.LaunchAtStartup);
            Assert.Equal("Dark", deserialized.Theme);
            Assert.Equal(@"C:\Desktop", deserialized.DefaultLauncherLocation);
        }
    }
}
