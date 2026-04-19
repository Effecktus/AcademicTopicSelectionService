using AcademicTopicSelectionService.Application.Abstractions;
using AcademicTopicSelectionService.Domain.Entities;
using AcademicTopicSelectionService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AcademicTopicSelectionService.Infrastructure.Repositories;

/// <summary>
/// Репозиторий сообщений чата.
/// </summary>
public sealed class ChatMessagesRepository(ApplicationDbContext db) : IChatMessagesRepository
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<ChatMessage>> GetByApplicationAsync(
        Guid applicationId, Guid? afterId, int limit, CancellationToken ct)
    {
        var take = Math.Clamp(limit, 1, 200);

        if (afterId is { } cursorId)
        {
            var cursor = await db.ChatMessages.AsNoTracking()
                .Where(m => m.Id == cursorId && m.ApplicationId == applicationId)
                .Select(m => new { m.SentAt, m.Id })
                .FirstOrDefaultAsync(ct);

            if (cursor is null)
                return [];

            return await db.ChatMessages.AsNoTracking()
                .Include(m => m.Sender)
                .Where(m => m.ApplicationId == applicationId &&
                    (m.SentAt > cursor.SentAt ||
                     (m.SentAt == cursor.SentAt && m.Id.CompareTo(cursor.Id) > 0)))
                .OrderBy(m => m.SentAt)
                .ThenBy(m => m.Id)
                .Take(take)
                .ToListAsync(ct);
        }

        var newestFirst = await db.ChatMessages.AsNoTracking()
            .Include(m => m.Sender)
            .Where(m => m.ApplicationId == applicationId)
            .OrderByDescending(m => m.SentAt)
            .ThenByDescending(m => m.Id)
            .Take(take)
            .ToListAsync(ct);

        newestFirst.Reverse();
        return newestFirst;
    }

    /// <inheritdoc />
    public async Task<ChatMessage> AddAsync(ChatMessage message, CancellationToken ct)
    {
        db.ChatMessages.Add(message);
        await db.SaveChangesAsync(ct);

        return await db.ChatMessages.AsNoTracking()
            .Include(m => m.Sender)
            .FirstAsync(m => m.Id == message.Id, ct);
    }

    /// <inheritdoc />
    public Task MarkIncomingAsReadAsync(Guid applicationId, Guid readerUserId, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        return db.ChatMessages
            .Where(m => m.ApplicationId == applicationId && m.SenderId != readerUserId && m.ReadAt == null)
            .ExecuteUpdateAsync(setters => setters.SetProperty(m => m.ReadAt, now), ct);
    }
}
