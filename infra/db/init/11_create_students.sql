-- Создание таблицы Students (Студенты)
-- Зависит от: Users

DROP TABLE IF EXISTS "Students" CASCADE;

CREATE TABLE "Students" (
    "Id" UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    "UserId" UUID NOT NULL UNIQUE,
    "Group" INTEGER NOT NULL,
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" TIMESTAMP NULL,
    
    CONSTRAINT "CK_Students_Group_Range" CHECK ("Group" BETWEEN 1000 AND 9999),

    CONSTRAINT "FK_Students_Users"
        FOREIGN KEY ("UserId")
        REFERENCES "Users"("Id")
        ON DELETE RESTRICT 
        ON UPDATE CASCADE
);

-- Комментарии к таблице
COMMENT ON TABLE "Students" IS 'Таблица студентов. Содержит дополнительную информацию о студентах: номер группы.';

-- Комментарии к столбцам
COMMENT ON COLUMN "Students"."Id" IS 'Уникальный идентификатор студента';
COMMENT ON COLUMN "Students"."UserId" IS 'Идентификатор пользователя-студента (внешний ключ к таблице Users)';
COMMENT ON COLUMN "Students"."Group" IS 'Номер группы студента (формат: XXXX, где первая цифра - факультет, вторая - курс, последние две - номер группы, например: 4411)';
COMMENT ON COLUMN "Students"."CreatedAt" IS 'Дата и время создания записи о студенте';
COMMENT ON COLUMN "Students"."UpdatedAt" IS 'Дата и время последнего обновления записи о студенте';
