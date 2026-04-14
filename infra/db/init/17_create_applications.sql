-- Создание таблицы StudentApplications (Заявки студентов)
-- Зависит от: Students, Topics, SupervisorRequests, ApplicationStatuses

DROP TABLE IF EXISTS "StudentApplications" CASCADE;

CREATE TABLE "StudentApplications" (
    "Id" UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    "StudentId" UUID NOT NULL,
    "TopicId" UUID NOT NULL,
    "SupervisorRequestId" UUID NULL,
    "StatusId" UUID NOT NULL,
    "CreatedAt" TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" TIMESTAMPTZ NULL,

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

    CONSTRAINT "FK_StudentApplications_SupervisorRequests"
        FOREIGN KEY ("SupervisorRequestId")
        REFERENCES "SupervisorRequests"("Id")
        ON DELETE RESTRICT
        ON UPDATE CASCADE,

    CONSTRAINT "FK_StudentApplications_ApplicationStatuses"
        FOREIGN KEY ("StatusId")
        REFERENCES "ApplicationStatuses"("Id")
        ON DELETE RESTRICT
        ON UPDATE CASCADE
);

-- Комментарии к таблице
COMMENT ON TABLE "StudentApplications" IS 'Таблица заявок студентов на темы ВКР. Содержит информацию о заявках: выбранные темы и текущий статус. История согласований хранится в таблице ApplicationActions.';

-- Комментарии к столбцам
COMMENT ON COLUMN "StudentApplications"."Id"        IS 'Уникальный идентификатор заявки';
COMMENT ON COLUMN "StudentApplications"."StudentId" IS 'Идентификатор студента, подавшего заявку (внешний ключ к таблице Students)';
COMMENT ON COLUMN "StudentApplications"."TopicId"   IS 'Идентификатор темы ВКР, на которую подана заявка (внешний ключ к таблице Topics)';
COMMENT ON COLUMN "StudentApplications"."SupervisorRequestId" IS 'Идентификатор одобренного запроса на научного руководителя (внешний ключ к таблице SupervisorRequests)';
COMMENT ON COLUMN "StudentApplications"."StatusId"  IS 'Идентификатор текущего статуса заявки (внешний ключ к таблице ApplicationStatuses), синхронизируется с последним действием';
COMMENT ON COLUMN "StudentApplications"."CreatedAt" IS 'Дата и время создания заявки';
COMMENT ON COLUMN "StudentApplications"."UpdatedAt" IS 'Дата и время последнего обновления заявки';
