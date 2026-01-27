-- Создание таблицы GraduateWorks (Выпускные квалификационные работы)
-- Зависит от: Students, Teachers

DROP TABLE IF EXISTS "GraduateWorks" CASCADE;

CREATE TABLE "GraduateWorks" (
    "Id" UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    "Title" CITEXT NOT NULL,
    "StudentId" UUID NOT NULL,
    "TeacherId" UUID NOT NULL,
    "Year" INTEGER NOT NULL,
    "Grade" INTEGER NOT NULL,
    "CommissionMembers" TEXT NOT NULL,
    "FilePath" TEXT NOT NULL,
    "PresentationPath" TEXT NULL,
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" TIMESTAMP NULL,
    
    CONSTRAINT "CK_GraduateWorks_Title_NotEmpty" CHECK (length(btrim("Title"::text)) > 0),
    CONSTRAINT "CK_GraduateWorks_Year_Range" CHECK ("Year" BETWEEN 2000 AND 2100),
    CONSTRAINT "CK_GraduateWorks_Grade_Range" CHECK ("Grade" BETWEEN 0 AND 100),
    CONSTRAINT "CK_GraduateWorks_CommissionMembers_NotEmpty" CHECK (length(btrim("CommissionMembers")) > 0),
    CONSTRAINT "CK_GraduateWorks_FilePath_NotEmpty" CHECK (length(btrim("FilePath")) > 0),
    CONSTRAINT "CK_GraduateWorks_PresentationPath_NotEmpty" CHECK ("PresentationPath" IS NULL OR length(btrim("PresentationPath")) > 0),


    CONSTRAINT "FK_GraduateWorks_Students"
        FOREIGN KEY ("StudentId")
        REFERENCES "Students"("Id")
        ON DELETE RESTRICT 
        ON UPDATE CASCADE,
    
    CONSTRAINT "FK_GraduateWorks_Teachers"
        FOREIGN KEY ("TeacherId")
        REFERENCES "Teachers"("Id")
        ON DELETE RESTRICT 
        ON UPDATE CASCADE
);

-- Комментарии к таблице
COMMENT ON TABLE "GraduateWorks" IS 'Таблица выпускных квалификационных работ (ВКР). Содержит информацию о завершенных работах студентов: название, оценки, файлы работ и презентаций, состав комиссии.';

-- Комментарии к столбцам
COMMENT ON COLUMN "GraduateWorks"."Id" IS 'Уникальный идентификатор выпускной квалификационной работы';
COMMENT ON COLUMN "GraduateWorks"."Title" IS 'Название выпускной квалификационной работы (регистронезависимо)';
COMMENT ON COLUMN "GraduateWorks"."StudentId" IS 'Идентификатор студента, выполнившего работу (внешний ключ к таблице Students)';
COMMENT ON COLUMN "GraduateWorks"."TeacherId" IS 'Идентификатор преподавателя-руководителя работы (внешний ключ к таблице Teachers)';
COMMENT ON COLUMN "GraduateWorks"."Year" IS 'Учебный год, в котором была выполнена работа';
COMMENT ON COLUMN "GraduateWorks"."Grade" IS 'Оценка за работу (от 0 до 100 баллов)';
COMMENT ON COLUMN "GraduateWorks"."CommissionMembers" IS 'Состав комиссии, оценивавшей работу (текстовое описание)';
COMMENT ON COLUMN "GraduateWorks"."FilePath" IS 'Путь к файлу выпускной квалификационной работы (не может быть пустым)';
COMMENT ON COLUMN "GraduateWorks"."PresentationPath" IS 'Путь к файлу презентации работы (опционально)';
COMMENT ON COLUMN "GraduateWorks"."CreatedAt" IS 'Дата и время создания записи о работе';
COMMENT ON COLUMN "GraduateWorks"."UpdatedAt" IS 'Дата и время последнего обновления записи о работе';