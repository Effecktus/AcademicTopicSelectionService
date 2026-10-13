using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace AcademicTopicSelectionService.Migrations.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:citext", ",,")
                .Annotation("Npgsql:PostgresExtension:pgcrypto", ",,");

            migrationBuilder.CreateTable(
                name: "AcademicDegrees",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()", comment: "Уникальный идентификатор ученой степени"),
                    CodeName = table.Column<string>(type: "citext", nullable: false, comment: "Системное значение степени (для кода), регистронезависимо"),
                    DisplayName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false, comment: "Отображаемое значение степени (для пользовательского интерфейса)"),
                    ShortName = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true, comment: "Сокращенное название степени (для отображения в кратких формах)"),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP", comment: "Дата и время создания записи о степени"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "Дата и время последнего обновления записи о степени")
                },
                constraints: table =>
                {
                    table.PrimaryKey("AcademicDegrees_pkey", x => x.Id);
                    table.CheckConstraint("CK_AcademicDegrees_CodeName_NotEmpty", "length(btrim(\"CodeName\"::text)) > 0");
                    table.CheckConstraint("CK_AcademicDegrees_DisplayName_NotEmpty", "length(btrim(\"DisplayName\")) > 0");
                    table.CheckConstraint("CK_AcademicDegrees_ShortName_NotEmpty", "\"ShortName\" IS NULL OR length(btrim(\"ShortName\")) > 0");
                },
                comment: "Справочник ученых степеней. Содержит системные, отображаемые и сокращенные названия степеней.");

            migrationBuilder.CreateTable(
                name: "AcademicTitles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()", comment: "Уникальный идентификатор ученого звания"),
                    CodeName = table.Column<string>(type: "citext", nullable: false, comment: "Системное значение звания (для кода), регистронезависимо"),
                    DisplayName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false, comment: "Отображаемое значение звания (для пользовательского интерфейса)"),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP", comment: "Дата и время создания записи о звании"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "Дата и время последнего обновления записи о звании")
                },
                constraints: table =>
                {
                    table.PrimaryKey("AcademicTitles_pkey", x => x.Id);
                    table.CheckConstraint("CK_AcademicTitles_CodeName_NotEmpty", "length(btrim(\"CodeName\"::text)) > 0");
                    table.CheckConstraint("CK_AcademicTitles_DisplayName_NotEmpty", "length(btrim(\"DisplayName\")) > 0");
                },
                comment: "Справочник ученых званий. Содержит системные и отображаемые названия званий.");

            migrationBuilder.CreateTable(
                name: "ApplicationActionStatuses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()", comment: "Уникальный идентификатор статуса действия"),
                    CodeName = table.Column<string>(type: "citext", nullable: false, comment: "Системное значение статуса (для кода), регистронезависимо"),
                    DisplayName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false, comment: "Отображаемое значение статуса (для пользовательского интерфейса)"),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP", comment: "Дата и время создания записи о статусе"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "Дата и время последнего обновления записи о статусе")
                },
                constraints: table =>
                {
                    table.PrimaryKey("ApplicationActionStatuses_pkey", x => x.Id);
                    table.CheckConstraint("CK_ApplicationActionStatuses_CodeName_NotEmpty", "length(btrim(\"CodeName\"::text)) > 0");
                    table.CheckConstraint("CK_ApplicationActionStatuses_DisplayName_NotEmpty", "length(btrim(\"DisplayName\")) > 0");
                },
                comment: "Справочник статусов действий по заявкам на темы ВКР. Определяет результат рассмотрения: на согласовании, согласовано, отклонено или отменено.");

            migrationBuilder.CreateTable(
                name: "ApplicationStatuses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()", comment: "Уникальный идентификатор статуса заявки"),
                    CodeName = table.Column<string>(type: "citext", nullable: false, comment: "Системное значение статуса (для кода), регистронезависимо"),
                    DisplayName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false, comment: "Отображаемое значение статуса (для пользовательского интерфейса)"),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP", comment: "Дата и время создания записи о статусе"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "Дата и время последнего обновления записи о статусе")
                },
                constraints: table =>
                {
                    table.PrimaryKey("ApplicationStatuses_pkey", x => x.Id);
                    table.CheckConstraint("CK_ApplicationStatuses_CodeName_NotEmpty", "length(btrim(\"CodeName\"::text)) > 0");
                    table.CheckConstraint("CK_ApplicationStatuses_DisplayName_NotEmpty", "length(btrim(\"DisplayName\")) > 0");
                },
                comment: "Справочник статусов заявок на темы ВКР. Содержит системные и отображаемые названия статусов.");

            migrationBuilder.CreateTable(
                name: "GraduateWorkStatuses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()", comment: "Уникальный идентификатор статуса ВКР"),
                    CodeName = table.Column<string>(type: "citext", nullable: false, comment: "Системное значение статуса (для кода), регистронезависимо"),
                    DisplayName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false, comment: "Отображаемое значение статуса (для пользовательского интерфейса)"),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP", comment: "Дата и время создания записи о статусе"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "Дата и время последнего обновления записи о статусе")
                },
                constraints: table =>
                {
                    table.PrimaryKey("GraduateWorkStatuses_pkey", x => x.Id);
                    table.CheckConstraint("CK_GraduateWorkStatuses_CodeName_NotEmpty", "length(btrim(\"CodeName\"::text)) > 0");
                    table.CheckConstraint("CK_GraduateWorkStatuses_DisplayName_NotEmpty", "length(btrim(\"DisplayName\")) > 0");
                },
                comment: "Справочник статусов выпускных квалификационных работ.");

            migrationBuilder.CreateTable(
                name: "NotificationTypes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()", comment: "Уникальный идентификатор типа уведомления"),
                    CodeName = table.Column<string>(type: "citext", nullable: false, comment: "Системное значение типа (для кода), регистронезависимо"),
                    DisplayName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false, comment: "Отображаемое значение типа (для пользовательского интерфейса)"),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP", comment: "Дата и время создания записи о типе уведомления"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "Дата и время последнего обновления записи о типе уведомления")
                },
                constraints: table =>
                {
                    table.PrimaryKey("NotificationTypes_pkey", x => x.Id);
                    table.CheckConstraint("CK_NotificationTypes_CodeName_NotEmpty", "length(btrim(\"CodeName\"::text)) > 0");
                    table.CheckConstraint("CK_NotificationTypes_DisplayName_NotEmpty", "length(btrim(\"DisplayName\")) > 0");
                },
                comment: "Справочник типов уведомлений системы. Содержит системные и отображаемые названия типов.");

            migrationBuilder.CreateTable(
                name: "Positions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()", comment: "Уникальный идентификатор должности"),
                    CodeName = table.Column<string>(type: "citext", nullable: false, comment: "Системное значение должности (для кода), регистронезависимо"),
                    DisplayName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false, comment: "Отображаемое значение должности (для пользовательского интерфейса)"),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP", comment: "Дата и время создания записи о должности"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "Дата и время последнего обновления записи о должности")
                },
                constraints: table =>
                {
                    table.PrimaryKey("Positions_pkey", x => x.Id);
                    table.CheckConstraint("CK_Positions_CodeName_NotEmpty", "length(btrim(\"CodeName\"::text)) > 0");
                    table.CheckConstraint("CK_Positions_DisplayName_NotEmpty", "length(btrim(\"DisplayName\")) > 0");
                },
                comment: "Справочник должностей преподавателей. Содержит системные и отображаемые названия должностей.");

            migrationBuilder.CreateTable(
                name: "StudyGroups",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()", comment: "Уникальный идентификатор группы"),
                    CodeName = table.Column<int>(type: "integer", nullable: false, comment: "Номер учебной группы (формат: XXXX, где первая цифра - факультет, вторая - курс, последние две - номер группы, например: 4411)"),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP", comment: "Дата и время создания записи о группе"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "Дата и время последнего обновления записи о группе")
                },
                constraints: table =>
                {
                    table.PrimaryKey("StudyGroups_pkey", x => x.Id);
                    table.CheckConstraint("CK_StudyGroups_CodeName_Range", "\"CodeName\" BETWEEN 1000 AND 9999");
                },
                comment: "Справочник учебных групп. Содержит номера групп в формате XXXX (факультет, курс, номер).");

            migrationBuilder.CreateTable(
                name: "TopicCreatorTypes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()", comment: "Уникальный идентификатор типа создателя темы"),
                    CodeName = table.Column<string>(type: "citext", nullable: false, comment: "Системное значение типа (для кода), регистронезависимо"),
                    DisplayName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false, comment: "Отображаемое значение типа (для пользовательского интерфейса)"),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP", comment: "Дата и время создания записи"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "Дата и время последнего обновления записи")
                },
                constraints: table =>
                {
                    table.PrimaryKey("TopicCreatorTypes_pkey", x => x.Id);
                    table.CheckConstraint("CK_TopicCreatorTypes_CodeName_NotEmpty", "length(btrim(\"CodeName\"::text)) > 0");
                    table.CheckConstraint("CK_TopicCreatorTypes_DisplayName_NotEmpty", "length(btrim(\"DisplayName\")) > 0");
                },
                comment: "Справочник типов пользователей, создающих темы ВКР. Определяет, кем была предложена тема: научным руководителем или студентом.");

            migrationBuilder.CreateTable(
                name: "TopicStatuses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()", comment: "Уникальный идентификатор статуса темы"),
                    CodeName = table.Column<string>(type: "citext", nullable: false, comment: "Системное значение статуса (для кода), регистронезависимо"),
                    DisplayName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false, comment: "Отображаемое значение статуса (для пользовательского интерфейса)"),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP", comment: "Дата и время создания записи о статусе"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "Дата и время последнего обновления записи о статусе")
                },
                constraints: table =>
                {
                    table.PrimaryKey("TopicStatuses_pkey", x => x.Id);
                    table.CheckConstraint("CK_TopicStatuses_CodeName_NotEmpty", "length(btrim(\"CodeName\"::text)) > 0");
                    table.CheckConstraint("CK_TopicStatuses_DisplayName_NotEmpty", "length(btrim(\"DisplayName\")) > 0");
                },
                comment: "Справочник статусов тем ВКР. Содержит системные и отображаемые названия статусов.");

            migrationBuilder.CreateTable(
                name: "UserRoles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()", comment: "Уникальный идентификатор роли пользователя"),
                    CodeName = table.Column<string>(type: "citext", nullable: false, comment: "Системное значение роли (для кода), регистронезависимо"),
                    DisplayName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false, comment: "Отображаемое значение роли (для пользовательского интерфейса)"),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP", comment: "Дата и время создания записи о роли"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "Дата и время последнего обновления записи о роли")
                },
                constraints: table =>
                {
                    table.PrimaryKey("UserRoles_pkey", x => x.Id);
                    table.CheckConstraint("CK_UserRoles_CodeName_NotEmpty", "length(btrim(\"CodeName\"::text)) > 0");
                    table.CheckConstraint("CK_UserRoles_DisplayName_NotEmpty", "length(btrim(\"DisplayName\")) > 0");
                },
                comment: "Справочник ролей пользователей системы. Содержит системные и отображаемые названия ролей.");

            migrationBuilder.CreateTable(
                name: "ApplicationActions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()", comment: "Уникальный идентификатор действия"),
                    ApplicationId = table.Column<Guid>(type: "uuid", nullable: false, comment: "Идентификатор заявки, к которой относится действие (внешний ключ к таблице StudentApplications)"),
                    ResponsibleId = table.Column<Guid>(type: "uuid", nullable: false, comment: "Идентификатор пользователя, ответственного за данный этап согласования (внешний ключ к таблице Users)"),
                    StatusId = table.Column<Guid>(type: "uuid", nullable: false, comment: "Идентификатор статуса действия: На согласовании / Согласовано / Отклонено / Отменено (внешний ключ к таблице ApplicationActionStatuses)"),
                    Comment = table.Column<string>(type: "text", nullable: true, comment: "Комментарий ответственного (причина отклонения или произвольный комментарий при согласовании)"),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP", comment: "Дата и время создания действия"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "Дата и время последнего обновления действия")
                },
                constraints: table =>
                {
                    table.PrimaryKey("ApplicationActions_pkey", x => x.Id);
                    table.CheckConstraint("CK_ApplicationActions_Comment_NotEmpty", "\"Comment\" IS NULL OR length(btrim(\"Comment\")) > 0");
                    table.ForeignKey(
                        name: "FK_ApplicationActions_ApplicationActionStatuses",
                        column: x => x.StatusId,
                        principalTable: "ApplicationActionStatuses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                },
                comment: "Таблица действий по заявкам студентов. Хранит историю согласований: каждое действие описывает один этап рассмотрения заявки (преподавателем или заведующим кафедрой). Новое действие создаётся при переходе заявки на следующий этап.");

            migrationBuilder.CreateTable(
                name: "ApplicationTopicChangeHistories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()", comment: "Уникальный идентификатор записи истории"),
                    ApplicationId = table.Column<Guid>(type: "uuid", nullable: false, comment: "Идентификатор заявки"),
                    ChangedByUserId = table.Column<Guid>(type: "uuid", nullable: false, comment: "Пользователь, внёсший изменение"),
                    ChangeKind = table.Column<string>(type: "citext", nullable: false, comment: "TopicTitle или TopicDescription"),
                    NewValue = table.Column<string>(type: "text", nullable: true, comment: "Значение после изменения"),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP", comment: "Момент записи"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "Момент последнего обновления записи")
                },
                constraints: table =>
                {
                    table.PrimaryKey("ApplicationTopicChangeHistories_pkey", x => x.Id);
                    table.CheckConstraint("CK_ApplicationTopicChangeHistories_ChangeKind", "\"ChangeKind\" IN ('TopicTitle', 'TopicDescription')");
                },
                comment: "История изменений заявки: правки названия и описания темы при доработке (OnEditing).");

            migrationBuilder.CreateTable(
                name: "ChatMessages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()", comment: "Уникальный идентификатор сообщения"),
                    ApplicationId = table.Column<Guid>(type: "uuid", nullable: false, comment: "Идентификатор заявки, к которой относится сообщение (внешний ключ к таблице Applications)"),
                    SenderId = table.Column<Guid>(type: "uuid", nullable: false, comment: "Идентификатор отправителя сообщения (внешний ключ к таблице Users)"),
                    Content = table.Column<string>(type: "text", nullable: false, comment: "Текст сообщения"),
                    SentAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP", comment: "Дата и время отправки сообщения"),
                    ReadAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "Дата и время прочтения сообщения получателем (NULL, если сообщение не прочитано)")
                },
                constraints: table =>
                {
                    table.PrimaryKey("ChatMessages_pkey", x => x.Id);
                    table.CheckConstraint("CK_ChatMessages_Content_NotEmpty", "length(btrim(\"Content\")) > 0");
                    table.CheckConstraint("CK_ChatMessages_ReadAt_AfterSentAt", "\"ReadAt\" IS NULL OR \"ReadAt\" >= \"SentAt\"");
                },
                comment: "Таблица сообщений чата между студентами и преподавателями по заявкам на темы ВКР. Содержит историю переписки и информацию о прочтении сообщений.");

            migrationBuilder.CreateTable(
                name: "Departments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()", comment: "Уникальный идентификатор кафедры"),
                    CodeName = table.Column<string>(type: "citext", nullable: false, comment: "Системное значение кафедры (для кода), регистронезависимо"),
                    DisplayName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false, comment: "Отображаемое значение кафедры (для пользовательского интерфейса)"),
                    HeadId = table.Column<Guid>(type: "uuid", nullable: true, comment: "Идентификатор заведующего кафедрой (внешний ключ к таблице Users)"),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP", comment: "Дата и время создания записи о кафедре"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "Дата и время последнего обновления записи о кафедре")
                },
                constraints: table =>
                {
                    table.PrimaryKey("Departments_pkey", x => x.Id);
                    table.CheckConstraint("CK_Departments_CodeName_NotEmpty", "length(btrim(\"CodeName\"::text)) > 0");
                    table.CheckConstraint("CK_Departments_DisplayName_NotEmpty", "length(btrim(\"DisplayName\")) > 0");
                },
                comment: "Таблица кафедр. Содержит информацию о кафедрах и их заведующих.");

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()", comment: "Уникальный идентификатор пользователя"),
                    Email = table.Column<string>(type: "citext", nullable: false, comment: "Email пользователя (уникальный, регистронезависимый)"),
                    PasswordHash = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false, comment: "Хеш пароля пользователя"),
                    FirstName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false, comment: "Имя пользователя"),
                    LastName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false, comment: "Фамилия пользователя"),
                    MiddleName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true, comment: "Отчество пользователя"),
                    RoleId = table.Column<Guid>(type: "uuid", nullable: false, comment: "Идентификатор роли пользователя (внешний ключ к таблице UserRoles)"),
                    DepartmentId = table.Column<Guid>(type: "uuid", nullable: true, comment: "Идентификатор кафедры пользователя (внешний ключ к таблице Departments)"),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP", comment: "Дата и время создания записи о пользователе"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "Дата и время последнего обновления записи о пользователе"),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true, comment: "Флаг активности пользователя (true - активен, false - деактивирован)")
                },
                constraints: table =>
                {
                    table.PrimaryKey("Users_pkey", x => x.Id);
                    table.CheckConstraint("CK_Users_Email_NoSpaces", "position(' ' in \"Email\"::text) = 0");
                    table.CheckConstraint("CK_Users_Email_NotEmpty", "length(btrim(\"Email\"::text)) > 0");
                    table.CheckConstraint("CK_Users_Email_Trimmed", "\"Email\"::text = btrim(\"Email\"::text)");
                    table.CheckConstraint("CK_Users_FirstName_NotEmpty", "length(btrim(\"FirstName\")) > 0");
                    table.CheckConstraint("CK_Users_LastName_NotEmpty", "length(btrim(\"LastName\")) > 0");
                    table.CheckConstraint("CK_Users_PasswordHash_NotEmpty", "length(btrim(\"PasswordHash\")) > 0");
                    table.ForeignKey(
                        name: "FK_Users_Departments",
                        column: x => x.DepartmentId,
                        principalTable: "Departments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Users_UserRoles",
                        column: x => x.RoleId,
                        principalTable: "UserRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                },
                comment: "Таблица пользователей системы. Содержит основную информацию о пользователях: учетные данные, персональные данные, роль и принадлежность к кафедре.");

            migrationBuilder.CreateTable(
                name: "Notifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()", comment: "Уникальный идентификатор уведомления"),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false, comment: "Идентификатор пользователя-получателя уведомления (внешний ключ к таблице Users)"),
                    TypeId = table.Column<Guid>(type: "uuid", nullable: false, comment: "Идентификатор типа уведомления (внешний ключ к таблице NotificationTypes)"),
                    Title = table.Column<string>(type: "text", nullable: false, comment: "Заголовок уведомления"),
                    Content = table.Column<string>(type: "text", nullable: false, comment: "Содержимое уведомления (полный текст)"),
                    RelatedEntityType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true, comment: "Тип связанной сущности для точной навигации в UI (Application, SupervisorRequest, GraduateWork)"),
                    RelatedEntityId = table.Column<Guid>(type: "uuid", nullable: true, comment: "Идентификатор связанной сущности для точной навигации в UI"),
                    IsRead = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false, comment: "Флаг прочтения уведомления (true - прочитано, false - не прочитано)"),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP", comment: "Дата и время создания уведомления")
                },
                constraints: table =>
                {
                    table.PrimaryKey("Notifications_pkey", x => x.Id);
                    table.CheckConstraint("CK_Notifications_Content_NotEmpty", "length(btrim(\"Content\")) > 0");
                    table.CheckConstraint("CK_Notifications_Title_NotEmpty", "length(btrim(\"Title\")) > 0");
                    table.ForeignKey(
                        name: "FK_Notifications_NotificationTypes",
                        column: x => x.TypeId,
                        principalTable: "NotificationTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Notifications_Users",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                },
                comment: "Таблица уведомлений пользователей системы. Содержит информацию о различных типах уведомлений и их статусе прочтения.");

            migrationBuilder.CreateTable(
                name: "Students",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()", comment: "Уникальный идентификатор студента"),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false, comment: "Идентификатор пользователя-студента (внешний ключ к таблице Users)"),
                    GroupId = table.Column<Guid>(type: "uuid", nullable: false, comment: "Идентификатор учебной группы студента (внешний ключ к таблице StudyGroups)"),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP", comment: "Дата и время создания записи о студенте"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "Дата и время последнего обновления записи о студенте")
                },
                constraints: table =>
                {
                    table.PrimaryKey("Students_pkey", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Students_StudyGroups",
                        column: x => x.GroupId,
                        principalTable: "StudyGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Students_Users",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                },
                comment: "Таблица студентов. Содержит дополнительную информацию о студентах: принадлежность к учебной группе.");

            migrationBuilder.CreateTable(
                name: "Teachers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()", comment: "Уникальный идентификатор преподавателя"),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false, comment: "Идентификатор пользователя-преподавателя (внешний ключ к таблице Users)"),
                    MaxStudentsLimit = table.Column<int>(type: "integer", nullable: true, comment: "Максимальное количество студентов, которых может взять преподаватель для руководства ВКР"),
                    GraduateWorksCount = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    AcademicDegreeId = table.Column<Guid>(type: "uuid", nullable: false, comment: "Идентификатор ученой степени преподавателя (внешний ключ к таблице AcademicDegrees)"),
                    AcademicTitleId = table.Column<Guid>(type: "uuid", nullable: false, comment: "Идентификатор ученого звания преподавателя (внешний ключ к таблице AcademicTitles)"),
                    PositionId = table.Column<Guid>(type: "uuid", nullable: false, comment: "Идентификатор должности преподавателя (внешний ключ к таблице Positions)"),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP", comment: "Дата и время создания записи о преподавателе"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "Дата и время последнего обновления записи о преподавателе")
                },
                constraints: table =>
                {
                    table.PrimaryKey("Teachers_pkey", x => x.Id);
                    table.CheckConstraint("CK_Teachers_MaxStudentsLimit_Positive", "\"MaxStudentsLimit\" IS NULL OR \"MaxStudentsLimit\" > 0");
                    table.ForeignKey(
                        name: "FK_Teachers_AcademicDegrees",
                        column: x => x.AcademicDegreeId,
                        principalTable: "AcademicDegrees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Teachers_AcademicTitles",
                        column: x => x.AcademicTitleId,
                        principalTable: "AcademicTitles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Teachers_Positions",
                        column: x => x.PositionId,
                        principalTable: "Positions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Teachers_Users",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                },
                comment: "Таблица преподавателей. Содержит дополнительную информацию о преподавателях: академические данные и лимит студентов.");

            migrationBuilder.CreateTable(
                name: "Topics",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()", comment: "Уникальный идентификатор темы ВКР"),
                    Title = table.Column<string>(type: "citext", nullable: false, comment: "Название темы выпускной квалификационной работы"),
                    Description = table.Column<string>(type: "text", nullable: true, comment: "Подробное описание темы ВКР, требования и особенности"),
                    CreatorTypeId = table.Column<Guid>(type: "uuid", nullable: false, comment: "Тип пользователя, создавшего тему (внешний ключ к таблице TopicCreatorTypes)"),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: false, comment: "Пользователь, создавший тему (внешний ключ к таблице Users)"),
                    StatusId = table.Column<Guid>(type: "uuid", nullable: false, comment: "Идентификатор статуса темы (внешний ключ к таблице TopicStatuses)"),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP", comment: "Дата и время создания записи о теме"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "Дата и время последнего обновления записи о теме")
                },
                constraints: table =>
                {
                    table.PrimaryKey("Topics_pkey", x => x.Id);
                    table.CheckConstraint("CK_Topics_Title_NotEmpty", "length(btrim(\"Title\"::text)) > 0");
                    table.ForeignKey(
                        name: "FK_Topics_TopicCreatorTypes",
                        column: x => x.CreatorTypeId,
                        principalTable: "TopicCreatorTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Topics_TopicStatuses",
                        column: x => x.StatusId,
                        principalTable: "TopicStatuses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Topics_Users",
                        column: x => x.CreatedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                },
                comment: "Таблица тем выпускных квалификационных работ (ВКР). Содержит темы, предложенные как научными руководителями, так и студентами.");

            migrationBuilder.CreateTable(
                name: "SupervisorRequests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    StudentId = table.Column<Guid>(type: "uuid", nullable: false),
                    TeacherUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    StatusId = table.Column<Guid>(type: "uuid", nullable: false),
                    Comment = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("SupervisorRequests_pkey", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SupervisorRequests_ApplicationStatuses",
                        column: x => x.StatusId,
                        principalTable: "ApplicationStatuses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SupervisorRequests_Students",
                        column: x => x.StudentId,
                        principalTable: "Students",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SupervisorRequests_Users_Teacher",
                        column: x => x.TeacherUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                },
                comment: "Запросы студентов на выбор научного руководителя.");

            migrationBuilder.CreateTable(
                name: "StudentApplications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()", comment: "Уникальный идентификатор заявки"),
                    StudentId = table.Column<Guid>(type: "uuid", nullable: false, comment: "Идентификатор студента, подавшего заявку (внешний ключ к таблице Students)"),
                    TopicId = table.Column<Guid>(type: "uuid", nullable: false, comment: "Идентификатор темы ВКР, на которую подана заявка (внешний ключ к таблице Topics)"),
                    SupervisorRequestId = table.Column<Guid>(type: "uuid", nullable: true, comment: "Идентификатор одобренного запроса на научного руководителя (внешний ключ к таблице SupervisorRequests)"),
                    StatusId = table.Column<Guid>(type: "uuid", nullable: false, comment: "Идентификатор текущего статуса заявки (внешний ключ к таблице ApplicationStatuses), синхронизируется с последним действием"),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP", comment: "Дата и время создания заявки"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "Дата и время последнего обновления заявки")
                },
                constraints: table =>
                {
                    table.PrimaryKey("StudentApplications_pkey", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StudentApplications_ApplicationStatuses",
                        column: x => x.StatusId,
                        principalTable: "ApplicationStatuses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StudentApplications_Students",
                        column: x => x.StudentId,
                        principalTable: "Students",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StudentApplications_SupervisorRequests",
                        column: x => x.SupervisorRequestId,
                        principalTable: "SupervisorRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StudentApplications_Topics",
                        column: x => x.TopicId,
                        principalTable: "Topics",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                },
                comment: "Таблица заявок студентов на темы ВКР. Содержит информацию о заявках: выбранные темы и текущий статус. История согласований хранится в таблице ApplicationActions.");

            migrationBuilder.CreateTable(
                name: "GraduateWorks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()", comment: "Уникальный идентификатор выпускной квалификационной работы"),
                    ApplicationId = table.Column<Guid>(type: "uuid", nullable: false, comment: "Идентификатор заявки студента (внешний ключ к таблице StudentApplications), одна ВКР на заявку"),
                    StatusId = table.Column<Guid>(type: "uuid", nullable: false, comment: "Идентификатор статуса ВКР (внешний ключ к таблице GraduateWorkStatuses)"),
                    Title = table.Column<string>(type: "citext", nullable: false, comment: "Название выпускной квалификационной работы (регистронезависимо)"),
                    StudentId = table.Column<Guid>(type: "uuid", nullable: false, comment: "Идентификатор студента, выполнившего работу (внешний ключ к таблице Students)"),
                    TeacherId = table.Column<Guid>(type: "uuid", nullable: false, comment: "Идентификатор преподавателя-руководителя работы (внешний ключ к таблице Teachers)"),
                    Year = table.Column<int>(type: "integer", nullable: false, comment: "Учебный год, в котором была выполнена работа"),
                    Grade = table.Column<int>(type: "integer", nullable: true, comment: "Оценка за работу (от 0 до 100 баллов); NULL до заполнения администратором после защиты"),
                    CommissionMembers = table.Column<string>(type: "text", nullable: true, comment: "Состав комиссии, оценивавшей работу (текстовое описание); NULL до заполнения администратором после защиты"),
                    FilePath = table.Column<string>(type: "text", nullable: true, comment: "Ключ объекта основного файла ВКР в объектном хранилище; NULL до подтверждения загрузки"),
                    FileName = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true, comment: "Оригинальное имя файла ВКР (с расширением); используется в Content-Disposition при скачивании"),
                    PresentationPath = table.Column<string>(type: "text", nullable: true, comment: "Путь к файлу презентации работы (опционально)"),
                    PresentationFileName = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true, comment: "Оригинальное имя файла презентации (с расширением); используется в Content-Disposition при скачивании"),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP", comment: "Дата и время создания записи о работе"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "Дата и время последнего обновления записи о работе")
                },
                constraints: table =>
                {
                    table.PrimaryKey("GraduateWorks_pkey", x => x.Id);
                    table.CheckConstraint("CK_GraduateWorks_CommissionMembers_NotEmpty", "\"CommissionMembers\" IS NULL OR length(btrim(\"CommissionMembers\")) > 0");
                    table.CheckConstraint("CK_GraduateWorks_FileName_NotEmpty", "\"FileName\" IS NULL OR length(btrim(\"FileName\")) > 0");
                    table.CheckConstraint("CK_GraduateWorks_FilePath_NotEmpty", "\"FilePath\" IS NULL OR length(btrim(\"FilePath\")) > 0");
                    table.CheckConstraint("CK_GraduateWorks_Grade_Range", "\"Grade\" IS NULL OR \"Grade\" BETWEEN 0 AND 100");
                    table.CheckConstraint("CK_GraduateWorks_PresentationFileName_NotEmpty", "\"PresentationFileName\" IS NULL OR length(btrim(\"PresentationFileName\")) > 0");
                    table.CheckConstraint("CK_GraduateWorks_PresentationPath_NotEmpty", "\"PresentationPath\" IS NULL OR length(btrim(\"PresentationPath\")) > 0");
                    table.CheckConstraint("CK_GraduateWorks_Title_NotEmpty", "length(btrim(\"Title\"::text)) > 0");
                    table.CheckConstraint("CK_GraduateWorks_Year_Range", "\"Year\" BETWEEN 2000 AND 2100");
                    table.ForeignKey(
                        name: "FK_GraduateWorks_GraduateWorkStatuses",
                        column: x => x.StatusId,
                        principalTable: "GraduateWorkStatuses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GraduateWorks_StudentApplications",
                        column: x => x.ApplicationId,
                        principalTable: "StudentApplications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GraduateWorks_Students",
                        column: x => x.StudentId,
                        principalTable: "Students",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GraduateWorks_Teachers",
                        column: x => x.TeacherId,
                        principalTable: "Teachers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                },
                comment: "Таблица выпускных квалификационных работ (ВКР). Содержит информацию о завершенных работах студентов: название, оценки, файлы работ и презентаций, состав комиссии.");

            migrationBuilder.InsertData(
                table: "AcademicDegrees",
                columns: new[] { "Id", "CodeName", "CreatedAt", "DisplayName", "ShortName", "UpdatedAt" },
                values: new object[,]
                {
                    { new Guid("082f63c9-6d9e-4462-973d-51e56f9c6580"), "DoctorOfPhilologicalSciences", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "доктор филологических наук", "д-р филол. наук", null },
                    { new Guid("12855269-4d2f-4725-8dfd-972dedc893e6"), "CandidateOfPhilosophicalSciences", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "кандидат философских наук", "канд. филос. наук", null },
                    { new Guid("13c2a1b7-2b3b-46d4-a057-9c7b35eb5cf9"), "DoctorOfSociologicalSciences", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "доктор социологических наук", "д-р социол. наук", null },
                    { new Guid("151b1b4e-3dac-469a-a282-ed3092255c07"), "CandidateOfAgriculturalSciences", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "кандидат сельскохозяйственных наук", "канд. с.-х. наук", null },
                    { new Guid("1e293e01-8b5d-480d-9408-77c684fd37bc"), "CandidateOfCulturalStudies", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "кандидат культурологии", "канд. культурологии", null },
                    { new Guid("25abae5b-8ef7-48ab-88f2-076f85fd3de7"), "CandidateOfMedicalSciences", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "кандидат медицинских наук", "канд. мед. наук", null },
                    { new Guid("2d88d4a1-a3c4-4f62-8263-075e10008600"), "DoctorOfChemicalSciences", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "доктор химических наук", "д-р хим. наук", null },
                    { new Guid("32335bdf-0ea2-4cce-a263-d2d6a5f0f082"), "CandidateOfVeterinarySciences", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "кандидат ветеринарных наук", "канд. ветеринар. наук", null },
                    { new Guid("39d26f17-2ec9-42bc-bc64-9fc821d622ff"), "CandidateOfEconomicSciences", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "кандидат экономических наук", "канд. экон. наук", null },
                    { new Guid("44fe22e5-3025-4f00-b7db-49035f7c6289"), "DoctorOfPsychologicalSciences", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "доктор психологических наук", "д-р психол. наук", null },
                    { new Guid("4569d55c-f052-4493-914a-11498d8119db"), "DoctorOfMilitarySciences", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "доктор военных наук", "д-р воен. наук", null },
                    { new Guid("5205b00e-1af4-42c0-b8a8-8270b7fbf6fc"), "CandidateOfTechnicalSciences", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "кандидат технических наук", "канд. техн. наук", null },
                    { new Guid("56ef0a07-6318-4baa-af41-2f7bb97904e8"), "DoctorOfPoliticalSciences", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "доктор политических наук", "д-р полит. наук", null },
                    { new Guid("576dc6b1-1dc5-4a28-a023-07b1f3783817"), "None", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Без степени", null, null },
                    { new Guid("65db8961-b214-4f13-a52a-d77efc6ae4fb"), "CandidateOfPsychologicalSciences", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "кандидат психологических наук", "канд. психол. наук", null },
                    { new Guid("6aab8f60-bfc5-4256-ad9b-a36332bbad44"), "DoctorOfLegalSciences", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "доктор юридических наук", "д-р юрид. наук", null },
                    { new Guid("6acde269-0f6d-4987-ab84-b7fc4a17092e"), "CandidateOfHistoricalSciences", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "кандидат исторических наук", "канд. ист. наук", null },
                    { new Guid("759075f2-bda2-4c85-9b47-294f9166ea27"), "DoctorOfArtHistory", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "доктор искусствоведения", "д-р искусствоведения", null },
                    { new Guid("82eb2516-61bd-4eb0-b185-82d45f21e50e"), "CandidateOfPedagogicalSciences", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "кандидат педагогических наук", "канд. пед. наук", null },
                    { new Guid("8898b736-c62d-45c3-82e6-20e556ff51d3"), "DoctorOfPhysicsAndMathematics", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "доктор физико-математических наук", "д-р физ.-мат. наук", null },
                    { new Guid("89ca22b2-bf66-4d9e-b047-ee93cf3729c4"), "CandidateOfPhysicsAndMathematics", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "кандидат физико-математических наук", "канд. физ.-мат. наук", null },
                    { new Guid("8bf652a0-58ff-44b3-adc1-c6628e200808"), "DoctorOfPedagogicalSciences", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "доктор педагогических наук", "д-р пед. наук", null },
                    { new Guid("9370c85e-0ea8-4820-9a8d-cc47ef173ea0"), "CandidateOfPoliticalSciences", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "кандидат политических наук", "канд. полит. наук", null },
                    { new Guid("b06e6242-9b43-40ad-aa6d-32b4458f0a8f"), "CandidateOfLegalSciences", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "кандидат юридических наук", "канд. юрид. наук", null },
                    { new Guid("b112a3b6-c56a-49b6-8c14-b9f8cb30e3b8"), "DoctorOfTechnicalSciences", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "доктор технических наук", "д-р техн. наук", null },
                    { new Guid("b5f1d21a-921f-4117-9873-7026651df706"), "DoctorOfEconomicSciences", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "доктор экономических наук", "д-р экон. наук", null },
                    { new Guid("b9171e4b-51b6-4397-8fa7-4621c63f557e"), "DoctorOfVeterinarySciences", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "доктор ветеринарных наук", "д-р ветеринар. наук", null },
                    { new Guid("c020767c-038f-4187-af35-78681f3e53f1"), "DoctorOfMedicalSciences", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "доктор медицинских наук", "д-р мед. наук", null },
                    { new Guid("c4242756-5170-4c92-a4f0-7d5c646c386a"), "DoctorOfHistoricalSciences", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "доктор исторических наук", "д-р ист. наук", null },
                    { new Guid("c6ddc379-46ef-456f-9363-43e584bd3112"), "CandidateOfChemicalSciences", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "кандидат химических наук", "канд. хим. наук", null },
                    { new Guid("cac1b0e2-860e-416d-a970-c17616dc34d4"), "CandidateOfArtHistory", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "кандидат искусствоведения", "канд. искусствоведения", null },
                    { new Guid("deac597f-0048-45cd-a7c3-388b71a642e4"), "CandidateOfPhilologicalSciences", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "кандидат филологических наук", "канд. филол. наук", null },
                    { new Guid("e11b58ea-ee79-4124-864f-66d66c937947"), "DoctorOfGeographicalSciences", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "доктор географических наук", "д-р геогр. наук", null },
                    { new Guid("e214d642-2b3e-4e10-b6b8-8dd96a796ce2"), "DoctorOfAgriculturalSciences", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "доктор сельскохозяйственных наук", "д-р с.-х. наук", null },
                    { new Guid("e7c44f14-4964-47b4-9b45-f2ee58884794"), "CandidateOfBiologicalSciences", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "кандидат биологических наук", "канд. биол. наук", null },
                    { new Guid("ec876ca2-a2ed-4b5c-9241-eccc01931c83"), "DoctorOfBiologicalSciences", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "доктор биологических наук", "д-р биол. наук", null },
                    { new Guid("ed402408-da72-4adc-9861-5ac9d835288d"), "CandidateOfMilitarySciences", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "кандидат военных наук", "канд. воен. наук", null },
                    { new Guid("f3669689-9263-4ac5-a33d-b6cb00bbbfd2"), "DoctorOfPhilosophicalSciences", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "доктор философских наук", "д-р филос. наук", null },
                    { new Guid("f466844e-2b3d-4706-921b-a3782f3f71fe"), "CandidateOfSociologicalSciences", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "кандидат социологических наук", "канд. социол. наук", null },
                    { new Guid("fe1b8f5c-8bb2-45cb-834c-9178bdf37814"), "DoctorOfCulturalStudies", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "доктор культурологии", "д-р культурологии", null },
                    { new Guid("fef7b9bd-eff4-4105-823a-7f7d8590d80e"), "CandidateOfGeographicalSciences", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "кандидат географических наук", "канд. геогр. наук", null }
                });

            migrationBuilder.InsertData(
                table: "AcademicTitles",
                columns: new[] { "Id", "CodeName", "CreatedAt", "DisplayName", "UpdatedAt" },
                values: new object[,]
                {
                    { new Guid("03f6f767-6ed5-41d4-a26a-0745b89143ee"), "AssociateProfessor", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Доцент", null },
                    { new Guid("b9715e4d-9eaf-4fcd-af4f-fd4edaaca4fe"), "Professor", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Профессор", null },
                    { new Guid("ef488515-0904-448f-88eb-86a0a7c36cc6"), "None", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Без звания", null }
                });

            migrationBuilder.InsertData(
                table: "ApplicationActionStatuses",
                columns: new[] { "Id", "CodeName", "CreatedAt", "DisplayName", "UpdatedAt" },
                values: new object[,]
                {
                    { new Guid("05a67ee5-eb98-4ff8-a511-abca3abcce10"), "Rejected", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Отклонено", null },
                    { new Guid("1fb2b19d-271f-4df7-b544-3a6cb97e2398"), "Pending", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "На согласовании", null },
                    { new Guid("459226af-b979-4af6-a598-a1c72f2ba865"), "Cancelled", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Отменено", null },
                    { new Guid("4f78fc4b-7116-4b48-bb57-84ca2af36175"), "Approved", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Согласовано", null },
                    { new Guid("b381e910-5967-45cd-a48b-a907c25e385c"), "ReturnedForEditing", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Возвращено на редактирование", null }
                });

            migrationBuilder.InsertData(
                table: "ApplicationStatuses",
                columns: new[] { "Id", "CodeName", "CreatedAt", "DisplayName", "UpdatedAt" },
                values: new object[,]
                {
                    { new Guid("5fcdc4ad-5c81-403c-95a4-2ab1e9bdb390"), "ApprovedByDepartmentHead", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Утверждено заведующим кафедрой", null },
                    { new Guid("683a9a61-148e-4de2-8c96-9bcbb1727241"), "ApprovedBySupervisor", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Одобрено преподавателем", null },
                    { new Guid("96191172-82c0-4760-bf02-f1fb8c695638"), "RejectedBySupervisor", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Отклонено преподавателем", null },
                    { new Guid("9fc52948-532c-45f1-8d1e-444dda62a7f4"), "OnEditing", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "На редактировании", null },
                    { new Guid("a196d568-1e19-4f44-937c-7f0b92b7877f"), "Cancelled", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Отменено студентом", null },
                    { new Guid("a5f8904b-96b2-4f33-ba23-3f1f0d347a66"), "RejectedByDepartmentHead", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Отклонено заведующим кафедрой", null },
                    { new Guid("ad95b86a-caf5-4e81-b056-def8ddd3a0f6"), "PendingDepartmentHead", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Отправлено заведующему кафедрой", null },
                    { new Guid("b673307e-6a2d-404d-b52c-5fe54f689ebc"), "Pending", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Ожидает ответа преподавателя", null }
                });

            migrationBuilder.InsertData(
                table: "GraduateWorkStatuses",
                columns: new[] { "Id", "CodeName", "CreatedAt", "DisplayName", "UpdatedAt" },
                values: new object[,]
                {
                    { new Guid("18812e15-4341-4bee-a254-93f8f2ea5fc8"), "Completed", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Заполнено", null },
                    { new Guid("965079ef-80ab-4b6b-ae7c-86d21f1abfc7"), "Draft", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Черновик", null }
                });

            migrationBuilder.InsertData(
                table: "NotificationTypes",
                columns: new[] { "Id", "CodeName", "CreatedAt", "DisplayName", "UpdatedAt" },
                values: new object[,]
                {
                    { new Guid("11a3ad63-8018-4318-aa61-dc3cbb476922"), "TopicRejected", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Тема отклонена", null },
                    { new Guid("1352b13f-8f9f-4f3b-802d-274a4fbc631c"), "SupervisorDecisionByDepartmentHead", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Решение заведующего по заявке студента", null },
                    { new Guid("1dbba1a7-1e14-4094-9b87-6ca0af52264d"), "SupervisorRequestStatusChanged", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Статус запроса на научрука изменен", null },
                    { new Guid("1f1a210b-271d-4964-86e6-00f1013a5332"), "GraduateWorkUploaded", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "ВКР загружена в архив", null },
                    { new Guid("2ada1cff-7051-4923-9db2-fbc5cc32881f"), "TopicApproved", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Тема утверждена", null },
                    { new Guid("397e63a0-5d1e-4330-9812-940fe8660344"), "NewMessage", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Новое сообщение", null },
                    { new Guid("846d97d2-1872-47c4-91e0-78f452a18eb9"), "SupervisorRequestCreated", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Новый запрос на научное руководство", null },
                    { new Guid("ae653734-653a-4c1f-8d75-ead52887fce1"), "ApplicationSubmittedToDepartmentHead", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Заявка передана на рассмотрение кафедры", null },
                    { new Guid("c0e778fd-2499-47e1-bad1-7ec7fe99aa74"), "ApplicationStatusChanged", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Статус заявки изменен", null },
                    { new Guid("db554618-92d5-4479-ba4f-4c1f3260cc05"), "ApplicationSubmittedToSupervisor", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Новая заявка на рассмотрение научруком", null }
                });

            migrationBuilder.InsertData(
                table: "Positions",
                columns: new[] { "Id", "CodeName", "CreatedAt", "DisplayName", "UpdatedAt" },
                values: new object[,]
                {
                    { new Guid("189e2454-9e62-4a43-8cfd-dfd806fbf32e"), "DepartmentHead", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Заведующий кафедрой", null },
                    { new Guid("1d90d84c-d463-4399-bf1e-1a461a4c7cdd"), "Professor", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Профессор", null },
                    { new Guid("3fb73bf0-f882-46f3-9e0f-e241c4f22c12"), "AssociateProfessor", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Доцент", null },
                    { new Guid("547ae28e-dcc6-4072-8e49-1a253b939546"), "SeniorLecturer", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Старший преподаватель", null },
                    { new Guid("d7f907c1-8a09-441b-8cd5-3f5ffb44095c"), "Assistant", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Ассистент", null }
                });

            migrationBuilder.InsertData(
                table: "TopicCreatorTypes",
                columns: new[] { "Id", "CodeName", "CreatedAt", "DisplayName", "UpdatedAt" },
                values: new object[,]
                {
                    { new Guid("208d385e-62a4-498d-9c5e-d67aeb96482b"), "Student", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Студент", null },
                    { new Guid("c3bde87f-6409-47a8-a527-9d9708296bb4"), "Teacher", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Научный руководитель", null }
                });

            migrationBuilder.InsertData(
                table: "TopicStatuses",
                columns: new[] { "Id", "CodeName", "CreatedAt", "DisplayName", "UpdatedAt" },
                values: new object[,]
                {
                    { new Guid("0d032d1c-692c-4566-95df-62df8e9ddcd3"), "Active", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Активна", null },
                    { new Guid("57d06be7-4dec-4515-945f-815969992f36"), "Inactive", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Неактивна", null }
                });

            migrationBuilder.InsertData(
                table: "UserRoles",
                columns: new[] { "Id", "CodeName", "CreatedAt", "DisplayName", "UpdatedAt" },
                values: new object[,]
                {
                    { new Guid("385409b2-385c-4d5e-9325-85c73ed4c8ba"), "DepartmentHead", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Заведующий кафедрой", null },
                    { new Guid("58f7ed6b-fdb7-404e-bdb9-ffa45abbba51"), "Admin", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Администратор", null },
                    { new Guid("b7fd4e95-d013-4633-b018-4a971199e6f4"), "Teacher", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Преподаватель", null },
                    { new Guid("dde522f4-db45-4961-9466-f041aea1b345"), "Student", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Студент", null }
                });

            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "CreatedAt", "DepartmentId", "Email", "FirstName", "IsActive", "LastName", "MiddleName", "PasswordHash", "RoleId", "UpdatedAt" },
                values: new object[] { new Guid("037b31dc-d13a-423e-8b0b-983494f8a7f9"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "z_admin@example.com", "Николай", true, "Системов", "Администраторович", "!", new Guid("58f7ed6b-fdb7-404e-bdb9-ffa45abbba51"), null });

            migrationBuilder.CreateIndex(
                name: "AcademicDegrees_CodeName_key",
                table: "AcademicDegrees",
                column: "CodeName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "AcademicTitles_CodeName_key",
                table: "AcademicTitles",
                column: "CodeName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationActions_ApplicationId",
                table: "ApplicationActions",
                column: "ApplicationId");

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationActions_CreatedAt",
                table: "ApplicationActions",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationActions_ResponsibleId",
                table: "ApplicationActions",
                column: "ResponsibleId");

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationActions_StatusId",
                table: "ApplicationActions",
                column: "StatusId");

            migrationBuilder.CreateIndex(
                name: "ApplicationActionStatuses_CodeName_key",
                table: "ApplicationActionStatuses",
                column: "CodeName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ApplicationStatuses_CodeName_key",
                table: "ApplicationStatuses",
                column: "CodeName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationTopicChangeHistories_ApplicationId",
                table: "ApplicationTopicChangeHistories",
                column: "ApplicationId");

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationTopicChangeHistories_ChangedByUserId",
                table: "ApplicationTopicChangeHistories",
                column: "ChangedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationTopicChangeHistories_CreatedAt",
                table: "ApplicationTopicChangeHistories",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ChatMessages_ApplicationId",
                table: "ChatMessages",
                column: "ApplicationId");

            migrationBuilder.CreateIndex(
                name: "IX_ChatMessages_ApplicationId_SenderId_ReadAt",
                table: "ChatMessages",
                columns: new[] { "ApplicationId", "SenderId" },
                filter: "\"ReadAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ChatMessages_ApplicationId_SentAt",
                table: "ChatMessages",
                columns: new[] { "ApplicationId", "SentAt" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_ChatMessages_SenderId",
                table: "ChatMessages",
                column: "SenderId");

            migrationBuilder.CreateIndex(
                name: "IX_ChatMessages_SentAt",
                table: "ChatMessages",
                column: "SentAt");

            migrationBuilder.CreateIndex(
                name: "IX_Departments_HeadId",
                table: "Departments",
                column: "HeadId");

            migrationBuilder.CreateIndex(
                name: "UQ_Departments_CodeName",
                table: "Departments",
                column: "CodeName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GraduateWorks_StatusId",
                table: "GraduateWorks",
                column: "StatusId");

            migrationBuilder.CreateIndex(
                name: "IX_GraduateWorks_StudentId",
                table: "GraduateWorks",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_GraduateWorks_TeacherId",
                table: "GraduateWorks",
                column: "TeacherId");

            migrationBuilder.CreateIndex(
                name: "IX_GraduateWorks_Year",
                table: "GraduateWorks",
                column: "Year");

            migrationBuilder.CreateIndex(
                name: "UX_GraduateWorks_ApplicationId",
                table: "GraduateWorks",
                column: "ApplicationId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "GraduateWorkStatuses_CodeName_key",
                table: "GraduateWorkStatuses",
                column: "CodeName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_CreatedAt",
                table: "Notifications",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_TypeId",
                table: "Notifications",
                column: "TypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_UserId",
                table: "Notifications",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_UserId_IsRead",
                table: "Notifications",
                columns: new[] { "UserId", "IsRead" });

            migrationBuilder.CreateIndex(
                name: "NotificationTypes_CodeName_key",
                table: "NotificationTypes",
                column: "CodeName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "Positions_CodeName_key",
                table: "Positions",
                column: "CodeName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StudentApplications_CreatedAt",
                table: "StudentApplications",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_StudentApplications_StatusId",
                table: "StudentApplications",
                column: "StatusId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentApplications_StudentId",
                table: "StudentApplications",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentApplications_StudentId_StatusId",
                table: "StudentApplications",
                columns: new[] { "StudentId", "StatusId" });

            migrationBuilder.CreateIndex(
                name: "IX_StudentApplications_SupervisorRequestId",
                table: "StudentApplications",
                column: "SupervisorRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentApplications_TopicId",
                table: "StudentApplications",
                column: "TopicId");

            migrationBuilder.CreateIndex(
                name: "IX_Students_GroupId",
                table: "Students",
                column: "GroupId");

            migrationBuilder.CreateIndex(
                name: "Students_UserId_key",
                table: "Students",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "StudyGroups_CodeName_key",
                table: "StudyGroups",
                column: "CodeName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SupervisorRequests_StatusId",
                table: "SupervisorRequests",
                column: "StatusId");

            migrationBuilder.CreateIndex(
                name: "IX_SupervisorRequests_StudentId",
                table: "SupervisorRequests",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_SupervisorRequests_StudentId_TeacherUserId",
                table: "SupervisorRequests",
                columns: new[] { "StudentId", "TeacherUserId" });

            migrationBuilder.CreateIndex(
                name: "IX_SupervisorRequests_TeacherUserId",
                table: "SupervisorRequests",
                column: "TeacherUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Teachers_AcademicDegreeId",
                table: "Teachers",
                column: "AcademicDegreeId");

            migrationBuilder.CreateIndex(
                name: "IX_Teachers_AcademicTitleId",
                table: "Teachers",
                column: "AcademicTitleId");

            migrationBuilder.CreateIndex(
                name: "IX_Teachers_PositionId",
                table: "Teachers",
                column: "PositionId");

            migrationBuilder.CreateIndex(
                name: "Teachers_UserId_key",
                table: "Teachers",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "TopicCreatorTypes_CodeName_key",
                table: "TopicCreatorTypes",
                column: "CodeName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Topics_CreatedBy",
                table: "Topics",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Topics_CreatorTypeId",
                table: "Topics",
                column: "CreatorTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Topics_StatusId",
                table: "Topics",
                column: "StatusId");

            migrationBuilder.CreateIndex(
                name: "TopicStatuses_CodeName_key",
                table: "TopicStatuses",
                column: "CodeName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UserRoles_CodeName_key",
                table: "UserRoles",
                column: "CodeName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_DepartmentId",
                table: "Users",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_IsActive",
                table: "Users",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Users_RoleId",
                table: "Users",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "Users_Email_key",
                table: "Users",
                column: "Email",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ApplicationActions_StudentApplications",
                table: "ApplicationActions",
                column: "ApplicationId",
                principalTable: "StudentApplications",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ApplicationActions_Users",
                table: "ApplicationActions",
                column: "ResponsibleId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ApplicationTopicChangeHistories_StudentApplications",
                table: "ApplicationTopicChangeHistories",
                column: "ApplicationId",
                principalTable: "StudentApplications",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ApplicationTopicChangeHistories_Users",
                table: "ApplicationTopicChangeHistories",
                column: "ChangedByUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ChatMessages_StudentApplications",
                table: "ChatMessages",
                column: "ApplicationId",
                principalTable: "StudentApplications",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ChatMessages_Users",
                table: "ChatMessages",
                column: "SenderId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Departments_Users_Head",
                table: "Departments",
                column: "HeadId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Departments_Users_Head",
                table: "Departments");

            migrationBuilder.DropTable(
                name: "ApplicationActions");

            migrationBuilder.DropTable(
                name: "ApplicationTopicChangeHistories");

            migrationBuilder.DropTable(
                name: "ChatMessages");

            migrationBuilder.DropTable(
                name: "GraduateWorks");

            migrationBuilder.DropTable(
                name: "Notifications");

            migrationBuilder.DropTable(
                name: "ApplicationActionStatuses");

            migrationBuilder.DropTable(
                name: "GraduateWorkStatuses");

            migrationBuilder.DropTable(
                name: "StudentApplications");

            migrationBuilder.DropTable(
                name: "Teachers");

            migrationBuilder.DropTable(
                name: "NotificationTypes");

            migrationBuilder.DropTable(
                name: "SupervisorRequests");

            migrationBuilder.DropTable(
                name: "Topics");

            migrationBuilder.DropTable(
                name: "AcademicDegrees");

            migrationBuilder.DropTable(
                name: "AcademicTitles");

            migrationBuilder.DropTable(
                name: "Positions");

            migrationBuilder.DropTable(
                name: "ApplicationStatuses");

            migrationBuilder.DropTable(
                name: "Students");

            migrationBuilder.DropTable(
                name: "TopicCreatorTypes");

            migrationBuilder.DropTable(
                name: "TopicStatuses");

            migrationBuilder.DropTable(
                name: "StudyGroups");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "Departments");

            migrationBuilder.DropTable(
                name: "UserRoles");
        }
    }
}
