-- Создание таблицы StudentApplications (Заявки студентов)
-- Зависит от: Students, Topics, ApplicationStatuses

DROP TABLE IF EXISTS "StudentApplications" CASCADE;

CREATE TABLE "StudentApplications" (
    "Id" UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    "StudentId" UUID NOT NULL,
    "TopicId" UUID NULL,
    "ProposedTitle" CITEXT NULL,
    "ProposedDescription" TEXT NULL,
    "StatusId" UUID NOT NULL,
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" TIMESTAMP NULL,
    "TeacherApprovedAt" TIMESTAMP NULL,
    "TeacherRejectedAt" TIMESTAMP NULL,
    "TeacherRejectionReason" TEXT NULL,
    "DepartmentHeadApprovedAt" TIMESTAMP NULL,
    "DepartmentHeadRejectedAt" TIMESTAMP NULL,
    "DepartmentHeadRejectionReason" TEXT NULL,
    "CancelledAt" TIMESTAMP NULL,
    
    -- либо выбираем существующую тему (TopicId), либо предлагаем свою (ProposedTitle)
    CONSTRAINT "CK_StudentApplications_TopicId_XOR_ProposedTitle" CHECK (
        ("TopicId" IS NOT NULL AND "ProposedTitle" IS NULL)
        OR
        ("TopicId" IS NULL AND "ProposedTitle" IS NOT NULL)
    ),
    CONSTRAINT "CK_StudentApplications_ProposedTitle_NotEmpty" CHECK (
        "ProposedTitle" IS NULL OR length(btrim("ProposedTitle"::text)) > 0
    ),
    CONSTRAINT "CK_StudentApplications_TeacherDecision_Exclusive" CHECK (
        NOT ("TeacherApprovedAt" IS NOT NULL AND "TeacherRejectedAt" IS NOT NULL)
    ),
    CONSTRAINT "CK_StudentApplications_DepartmentHeadDecision_Exclusive" CHECK (
        NOT ("DepartmentHeadApprovedAt" IS NOT NULL AND "DepartmentHeadRejectedAt" IS NOT NULL)
    ),
    CONSTRAINT "CK_StudentApplications_TeacherRejectionReason_Required" CHECK (
        "TeacherRejectedAt" IS NULL OR ("TeacherRejectionReason" IS NOT NULL AND length(btrim("TeacherRejectionReason")) > 0)
    ),
    CONSTRAINT "CK_StudentApplications_DepartmentHeadRejectionReason_Required" CHECK (
        "DepartmentHeadRejectedAt" IS NULL OR ("DepartmentHeadRejectionReason" IS NOT NULL AND length(btrim("DepartmentHeadRejectionReason")) > 0)
    ),

    CONSTRAINT "FK_StudentApplications_Students"
        FOREIGN KEY ("StudentId")
        REFERENCES "Students"("Id")
        ON DELETE RESTRICT
        ON UPDATE CASCADE,
        
    CONSTRAINT "FK_StudentApplications_Topics"
        FOREIGN KEY ("TopicId")
        REFERENCES "Topics"("Id")
        ON DELETE RESTRICT
        ON UPDATE CASCADE,
    
    CONSTRAINT "FK_StudentApplications_ApplicationStatuses"
        FOREIGN KEY ("StatusId")
        REFERENCES "ApplicationStatuses"("Id")
        ON DELETE RESTRICT
        ON UPDATE CASCADE
);

-- Комментарии к таблице
COMMENT ON TABLE "StudentApplications" IS 'Таблица заявок студентов на темы ВКР. Содержит информацию о заявках: выбранные или предложенные темы, статусы обработки и временные метки действий преподавателей и заведующих кафедрой.';

-- Комментарии к столбцам
COMMENT ON COLUMN "StudentApplications"."Id" IS 'Уникальный идентификатор заявки';
COMMENT ON COLUMN "StudentApplications"."StudentId" IS 'Идентификатор студента, подавшего заявку (внешний ключ к таблице Students)';
COMMENT ON COLUMN "StudentApplications"."TopicId" IS 'Идентификатор выбранной темы ВКР (внешний ключ к таблице Topics). NULL, если студент предлагает свою тему';
COMMENT ON COLUMN "StudentApplications"."ProposedTitle" IS 'Название предложенной темы ВКР (регистронезависимо). Используется, если студент предлагает свою тему вместо выбора существующей';
COMMENT ON COLUMN "StudentApplications"."ProposedDescription" IS 'Подробное описание предложенной темы ВКР, требования и особенности';
COMMENT ON COLUMN "StudentApplications"."StatusId" IS 'Идентификатор текущего статуса заявки (внешний ключ к таблице StudentApplicationtatuses)';
COMMENT ON COLUMN "StudentApplications"."CreatedAt" IS 'Дата и время создания заявки';
COMMENT ON COLUMN "StudentApplications"."UpdatedAt" IS 'Дата и время последнего обновления заявки';
COMMENT ON COLUMN "StudentApplications"."TeacherApprovedAt" IS 'Дата и время одобрения заявки преподавателем';
COMMENT ON COLUMN "StudentApplications"."TeacherRejectedAt" IS 'Дата и время отклонения заявки преподавателем';
COMMENT ON COLUMN "StudentApplications"."TeacherRejectionReason" IS 'Причина отклонения заявки преподавателем';
COMMENT ON COLUMN "StudentApplications"."DepartmentHeadApprovedAt" IS 'Дата и время утверждения заявки заведующим кафедрой';
COMMENT ON COLUMN "StudentApplications"."DepartmentHeadRejectedAt" IS 'Дата и время отклонения заявки заведующим кафедрой';
COMMENT ON COLUMN "StudentApplications"."DepartmentHeadRejectionReason" IS 'Причина отклонения заявки заведующим кафедрой';
COMMENT ON COLUMN "StudentApplications"."CancelledAt" IS 'Дата и время отмены заявки студентом';