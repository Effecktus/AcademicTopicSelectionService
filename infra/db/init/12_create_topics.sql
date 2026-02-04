-- Создание таблицы Topics (Темы ВКР)
-- Зависит от: Teachers, TopicStatuses

DROP TABLE IF EXISTS "Topics" CASCADE;

CREATE TABLE "Topics" (
    "Id" UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    "Title" CITEXT NOT NULL,
    "Description" TEXT NULL,
    "Year" INTEGER NOT NULL,
    "TeacherId" UUID NOT NULL,
    "StatusId" UUID NOT NULL,
    "CreatedAt" TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" TIMESTAMPTZ NULL,
    
    CONSTRAINT "CK_Topics_Title_NotEmpty" CHECK (length(btrim("Title"::text)) > 0),
    CONSTRAINT "CK_Topics_Year_Range" CHECK ("Year" BETWEEN 2000 AND 2100),
    CONSTRAINT "UQ_Topics_Teacher_Year_Title" UNIQUE ("TeacherId", "Year", "Title"),

    CONSTRAINT "FK_Topics_Teachers"
        FOREIGN KEY ("TeacherId")
        REFERENCES "Teachers"("Id")
        ON DELETE RESTRICT 
        ON UPDATE CASCADE,
        
    CONSTRAINT "FK_Topics_TopicStatuses"
        FOREIGN KEY ("StatusId")
        REFERENCES "TopicStatuses"("Id")
        ON DELETE RESTRICT 
        ON UPDATE CASCADE
);

-- Комментарии к таблице
COMMENT ON TABLE "Topics" IS 'Таблица тем выпускных квалификационных работ (ВКР). Содержит информацию о темах, предлагаемых преподавателями для студентов.';

-- Комментарии к столбцам
COMMENT ON COLUMN "Topics"."Id" IS 'Уникальный идентификатор темы ВКР';
COMMENT ON COLUMN "Topics"."Title" IS 'Название темы выпускной квалификационной работы';
COMMENT ON COLUMN "Topics"."Description" IS 'Подробное описание темы ВКР, требования и особенности';
COMMENT ON COLUMN "Topics"."Year" IS 'Учебный год, для которого предназначена тема';
COMMENT ON COLUMN "Topics"."TeacherId" IS 'Идентификатор преподавателя, предложившего тему (внешний ключ к таблице Teachers)';
COMMENT ON COLUMN "Topics"."StatusId" IS 'Идентификатор статуса темы (внешний ключ к таблице TopicStatuses)';
COMMENT ON COLUMN "Topics"."CreatedAt" IS 'Дата и время создания записи о теме';
COMMENT ON COLUMN "Topics"."UpdatedAt" IS 'Дата и время последнего обновления записи о теме';