using System.Security.Cryptography;
using System.Text;
using AcademicTopicSelectionService.Application.Abstractions;
using StackExchange.Redis;

namespace AcademicTopicSelectionService.Infrastructure.Auth;

/// <summary>
/// Реализация кэша refresh-токенов на основе Redis (StackExchange.Redis).
/// Ключ: <c>refresh:{SHA256(UTF-8 токена)}</c> → значение <c>{userId}</c>, TTL = время жизни токена.
/// Сырой токен в ключе не хранится (короче ключ, нет проблем с кодировкой символов в ключе).
/// </summary>
public sealed class RedisRefreshTokenCache(IConnectionMultiplexer redis) : IRefreshTokenCache
{
    private static string Key(string token)
    {
        var normalized = token.Trim();
        if (normalized.Length == 0)
            throw new ArgumentException("Refresh token is empty.", nameof(token));

        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(normalized));
        return $"refresh:{Convert.ToHexString(hash)}";
    }

    /// <inheritdoc />
    public async Task StoreAsync(string token, Guid userId, TimeSpan expiry, CancellationToken ct)
    {
        var db = redis.GetDatabase();
        var ok = await db.StringSetAsync(Key(token), userId.ToString(), expiry).WaitAsync(ct);
        if (!ok)
            throw new InvalidOperationException("Redis SET did not persist the refresh token (returned false).");
    }

    /// <inheritdoc />
    public async Task<Guid?> GetUserIdAsync(string token, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(token))
            return null;

        var db = redis.GetDatabase();
        var value = await db.StringGetAsync(Key(token)).WaitAsync(ct);

        if (!value.HasValue || !Guid.TryParse((string?)value, out var userId))
            return null;

        return userId;
    }

    /// <inheritdoc />
    public async Task RemoveAsync(string token, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(token))
            return;

        var db = redis.GetDatabase();
        await db.KeyDeleteAsync(Key(token)).WaitAsync(ct);
    }
}
