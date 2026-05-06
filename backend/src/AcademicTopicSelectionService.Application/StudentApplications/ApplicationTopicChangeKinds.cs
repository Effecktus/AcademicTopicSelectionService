namespace AcademicTopicSelectionService.Application.StudentApplications;

/// <summary>
/// Коды типов изменений темы заявки в истории.
/// </summary>
public static class ApplicationTopicChangeKinds
{
    public const string TopicTitle = "TopicTitle";

    public const string TopicDescription = "TopicDescription";

    public static string GetDisplayName(string changeKind) => changeKind switch
    {
        TopicTitle => "Название темы",
        TopicDescription => "Описание темы",
        _ => changeKind,
    };
}
