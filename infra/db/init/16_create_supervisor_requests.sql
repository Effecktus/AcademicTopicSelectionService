-- Создание таблицы SupervisorRequests (запросы на выбор научного руководителя)
-- Зависит от: Students, Users, ApplicationStatuses

DROP TABLE IF EXISTS "SupervisorRequests" CASCADE;

CREATE TABLE "SupervisorRequests" (
    "Id" UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    "StudentId" UUID NOT NULL,
    "TeacherUserId" UUID NOT NULL,
    "StatusId" UUID NOT NULL,
    "Comment" TEXT NULL,
    "CreatedAt" TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" TIMESTAMPTZ NULL,

    CONSTRAINT "FK_SupervisorRequests_Students"
        FOREIGN KEY ("StudentId")
        REFERENCES "Students"("Id")
        ON DELETE RESTRICT
        ON UPDATE CASCADE,

    CONSTRAINT "FK_SupervisorRequests_Users_Teacher"
        FOREIGN KEY ("TeacherUserId")
        REFERENCES "Users"("Id")
        ON DELETE RESTRICT
        ON UPDATE CASCADE,

    CONSTRAINT "FK_SupervisorRequests_ApplicationStatuses"
        FOREIGN KEY ("StatusId")
        REFERENCES "ApplicationStatuses"("Id")
        ON DELETE RESTRICT
        ON UPDATE CASCADE
);

CREATE INDEX "IX_SupervisorRequests_StudentId" ON "SupervisorRequests" ("StudentId");
CREATE INDEX "IX_SupervisorRequests_TeacherUserId" ON "SupervisorRequests" ("TeacherUserId");
CREATE INDEX "IX_SupervisorRequests_StudentId_TeacherUserId" ON "SupervisorRequests" ("StudentId", "TeacherUserId");
CREATE INDEX "IX_SupervisorRequests_StatusId" ON "SupervisorRequests" ("StatusId");
