# Dependency Track BOM Uploader

A standalone C# command-line tool for uploading SBOMs (Software Bill of Materials) to [Dependency-Track](https://dependencytrack.org/). Designed to be downloaded and executed directly during CI/CD pipeline runs—no runtime dependencies beyond .NET 8.

---

## Features

- Upload SBOMs (CycloneDX format) to Dependency-Track
- Automatically create projects if they don't exist
- **Automatically create parent projects** if they don't exist (no more manual setup)
- Organize projects in a parent-child hierarchy
- Update project metadata (description, classifier, tags, group, etc.)
- Fail or warn builds based on vulnerability and policy violation thresholds
- Support for custom CA certificates (self-signed / internal CAs)
- Configuration via **YAML file**, **command-line arguments**, or **both** (CLI overrides YAML)

---

## Getting Started

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) (for building from source)

### Build

```bash
dotnet build src/DependencyTrackUploader/DependencyTrackUploader.csproj -c Release
```

### Publish as a Self-Contained Binary

Produce a single standalone binary that requires no .NET runtime on the target machine:

```bash
# Linux (x64)
dotnet publish src/DependencyTrackUploader/DependencyTrackUploader.csproj \
  -c Release -r linux-x64 --self-contained -p:PublishSingleFile=true -o publish/linux-x64

# Windows (x64)
dotnet publish src/DependencyTrackUploader/DependencyTrackUploader.csproj \
  -c Release -r win-x64 --self-contained -p:PublishSingleFile=true -o publish/win-x64

# macOS (x64)
dotnet publish src/DependencyTrackUploader/DependencyTrackUploader.csproj \
  -c Release -r osx-x64 --self-contained -p:PublishSingleFile=true -o publish/osx-x64
```

### Run Tests

```bash
dotnet test tests/DependencyTrackUploader.Tests/DependencyTrackUploader.Tests.csproj
```

---

## Usage

### Purely via Command-Line Arguments

```bash
./dtrack-uploader \
  --dtrack-url https://dependency-track.example.com \
  --api-key YOUR_API_KEY \
  --project-name my-app \
  --project-version 1.0.0 \
  --auto-create \
  --bom-file-path bom.xml \
  --threshold-action warn \
  --threshold-critical 0
```

### Using a YAML Config File

```bash
./dtrack-uploader --config config.yaml
```

See [`config-template.yaml`](config-template.yaml) for all available YAML options.

### Combining YAML Config with CLI Overrides

Define common settings in a YAML file and override specific values per run via CLI arguments. **CLI arguments always take precedence over YAML values.**

```bash
# config.yaml contains base URL, API key, project name, thresholds, etc.
# Override just the version and BOM path per pipeline run:
./dtrack-uploader \
  --config config.yaml \
  --project-version $BUILD_VERSION \
  --bom-file-path $BOM_PATH
```

### CI/CD Pipeline Example (Azure Pipelines)

```yaml
steps:
- script: |
    curl -L -o dtrack-uploader https://your-artifacts-host/dtrack-uploader-linux-x64
    chmod +x dtrack-uploader
  displayName: 'Download dtrack-uploader'

- script: |
    ./dtrack-uploader \
      --config config.yaml \
      --project-version $(Build.BuildNumber) \
      --bom-file-path $(Build.ArtifactStagingDirectory)/bom.xml
  displayName: 'Upload SBOM to Dependency-Track'
  env:
    DTRACK_API_KEY: $(DTrackApiKey)
```

---

## Command-Line Arguments

| Argument | Alias | Description |
|----------|-------|-------------|
| `--config` | `-c` | Path to a YAML configuration file. All settings can be defined there. |
| `--bom-file-path` | `-b` | **(Required)** Path to the BOM file to upload. |
| `--dtrack-url` | | **(Required)** Dependency Track server URL. |
| `--api-key` | | **(Required)** Dependency Track API key. |

### Project Identification

Provide **either** `--project-id`, **or** `--project-name` and `--project-version`:

| Argument | Description |
|----------|-------------|
| `--project-id` | UUID of an existing project in Dependency Track. |
| `--project-name` | Project name. Required if `--project-id` is not specified, or when using `--auto-create`. |
| `--project-version` | Project version. Required if `--project-id` is not specified, or when using `--auto-create`. |

### Project Settings (Optional)

| Argument | Description |
|----------|-------------|
| `--auto-create` | Auto-create the project if it doesn't exist. Requires `--project-name` and `--project-version`. |
| `--is-latest` | Mark this project version as the latest. Defaults to `false`. |
| `--project-description` | Set the project description in Dependency Track. |
| `--project-classifier` | Set the project classifier. Valid values: `APPLICATION`, `FRAMEWORK`, `LIBRARY`, `CONTAINER`, `OPERATING_SYSTEM`, `DEVICE`, `FIRMWARE`, `FILE`, `PLATFORM`, `DEVICE_DRIVER`, `MACHINE_LEARNING_MODEL`, `DATA`. |
| `--project-swid-tag-id` | Set the project SWID Tag ID. |
| `--project-group` | Set the project namespace / group / vendor identifier. |
| `--project-tags` | Set project tags. Can specify multiple values (e.g., `--project-tags tag1 tag2 tag3`). |

### Parent Project (Optional)

When using `--auto-create`, you can specify a parent project. If the parent project does not exist, it will be **created automatically**.

| Argument | Description |
|----------|-------------|
| `--parent-project-name` | Name of the parent project. |
| `--parent-project-version` | Version of the parent project. If not specified, defaults to `"default"` when the parent needs to be created. |

### SSL / TLS (Optional)

| Argument | Description |
|----------|-------------|
| `--ca-file-path` | Path to a PEM-encoded CA certificate. Use when Dependency Track uses a self-signed certificate or an internal CA. |

### Threshold Controls (Optional)

Configure thresholds to warn or fail the build based on vulnerability counts or policy violations. A threshold value of `-1` disables that particular check.

| Argument | Description |
|----------|-------------|
| `--threshold-action` | Action to take when a threshold is exceeded: `none` (default), `warn`, or `error`. |
| `--threshold-critical` | Maximum allowed critical vulnerabilities. Default: `-1` (disabled). |
| `--threshold-high` | Maximum allowed high vulnerabilities. Default: `-1` (disabled). |
| `--threshold-medium` | Maximum allowed medium vulnerabilities. Default: `-1` (disabled). |
| `--threshold-low` | Maximum allowed low vulnerabilities. Default: `-1` (disabled). |
| `--threshold-unassigned` | Maximum allowed unassigned vulnerabilities. Default: `-1` (disabled). |
| `--threshold-policy-violations-fail` | Maximum allowed fail policy violations. Default: `-1` (disabled). |
| `--threshold-policy-violations-warn` | Maximum allowed warn policy violations. Default: `-1` (disabled). |
| `--threshold-policy-violations-info` | Maximum allowed info policy violations. Default: `-1` (disabled). |
| `--threshold-policy-violations-total` | Maximum allowed total policy violations. Default: `-1` (disabled). |

### Other

| Argument | Description |
|----------|-------------|
| `--version` | Show version information. |
| `-h`, `--help` | Show help and usage information. |

---

## YAML Configuration

All command-line arguments can also be specified in a YAML configuration file. See [`config-template.yaml`](config-template.yaml) for a complete example with all available options.

When both a YAML config file and CLI arguments are provided, **CLI arguments override the YAML values**. This allows you to define common settings in YAML (e.g., server URL, API key, thresholds) and override run-specific values from the command line (e.g., project version, BOM file path).

---

## Required Permissions

The following table outlines the minimum permissions required in Dependency-Track for each operation:

| Use Case | Required Permissions |
|----------|---------------------|
| **Basic upload to existing project** | `BOM_UPLOAD` |
| **Upload and create project** | `BOM_UPLOAD` + `PROJECT_CREATION_UPLOAD` |
| **Use thresholds** | `VIEW_PORTFOLIO` |
| **Update project properties** | `PORTFOLIO_MANAGEMENT` |

### Recommended Setup

For most CI/CD scenarios:

```
BOM_UPLOAD + PROJECT_CREATION_UPLOAD + VIEW_PORTFOLIO
```

Add `PORTFOLIO_MANAGEMENT` if you need to set project descriptions, tags, or other properties.

---

## Exit Codes

| Code | Meaning |
|------|---------|
| `0` | Success (or threshold exceeded in `warn` mode). |
| `1` | Error — validation failure, upload failure, or threshold exceeded in `error` mode. |

---

## Notes

- The BOM file must be in [CycloneDX](https://cyclonedx.org/) format.
- When `--auto-create` is enabled and a `--parent-project-name` is specified, the parent project will be created automatically if it does not already exist.
- Threshold checks only run when `--threshold-action` is set to `warn` or `error` **and** at least one threshold value is set to `0` or higher.

---

## Links

- [Dependency-Track](https://dependencytrack.org/)
- [CycloneDX](https://cyclonedx.org/)
