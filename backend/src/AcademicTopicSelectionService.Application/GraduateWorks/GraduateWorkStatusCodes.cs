namespace AcademicTopicSelectionService.Application.GraduateWorks;

/// <summary>
/// Системные коды статусов выпускных квалификационных работ.
/// </summary>
public static class GraduateWorkStatusCodes
{
    /// <summary>Черновик — создан автоматически при утверждении заявки завкафедрой; оценка и файлы не заполнены.</summary>
    public const string Draft = "Draft";

    /// <summary>Заполнено — оценка, комиссия и файл ВКР загружены администратором после защиты.</summary>
    public const string Completed = "Completed";
}
