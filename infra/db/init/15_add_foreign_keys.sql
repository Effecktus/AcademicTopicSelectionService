-- Добавление внешних ключей, которые не были созданы вместе с таблицами
-- Выполняется после создания всех основных таблиц

-- Добавить обратную связь в Departments (HeadId -> Users)
-- Это циклическая зависимость, поэтому FK добавляется отдельно
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint 
        WHERE conname = 'FK_Departments_Users_Head'
    ) THEN
        ALTER TABLE "Departments"
        ADD CONSTRAINT "FK_Departments_Users_Head"
            FOREIGN KEY ("HeadId") 
            REFERENCES "Users"("Id") 
            ON DELETE SET NULL 
            ON UPDATE CASCADE;
    END IF;
END $$;

COMMENT ON COLUMN "Departments"."HeadId" IS 'Ссылка на пользователя - заведующего кафедрой';