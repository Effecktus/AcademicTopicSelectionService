using AcademicTopicSelectionService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AcademicTopicSelectionService.Infrastructure.Data;

/// <summary>
/// Обязательное наполнение системы: справочники и учётная запись администратора.
/// Попадает в базу миграциями во всех окружениях. Тестовые данные сюда не добавляются —
/// они загружаются отдельной командой (см. README, раздел про миграции).
/// </summary>
public partial class ApplicationDbContext
{
    /// <summary>
    /// Дата создания строк обязательного наполнения. Фиксирована, потому что <c>HasData</c>
    /// требует детерминированных значений: иначе каждая новая миграция пересоздавала бы строки.
    /// </summary>
    private static readonly DateTime SeedCreatedAt = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<UserRole>().HasData(
            Reference(SystemSeed.UserRoles.Student, "Student", "Студент"),
            Reference(SystemSeed.UserRoles.Teacher, "Teacher", "Преподаватель"),
            Reference(SystemSeed.UserRoles.DepartmentHead, "DepartmentHead", "Заведующий кафедрой"),
            Reference(SystemSeed.UserRoles.Admin, "Admin", "Администратор"));

        modelBuilder.Entity<ApplicationStatus>().HasData(
            Reference(SystemSeed.ApplicationStatuses.OnEditing, "OnEditing", "На редактировании"),
            Reference(SystemSeed.ApplicationStatuses.Pending, "Pending", "Ожидает ответа преподавателя"),
            Reference(SystemSeed.ApplicationStatuses.ApprovedBySupervisor, "ApprovedBySupervisor", "Одобрено преподавателем"),
            Reference(SystemSeed.ApplicationStatuses.RejectedBySupervisor, "RejectedBySupervisor", "Отклонено преподавателем"),
            Reference(SystemSeed.ApplicationStatuses.PendingDepartmentHead, "PendingDepartmentHead", "Отправлено заведующему кафедрой"),
            Reference(SystemSeed.ApplicationStatuses.ApprovedByDepartmentHead, "ApprovedByDepartmentHead", "Утверждено заведующим кафедрой"),
            Reference(SystemSeed.ApplicationStatuses.RejectedByDepartmentHead, "RejectedByDepartmentHead", "Отклонено заведующим кафедрой"),
            Reference(SystemSeed.ApplicationStatuses.Cancelled, "Cancelled", "Отменено студентом"));

        modelBuilder.Entity<ApplicationActionStatus>().HasData(
            Reference(SystemSeed.ApplicationActionStatuses.Pending, "Pending", "На согласовании"),
            Reference(SystemSeed.ApplicationActionStatuses.Approved, "Approved", "Согласовано"),
            Reference(SystemSeed.ApplicationActionStatuses.Rejected, "Rejected", "Отклонено"),
            Reference(SystemSeed.ApplicationActionStatuses.ReturnedForEditing, "ReturnedForEditing", "Возвращено на редактирование"),
            Reference(SystemSeed.ApplicationActionStatuses.Cancelled, "Cancelled", "Отменено"));

        modelBuilder.Entity<TopicStatus>().HasData(
            Reference(SystemSeed.TopicStatuses.Active, "Active", "Активна"),
            Reference(SystemSeed.TopicStatuses.Inactive, "Inactive", "Неактивна"));

        modelBuilder.Entity<NotificationType>().HasData(
            Reference(SystemSeed.NotificationTypes.ApplicationStatusChanged, "ApplicationStatusChanged", "Статус заявки изменен"),
            Reference(SystemSeed.NotificationTypes.NewMessage, "NewMessage", "Новое сообщение"),
            Reference(SystemSeed.NotificationTypes.TopicApproved, "TopicApproved", "Тема утверждена"),
            Reference(SystemSeed.NotificationTypes.TopicRejected, "TopicRejected", "Тема отклонена"),
            Reference(SystemSeed.NotificationTypes.SupervisorRequestStatusChanged, "SupervisorRequestStatusChanged", "Статус запроса на научрука изменен"),
            Reference(SystemSeed.NotificationTypes.SupervisorRequestCreated, "SupervisorRequestCreated", "Новый запрос на научное руководство"),
            Reference(SystemSeed.NotificationTypes.ApplicationSubmittedToSupervisor, "ApplicationSubmittedToSupervisor", "Новая заявка на рассмотрение научруком"),
            Reference(SystemSeed.NotificationTypes.ApplicationSubmittedToDepartmentHead, "ApplicationSubmittedToDepartmentHead", "Заявка передана на рассмотрение кафедры"),
            Reference(SystemSeed.NotificationTypes.SupervisorDecisionByDepartmentHead, "SupervisorDecisionByDepartmentHead", "Решение заведующего по заявке студента"),
            Reference(SystemSeed.NotificationTypes.GraduateWorkUploaded, "GraduateWorkUploaded", "ВКР загружена в архив"));

        modelBuilder.Entity<AcademicDegree>().HasData(
            Reference(SystemSeed.AcademicDegrees.None, "None", "Без степени", null),
            Reference(SystemSeed.AcademicDegrees.DoctorOfBiologicalSciences, "DoctorOfBiologicalSciences", "доктор биологических наук", "д-р биол. наук"),
            Reference(SystemSeed.AcademicDegrees.DoctorOfVeterinarySciences, "DoctorOfVeterinarySciences", "доктор ветеринарных наук", "д-р ветеринар. наук"),
            Reference(SystemSeed.AcademicDegrees.DoctorOfMilitarySciences, "DoctorOfMilitarySciences", "доктор военных наук", "д-р воен. наук"),
            Reference(SystemSeed.AcademicDegrees.DoctorOfGeographicalSciences, "DoctorOfGeographicalSciences", "доктор географических наук", "д-р геогр. наук"),
            Reference(SystemSeed.AcademicDegrees.DoctorOfArtHistory, "DoctorOfArtHistory", "доктор искусствоведения", "д-р искусствоведения"),
            Reference(SystemSeed.AcademicDegrees.DoctorOfHistoricalSciences, "DoctorOfHistoricalSciences", "доктор исторических наук", "д-р ист. наук"),
            Reference(SystemSeed.AcademicDegrees.DoctorOfCulturalStudies, "DoctorOfCulturalStudies", "доктор культурологии", "д-р культурологии"),
            Reference(SystemSeed.AcademicDegrees.DoctorOfMedicalSciences, "DoctorOfMedicalSciences", "доктор медицинских наук", "д-р мед. наук"),
            Reference(SystemSeed.AcademicDegrees.DoctorOfPedagogicalSciences, "DoctorOfPedagogicalSciences", "доктор педагогических наук", "д-р пед. наук"),
            Reference(SystemSeed.AcademicDegrees.DoctorOfPoliticalSciences, "DoctorOfPoliticalSciences", "доктор политических наук", "д-р полит. наук"),
            Reference(SystemSeed.AcademicDegrees.DoctorOfPsychologicalSciences, "DoctorOfPsychologicalSciences", "доктор психологических наук", "д-р психол. наук"),
            Reference(SystemSeed.AcademicDegrees.DoctorOfAgriculturalSciences, "DoctorOfAgriculturalSciences", "доктор сельскохозяйственных наук", "д-р с.-х. наук"),
            Reference(SystemSeed.AcademicDegrees.DoctorOfSociologicalSciences, "DoctorOfSociologicalSciences", "доктор социологических наук", "д-р социол. наук"),
            Reference(SystemSeed.AcademicDegrees.DoctorOfTechnicalSciences, "DoctorOfTechnicalSciences", "доктор технических наук", "д-р техн. наук"),
            Reference(SystemSeed.AcademicDegrees.DoctorOfPhysicsAndMathematics, "DoctorOfPhysicsAndMathematics", "доктор физико-математических наук", "д-р физ.-мат. наук"),
            Reference(SystemSeed.AcademicDegrees.DoctorOfPhilologicalSciences, "DoctorOfPhilologicalSciences", "доктор филологических наук", "д-р филол. наук"),
            Reference(SystemSeed.AcademicDegrees.DoctorOfPhilosophicalSciences, "DoctorOfPhilosophicalSciences", "доктор философских наук", "д-р филос. наук"),
            Reference(SystemSeed.AcademicDegrees.DoctorOfChemicalSciences, "DoctorOfChemicalSciences", "доктор химических наук", "д-р хим. наук"),
            Reference(SystemSeed.AcademicDegrees.DoctorOfEconomicSciences, "DoctorOfEconomicSciences", "доктор экономических наук", "д-р экон. наук"),
            Reference(SystemSeed.AcademicDegrees.DoctorOfLegalSciences, "DoctorOfLegalSciences", "доктор юридических наук", "д-р юрид. наук"),
            Reference(SystemSeed.AcademicDegrees.CandidateOfBiologicalSciences, "CandidateOfBiologicalSciences", "кандидат биологических наук", "канд. биол. наук"),
            Reference(SystemSeed.AcademicDegrees.CandidateOfVeterinarySciences, "CandidateOfVeterinarySciences", "кандидат ветеринарных наук", "канд. ветеринар. наук"),
            Reference(SystemSeed.AcademicDegrees.CandidateOfMilitarySciences, "CandidateOfMilitarySciences", "кандидат военных наук", "канд. воен. наук"),
            Reference(SystemSeed.AcademicDegrees.CandidateOfGeographicalSciences, "CandidateOfGeographicalSciences", "кандидат географических наук", "канд. геогр. наук"),
            Reference(SystemSeed.AcademicDegrees.CandidateOfArtHistory, "CandidateOfArtHistory", "кандидат искусствоведения", "канд. искусствоведения"),
            Reference(SystemSeed.AcademicDegrees.CandidateOfHistoricalSciences, "CandidateOfHistoricalSciences", "кандидат исторических наук", "канд. ист. наук"),
            Reference(SystemSeed.AcademicDegrees.CandidateOfCulturalStudies, "CandidateOfCulturalStudies", "кандидат культурологии", "канд. культурологии"),
            Reference(SystemSeed.AcademicDegrees.CandidateOfMedicalSciences, "CandidateOfMedicalSciences", "кандидат медицинских наук", "канд. мед. наук"),
            Reference(SystemSeed.AcademicDegrees.CandidateOfPedagogicalSciences, "CandidateOfPedagogicalSciences", "кандидат педагогических наук", "канд. пед. наук"),
            Reference(SystemSeed.AcademicDegrees.CandidateOfPoliticalSciences, "CandidateOfPoliticalSciences", "кандидат политических наук", "канд. полит. наук"),
            Reference(SystemSeed.AcademicDegrees.CandidateOfPsychologicalSciences, "CandidateOfPsychologicalSciences", "кандидат психологических наук", "канд. психол. наук"),
            Reference(SystemSeed.AcademicDegrees.CandidateOfAgriculturalSciences, "CandidateOfAgriculturalSciences", "кандидат сельскохозяйственных наук", "канд. с.-х. наук"),
            Reference(SystemSeed.AcademicDegrees.CandidateOfSociologicalSciences, "CandidateOfSociologicalSciences", "кандидат социологических наук", "канд. социол. наук"),
            Reference(SystemSeed.AcademicDegrees.CandidateOfTechnicalSciences, "CandidateOfTechnicalSciences", "кандидат технических наук", "канд. техн. наук"),
            Reference(SystemSeed.AcademicDegrees.CandidateOfPhysicsAndMathematics, "CandidateOfPhysicsAndMathematics", "кандидат физико-математических наук", "канд. физ.-мат. наук"),
            Reference(SystemSeed.AcademicDegrees.CandidateOfPhilologicalSciences, "CandidateOfPhilologicalSciences", "кандидат филологических наук", "канд. филол. наук"),
            Reference(SystemSeed.AcademicDegrees.CandidateOfPhilosophicalSciences, "CandidateOfPhilosophicalSciences", "кандидат философских наук", "канд. филос. наук"),
            Reference(SystemSeed.AcademicDegrees.CandidateOfChemicalSciences, "CandidateOfChemicalSciences", "кандидат химических наук", "канд. хим. наук"),
            Reference(SystemSeed.AcademicDegrees.CandidateOfEconomicSciences, "CandidateOfEconomicSciences", "кандидат экономических наук", "канд. экон. наук"),
            Reference(SystemSeed.AcademicDegrees.CandidateOfLegalSciences, "CandidateOfLegalSciences", "кандидат юридических наук", "канд. юрид. наук"));

        modelBuilder.Entity<AcademicTitle>().HasData(
            Reference(SystemSeed.AcademicTitles.None, "None", "Без звания"),
            Reference(SystemSeed.AcademicTitles.AssociateProfessor, "AssociateProfessor", "Доцент"),
            Reference(SystemSeed.AcademicTitles.Professor, "Professor", "Профессор"));

        modelBuilder.Entity<Position>().HasData(
            Reference(SystemSeed.Positions.Assistant, "Assistant", "Ассистент"),
            Reference(SystemSeed.Positions.SeniorLecturer, "SeniorLecturer", "Старший преподаватель"),
            Reference(SystemSeed.Positions.AssociateProfessor, "AssociateProfessor", "Доцент"),
            Reference(SystemSeed.Positions.Professor, "Professor", "Профессор"),
            Reference(SystemSeed.Positions.DepartmentHead, "DepartmentHead", "Заведующий кафедрой"));

        modelBuilder.Entity<TopicCreatorType>().HasData(
            Reference(SystemSeed.TopicCreatorTypes.Teacher, "Teacher", "Научный руководитель"),
            Reference(SystemSeed.TopicCreatorTypes.Student, "Student", "Студент"));

        modelBuilder.Entity<GraduateWorkStatus>().HasData(
            Reference(SystemSeed.GraduateWorkStatuses.Draft, "Draft", "Черновик"),
            Reference(SystemSeed.GraduateWorkStatuses.Completed, "Completed", "Заполнено"));

        // Пароль задаётся при старте приложения из секрета (Admin:PasswordFile), см. DatabaseInitializer.
        modelBuilder.Entity<User>().HasData(new
        {
            Id = SystemSeed.Users.Admin,
            Email = "z_admin@example.com",
            PasswordHash = SystemSeed.AdminPasswordPlaceholder,
            FirstName = "Николай",
            LastName = "Системов",
            MiddleName = "Администраторович",
            RoleId = SystemSeed.UserRoles.Admin,
            IsActive = true,
            CreatedAt = SeedCreatedAt
        });
    }

    private static object Reference(Guid id, string codeName, string displayName) =>
        new { Id = id, CodeName = codeName, DisplayName = displayName, CreatedAt = SeedCreatedAt };

    private static object Reference(Guid id, string codeName, string displayName, string? shortName) =>
        new { Id = id, CodeName = codeName, DisplayName = displayName, ShortName = shortName, CreatedAt = SeedCreatedAt };
}
