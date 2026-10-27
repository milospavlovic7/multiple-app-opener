using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using WorkspaceLauncher.Models;

namespace WorkspaceLauncher.Services
{
    public class PersistenceService
    {
        private static readonly Lazy<PersistenceService> _instance = 
            new Lazy<PersistenceService>(() => new PersistenceService());

        public static PersistenceService Instance => _instance.Value;

        private readonly string _appDataFolder;
        private readonly string _settingsFilePath;
        private readonly string _workflowsFilePath;

        private readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true
        };

        private PersistenceService()
        {
            _appDataFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), 
                "WorkspaceLauncher"
            );
            
            if (!Directory.Exists(_appDataFolder))
            {
                Directory.CreateDirectory(_appDataFolder);
            }

            _settingsFilePath = Path.Combine(_appDataFolder, "settings.json");
            _workflowsFilePath = Path.Combine(_appDataFolder, "workflows.json");
        }

        public string AppDataFolder => _appDataFolder;

        public Settings LoadSettings()
        {
            try
            {
                if (File.Exists(_settingsFilePath))
                {
                    string json = File.ReadAllText(_settingsFilePath);
                    var settings = JsonSerializer.Deserialize<Settings>(json, _jsonOptions);
                    if (settings != null)
                    {
                        return settings;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading settings: {ex.Message}");
            }

            // Return defaults if not found or failed
            var defaultSettings = new Settings();
            SaveSettings(defaultSettings);
            return defaultSettings;
        }

        public void SaveSettings(Settings settings)
        {
            try
            {
                string json = JsonSerializer.Serialize(settings, _jsonOptions);
                File.WriteAllText(_settingsFilePath, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving settings: {ex.Message}");
            }
        }

        public List<Workflow> LoadWorkflows()
        {
            try
            {
                if (File.Exists(_workflowsFilePath))
                {
                    string json = File.ReadAllText(_workflowsFilePath);
                    var workflows = JsonSerializer.Deserialize<List<Workflow>>(json, _jsonOptions);
                    if (workflows != null)
                    {
                        return workflows;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading workflows: {ex.Message}");
            }

            // Return empty list if not found or failed
            var emptyList = new List<Workflow>();
            SaveWorkflows(emptyList);
            return emptyList;
        }

        public void SaveWorkflows(List<Workflow> workflows)
        {
            try
            {
                string json = JsonSerializer.Serialize(workflows, _jsonOptions);
                File.WriteAllText(_workflowsFilePath, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving workflows: {ex.Message}");
            }
        }
    }
}
