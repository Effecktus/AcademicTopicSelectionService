using System.Text.Json;
using System.Text.Json.Serialization;

namespace AcademicTopicSelectionService.API.Serialization;

/// <summary>
/// Пишет все <see cref="DateTime"/> как UTC (с суффиксом <c>Z</c>).
/// </summary>
/// <remarks>
/// Все даты в системе хранятся в UTC. Npgsql возвращает их с <see cref="DateTimeKind.Utc"/>,
/// а SQL Server (<c>datetime2</c>) — с <see cref="DateTimeKind.Unspecified"/>: без конвертера
/// клиент получил бы время без часового пояса и сдвинул бы его в локальное.
/// </remarks>
public sealed class UtcDateTimeJsonConverter : JsonConverter<DateTime>
{
    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        => reader.GetDateTime();

    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
    {
        var utc = value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
        };
        writer.WriteStringValue(utc);
    }
}
