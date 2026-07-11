using System.Text.Json;
using System.Text.Json.Serialization;

namespace CitizenPlatform.Application.Common;

internal static class OutboxJson
{
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };
}
