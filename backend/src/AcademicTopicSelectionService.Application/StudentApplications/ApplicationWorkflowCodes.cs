namespace AcademicTopicSelectionService.Application.StudentApplications;

/// <summary>
/// Коды статусов заявки студента (справочник application_statuses.code_name).
/// </summary>
internal static class ApplicationStatusCodes
{
    public const string Pending = "Pending";
    public const string ApprovedBySupervisor = "ApprovedBySupervisor";
    public const string RejectedBySupervisor = "RejectedBySupervisor";
    public const string PendingDepartmentHead = "PendingDepartmentHead";
    public const string ApprovedByDepartmentHead = "ApprovedByDepartmentHead";
    public const string RejectedByDepartmentHead = "RejectedByDepartmentHead";
    public const string Cancelled = "Cancelled";
}

/// <summary>
/// Коды статусов действия по заявке (справочник application_action_statuses.code_name).
/// </summary>
internal static class ApplicationActionStatusCodes
{
    public const string Pending = "Pending";
    public const string Approved = "Approved";
    public const string Rejected = "Rejected";
    public const string Cancelled = "Cancelled";
}

/// <summary>
/// Коды ролей пользователя (справочник user_roles.code_name).
/// </summary>
internal static class UserRoleCodes
{
    public const string Student = "Student";
    public const string DepartmentHead = "DepartmentHead";
}

/// <summary>
/// Коды типов создателя темы (справочник topic_creator_types.code_name).
/// </summary>
internal static class TopicCreatorTypeCodes
{
    public const string Student = "Student";
}

/// <summary>
/// Коды статусов темы (справочник topic_statuses.code_name).
/// </summary>
internal static class TopicStatusCodes
{
    public const string Active = "Active";
}

/// <summary>
/// Коды типов уведомлений (справочник notification_types.code_name).
/// </summary>
internal static class NotificationTypeCodes
{
    public const string ApplicationSubmittedToSupervisor = "ApplicationSubmittedToSupervisor";
    public const string ApplicationSubmittedToDepartmentHead = "ApplicationSubmittedToDepartmentHead";
    public const string ApplicationStatusChanged = "ApplicationStatusChanged";
}
