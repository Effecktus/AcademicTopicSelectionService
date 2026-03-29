-- Создание справочника ученых степеней

DROP TABLE IF EXISTS "AcademicDegrees" CASCADE;

CREATE TABLE "AcademicDegrees" (
    "Id" UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    "CodeName" CITEXT NOT NULL UNIQUE,
    "DisplayName" VARCHAR(100) NOT NULL,
    "ShortName" VARCHAR(50) NULL,
    "CreatedAt" TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" TIMESTAMPTZ NULL,

    CONSTRAINT "CK_AcademicDegrees_CodeName_NotEmpty" CHECK (length(btrim("CodeName"::text)) > 0),
    CONSTRAINT "CK_AcademicDegrees_DisplayName_NotEmpty" CHECK (length(btrim("DisplayName")) > 0),
    CONSTRAINT "CK_AcademicDegrees_ShortName_NotEmpty" CHECK ("ShortName" IS NULL OR length(btrim("ShortName")) > 0)
);

-- Вставка начальных значений
INSERT INTO "AcademicDegrees" ("CodeName", "DisplayName", "ShortName") VALUES
-- Без степени
('None', 'Без степени', NULL),
-- Доктор наук
('DoctorOfBiologicalSciences', 'доктор биологических наук', 'д-р биол. наук'),
('DoctorOfVeterinarySciences', 'доктор ветеринарных наук', 'д-р ветеринар. наук'),
('DoctorOfMilitarySciences', 'доктор военных наук', 'д-р воен. наук'),
('DoctorOfGeographicalSciences', 'доктор географических наук', 'д-р геогр. наук'),
('DoctorOfArtHistory', 'доктор искусствоведения', 'д-р искусствоведения'),
('DoctorOfHistoricalSciences', 'доктор исторических наук', 'д-р ист. наук'),
('DoctorOfCulturalStudies', 'доктор культурологии', 'д-р культурологии'),
('DoctorOfMedicalSciences', 'доктор медицинских наук', 'д-р мед. наук'),
('DoctorOfPedagogicalSciences', 'доктор педагогических наук', 'д-р пед. наук'),
('DoctorOfPoliticalSciences', 'доктор политических наук', 'д-р полит. наук'),
('DoctorOfPsychologicalSciences', 'доктор психологических наук', 'д-р психол. наук'),
('DoctorOfAgriculturalSciences', 'доктор сельскохозяйственных наук', 'д-р с.-х. наук'),
('DoctorOfSociologicalSciences', 'доктор социологических наук', 'д-р социол. наук'),
('DoctorOfTechnicalSciences', 'доктор технических наук', 'д-р техн. наук'),
('DoctorOfPhysicsAndMathematics', 'доктор физико-математических наук', 'д-р физ.-мат. наук'),
('DoctorOfPhilologicalSciences', 'доктор филологических наук', 'д-р филол. наук'),
('DoctorOfPhilosophicalSciences', 'доктор философских наук', 'д-р филос. наук'),
('DoctorOfChemicalSciences', 'доктор химических наук', 'д-р хим. наук'),
('DoctorOfEconomicSciences', 'доктор экономических наук', 'д-р экон. наук'),
('DoctorOfLegalSciences', 'доктор юридических наук', 'д-р юрид. наук'),
-- Кандидат наук
('CandidateOfBiologicalSciences', 'кандидат биологических наук', 'канд. биол. наук'),
('CandidateOfVeterinarySciences', 'кандидат ветеринарных наук', 'канд. ветеринар. наук'),
('CandidateOfMilitarySciences', 'кандидат военных наук', 'канд. воен. наук'),
('CandidateOfGeographicalSciences', 'кандидат географических наук', 'канд. геогр. наук'),
('CandidateOfArtHistory', 'кандидат искусствоведения', 'канд. искусствоведения'),
('CandidateOfHistoricalSciences', 'кандидат исторических наук', 'канд. ист. наук'),
('CandidateOfCulturalStudies', 'кандидат культурологии', 'канд. культурологии'),
('CandidateOfMedicalSciences', 'кандидат медицинских наук', 'канд. мед. наук'),
('CandidateOfPedagogicalSciences', 'кандидат педагогических наук', 'канд. пед. наук'),
('CandidateOfPoliticalSciences', 'кандидат политических наук', 'канд. полит. наук'),
('CandidateOfPsychologicalSciences', 'кандидат психологических наук', 'канд. психол. наук'),
('CandidateOfAgriculturalSciences', 'кандидат сельскохозяйственных наук', 'канд. с.-х. наук'),
('CandidateOfSociologicalSciences', 'кандидат социологических наук', 'канд. социол. наук'),
('CandidateOfTechnicalSciences', 'кандидат технических наук', 'канд. техн. наук'),
('CandidateOfPhysicsAndMathematics', 'кандидат физико-математических наук', 'канд. физ.-мат. наук'),
('CandidateOfPhilologicalSciences', 'кандидат филологических наук', 'канд. филол. наук'),
('CandidateOfPhilosophicalSciences', 'кандидат философских наук', 'канд. филос. наук'),
('CandidateOfChemicalSciences', 'кандидат химических наук', 'канд. хим. наук'),
('CandidateOfEconomicSciences', 'кандидат экономических наук', 'канд. экон. наук'),
('CandidateOfLegalSciences', 'кандидат юридических наук', 'канд. юрид. наук');

-- Комментарии к таблице
COMMENT ON TABLE "AcademicDegrees" IS 'Справочник ученых степеней. Содержит системные, отображаемые и сокращенные названия степеней.';

-- Комментарии к столбцам
COMMENT ON COLUMN "AcademicDegrees"."Id" IS 'Уникальный идентификатор ученой степени';
COMMENT ON COLUMN "AcademicDegrees"."CodeName" IS 'Системное значение степени (для кода), регистронезависимо';
COMMENT ON COLUMN "AcademicDegrees"."DisplayName" IS 'Отображаемое значение степени (для пользовательского интерфейса)';
COMMENT ON COLUMN "AcademicDegrees"."ShortName" IS 'Сокращенное название степени (для отображения в кратких формах)';
COMMENT ON COLUMN "AcademicDegrees"."CreatedAt" IS 'Дата и время создания записи о степени';
COMMENT ON COLUMN "AcademicDegrees"."UpdatedAt" IS 'Дата и время последнего обновления записи о степени';
