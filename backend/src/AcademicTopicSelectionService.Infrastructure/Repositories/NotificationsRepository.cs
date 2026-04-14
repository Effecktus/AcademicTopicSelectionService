using AcademicTopicSelectionService.Application.Abstractions;
using AcademicTopicSelectionService.Application.Dictionaries;
using AcademicTopicSelectionService.Application.Notifications;
using AcademicTopicSelectionService.Domain.Entities;
using AcademicTopicSelectionService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AcademicTopicSelectionService.Infrastructure.Repositories;

public sealed class NotificationsRepository(ApplicationDbContext db) : INotificationsRepository
{
    public async Task<PagedResult<NotificationDto>> ListByUserAsync(
        Guid userId,
        ListNotificationsQuery query,
        CancellationToken ct)
    {
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 200);

        var queryToDb = db.Notifications
            .AsNoTracking()
            .Where(x => x.UserId == userId);

        if (query.IsRead.HasValue)
            queryToDb = queryToDb.Where(x => x.IsRead == query.IsRead.Value);

        var total = await queryToDb.LongCountAsync(ct);
        var items = await queryToDb
            .OrderByDescending(x => x.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new NotificationDto(
                x.Id,
                x.Type.CodeName,
                x.Type.DisplayName,
                x.Title,
                x.Content,
                x.IsRead,
                x.CreatedAt))
            .ToListAsync(ct);

        return new PagedResult<NotificationDto>(page, pageSize, total, items);
    }

    public Task<Notification?> GetByIdAsync(Guid id, CancellationToken ct)
        => db.Notifications.FirstOrDefaultAsync(x => x.Id == id, ct);

    public void Add(Notification notification)
        => db.Notifications.Add(notification);

    public Task<int> MarkAllAsReadAsync(Guid userId, DateTime readAtUtc, CancellationToken ct)
        => db.Notifications
            .Where(x => x.UserId == userId && !x.IsRead)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.IsRead, true), ct);

    public Task SaveChangesAsync(CancellationToken ct)
        => db.SaveChangesAsync(ct);

    public Task<NotificationType?> GetTypeByCodeNameAsync(string codeName, CancellationToken ct)
        => db.NotificationTypes
            .FirstOrDefaultAsync(x => EF.Functions.ILike(x.CodeName, codeName), ct);
}
