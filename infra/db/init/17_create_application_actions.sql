-- Создание таблицы ApplicationActions (Действия по заявкам)
-- Зависит от: StudentApplications, Users, ApplicationActionStatuses
-- Описание: каждая запись — одно действие в рамках процесса согласования заявки.
-- Порядок действий: студент подаёт заявку → создаётся действие для научрука →
-- при одобрении создаётся новое действие для заведующего кафедрой.

DROP TABLE IF EXISTS "ApplicationActions" CASCADE;

CREATE TABLE "ApplicationActions" (
    "Id" UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    "ApplicationId" UUID NOT NULL,
    "ResponsibleId" UUID NOT NULL,
    "StatusId" UUID NOT NULL,
    "Comment" TEXT NULL,
    "CreatedAt" TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" TIMESTAMPTZ NULL,

    CONSTRAINT "CK_ApplicationActions_Comment_NotEmpty" CHECK (
        "Comment" IS NULL OR length(btrim("Comment")) > 0
    ),

    CONSTRAINT "FK_ApplicationActions_StudentApplications"
        FOREIGN KEY ("ApplicationId")
        REFERENCES "StudentApplications"("Id")
        ON DELETE RESTRICT
        ON UPDATE CASCADE,

    CONSTRAINT "FK_ApplicationActions_Users"
        FOREIGN KEY ("ResponsibleId")
        REFERENCES "Users"("Id")
        ON DELETE RESTRICT
        ON UPDATE CASCADE,

    CONSTRAINT "FK_ApplicationActions_ApplicationActionStatuses"
        FOREIGN KEY ("StatusId")
        REFERENCES "ApplicationActionStatuses"("Id")
        ON DELETE RESTRICT
        ON UPDATE CASCADE
);

-- Комментарии к таблице
COMMENT ON TABLE "ApplicationActions" IS 'Таблица действий по заявкам студентов. Хранит историю согласований: каждое действие описывает один этап рассмотрения заявки (преподавателем или заведующим кафедрой). Новое действие создаётся при переходе заявки на следующий этап.';

-- Комментарии к столбцам
COMMENT ON COLUMN "ApplicationActions"."Id"                IS 'Уникальный идентификатор действия';
COMMENT ON COLUMN "ApplicationActions"."ApplicationId"     IS 'Идентификатор заявки, к которой относится действие (внешний ключ к таблице StudentApplications)';
COMMENT ON COLUMN "ApplicationActions"."ResponsibleId" IS 'Идентификатор пользователя, ответственного за данный этап согласования (внешний ключ к таблице Users)';
COMMENT ON COLUMN "ApplicationActions"."StatusId"          IS 'Идентификатор статуса действия: На согласовании / Согласовано / Отклонено / Отменено (внешний ключ к таблице ApplicationActionStatuses)';
COMMENT ON COLUMN "ApplicationActions"."Comment"           IS 'Комментарий ответственного (причина отклонения или произвольный комментарий при согласовании)';
COMMENT ON COLUMN "ApplicationActions"."CreatedAt"         IS 'Дата и время создания действия';
COMMENT ON COLUMN "ApplicationActions"."UpdatedAt"         IS 'Дата и время последнего обновления действия';
