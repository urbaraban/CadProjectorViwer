using System.Text.Json;
using System.Text.Json.Serialization;
using CadProjector.Core.Devices;
using CadProjector.Logging;

namespace CadProjector.FileFormats.ProjectJson;

/// <summary>Save/load device profile (+ mesh + modules) as .cdev JSON.</summary>
public static class ProfileJsonStore
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public static async Task SaveAsync(ProjectorProfile profile, string path, CancellationToken ct = default)
    {
        var dto = DeviceSnapshot.From(profile);
        await using var fs = File.Create(path);
        await JsonSerializer.SerializeAsync(fs, dto, Options, ct);
        CadLog.Good($"Device profile saved: {Path.GetFileName(path)} ({profile.DisplayName})");
    }

    public static async Task LoadIntoAsync(ProjectorProfile profile, string path, CancellationToken ct = default)
    {
        await using var fs = File.OpenRead(path);
        var dto = await JsonSerializer.DeserializeAsync<DeviceSnapshot>(fs, Options, ct)
            ?? throw new InvalidDataException("Empty profile file.");
        dto.ApplyTo(profile);
        CadLog.Good($"Device profile loaded into {profile.DisplayName}");
    }
}
