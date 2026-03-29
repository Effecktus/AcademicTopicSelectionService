-- Создание справочника учебных групп
-- Зависит от: ничего
-- Должен быть выполнен ДО создания таблицы Students (12_create_students.sql)

DROP TABLE IF EXISTS "StudyGroups" CASCADE;

CREATE TABLE "StudyGroups" (
    "Id" UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    "CodeName" INTEGER NOT NULL UNIQUE,
    "CreatedAt" TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" TIMESTAMPTZ NULL,

    CONSTRAINT "CK_StudyGroups_CodeName_Range" CHECK ("CodeName" BETWEEN 1000 AND 9999)
);

-- Комментарии к таблице
COMMENT ON TABLE "StudyGroups" IS 'Справочник учебных групп. Содержит номера групп в формате XXXX (факультет, курс, номер).';

-- Комментарии к столбцам
COMMENT ON COLUMN "StudyGroups"."Id" IS 'Уникальный идентификатор группы';
COMMENT ON COLUMN "StudyGroups"."CodeName" IS 'Номер учебной группы (формат: XXXX, где первая цифра - факультет, вторая - курс, последние две - номер группы, например: 4411)';
COMMENT ON COLUMN "StudyGroups"."CreatedAt" IS 'Дата и время создания записи о группе';
COMMENT ON COLUMN "StudyGroups"."UpdatedAt" IS 'Дата и время последнего обновления записи о группе';
