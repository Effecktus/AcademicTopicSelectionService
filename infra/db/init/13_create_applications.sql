-- Создание таблицы Applications (Заявки студентов)
-- Зависит от: Students, Topics, ApplicationStatuses

DROP TABLE IF EXISTS "Applications" CASCADE;

CREATE TABLE "Applications" (
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
    CONSTRAINT "CK_Applications_TopicId_XOR_ProposedTitle" CHECK (
        ("TopicId" IS NOT NULL AND "ProposedTitle" IS NULL)
        OR
        ("TopicId" IS NULL AND "ProposedTitle" IS NOT NULL)
    ),
    CONSTRAINT "CK_Applications_ProposedTitle_NotEmpty" CHECK (
        "ProposedTitle" IS NULL OR length(btrim("ProposedTitle"::text)) > 0
    ),
    CONSTRAINT "CK_Applications_TeacherDecision_Exclusive" CHECK (
        NOT ("TeacherApprovedAt" IS NOT NULL AND "TeacherRejectedAt" IS NOT NULL)
    ),
    CONSTRAINT "CK_Applications_DepartmentHeadDecision_Exclusive" CHECK (
        NOT ("DepartmentHeadApprovedAt" IS NOT NULL AND "DepartmentHeadRejectedAt" IS NOT NULL)
    ),
    CONSTRAINT "CK_Applications_TeacherRejectionReason_Required" CHECK (
        "TeacherRejectedAt" IS NULL OR ("TeacherRejectionReason" IS NOT NULL AND length(btrim("TeacherRejectionReason")) > 0)
    ),
    CONSTRAINT "CK_Applications_DepartmentHeadRejectionReason_Required" CHECK (
        "DepartmentHeadRejectedAt" IS NULL OR ("DepartmentHeadRejectionReason" IS NOT NULL AND length(btrim("DepartmentHeadRejectionReason")) > 0)
    ),

    CONSTRAINT "FK_Applications_Students"
        FOREIGN KEY ("StudentId")
        REFERENCES "Students"("Id")
        ON DELETE RESTRICT
        ON UPDATE CASCADE,
        
    CONSTRAINT "FK_Applications_Topics"
        FOREIGN KEY ("TopicId")
        REFERENCES "Topics"("Id")
        ON DELETE RESTRICT
        ON UPDATE CASCADE,
    
    CONSTRAINT "FK_Applications_ApplicationStatuses"
        FOREIGN KEY ("StatusId")
        REFERENCES "ApplicationStatuses"("Id")
        ON DELETE RESTRICT
        ON UPDATE CASCADE
);

-- Комментарии к таблице
COMMENT ON TABLE "Applications" IS 'Таблица заявок студентов на темы ВКР. Содержит информацию о заявках: выбранные или предложенные темы, статусы обработки и временные метки действий преподавателей и заведующих кафедрой.';

-- Комментарии к столбцам
COMMENT ON COLUMN "Applications"."Id" IS 'Уникальный идентификатор заявки';
COMMENT ON COLUMN "Applications"."StudentId" IS 'Идентификатор студента, подавшего заявку (внешний ключ к таблице Students)';
COMMENT ON COLUMN "Applications"."TopicId" IS 'Идентификатор выбранной темы ВКР (внешний ключ к таблице Topics). NULL, если студент предлагает свою тему';
COMMENT ON COLUMN "Applications"."ProposedTitle" IS 'Название предложенной темы ВКР (регистронезависимо). Используется, если студент предлагает свою тему вместо выбора существующей';
COMMENT ON COLUMN "Applications"."ProposedDescription" IS 'Подробное описание предложенной темы ВКР, требования и особенности';
COMMENT ON COLUMN "Applications"."StatusId" IS 'Идентификатор текущего статуса заявки (внешний ключ к таблице ApplicationStatuses)';
COMMENT ON COLUMN "Applications"."CreatedAt" IS 'Дата и время создания заявки';
COMMENT ON COLUMN "Applications"."UpdatedAt" IS 'Дата и время последнего обновления заявки';
COMMENT ON COLUMN "Applications"."TeacherApprovedAt" IS 'Дата и время одобрения заявки преподавателем';
COMMENT ON COLUMN "Applications"."TeacherRejectedAt" IS 'Дата и время отклонения заявки преподавателем';
COMMENT ON COLUMN "Applications"."TeacherRejectionReason" IS 'Причина отклонения заявки преподавателем';
COMMENT ON COLUMN "Applications"."DepartmentHeadApprovedAt" IS 'Дата и время утверждения заявки заведующим кафедрой';
COMMENT ON COLUMN "Applications"."DepartmentHeadRejectedAt" IS 'Дата и время отклонения заявки заведующим кафедрой';
COMMENT ON COLUMN "Applications"."DepartmentHeadRejectionReason" IS 'Причина отклонения заявки заведующим кафедрой';
COMMENT ON COLUMN "Applications"."CancelledAt" IS 'Дата и время отмены заявки студентом';