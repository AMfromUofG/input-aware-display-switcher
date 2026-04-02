using System.Text.Json;
using System.Text.Json.Serialization;

namespace InputAwareDisplaySwitcher.Infrastructure.Configuration;

internal static class ConfigurationJsonSerializerOptions
{
    public static JsonSerializerOptions Default { get; } = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        Converters =
        {
            new JsonStringEnumConverter()
        }
    };
}
