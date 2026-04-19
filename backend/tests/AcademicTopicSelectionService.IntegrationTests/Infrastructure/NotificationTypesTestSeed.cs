using AcademicTopicSelectionService.Domain.Entities;
using AcademicTopicSelectionService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AcademicTopicSelectionService.IntegrationTests.Infrastructure;

/// <summary>
/// Справочник <c>NotificationTypes</c> при <see cref="DatabaseFixture"/> не заполняется SQL init —
/// добавляем строки, совпадающие с <c>infra/db/init/05_create_notification_types.sql</c>.
/// </summary>
internal static class NotificationTypesTestSeed
{
    private static readonly (string CodeName, string DisplayName)[] Rows =
    [
        ("ApplicationStatusChanged", "Статус заявки изменен"),
        ("NewMessage", "Новое сообщение"),
        ("TopicApproved", "Тема утверждена"),
        ("TopicRejected", "Тема отклонена"),
        ("SupervisorRequestStatusChanged", "Статус запроса на научрука изменен"),
        ("SupervisorRequestCreated", "Новый запрос на научное руководство"),
        ("ApplicationSubmittedToSupervisor", "Новая заявка на рассмотрение научруком"),
        ("ApplicationSubmittedToDepartmentHead", "Заявка передана на рассмотрение кафедры"),
        ("GraduateWorkUploaded", "ВКР загружена в архив")
    ];

    public static async Task EnsureAsync(ApplicationDbContext db, CancellationToken ct = default)
    {
        foreach (var (codeName, displayName) in Rows)
        {
            var exists = await db.NotificationTypes.AnyAsync(
                t => EF.Functions.ILike(t.CodeName, codeName), ct);
            if (!exists)
                db.NotificationTypes.Add(new NotificationType { CodeName = codeName, DisplayName = displayName });
        }
    }
}
