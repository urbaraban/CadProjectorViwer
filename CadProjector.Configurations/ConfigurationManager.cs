using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace CadProjector.Configurations
{
    public class ConfigurationManager
    {
        private static readonly Lazy<ConfigurationManager> instance =
      new Lazy<ConfigurationManager>(() => new ConfigurationManager());

        public static ConfigurationManager Instance => instance.Value;

        public ProjectorSettings ProjectorSettings { get; private set; }
        public DisplaySettings DisplaySettings { get; private set; }

        private readonly string configPath;

        private ConfigurationManager()
        {
            configPath = Path.Combine(
               Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "CadProjector",
                "settings.json");

            ProjectorSettings = new ProjectorSettings();
            DisplaySettings = new DisplaySettings();
        }

        public async Task LoadAsync()
        {
            try
            {
                if (File.Exists(configPath))
                {
                    var json = await File.ReadAllTextAsync(configPath);
                    var config = JsonSerializer.Deserialize<ConfigurationData>(json);

                    if (config != null)
                    {
                        ProjectorSettings = config.ProjectorSettings ?? new ProjectorSettings();
                        DisplaySettings = config.DisplaySettings ?? new DisplaySettings();
                    }
                }
            }
            catch (Exception ex)
            {
                // Log error and use default settings
                Console.WriteLine($"Error loading configuration: {ex.Message}");
            }
        }

        public async Task SaveAsync()
        {
            try
            {
                var directory = Path.GetDirectoryName(configPath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory!);
                }

                var config = new ConfigurationData
                {
                    ProjectorSettings = ProjectorSettings,
                    DisplaySettings = DisplaySettings
                };

                var json = JsonSerializer.Serialize(config, new JsonSerializerOptions
                {
                    WriteIndented = true
                });

                await File.WriteAllTextAsync(configPath, json);
            }
            catch (Exception ex)
            {
                // Log error
                Console.WriteLine($"Error saving configuration: {ex.Message}");
            }
        }

        private class ConfigurationData
        {
            public ProjectorSettings ProjectorSettings { get; set; }
            public DisplaySettings DisplaySettings { get; set; }
        }
    }
}