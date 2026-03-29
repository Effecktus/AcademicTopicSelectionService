-- Создание таблицы Topics (Темы ВКР)
-- Зависит от: TopicCreatorTypes, Users, TopicStatuses

DROP TABLE IF EXISTS "Topics" CASCADE;

CREATE TABLE "Topics" (
    "Id" UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    "Title" CITEXT NOT NULL,
    "Description" TEXT NULL,
    "CreatorTypeId" UUID NOT NULL,
    "CreatedBy" UUID NOT NULL,
    "StatusId" UUID NOT NULL,
    "CreatedAt" TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" TIMESTAMPTZ NULL,
    
    CONSTRAINT "CK_Topics_Title_NotEmpty" CHECK (length(btrim("Title"::text)) > 0),

    CONSTRAINT "FK_Topics_TopicCreatorTypes"
        FOREIGN KEY ("CreatorTypeId")
        REFERENCES "TopicCreatorTypes"("Id")
        ON DELETE RESTRICT 
        ON UPDATE CASCADE,

    CONSTRAINT "FK_Topics_Users"
        FOREIGN KEY ("CreatedBy")
        REFERENCES "Users"("Id")
        ON DELETE RESTRICT 
        ON UPDATE CASCADE,
        
    CONSTRAINT "FK_Topics_TopicStatuses"
        FOREIGN KEY ("StatusId")
        REFERENCES "TopicStatuses"("Id")
        ON DELETE RESTRICT 
        ON UPDATE CASCADE
);

-- Комментарии к таблице
COMMENT ON TABLE "Topics" IS 'Таблица тем выпускных квалификационных работ (ВКР). Содержит темы, предложенные как научными руководителями, так и студентами.';

-- Комментарии к столбцам
COMMENT ON COLUMN "Topics"."Id" IS 'Уникальный идентификатор темы ВКР';
COMMENT ON COLUMN "Topics"."Title" IS 'Название темы выпускной квалификационной работы';
COMMENT ON COLUMN "Topics"."Description" IS 'Подробное описание темы ВКР, требования и особенности';
COMMENT ON COLUMN "Topics"."CreatorTypeId" IS 'Тип пользователя, создавшего тему (внешний ключ к таблице TopicCreatorTypes)';
COMMENT ON COLUMN "Topics"."CreatedBy" IS 'Пользователь, создавший тему (внешний ключ к таблице Users)';
COMMENT ON COLUMN "Topics"."StatusId" IS 'Идентификатор статуса темы (внешний ключ к таблице TopicStatuses)';
COMMENT ON COLUMN "Topics"."CreatedAt" IS 'Дата и время создания записи о теме';
COMMENT ON COLUMN "Topics"."UpdatedAt" IS 'Дата и время последнего обновления записи о теме';
