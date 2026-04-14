-- Индекс для быстрого поиска заявок студента по статусу
CREATE INDEX IF NOT EXISTS "IX_StudentApplications_StudentId_StatusId"
    ON "StudentApplications" ("StudentId", "StatusId");

-- Примечание: защита от конкурентных заявок на одну тему (два студента
-- одновременно подают на одну тему) реализована на уровне приложения:
--   StudentApplicationsService.CreateAsync → HasActiveApplicationOnTopicAsync
-- в рамках транзакции. PostgreSQL не позволяет использовать подзапрос
-- в предикате partial unique index, поэтому на уровне БД этот constraint
-- опущен — полагается на транзакционную защиту в сервисе.
