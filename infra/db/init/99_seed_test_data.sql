-- Тестовые данные для сценариев UI/бэкенда.
-- Важно: справочники уже должны быть заполнены (01..07), таблицы созданы.
--
-- =============================================================================
-- Учётные записи (логин = Email, пароль указан ниже)
-- =============================================================================
-- Администратор:
--   z_admin@example.com                    /  TestPassword123!
-- Преподаватели (серия):
--   teacher01@example.com … teacher10@example.com   /  TestPassword123!
-- Преподаватель (кафедра 01, без заявок в сиде):
--   Aydar.Ilin@norbit.ru                 /  Password123!
-- Студенты (серия):
--   student01@example.com … student20@example.com   /  TestPassword123!
-- Студент (кафедра 01, без заявок в сиде):
--   fqlfh2004@gmail.com                  /  Password123!
-- Заведующие (серия, кафедры 02–04):
--   head02@example.com … head04@example.com         /  TestPassword123!
-- Заведующий кафедры 01 (заменяет head01):
--   effecktus@yandex.ru                  /  Password123!
-- =============================================================================
--
-- Ключевая модель:
--   - 4 кафедры; заведующий кафедры 01 — effecktus@yandex.ru
--   - 11 преподавателей (10 серийных 3+3+2+2 + Aydar.Ilin@norbit.ru в кафедре 01)
--   - 21 студент (20 серийных по кафедрам + fqlfh2004@gmail.com в кафедре 01)
--   - у каждого преподавателя по 3 темы (33 преподавательские темы)
--   - 20 студентов student01..20: по 2 на преподавателя teacher01..10; одобренные SR + заявки на тему
--   - Aydar.Ilin@norbit.ru и fqlfh2004@gmail.com: без SupervisorRequests и без StudentApplications
--   - на каждую кафедру 1 заявка на тему в статусе PendingDepartmentHead
--   - остальные заявки на тему — Pending
--   - ~половина заявок на тему на «студенческих» темах (только у student01..20)

TRUNCATE TABLE
    "GraduateWorks",
    "Notifications",
    "ChatMessages",
    "ApplicationActions",
    "StudentApplications",
    "SupervisorRequests",
    "Topics",
    "Students",
    "StudyGroups",
    "Teachers",
    "Users",
    "Departments"
RESTART IDENTITY CASCADE;

-- ---------------------------------------------------------------------
-- Departments (4)
INSERT INTO "Departments" ("CodeName", "DisplayName")
SELECT
    format('Department%s', lpad(gs::text, 2, '0'))::citext,
    format('Кафедра %s', lpad(gs::text, 2, '0'))
FROM generate_series(1, 4) AS gs;

-- ---------------------------------------------------------------------
-- Users: 10 серийных Teacher + 20 Student + 3 серийных DepartmentHead (02–04)
--        + 1 Admin + 3 персональных (зав. каф. 01, преподаватель, студент)

INSERT INTO "Users" ("Email", "PasswordHash", "FirstName", "LastName", "MiddleName", "RoleId", "DepartmentId", "IsActive")
SELECT
    format('teacher%s@example.com', lpad(gs::text, 2, '0'))::citext,
    crypt('TestPassword123!', gen_salt('bf', 10)),
    format('Иван%s', lpad(gs::text, 2, '0')),
    format('Петров%s', lpad(gs::text, 2, '0')),
    NULL,
    (SELECT "Id" FROM "UserRoles" WHERE "CodeName" = 'Teacher' LIMIT 1),
    (SELECT "Id" FROM "Departments" ORDER BY "CodeName" OFFSET (
        CASE
            WHEN gs <= 3 THEN 0
            WHEN gs <= 6 THEN 1
            WHEN gs <= 8 THEN 2
            ELSE 3
        END
    ) LIMIT 1),
    TRUE
FROM generate_series(1, 10) AS gs;

INSERT INTO "Users" ("Email", "PasswordHash", "FirstName", "LastName", "MiddleName", "RoleId", "DepartmentId", "IsActive")
SELECT
    format('student%s@example.com', lpad(gs::text, 2, '0'))::citext,
    crypt('TestPassword123!', gen_salt('bf', 10)),
    format('Алексей%s', lpad(gs::text, 2, '0')),
    format('Иванов%s', lpad(gs::text, 2, '0')),
    NULL,
    (SELECT "Id" FROM "UserRoles" WHERE "CodeName" = 'Student' LIMIT 1),
    (SELECT "Id" FROM "Departments" ORDER BY "CodeName" OFFSET ((gs - 1) / 5) LIMIT 1),
    TRUE
FROM generate_series(1, 20) AS gs;

-- Заведующие кафедр 02–04 (head01 не создаём — его заменяет effecktus@yandex.ru)
INSERT INTO "Users" ("Email", "PasswordHash", "FirstName", "LastName", "MiddleName", "RoleId", "DepartmentId", "IsActive")
SELECT
    format('head%s@example.com', lpad(gs::text, 2, '0'))::citext,
    crypt('TestPassword123!', gen_salt('bf', 10)),
    format('Сергей%s', lpad(gs::text, 2, '0')),
    format('Завкафедров%s', lpad(gs::text, 2, '0')),
    NULL,
    (SELECT "Id" FROM "UserRoles" WHERE "CodeName" = 'DepartmentHead' LIMIT 1),
    (SELECT "Id" FROM "Departments" ORDER BY "CodeName" OFFSET (gs - 1) LIMIT 1),
    TRUE
FROM generate_series(2, 4) AS gs;

INSERT INTO "Users" ("Email", "PasswordHash", "FirstName", "LastName", "MiddleName", "RoleId", "DepartmentId", "IsActive")
VALUES (
    'effecktus@yandex.ru'::citext,
    crypt('Password123!', gen_salt('bf', 10)),
    'Заведующий',
    'Кафедрой',
    NULL,
    (SELECT "Id" FROM "UserRoles" WHERE "CodeName" = 'DepartmentHead' LIMIT 1),
    (SELECT "Id" FROM "Departments" ORDER BY "CodeName" LIMIT 1),
    TRUE
);

INSERT INTO "Users" ("Email", "PasswordHash", "FirstName", "LastName", "MiddleName", "RoleId", "DepartmentId", "IsActive")
VALUES (
    'Aydar.Ilin@norbit.ru'::citext,
    crypt('Password123!', gen_salt('bf', 10)),
    'Айдар',
    'Ильин',
    NULL,
    (SELECT "Id" FROM "UserRoles" WHERE "CodeName" = 'Teacher' LIMIT 1),
    (SELECT "Id" FROM "Departments" ORDER BY "CodeName" LIMIT 1),
    TRUE
);

INSERT INTO "Users" ("Email", "PasswordHash", "FirstName", "LastName", "MiddleName", "RoleId", "DepartmentId", "IsActive")
VALUES (
    'fqlfh2004@gmail.com'::citext,
    crypt('Password123!', gen_salt('bf', 10)),
    'Демо',
    'Студент',
    NULL,
    (SELECT "Id" FROM "UserRoles" WHERE "CodeName" = 'Student' LIMIT 1),
    (SELECT "Id" FROM "Departments" ORDER BY "CodeName" LIMIT 1),
    TRUE
);

INSERT INTO "Users" ("Email", "PasswordHash", "FirstName", "LastName", "MiddleName", "RoleId", "DepartmentId", "IsActive")
VALUES (
    'z_admin@example.com'::citext,
    crypt('TestPassword123!', gen_salt('bf', 10)),
    'Администратор',
    'Системный',
    NULL,
    (SELECT "Id" FROM "UserRoles" WHERE "CodeName" = 'Admin' LIMIT 1),
    NULL,
    TRUE
);

UPDATE "Departments" d
SET "HeadId" = (
    SELECT u."Id"
    FROM "Users" u
    WHERE u."Email" = 'effecktus@yandex.ru'::citext
      AND u."RoleId" = (SELECT "Id" FROM "UserRoles" WHERE "CodeName" = 'DepartmentHead' LIMIT 1)
    LIMIT 1
)
WHERE d."CodeName" = 'Department01';

UPDATE "Departments" d
SET "HeadId" = (
    SELECT u."Id"
    FROM "Users" u
    WHERE u."Email" = format(
        'head%s@example.com',
        regexp_replace(d."CodeName"::text, '\D', '', 'g')
    )::citext
      AND u."RoleId" = (SELECT "Id" FROM "UserRoles" WHERE "CodeName" = 'DepartmentHead' LIMIT 1)
    LIMIT 1
)
WHERE d."CodeName" IN ('Department02', 'Department03', 'Department04');

-- ---------------------------------------------------------------------
-- Teachers: все 11 преподавателей (серийные + Aydar.Ilin@norbit.ru)
INSERT INTO "Teachers" ("UserId", "MaxStudentsLimit", "AcademicDegreeId", "AcademicTitleId", "PositionId")
SELECT
    u."Id",
    (6 + (u.gs % 6)),
    (SELECT "Id" FROM "AcademicDegrees" WHERE "CodeName" = (CASE (u.gs % 5)
        WHEN 0 THEN 'None'
        WHEN 1 THEN 'CandidateOfTechnicalSciences'
        WHEN 2 THEN 'CandidateOfEconomicSciences'
        WHEN 3 THEN 'DoctorOfTechnicalSciences'
        ELSE 'DoctorOfEconomicSciences'
    END) LIMIT 1),
    (SELECT "Id" FROM "AcademicTitles" WHERE "CodeName" = (CASE (u.gs % 3)
        WHEN 0 THEN 'None'
        WHEN 1 THEN 'AssociateProfessor'
        ELSE 'Professor'
    END) LIMIT 1),
    (SELECT "Id" FROM "Positions" WHERE "CodeName" = (CASE (u.gs % 4)
        WHEN 0 THEN 'Assistant'
        WHEN 1 THEN 'SeniorLecturer'
        WHEN 2 THEN 'AssociateProfessor'
        ELSE 'Professor'
    END) LIMIT 1)
FROM (
    SELECT u."Id", row_number() OVER (ORDER BY u."Email") AS gs
    FROM "Users" u
    WHERE u."RoleId" = (SELECT "Id" FROM "UserRoles" WHERE "CodeName" = 'Teacher' LIMIT 1)
    ORDER BY u."Email"
) AS u;

-- Группы 4001–4021 (доп. группа для демо-студента)
INSERT INTO "StudyGroups" ("CodeName")
SELECT (4000 + gs)::int
FROM generate_series(1, 21) AS gs
ON CONFLICT ("CodeName") DO NOTHING;

-- Студенты: 20 серийных + демо
INSERT INTO "Students" ("UserId", "GroupId")
SELECT
    u."Id",
    (SELECT "Id" FROM "StudyGroups" WHERE "CodeName" = (4000 + u.gs)::int LIMIT 1)
FROM (
    SELECT u."Id", row_number() OVER (ORDER BY u."Email") AS gs
    FROM "Users" u
    WHERE u."RoleId" = (SELECT "Id" FROM "UserRoles" WHERE "CodeName" = 'Student' LIMIT 1)
      AND u."Email"::text LIKE 'student%@example.com'
    ORDER BY u."Email"
) AS u;

INSERT INTO "Students" ("UserId", "GroupId")
SELECT
    u."Id",
    (SELECT "Id" FROM "StudyGroups" WHERE "CodeName" = 4021 LIMIT 1)
FROM "Users" u
WHERE u."Email" = 'fqlfh2004@gmail.com'::citext;

-- ---------------------------------------------------------------------
-- Темы преподавателей: по 3 на каждого из 11
INSERT INTO "Topics" ("Title", "Description", "CreatorTypeId", "CreatedBy", "StatusId")
SELECT
    format('Тема преподавателя %s.%s', lpad(t.gs::text, 2, '0'), topic_idx)::citext,
    format('Описание темы преподавателя %s.%s', lpad(t.gs::text, 2, '0'), topic_idx),
    (SELECT "Id" FROM "TopicCreatorTypes" WHERE "CodeName" = 'Teacher' LIMIT 1),
    t."UserId",
    (SELECT "Id" FROM "TopicStatuses" WHERE "CodeName" = 'Active' LIMIT 1)
FROM (
    SELECT u."Id" AS "UserId", row_number() OVER (ORDER BY u."Email") AS gs
    FROM "Users" u
    WHERE u."RoleId" = (SELECT "Id" FROM "UserRoles" WHERE "CodeName" = 'Teacher' LIMIT 1)
    ORDER BY u."Email"
) t
CROSS JOIN generate_series(1, 3) AS topic_idx;

-- Темы студентов: 20 серийных
INSERT INTO "Topics" ("Title", "Description", "CreatorTypeId", "CreatedBy", "StatusId")
SELECT
    format('Тема студента %s', lpad(s.gs::text, 2, '0'))::citext,
    format('Описание темы студента %s', lpad(s.gs::text, 2, '0')),
    (SELECT "Id" FROM "TopicCreatorTypes" WHERE "CodeName" = 'Student' LIMIT 1),
    s."UserId",
    (SELECT "Id" FROM "TopicStatuses" WHERE "CodeName" = 'Inactive' LIMIT 1)
FROM (
    SELECT u."Id" AS "UserId", row_number() OVER (ORDER BY u."Email") AS gs
    FROM "Users" u
    WHERE u."RoleId" = (SELECT "Id" FROM "UserRoles" WHERE "CodeName" = 'Student' LIMIT 1)
      AND u."Email"::text LIKE 'student%@example.com'
    ORDER BY u."Email"
) s;

-- Тема демо-студента (без заявок в сиде)
INSERT INTO "Topics" ("Title", "Description", "CreatorTypeId", "CreatedBy", "StatusId")
SELECT
    'Тема студента (демо, fqlfh2004)'::citext,
    'Черновая тема демо-аккаунта; заявки в сиде не создаются.',
    (SELECT "Id" FROM "TopicCreatorTypes" WHERE "CodeName" = 'Student' LIMIT 1),
    u."Id",
    (SELECT "Id" FROM "TopicStatuses" WHERE "CodeName" = 'Inactive' LIMIT 1)
FROM "Users" u
WHERE u."Email" = 'fqlfh2004@gmail.com'::citext;

-- ---------------------------------------------------------------------
-- SupervisorRequests (20): student01..student20 ↔ teacher01..teacher10 (по 2).
-- Без Aydar.Ilin@norbit.ru как научрука и без fqlfh2004@gmail.com как студента.
WITH students_flow AS (
    SELECT
        s."Id" AS "StudentId",
        su."DepartmentId",
        row_number() OVER (ORDER BY su."Email") AS rn
    FROM "Students" s
    JOIN "Users" su ON su."Id" = s."UserId"
    WHERE su."Email"::text LIKE 'student%@example.com'
),
students_paired AS (
    SELECT
        "StudentId",
        "DepartmentId",
        rn,
        row_number() OVER (ORDER BY rn) AS sp
    FROM students_flow
),
teachers_flow AS (
    SELECT
        tu."Id" AS "TeacherUserId",
        row_number() OVER (ORDER BY tu."Email") AS tpos
    FROM "Users" tu
    WHERE tu."RoleId" = (SELECT "Id" FROM "UserRoles" WHERE "CodeName" = 'Teacher' LIMIT 1)
      AND tu."Email"::text LIKE 'teacher%@example.com'
)
INSERT INTO "SupervisorRequests" ("StudentId", "TeacherUserId", "StatusId", "Comment")
SELECT
    sp."StudentId",
    tf."TeacherUserId",
    (SELECT "Id" FROM "ApplicationStatuses" WHERE "CodeName" = 'ApprovedBySupervisor' LIMIT 1),
    'Одобрено преподавателем в тестовых данных'
FROM students_paired sp
JOIN LATERAL (
    SELECT (sp.sp + 1) / 2 AS tpos
) slot ON TRUE
JOIN teachers_flow tf ON tf.tpos = slot.tpos;

-- ---------------------------------------------------------------------
-- StudentApplications (20)
WITH students_indexed AS (
    SELECT
        s."Id" AS "StudentId",
        su."Id" AS "StudentUserId",
        su."DepartmentId",
        row_number() OVER (ORDER BY su."Email") AS rn
    FROM "Students" s
    JOIN "Users" su ON su."Id" = s."UserId"
    WHERE su."Email"::text LIKE 'student%@example.com'
),
approved_requests AS (
    SELECT
        sr."Id" AS "SupervisorRequestId",
        sr."StudentId",
        sr."TeacherUserId"
    FROM "SupervisorRequests" sr
    WHERE sr."StatusId" = (SELECT "Id" FROM "ApplicationStatuses" WHERE "CodeName" = 'ApprovedBySupervisor' LIMIT 1)
),
student_topics AS (
    SELECT
        t."Id" AS "TopicId",
        t."CreatedBy" AS "StudentUserId"
    FROM "Topics" t
    WHERE t."CreatorTypeId" = (SELECT "Id" FROM "TopicCreatorTypes" WHERE "CodeName" = 'Student' LIMIT 1)
),
teacher_topics_ranked AS (
    SELECT
        t."Id" AS "TopicId",
        t."CreatedBy" AS "TeacherUserId",
        row_number() OVER (PARTITION BY t."CreatedBy" ORDER BY t."Title", t."Id") AS topic_rn
    FROM "Topics" t
    WHERE t."CreatorTypeId" = (SELECT "Id" FROM "TopicCreatorTypes" WHERE "CodeName" = 'Teacher' LIMIT 1)
),
app_candidates AS (
    SELECT
        s."StudentId",
        s."StudentUserId",
        s."DepartmentId",
        s.rn,
        row_number() OVER (ORDER BY s.rn) AS app_seq
    FROM students_indexed s
)
INSERT INTO "StudentApplications" ("StudentId", "TopicId", "SupervisorRequestId", "StatusId")
SELECT
    ac."StudentId",
    CASE
        WHEN ac.app_seq <= 10 THEN st."TopicId"
        ELSE tt."TopicId"
    END AS "TopicId",
    ar."SupervisorRequestId",
    CASE
        WHEN ac.rn IN (2, 6, 11, 16)
            THEN (SELECT "Id" FROM "ApplicationStatuses" WHERE "CodeName" = 'PendingDepartmentHead' LIMIT 1)
        ELSE
            (SELECT "Id" FROM "ApplicationStatuses" WHERE "CodeName" = 'Pending' LIMIT 1)
    END AS "StatusId"
FROM app_candidates ac
JOIN approved_requests ar ON ar."StudentId" = ac."StudentId"
JOIN student_topics st ON st."StudentUserId" = ac."StudentUserId"
JOIN teacher_topics_ranked tt ON tt."TeacherUserId" = ar."TeacherUserId" AND tt.topic_rn = 1;

-- ---------------------------------------------------------------------
-- ApplicationActions
INSERT INTO "ApplicationActions" ("ApplicationId", "ResponsibleId", "StatusId", "Comment")
SELECT
    a."Id",
    sr."TeacherUserId",
    (SELECT "Id" FROM "ApplicationActionStatuses" WHERE "CodeName" = 'Pending' LIMIT 1),
    'Ожидает ответа научного руководителя'
FROM "StudentApplications" a
JOIN "SupervisorRequests" sr ON sr."Id" = a."SupervisorRequestId"
WHERE a."StatusId" = (SELECT "Id" FROM "ApplicationStatuses" WHERE "CodeName" = 'Pending' LIMIT 1);

INSERT INTO "ApplicationActions" ("ApplicationId", "ResponsibleId", "StatusId", "Comment")
SELECT
    a."Id",
    sr."TeacherUserId",
    (SELECT "Id" FROM "ApplicationActionStatuses" WHERE "CodeName" = 'Approved' LIMIT 1),
    'Одобрено научным руководителем'
FROM "StudentApplications" a
JOIN "SupervisorRequests" sr ON sr."Id" = a."SupervisorRequestId"
WHERE a."StatusId" = (SELECT "Id" FROM "ApplicationStatuses" WHERE "CodeName" = 'PendingDepartmentHead' LIMIT 1);

INSERT INTO "ApplicationActions" ("ApplicationId", "ResponsibleId", "StatusId", "Comment")
SELECT
    a."Id",
    d."HeadId",
    (SELECT "Id" FROM "ApplicationActionStatuses" WHERE "CodeName" = 'Pending' LIMIT 1),
    'Ожидает решения заведующего кафедрой'
FROM "StudentApplications" a
JOIN "Students" s ON s."Id" = a."StudentId"
JOIN "Users" su ON su."Id" = s."UserId"
JOIN "Departments" d ON d."Id" = su."DepartmentId"
WHERE a."StatusId" = (SELECT "Id" FROM "ApplicationStatuses" WHERE "CodeName" = 'PendingDepartmentHead' LIMIT 1);

-- ---------------------------------------------------------------------
-- ChatMessages: по 1 сообщению на заявку
INSERT INTO "ChatMessages" ("ApplicationId", "SenderId", "Content", "SentAt", "ReadAt")
SELECT
    a."Id",
    su."Id",
    format('Тестовое сообщение по заявке %s', a."Id"::text),
    (CURRENT_TIMESTAMP - make_interval(mins => seq.seq_num::int)),
    CASE
        WHEN (seq.seq_num % 2) = 0 THEN (CURRENT_TIMESTAMP - make_interval(mins => (seq.seq_num - 1)::int))
        ELSE NULL
    END
FROM (
    SELECT "Id", "StudentId", row_number() OVER (ORDER BY "CreatedAt", "Id") AS seq_num
    FROM "StudentApplications"
) seq
JOIN "StudentApplications" a ON a."Id" = seq."Id"
JOIN "Students" s ON s."Id" = a."StudentId"
JOIN "Users" su ON su."Id" = s."UserId";

-- ---------------------------------------------------------------------
-- Notifications (20)
INSERT INTO "Notifications" ("UserId", "TypeId", "Title", "Content", "IsRead", "CreatedAt")
SELECT
    u."Id",
    (SELECT nt."Id" FROM "NotificationTypes" nt
     WHERE nt."CodeName" = (CASE (u.gs % 4)
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
    WHERE u."Email" NOT IN (
        'Aydar.Ilin@norbit.ru'::citext,
        'fqlfh2004@gmail.com'::citext
    )
    ORDER BY u."Email"
    LIMIT 20
) u;

-- ---------------------------------------------------------------------
-- GraduateWorks: по 1 записи на заявку
INSERT INTO "GraduateWorks" (
    "ApplicationId",
    "Title",
    "StudentId",
    "TeacherId",
    "Year",
    "Grade",
    "CommissionMembers",
    "FilePath",
    "FileName",
    "PresentationPath",
    "PresentationFileName"
)
SELECT
    app."Id",
    format('ВКР %s', lpad(seq.seq_num::text, 2, '0'))::citext,
    st."Id",
    t."Id",
    2025,
    (65 + (seq.seq_num % 31)),
    format('Иванов И.И.; Петров П.П.; Сидоров С.С. (комиссия %s)', lpad(seq.seq_num::text, 2, '0')),
    format('vkr/2025/work_%s/thesis.pdf', lpad(seq.seq_num::text, 2, '0')),
    format('ВКР_%s.pdf', lpad(seq.seq_num::text, 2, '0')),
    CASE WHEN (seq.seq_num % 2) = 0 THEN format('vkr/2025/work_%s/presentation.pptx', lpad(seq.seq_num::text, 2, '0')) ELSE NULL END,
    CASE WHEN (seq.seq_num % 2) = 0 THEN format('Презентация_%s.pptx', lpad(seq.seq_num::text, 2, '0')) ELSE NULL END
FROM (
    SELECT app."Id", app."StudentId", sr."TeacherUserId", row_number() OVER (ORDER BY app."CreatedAt", app."Id") AS seq_num
    FROM "StudentApplications" app
    JOIN "SupervisorRequests" sr ON sr."Id" = app."SupervisorRequestId"
) seq
JOIN "StudentApplications" app ON app."Id" = seq."Id"
JOIN "Students" st ON st."Id" = seq."StudentId"
JOIN "Teachers" t ON t."UserId" = seq."TeacherUserId";
