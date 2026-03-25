-- Тестовые данные для проверки схемы БД
-- ВАЖНО: рассчитано на то, что справочники уже заполнены (01..07), а таблицы созданы.
-- Скрипт вставляет по 20 записей на каждую НЕсправочную таблицу.
--
-- Тестовые учётные данные:
--   Преподаватели: teacher01@example.com .. teacher20@example.com  /  TestPassword123!
--   Студенты:      student01@example.com .. student20@example.com  /  TestPassword123!
--
-- Примечание по количеству пользователей:
-- Чтобы получить 20 записей в Teachers и 20 записей в Students при ограничении UNIQUE(UserId) в обеих таблицах,
-- требуется минимум 40 записей в Users (20 пользователей-преподавателей + 20 пользователей-студентов).

-- ---------------------------------------------------------------------
-- Сделать скрипт переиспользуемым (можно запускать повторно)
TRUNCATE TABLE
    "GraduateWorks",
    "Notifications",
    "ChatMessages",
    "StudentApplications",
    "Topics",
    "Students",
    "Teachers",
    "Users",
    "Departments"
RESTART IDENTITY CASCADE;

-- ---------------------------------------------------------------------
-- Departments (20)
INSERT INTO "Departments" ("Name")
SELECT format('Кафедра %s', lpad(gs::text, 2, '0'))
FROM generate_series(1, 20) AS gs;

-- ---------------------------------------------------------------------
-- Users (40): 20 Teacher + 20 Student
-- Teacher users
INSERT INTO "Users" ("Email", "PasswordHash", "FirstName", "LastName", "MiddleName", "RoleId", "DepartmentId", "IsActive")
SELECT
    format('teacher%s@example.com', lpad(gs::text, 2, '0'))::citext,
    crypt('TestPassword123!', gen_salt('bf', 10)),
    format('Иван%s', lpad(gs::text, 2, '0')),
    format('Петров%s', lpad(gs::text, 2, '0')),
    NULL,
    (SELECT "Id" FROM "UserRoles" WHERE "Name" = 'Teacher' LIMIT 1),
    (SELECT "Id" FROM "Departments" ORDER BY "Name" OFFSET ((gs - 1) % 20) LIMIT 1),
    TRUE
FROM generate_series(1, 20) AS gs;

-- Student users
INSERT INTO "Users" ("Email", "PasswordHash", "FirstName", "LastName", "MiddleName", "RoleId", "DepartmentId", "IsActive")
SELECT
    format('student%s@example.com', lpad(gs::text, 2, '0'))::citext,
    crypt('TestPassword123!', gen_salt('bf', 10)),
    format('Алексей%s', lpad(gs::text, 2, '0')),
    format('Иванов%s', lpad(gs::text, 2, '0')),
    NULL,
    (SELECT "Id" FROM "UserRoles" WHERE "Name" = 'Student' LIMIT 1),
    (SELECT "Id" FROM "Departments" ORDER BY "Name" OFFSET ((gs - 1) % 20) LIMIT 1),
    TRUE
FROM generate_series(1, 20) AS gs;

-- ---------------------------------------------------------------------
-- Teachers (20) — на основе Teacher users
INSERT INTO "Teachers" ("UserId", "MaxStudentsLimit", "AcademicDegreeId", "AcademicTitleId", "PositionId")
SELECT
    u."Id",
    (5 + (u.gs % 10)),
    (SELECT "Id" FROM "AcademicDegrees" WHERE "Name" = (CASE (u.gs % 5)
        WHEN 0 THEN 'None'
        WHEN 1 THEN 'CandidateOfTechnicalSciences'
        WHEN 2 THEN 'CandidateOfEconomicSciences'
        WHEN 3 THEN 'DoctorOfTechnicalSciences'
        ELSE 'DoctorOfEconomicSciences'
    END) LIMIT 1),
    (SELECT "Id" FROM "AcademicTitles" WHERE "Name" = (CASE (u.gs % 3)
        WHEN 0 THEN 'None'
        WHEN 1 THEN 'AssociateProfessor'
        ELSE 'Professor'
    END) LIMIT 1),
    (SELECT "Id" FROM "Positions" WHERE "Name" = (CASE (u.gs % 4)
        WHEN 0 THEN 'Assistant'
        WHEN 1 THEN 'SeniorLecturer'
        WHEN 2 THEN 'AssociateProfessor'
        ELSE 'Professor'
    END) LIMIT 1)
FROM (
    SELECT u."Id", row_number() OVER (ORDER BY u."Email") AS gs
    FROM "Users" u
    WHERE u."RoleId" = (SELECT "Id" FROM "UserRoles" WHERE "Name" = 'Teacher' LIMIT 1)
    ORDER BY u."Email"
    LIMIT 20
) AS u;

-- ---------------------------------------------------------------------
-- Students (20) — на основе Student users
INSERT INTO "Students" ("UserId", "Group")
SELECT
    u."Id",
    (4000 + u.gs)::int
FROM (
    SELECT u."Id", row_number() OVER (ORDER BY u."Email") AS gs
    FROM "Users" u
    WHERE u."RoleId" = (SELECT "Id" FROM "UserRoles" WHERE "Name" = 'Student' LIMIT 1)
    ORDER BY u."Email"
    LIMIT 20
) AS u;

-- ---------------------------------------------------------------------
-- Topics (20)
INSERT INTO "Topics" ("Title", "Description", "Year", "TeacherId", "StatusId")
SELECT
    format('Тема %s', lpad(t.gs::text, 2, '0'))::citext,
    format('Описание темы %s', lpad(t.gs::text, 2, '0')),
    2025,
    t."Id",
    (SELECT "Id" FROM "TopicStatuses" WHERE "Name" = (CASE WHEN (t.gs % 2) = 0 THEN 'Active' ELSE 'Inactive' END) LIMIT 1)
FROM (
    SELECT t."Id", row_number() OVER (ORDER BY t."Id") AS gs
    FROM "Teachers" t
    ORDER BY t."Id"
    LIMIT 20
) AS t;

-- ---------------------------------------------------------------------
-- StudentApplications (20): 10 по существующим темам + 10 со своей темой
INSERT INTO "StudentApplications" ("StudentId", "TopicId", "ProposedTitle", "ProposedDescription", "StatusId")
SELECT
    s."Id",
    tp."Id",
    NULL,
    NULL,
    (SELECT "Id" FROM "ApplicationStatuses" WHERE "Name" = 'Pending' LIMIT 1)
FROM (
    SELECT s."Id", row_number() OVER (ORDER BY s."Id") AS gs
    FROM "Students" s
    ORDER BY s."Id"
    LIMIT 10
) s
JOIN (
    SELECT t."Id", row_number() OVER (ORDER BY t."Id") AS gs
    FROM "Topics" t
    ORDER BY t."Id"
    LIMIT 10
) tp ON tp.gs = s.gs;

INSERT INTO "StudentApplications" ("StudentId", "TopicId", "ProposedTitle", "ProposedDescription", "StatusId")
SELECT
    s."Id",
    NULL,
    format('Предложенная тема %s', lpad(s.gs::text, 2, '0'))::citext,
    format('Описание предложенной темы %s', lpad(s.gs::text, 2, '0')),
    (SELECT "Id" FROM "ApplicationStatuses" WHERE "Name" = 'Pending' LIMIT 1)
FROM (
    SELECT s."Id", row_number() OVER (ORDER BY s."Id") AS gs
    FROM "Students" s
    ORDER BY s."Id"
    OFFSET 10
    LIMIT 10
) s;

-- ---------------------------------------------------------------------
-- ChatMessages (20): по 1 сообщению на заявку
INSERT INTO "ChatMessages" ("ApplicationId", "SenderId", "Content", "SentAt", "ReadAt")
SELECT
    a."Id",
    u."Id",
    format('Тестовое сообщение по заявке %s', a."Id"::text),
    (CURRENT_TIMESTAMP - make_interval(mins => a.gs::int)),
    CASE WHEN (a.gs % 2) = 0 THEN (CURRENT_TIMESTAMP - make_interval(mins => (a.gs - 1)::int)) ELSE NULL END
FROM (
    SELECT a."Id", a."StudentId", row_number() OVER (ORDER BY a."Id") AS gs
    FROM "StudentApplications" a
    ORDER BY a."Id"
    LIMIT 20
) a
JOIN "Students" s ON s."Id" = a."StudentId"
JOIN "Users" u ON u."Id" = s."UserId";

-- ---------------------------------------------------------------------
-- Notifications (20)
INSERT INTO "Notifications" ("UserId", "TypeId", "Title", "Content", "IsRead", "CreatedAt")
SELECT
    u."Id",
    (SELECT nt."Id" FROM "NotificationTypes" nt
     WHERE nt."Name" = (CASE (u.gs % 4)
        WHEN 0 THEN 'ApplicationStatusChanged'
        WHEN 1 THEN 'NewMessage'
        WHEN 2 THEN 'TopicApproved'
        ELSE 'TopicRejected'
     END) LIMIT 1),
    format('Уведомление %s', lpad(u.gs::text, 2, '0')),
    format('Тестовый контент уведомления %s', lpad(u.gs::text, 2, '0')),
    CASE WHEN (u.gs % 3) = 0 THEN TRUE ELSE FALSE END,
    (CURRENT_TIMESTAMP - make_interval(hours => u.gs::int))
FROM (
    SELECT u."Id", row_number() OVER (ORDER BY u."Email") AS gs
    FROM "Users" u
    ORDER BY u."Email"
    LIMIT 20
) u;

-- ---------------------------------------------------------------------
-- GraduateWorks (20)
INSERT INTO "GraduateWorks" ("Title", "StudentId", "TeacherId", "Year", "Grade", "CommissionMembers", "FilePath", "PresentationPath")
SELECT
    format('ВКР %s', lpad(s.gs::text, 2, '0'))::citext,
    s."Id",
    t."Id",
    2025,
    (50 + (s.gs % 51)), -- 50..100
    format('Иванов И.И.; Петров П.П.; Сидоров С.С. (комиссия %s)', lpad(s.gs::text, 2, '0')),
    format('vkr/2025/work_%s/thesis.pdf', lpad(s.gs::text, 2, '0')),
    CASE WHEN (s.gs % 2) = 0 THEN format('vkr/2025/work_%s/presentation.pptx', lpad(s.gs::text, 2, '0')) ELSE NULL END
FROM (
    SELECT s."Id", row_number() OVER (ORDER BY s."Id") AS gs
    FROM "Students" s
    ORDER BY s."Id"
    LIMIT 20
) s
JOIN (
    SELECT t."Id", row_number() OVER (ORDER BY t."Id") AS gs
    FROM "Teachers" t
    ORDER BY t."Id"
    LIMIT 20
) t ON t.gs = s.gs;
