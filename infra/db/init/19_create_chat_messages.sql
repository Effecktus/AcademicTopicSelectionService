-- Создание таблицы ChatMessages (Сообщения чата)
-- Зависит от: StudentApplications, Users

DROP TABLE IF EXISTS "ChatMessages" CASCADE;

CREATE TABLE "ChatMessages" (
    "Id" UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    "ApplicationId" UUID NOT NULL,
    "SenderId" UUID NOT NULL,
    "Content" TEXT NOT NULL,
    "SentAt" TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "ReadAt" TIMESTAMPTZ NULL,
    
    CONSTRAINT "CK_ChatMessages_Content_NotEmpty" CHECK (length(btrim("Content")) > 0),
    CONSTRAINT "CK_ChatMessages_ReadAt_AfterSentAt" CHECK ("ReadAt" IS NULL OR "ReadAt" >= "SentAt"),

    CONSTRAINT "FK_ChatMessages_StudentApplications"
        FOREIGN KEY ("ApplicationId")
        REFERENCES "StudentApplications"("Id")
        ON DELETE CASCADE
        ON UPDATE CASCADE,
        
    CONSTRAINT "FK_ChatMessages_Users"
        FOREIGN KEY ("SenderId")
        REFERENCES "Users"("Id")
        ON DELETE RESTRICT
        ON UPDATE CASCADE
);

-- Комментарии к таблице
COMMENT ON TABLE "ChatMessages" IS 'Таблица сообщений чата между студентами и преподавателями по заявкам на темы ВКР. Содержит историю переписки и информацию о прочтении сообщений.';

-- Комментарии к столбцам
COMMENT ON COLUMN "ChatMessages"."Id" IS 'Уникальный идентификатор сообщения';
COMMENT ON COLUMN "ChatMessages"."ApplicationId" IS 'Идентификатор заявки, к которой относится сообщение (внешний ключ к таблице Applications)';
COMMENT ON COLUMN "ChatMessages"."SenderId" IS 'Идентификатор отправителя сообщения (внешний ключ к таблице Users)';
COMMENT ON COLUMN "ChatMessages"."Content" IS 'Текст сообщения';
COMMENT ON COLUMN "ChatMessages"."SentAt" IS 'Дата и время отправки сообщения';
COMMENT ON COLUMN "ChatMessages"."ReadAt" IS 'Дата и время прочтения сообщения получателем (NULL, если сообщение не прочитано)';
