namespace AcademicTopicSelectionService.Application.Notifications;

/// <summary>
/// Коды типов уведомлений (справочник <c>notification_types.code_name</c>).
/// </summary>
public static class NotificationTypeCodes
{
    public const string ApplicationSubmittedToSupervisor = "ApplicationSubmittedToSupervisor";
    public const string ApplicationSubmittedToDepartmentHead = "ApplicationSubmittedToDepartmentHead";
    public const string ApplicationStatusChanged = "ApplicationStatusChanged";
    public const string NewMessage = "NewMessage";
    public const string GraduateWorkUploaded = "GraduateWorkUploaded";
}
