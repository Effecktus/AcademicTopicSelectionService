-- Создание справочника ролей пользователей

DROP TABLE IF EXISTS "UserRoles" CASCADE;

CREATE TABLE "UserRoles" (
    "Id" UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    "Name" CITEXT NOT NULL UNIQUE,           -- Системное имя (для кода), регистронезависимо
    "DisplayName" VARCHAR(100) NOT NULL,     -- Отображаемое имя (для UI)
    "CreatedAt" TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" TIMESTAMPTZ NULL,

    CONSTRAINT "CK_UserRoles_Name_NotEmpty" CHECK (length(btrim("Name"::text)) > 0),
    CONSTRAINT "CK_UserRoles_DisplayName_NotEmpty" CHECK (length(btrim("DisplayName")) > 0)
);

-- Вставка начальных значений
INSERT INTO "UserRoles" ("Name", "DisplayName") VALUES
('Student', 'Студент'),
('Teacher', 'Преподаватель'),
('DepartmentHead', 'Заведующий кафедрой'),
('Admin', 'Администратор');

-- Комментарии к таблице
COMMENT ON TABLE "UserRoles" IS 'Справочник ролей пользователей системы. Содержит системные и отображаемые названия ролей.';

-- Комментарии к столбцам
COMMENT ON COLUMN "UserRoles"."Id" IS 'Уникальный идентификатор роли пользователя';
COMMENT ON COLUMN "UserRoles"."Name" IS 'Системное значение роли (для кода), регистронезависимо';
COMMENT ON COLUMN "UserRoles"."DisplayName" IS 'Отображаемое значение роли (для пользовательского интерфейса)';
COMMENT ON COLUMN "UserRoles"."CreatedAt" IS 'Дата и время создания записи о роли';
COMMENT ON COLUMN "UserRoles"."UpdatedAt" IS 'Дата и время последнего обновления записи о роли';
