-- Создание таблицы Students (Студенты)
-- Зависит от: Users, StudyGroups

DROP TABLE IF EXISTS "Students" CASCADE;

CREATE TABLE "Students" (
    "Id" UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    "UserId" UUID NOT NULL UNIQUE,
    "GroupId" UUID NOT NULL,
    "CreatedAt" TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" TIMESTAMPTZ NULL,

    CONSTRAINT "FK_Students_Users"
        FOREIGN KEY ("UserId")
        REFERENCES "Users"("Id")
        ON DELETE RESTRICT
        ON UPDATE CASCADE,

    CONSTRAINT "FK_Students_StudyGroups"
        FOREIGN KEY ("GroupId")
        REFERENCES "StudyGroups"("Id")
        ON DELETE RESTRICT
        ON UPDATE CASCADE
);

-- Комментарии к таблице
COMMENT ON TABLE "Students" IS 'Таблица студентов. Содержит дополнительную информацию о студентах: принадлежность к учебной группе.';

-- Комментарии к столбцам
COMMENT ON COLUMN "Students"."Id" IS 'Уникальный идентификатор студента';
COMMENT ON COLUMN "Students"."UserId" IS 'Идентификатор пользователя-студента (внешний ключ к таблице Users)';
COMMENT ON COLUMN "Students"."GroupId" IS 'Идентификатор учебной группы студента (внешний ключ к таблице StudyGroups)';
COMMENT ON COLUMN "Students"."CreatedAt" IS 'Дата и время создания записи о студенте';
COMMENT ON COLUMN "Students"."UpdatedAt" IS 'Дата и время последнего обновления записи о студенте';
