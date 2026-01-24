-- Создание таблицы Teachers (Преподаватели)
-- Зависит от: Users (03_create_users.sql)

DROP TABLE IF EXISTS "Teachers" CASCADE;

CREATE TABLE "Teachers" (
    "Id" UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    "UserId" UUID NOT NULL UNIQUE,
    "MaxStudentsLimit" INTEGER NULL,
    "AcademicDegree" VARCHAR(100) NULL,
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" TIMESTAMP NULL,
    
    CONSTRAINT "FK_Teachers_Users"
        FOREIGN KEY ("UserId")
        REFERENCES "Users"("Id")
        ON DELETE RESTRICT 
        ON UPDATE CASCADE
);

-- Комментарии
COMMENT ON TABLE "Teachers" IS 'Таблица преподавателей';
COMMENT ON COLUMN "Teachers"."Id" IS 'Уникальный идентификатор преподавателя';
COMMENT ON COLUMN "Teachers"."UserId" IS 'Ссылка на пользователя (1:1 связь)';
COMMENT ON COLUMN "Teachers"."MaxStudentsLimit" IS 'Лимит студентов, которых может взять преподаватель';
COMMENT ON COLUMN "Teachers"."AcademicDegree" IS 'Ученая степень преподавателя';
COMMENT ON COLUMN "Teachers"."CreatedAt" IS 'Дата и время создания записи';
COMMENT ON COLUMN "Teachers"."UpdatedAt" IS 'Дата и время последнего обновления записи (обновляется автоматически триггером)';