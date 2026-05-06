namespace AcademicTopicSelectionService.Application.StudentApplications;

/// <summary>
/// Коды статусов заявки студента (справочник application_statuses.code_name).
/// </summary>
public static class ApplicationStatusCodes
{
    public const string OnEditing = "OnEditing";
    public const string Pending = "Pending";
    public const string ApprovedBySupervisor = "ApprovedBySupervisor";
    public const string RejectedBySupervisor = "RejectedBySupervisor";
    public const string PendingDepartmentHead = "PendingDepartmentHead";
    public const string ApprovedByDepartmentHead = "ApprovedByDepartmentHead";
    public const string RejectedByDepartmentHead = "RejectedByDepartmentHead";
    public const string Cancelled = "Cancelled";
}

/// <summary>
/// Коды статусов запроса на научного руководителя (справочник supervisor_request_statuses.code_name).
/// </summary>
public static class SupervisorRequestStatusCodes
{
    public const string Pending = "Pending";
    public const string ApprovedBySupervisor = "ApprovedBySupervisor";
}

/// <summary>
/// Коды статусов действия по заявке (справочник application_action_statuses.code_name).
/// </summary>
internal static class ApplicationActionStatusCodes
{
    public const string Pending = "Pending";
    public const string Approved = "Approved";
    public const string Rejected = "Rejected";
    public const string ReturnedForEditing = "ReturnedForEditing";
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
