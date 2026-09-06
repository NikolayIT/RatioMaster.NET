using System.Text.Json;
using System.Text.Json.Serialization;
using RatioMaster.Core.Sessions;

namespace RatioMaster.Core.Settings;

/// <summary>Source-generated JSON for the settings and session files.</summary>
[JsonSourceGenerationOptions(
    WriteIndented = true,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    PropertyNameCaseInsensitive = true,
    UseStringEnumConverter = true,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    ReadCommentHandling = JsonCommentHandling.Skip,
    AllowTrailingCommas = true)]
[JsonSerializable(typeof(AppSettings))]
[JsonSerializable(typeof(TorrentSettings))]
[JsonSerializable(typeof(SessionDocument))]
public sealed partial class CoreJsonContext : JsonSerializerContext;
