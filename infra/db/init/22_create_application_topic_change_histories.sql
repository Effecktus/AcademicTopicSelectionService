-- История изменений названия и описания темы по заявке (доработка в OnEditing).
-- Зависит от: StudentApplications, Users

DROP TABLE IF EXISTS "ApplicationTopicChangeHistories" CASCADE;

CREATE TABLE "ApplicationTopicChangeHistories" (
    "Id" UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    "ApplicationId" UUID NOT NULL,
    "ChangedByUserId" UUID NOT NULL,
    "ChangeKind" CITEXT NOT NULL,
    "NewValue" TEXT NULL,
    "CreatedAt" TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" TIMESTAMPTZ NULL,

    CONSTRAINT "CK_ApplicationTopicChangeHistories_ChangeKind"
        CHECK ("ChangeKind" IN ('TopicTitle', 'TopicDescription')),

    CONSTRAINT "FK_ApplicationTopicChangeHistories_StudentApplications"
        FOREIGN KEY ("ApplicationId")
        REFERENCES "StudentApplications"("Id")
        ON DELETE RESTRICT
        ON UPDATE CASCADE,

    CONSTRAINT "FK_ApplicationTopicChangeHistories_Users"
        FOREIGN KEY ("ChangedByUserId")
        REFERENCES "Users"("Id")
        ON DELETE RESTRICT
        ON UPDATE CASCADE
);

COMMENT ON TABLE "ApplicationTopicChangeHistories" IS 'История изменений заявки: правки названия и описания темы при доработке (OnEditing).';
COMMENT ON COLUMN "ApplicationTopicChangeHistories"."ApplicationId" IS 'Идентификатор заявки';
COMMENT ON COLUMN "ApplicationTopicChangeHistories"."ChangedByUserId" IS 'Пользователь, внёсший изменение';
COMMENT ON COLUMN "ApplicationTopicChangeHistories"."ChangeKind" IS 'TopicTitle или TopicDescription';
COMMENT ON COLUMN "ApplicationTopicChangeHistories"."NewValue" IS 'Значение после изменения';
COMMENT ON COLUMN "ApplicationTopicChangeHistories"."CreatedAt" IS 'Момент записи';
COMMENT ON COLUMN "ApplicationTopicChangeHistories"."UpdatedAt" IS 'Момент последнего обновления записи';
