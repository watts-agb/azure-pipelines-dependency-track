using System.CommandLine;
using DependencyTrackUploader.Client;
using DependencyTrackUploader.Configuration;
using DependencyTrackUploader.Services;

namespace DependencyTrackUploader;

public class Program
{
    public static async Task<int> Main(string[] args)
    {
        var rootCommand = new RootCommand("Dependency Track BOM Uploader - Upload SBOMs to Dependency Track")
        {
            TreatUnmatchedTokensAsErrors = true
        };

        // Config file option
        var configOption = new Option<string?>("--config", "Path to YAML configuration file");
        configOption.AddAlias("-c");
        rootCommand.AddOption(configOption);

        // Connection options
        var urlOption = new Option<string?>("--dtrack-url", "Dependency Track server URL");
        var apiKeyOption = new Option<string?>("--api-key", "Dependency Track API key");
        rootCommand.AddOption(urlOption);
        rootCommand.AddOption(apiKeyOption);

        // Project options
        var projectIdOption = new Option<string?>("--project-id", "Existing project UUID");
        var projectNameOption = new Option<string?>("--project-name", "Project name");
        var projectVersionOption = new Option<string?>("--project-version", "Project version");
        var projectDescriptionOption = new Option<string?>("--project-description", "Project description");
        var projectClassifierOption = new Option<string?>("--project-classifier", "Project classifier (APPLICATION, FRAMEWORK, LIBRARY, etc.)");
        var projectSwidTagIdOption = new Option<string?>("--project-swid-tag-id", "Project SWID Tag ID");
        var projectGroupOption = new Option<string?>("--project-group", "Project namespace/group/vendor");
        var projectTagsOption = new Option<string[]?>("--project-tags", "Project tags (can specify multiple)") { AllowMultipleArgumentsPerToken = true };
        var autoCreateOption = new Option<bool?>("--auto-create", "Auto-create project if it doesn't exist");
        var isLatestOption = new Option<bool?>("--is-latest", "Mark this as the latest project version");
        rootCommand.AddOption(projectIdOption);
        rootCommand.AddOption(projectNameOption);
        rootCommand.AddOption(projectVersionOption);
        rootCommand.AddOption(projectDescriptionOption);
        rootCommand.AddOption(projectClassifierOption);
        rootCommand.AddOption(projectSwidTagIdOption);
        rootCommand.AddOption(projectGroupOption);
        rootCommand.AddOption(projectTagsOption);
        rootCommand.AddOption(autoCreateOption);
        rootCommand.AddOption(isLatestOption);

        // Parent project options
        var parentProjectNameOption = new Option<string?>("--parent-project-name", "Parent project name (used with --auto-create)");
        var parentProjectVersionOption = new Option<string?>("--parent-project-version", "Parent project version (used with --auto-create)");
        rootCommand.AddOption(parentProjectNameOption);
        rootCommand.AddOption(parentProjectVersionOption);

        // BOM file
        var bomFilePathOption = new Option<string?>("--bom-file-path", "Path to the BOM file");
        bomFilePathOption.AddAlias("-b");
        rootCommand.AddOption(bomFilePathOption);

        // SSL options
        var caFilePathOption = new Option<string?>("--ca-file-path", "Path to PEM-encoded CA certificate");
        rootCommand.AddOption(caFilePathOption);

        // Threshold options
        var thresholdActionOption = new Option<string?>("--threshold-action", "Action on threshold breach (none, warn, error)");
        var thresholdCriticalOption = new Option<int?>("--threshold-critical", "Maximum critical vulnerabilities (-1 to disable)");
        var thresholdHighOption = new Option<int?>("--threshold-high", "Maximum high vulnerabilities (-1 to disable)");
        var thresholdMediumOption = new Option<int?>("--threshold-medium", "Maximum medium vulnerabilities (-1 to disable)");
        var thresholdLowOption = new Option<int?>("--threshold-low", "Maximum low vulnerabilities (-1 to disable)");
        var thresholdUnassignedOption = new Option<int?>("--threshold-unassigned", "Maximum unassigned vulnerabilities (-1 to disable)");
        var thresholdPolicyFailOption = new Option<int?>("--threshold-policy-violations-fail", "Maximum fail policy violations (-1 to disable)");
        var thresholdPolicyWarnOption = new Option<int?>("--threshold-policy-violations-warn", "Maximum warn policy violations (-1 to disable)");
        var thresholdPolicyInfoOption = new Option<int?>("--threshold-policy-violations-info", "Maximum info policy violations (-1 to disable)");
        var thresholdPolicyTotalOption = new Option<int?>("--threshold-policy-violations-total", "Maximum total policy violations (-1 to disable)");
        rootCommand.AddOption(thresholdActionOption);
        rootCommand.AddOption(thresholdCriticalOption);
        rootCommand.AddOption(thresholdHighOption);
        rootCommand.AddOption(thresholdMediumOption);
        rootCommand.AddOption(thresholdLowOption);
        rootCommand.AddOption(thresholdUnassignedOption);
        rootCommand.AddOption(thresholdPolicyFailOption);
        rootCommand.AddOption(thresholdPolicyWarnOption);
        rootCommand.AddOption(thresholdPolicyInfoOption);
        rootCommand.AddOption(thresholdPolicyTotalOption);

        rootCommand.SetHandler(async (context) =>
        {
            var configFile = context.ParseResult.GetValueForOption(configOption);

            var cliSettings = new AppSettings
            {
                DTrackUrl = context.ParseResult.GetValueForOption(urlOption),
                ApiKey = context.ParseResult.GetValueForOption(apiKeyOption),
                ProjectId = context.ParseResult.GetValueForOption(projectIdOption),
                ProjectName = context.ParseResult.GetValueForOption(projectNameOption),
                ProjectVersion = context.ParseResult.GetValueForOption(projectVersionOption),
                ProjectDescription = context.ParseResult.GetValueForOption(projectDescriptionOption),
                ProjectClassifier = context.ParseResult.GetValueForOption(projectClassifierOption),
                ProjectSwidTagId = context.ParseResult.GetValueForOption(projectSwidTagIdOption),
                ProjectGroup = context.ParseResult.GetValueForOption(projectGroupOption),
                ProjectTags = context.ParseResult.GetValueForOption(projectTagsOption)?.ToList(),
                AutoCreate = context.ParseResult.GetValueForOption(autoCreateOption),
                IsLatest = context.ParseResult.GetValueForOption(isLatestOption),
                ParentProjectName = context.ParseResult.GetValueForOption(parentProjectNameOption),
                ParentProjectVersion = context.ParseResult.GetValueForOption(parentProjectVersionOption),
                BomFilePath = context.ParseResult.GetValueForOption(bomFilePathOption),
                CaFilePath = context.ParseResult.GetValueForOption(caFilePathOption),
                ThresholdAction = context.ParseResult.GetValueForOption(thresholdActionOption),
                ThresholdCritical = context.ParseResult.GetValueForOption(thresholdCriticalOption),
                ThresholdHigh = context.ParseResult.GetValueForOption(thresholdHighOption),
                ThresholdMedium = context.ParseResult.GetValueForOption(thresholdMediumOption),
                ThresholdLow = context.ParseResult.GetValueForOption(thresholdLowOption),
                ThresholdUnassigned = context.ParseResult.GetValueForOption(thresholdUnassignedOption),
                ThresholdPolicyViolationsFail = context.ParseResult.GetValueForOption(thresholdPolicyFailOption),
                ThresholdPolicyViolationsWarn = context.ParseResult.GetValueForOption(thresholdPolicyWarnOption),
                ThresholdPolicyViolationsInfo = context.ParseResult.GetValueForOption(thresholdPolicyInfoOption),
                ThresholdPolicyViolationsTotal = context.ParseResult.GetValueForOption(thresholdPolicyTotalOption),
            };

            try
            {
                var settings = ConfigurationLoader.BuildConfiguration(configFile, cliSettings);
                settings.Validate();

                var exitCode = await RunAsync(settings);
                context.ExitCode = exitCode;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error: {ex.Message}");
                context.ExitCode = 1;
            }
        });

        return await rootCommand.InvokeAsync(args);
    }

    internal static async Task<int> RunAsync(AppSettings settings)
    {
        var bomContent = File.ReadAllText(settings.BomFilePath!);
        Console.WriteLine($"Reading BOM from location: {settings.BomFilePath}");

        using var client = new DTrackClient(settings.DTrackUrl!, settings.ApiKey!, settings.CaFilePath);
        var manager = new DTrackManager(client);

        var projectId = settings.ProjectId;
        string? token = null;
        var isAutoCreate = settings.AutoCreate ?? false;
        var isLatest = settings.IsLatest ?? false;

        if (isAutoCreate)
        {
            if (!string.IsNullOrWhiteSpace(settings.ParentProjectName))
            {
                Console.WriteLine($"Starting BOM upload to Dependency Track at url: {settings.DTrackUrl}");
                Console.WriteLine($"Project with name '{settings.ProjectName}' and version '{settings.ProjectVersion}' will be created as a child of project with name '{settings.ParentProjectName}' and version '{settings.ParentProjectVersion}'.");
                token = await manager.UploadBomAndCreateChildProjectAsync(
                    settings.ProjectName!, settings.ProjectVersion!,
                    settings.ParentProjectName, settings.ParentProjectVersion,
                    isLatest, bomContent);
            }
            else
            {
                Console.WriteLine($"Starting BOM upload to Dependency Track at url: {settings.DTrackUrl}");
                Console.WriteLine($"Project with name '{settings.ProjectName}' and version '{settings.ProjectVersion}' will be created if it does not exist.");
                token = await manager.UploadBomAndCreateProjectAsync(
                    settings.ProjectName!, settings.ProjectVersion!, isLatest, bomContent);
            }

            Console.WriteLine($"Getting project id using name '{settings.ProjectName}' and version '{settings.ProjectVersion}'.");
            projectId = await manager.GetProjectUuidAsync(settings.ProjectName!, settings.ProjectVersion);
        }
        else
        {
            if (string.IsNullOrWhiteSpace(projectId))
            {
                Console.WriteLine($"Getting project id using name '{settings.ProjectName}' and version '{settings.ProjectVersion}'.");
                projectId = await manager.GetProjectUuidAsync(settings.ProjectName!, settings.ProjectVersion);
            }

            Console.WriteLine($"Starting BOM upload to Dependency Track for project with id '{projectId}' at url: {settings.DTrackUrl}");
            token = await manager.UploadBomAsync(projectId!, bomContent);
        }

        Console.WriteLine($"Uploading the BOM succeeded! Token: {token}");

        await manager.UpdateProjectAsync(
            projectId!,
            settings.ProjectDescription,
            settings.ProjectClassifier,
            settings.ProjectSwidTagId,
            settings.ProjectGroup,
            settings.ProjectTags,
            settings.IsLatest);

        var thresholdAction = settings.ThresholdAction ?? "none";
        var thresholdExpert = new ThresholdExpert(
            settings.ThresholdCritical ?? -1,
            settings.ThresholdHigh ?? -1,
            settings.ThresholdMedium ?? -1,
            settings.ThresholdLow ?? -1,
            settings.ThresholdUnassigned ?? -1,
            settings.ThresholdPolicyViolationsFail ?? -1,
            settings.ThresholdPolicyViolationsWarn ?? -1,
            settings.ThresholdPolicyViolationsInfo ?? -1,
            settings.ThresholdPolicyViolationsTotal ?? -1);

        if ((thresholdAction == "warn" || thresholdAction == "error") && thresholdExpert.AreThresholdsConfigured())
        {
            Console.WriteLine("Waiting for Dependency Track to finish processing the BOM...");
            await manager.WaitBomProcessingAsync(token);

            Console.WriteLine("Waiting for metrics to refresh...");
            await manager.WaitMetricsRefreshAsync(projectId!);
            var metrics = await manager.GetProjectMetricsAsync(projectId!);

            Console.WriteLine($"Current Vulnerability Count:");
            Console.WriteLine($"  Critical: {metrics.Critical}");
            Console.WriteLine($"  High: {metrics.High}");
            Console.WriteLine($"  Medium: {metrics.Medium}");
            Console.WriteLine($"  Low: {metrics.Low}");
            Console.WriteLine($"  Unassigned: {metrics.Unassigned}");
            Console.WriteLine($"  Suppressed: {metrics.Suppressed}");

            Console.WriteLine($"Current Policy Violation Count:");
            Console.WriteLine($"  Fail: {metrics.PolicyViolationsFail}");
            Console.WriteLine($"  Warn: {metrics.PolicyViolationsWarn}");
            Console.WriteLine($"  Info: {metrics.PolicyViolationsInfo}");
            Console.WriteLine($"  Total: {metrics.PolicyViolationsTotal}");

            try
            {
                thresholdExpert.ValidateThresholds(metrics);
            }
            catch (ThresholdExceededException ex)
            {
                if (thresholdAction == "error")
                {
                    Console.Error.WriteLine($"Error: {ex.Message}");
                    return 1;
                }

                // Warn mode
                Console.WriteLine($"Warning: {ex.Message}");
                return 0;
            }
        }

        Console.WriteLine("Finished task execution successfully!");
        return 0;
    }
}
