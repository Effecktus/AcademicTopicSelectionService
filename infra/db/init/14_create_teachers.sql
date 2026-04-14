-- Создание таблицы Teachers (Преподаватели)
-- Зависит от: Users, AcademicDegrees, AcademicTitles, Positions

DROP TABLE IF EXISTS "Teachers" CASCADE;

CREATE TABLE "Teachers" (
    "Id" UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    "UserId" UUID NOT NULL UNIQUE,
    "MaxStudentsLimit" INTEGER NULL,
    "AcademicDegreeId" UUID NOT NULL,
    "AcademicTitleId" UUID NOT NULL,
    "PositionId" UUID NOT NULL,
    "CreatedAt" TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" TIMESTAMPTZ NULL,
    
    CONSTRAINT "CK_Teachers_MaxStudentsLimit_Positive" CHECK ("MaxStudentsLimit" IS NULL OR "MaxStudentsLimit" > 0),

    CONSTRAINT "FK_Teachers_Users"
        FOREIGN KEY ("UserId")
        REFERENCES "Users"("Id")
        ON DELETE RESTRICT 
        ON UPDATE CASCADE,
    
    CONSTRAINT "FK_Teachers_AcademicDegrees"
        FOREIGN KEY ("AcademicDegreeId")
        REFERENCES "AcademicDegrees"("Id")
        ON DELETE RESTRICT
        ON UPDATE CASCADE,
    
    CONSTRAINT "FK_Teachers_AcademicTitles"
        FOREIGN KEY ("AcademicTitleId")
        REFERENCES "AcademicTitles"("Id")
        ON DELETE RESTRICT
        ON UPDATE CASCADE,
    
    CONSTRAINT "FK_Teachers_Positions"
        FOREIGN KEY ("PositionId")
        REFERENCES "Positions"("Id")
        ON DELETE RESTRICT
        ON UPDATE CASCADE
);

-- Комментарии к таблице
COMMENT ON TABLE "Teachers" IS 'Таблица преподавателей. Содержит дополнительную информацию о преподавателях: академические данные и лимит студентов.';

-- Комментарии к столбцам
COMMENT ON COLUMN "Teachers"."Id" IS 'Уникальный идентификатор преподавателя';
COMMENT ON COLUMN "Teachers"."UserId" IS 'Идентификатор пользователя-преподавателя (внешний ключ к таблице Users)';
COMMENT ON COLUMN "Teachers"."MaxStudentsLimit" IS 'Максимальное количество студентов, которых может взять преподаватель для руководства ВКР';
COMMENT ON COLUMN "Teachers"."AcademicDegreeId" IS 'Идентификатор ученой степени преподавателя (внешний ключ к таблице AcademicDegrees)';
COMMENT ON COLUMN "Teachers"."AcademicTitleId" IS 'Идентификатор ученого звания преподавателя (внешний ключ к таблице AcademicTitles)';
COMMENT ON COLUMN "Teachers"."PositionId" IS 'Идентификатор должности преподавателя (внешний ключ к таблице Positions)';
COMMENT ON COLUMN "Teachers"."CreatedAt" IS 'Дата и время создания записи о преподавателе';
COMMENT ON COLUMN "Teachers"."UpdatedAt" IS 'Дата и время последнего обновления записи о преподавателе';
