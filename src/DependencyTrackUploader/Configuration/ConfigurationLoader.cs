using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace DependencyTrackUploader.Configuration;

public static class ConfigurationLoader
{
    public static AppSettings LoadFromYaml(string filePath)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"Configuration file not found: {filePath}");

        var yaml = File.ReadAllText(filePath);
        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(HyphenatedNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build();

        return deserializer.Deserialize<AppSettings>(yaml) ?? new AppSettings();
    }

    public static AppSettings BuildConfiguration(string? configFilePath, AppSettings cliSettings)
    {
        var settings = new AppSettings();

        if (!string.IsNullOrWhiteSpace(configFilePath))
        {
            settings = LoadFromYaml(configFilePath);
        }

        // CLI arguments override YAML settings
        settings.MergeFrom(cliSettings);

        return settings;
    }
}
