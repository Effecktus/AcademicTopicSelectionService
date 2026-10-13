-- Тестовые данные для SQL Server — T-SQL-версия 99_seed_test_data.sql (PostgreSQL).
-- Набор данных тот же; при изменении одного файла обновляйте и второй.
-- Схема, справочники и учётная запись администратора создаются миграциями EF Core —
-- этот скрипт их не трогает. Запуск (из infra/docker):
--   docker compose -f compose.mssql.yml --profile seed run --rm db-seed
-- Повторный запуск пересоздаёт тестовые данные с нуля.
--
-- Учётные записи — как в 99_seed_test_data.sql. Хеши паролей готовые (bcrypt, cost 10),
-- потому что в T-SQL нет функции crypt():
--   TestPassword123!  — серийные учётные записи
--   Password123!      — персональные учётные записи

-- Нужно для DML по таблицам с фильтрованными индексами (ChatMessages); у sqlcmd по умолчанию OFF.
SET QUOTED_IDENTIFIER ON;
SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRANSACTION;

DECLARE @TestPasswordHash nvarchar(255) = N'$2a$10$W6YiUWQopmXifrCbx8ZPUuWU8Vr/70wkfKXCooRHLAXTfVq78XO76';
DECLARE @PersonalPasswordHash nvarchar(255) = N'$2a$10$jP2vn.t5NtiAkd2md8ZzUuhCOBnsKUsaDK8LOdSyzLzTF7um5nM/G';

DECLARE @RoleTeacher uniqueidentifier = (SELECT [Id] FROM [UserRoles] WHERE [CodeName] = N'Teacher');
DECLARE @RoleStudent uniqueidentifier = (SELECT [Id] FROM [UserRoles] WHERE [CodeName] = N'Student');
DECLARE @RoleHead uniqueidentifier = (SELECT [Id] FROM [UserRoles] WHERE [CodeName] = N'DepartmentHead');
DECLARE @CreatorTeacher uniqueidentifier = (SELECT [Id] FROM [TopicCreatorTypes] WHERE [CodeName] = N'Teacher');
DECLARE @CreatorStudent uniqueidentifier = (SELECT [Id] FROM [TopicCreatorTypes] WHERE [CodeName] = N'Student');
DECLARE @TopicActive uniqueidentifier = (SELECT [Id] FROM [TopicStatuses] WHERE [CodeName] = N'Active');
DECLARE @TopicInactive uniqueidentifier = (SELECT [Id] FROM [TopicStatuses] WHERE [CodeName] = N'Inactive');
DECLARE @AppApprovedBySupervisor uniqueidentifier = (SELECT [Id] FROM [ApplicationStatuses] WHERE [CodeName] = N'ApprovedBySupervisor');
DECLARE @AppPending uniqueidentifier = (SELECT [Id] FROM [ApplicationStatuses] WHERE [CodeName] = N'Pending');
DECLARE @AppPendingHead uniqueidentifier = (SELECT [Id] FROM [ApplicationStatuses] WHERE [CodeName] = N'PendingDepartmentHead');
DECLARE @ActionPending uniqueidentifier = (SELECT [Id] FROM [ApplicationActionStatuses] WHERE [CodeName] = N'Pending');
DECLARE @ActionApproved uniqueidentifier = (SELECT [Id] FROM [ApplicationActionStatuses] WHERE [CodeName] = N'Approved');
DECLARE @GraduateWorkCompleted uniqueidentifier = (SELECT [Id] FROM [GraduateWorkStatuses] WHERE [CodeName] = N'Completed');

-- Очистка тестовых данных. Обязательное наполнение (справочники и администратор) сохраняется.
-- TRUNCATE в SQL Server запрещён для таблиц, на которые ссылаются FK, поэтому DELETE в порядке зависимостей.
DELETE FROM [GraduateWorks];
DELETE FROM [Notifications];
DELETE FROM [ChatMessages];
DELETE FROM [ApplicationTopicChangeHistories];
DELETE FROM [ApplicationActions];
DELETE FROM [StudentApplications];
DELETE FROM [SupervisorRequests];
DELETE FROM [Topics];
DELETE FROM [Students];
DELETE FROM [StudyGroups];
DELETE FROM [Teachers];
-- Сначала кафедры: FK Departments.HeadId -> Users без каскада, а Users.DepartmentId обнулится сам (SET NULL).
DELETE FROM [Departments];
-- Id администратора — SystemSeed.Users.Admin в backend
DELETE FROM [Users] WHERE [Id] <> '037b31dc-d13a-423e-8b0b-983494f8a7f9';

-- ---------------------------------------------------------------------
-- Departments (4)
INSERT INTO [Departments] ([CodeName], [DisplayName])
VALUES
    (N'Department01', N'Прикладная математика и информатика'),
    (N'Department02', N'Автоматизированные системы обработки информации и управления'),
    (N'Department03', N'Компьютерные сети'),
    (N'Department04', N'Динамика процессов и управления');

-- ---------------------------------------------------------------------
-- Users: преподаватели (серия, 10 чел.)
INSERT INTO [Users] ([Email], [PasswordHash], [FirstName], [LastName], [MiddleName], [RoleId], [DepartmentId], [IsActive])
SELECT
    td.email,
    @TestPasswordHash,
    td.first_name,
    td.last_name,
    td.middle_name,
    @RoleTeacher,
    (SELECT [Id] FROM [Departments] ORDER BY [CodeName] OFFSET (
        CASE
            WHEN td.pos <= 3 THEN 0
            WHEN td.pos <= 6 THEN 1
            WHEN td.pos <= 8 THEN 2
            ELSE 3
        END
    ) ROWS FETCH NEXT 1 ROWS ONLY),
    1
FROM (VALUES
    (1,  N'teacher01@example.com', N'Алексей',   N'Смирнов',    N'Николаевич'),
    (2,  N'teacher02@example.com', N'Ольга',     N'Кузнецова',  N'Дмитриевна'),
    (3,  N'teacher03@example.com', N'Михаил',    N'Попов',      N'Андреевич'),
    (4,  N'teacher04@example.com', N'Артём',     N'Новиков',    N'Владимирович'),
    (5,  N'teacher05@example.com', N'Марина',    N'Лебедева',   N'Игоревна'),
    (6,  N'teacher06@example.com', N'Виктор',    N'Соколов',    N'Петрович'),
    (7,  N'teacher07@example.com', N'Евгений',   N'Михайлов',   N'Александрович'),
    (8,  N'teacher08@example.com', N'Наталья',   N'Морозова',   N'Геннадьевна'),
    (9,  N'teacher09@example.com', N'Павел',     N'Фёдоров',    N'Константинович'),
    (10, N'teacher10@example.com', N'Игорь',     N'Волков',     N'Дмитриевич')
) AS td(pos, email, first_name, last_name, middle_name);

-- Users: студенты (серия, 20 чел.)
INSERT INTO [Users] ([Email], [PasswordHash], [FirstName], [LastName], [MiddleName], [RoleId], [DepartmentId], [IsActive])
SELECT
    sd.email,
    @TestPasswordHash,
    sd.first_name,
    sd.last_name,
    sd.middle_name,
    @RoleStudent,
    (SELECT [Id] FROM [Departments] ORDER BY [CodeName] OFFSET ((sd.pos - 1) / 5) ROWS FETCH NEXT 1 ROWS ONLY),
    1
FROM (VALUES
    (1,  N'student01@example.com', N'Иван',      N'Захаров',    N'Алексеевич'),
    (2,  N'student02@example.com', N'Анна',      N'Яковлева',   N'Сергеевна'),
    (3,  N'student03@example.com', N'Роман',     N'Гусев',      N'Дмитриевич'),
    (4,  N'student04@example.com', N'Мария',     N'Никитина',   N'Александровна'),
    (5,  N'student05@example.com', N'Артём',     N'Степанов',   N'Павлович'),
    (6,  N'student06@example.com', N'Денис',     N'Орлов',      N'Николаевич'),
    (7,  N'student07@example.com', N'Кирилл',    N'Сергеев',    N'Олегович'),
    (8,  N'student08@example.com', N'Екатерина', N'Кузьмина',   N'Андреевна'),
    (9,  N'student09@example.com', N'Максим',    N'Козлов',     N'Игоревич'),
    (10, N'student10@example.com', N'Антон',     N'Белов',      N'Владимирович'),
    (11, N'student11@example.com', N'Алексей',   N'Медведев',   N'Юрьевич'),
    (12, N'student12@example.com', N'Виктория',  N'Фёдорова',   N'Евгеньевна'),
    (13, N'student13@example.com', N'Дарья',     N'Петрова',    N'Михайловна'),
    (14, N'student14@example.com', N'Владимир',  N'Егоров',     N'Сергеевич'),
    (15, N'student15@example.com', N'Тимур',     N'Макаров',    N'Олегович'),
    (16, N'student16@example.com', N'Наталья',   N'Борисова',   N'Дмитриевна'),
    (17, N'student17@example.com', N'Дмитрий',   N'Романов',    N'Константинович'),
    (18, N'student18@example.com', N'Полина',    N'Тихонова',   N'Андреевна'),
    (19, N'student19@example.com', N'Алексей',   N'Соловьёв',   N'Иванович'),
    (20, N'student20@example.com', N'Евгений',   N'Громов',     N'Сергеевич')
) AS sd(pos, email, first_name, last_name, middle_name);

-- Users: заведующие кафедр АСОИУ, КС, ДПУ (head02–head04)
INSERT INTO [Users] ([Email], [PasswordHash], [FirstName], [LastName], [MiddleName], [RoleId], [DepartmentId], [IsActive])
SELECT
    hd.email,
    @TestPasswordHash,
    hd.first_name,
    hd.last_name,
    hd.middle_name,
    @RoleHead,
    (SELECT [Id] FROM [Departments] ORDER BY [CodeName] OFFSET (hd.pos - 1) ROWS FETCH NEXT 1 ROWS ONLY),
    1
FROM (VALUES
    (2, N'head02@example.com', N'Константин', N'Власов',     N'Юрьевич'),
    (3, N'head03@example.com', N'Ирина',      N'Щербакова',  N'Борисовна'),
    (4, N'head04@example.com', N'Олег',       N'Климов',     N'Валентинович')
) AS hd(pos, email, first_name, last_name, middle_name);

-- Users: персональные аккаунты
INSERT INTO [Users] ([Email], [PasswordHash], [FirstName], [LastName], [MiddleName], [RoleId], [DepartmentId], [IsActive])
VALUES
    (N'effecktus@yandex.ru', @PersonalPasswordHash, N'Евгений', N'Рогов', N'Борисович', @RoleHead,
     (SELECT [Id] FROM [Departments] WHERE [CodeName] = N'Department01'), 1),
    (N'Aydar.Ilin@norbit.ru', @PersonalPasswordHash, N'Айдар', N'Ильин', N'Альбертович', @RoleTeacher,
     (SELECT [Id] FROM [Departments] WHERE [CodeName] = N'Department01'), 1),
    (N'fqlfh2004@gmail.com', @PersonalPasswordHash, N'Фёдор', N'Чернов', N'Андреевич', @RoleStudent,
     (SELECT [Id] FROM [Departments] WHERE [CodeName] = N'Department01'), 1);

-- Привязка заведующих к кафедрам
UPDATE [Departments] SET [HeadId] = (
    SELECT [Id] FROM [Users] WHERE [Email] = N'effecktus@yandex.ru'
) WHERE [CodeName] = N'Department01';

-- Department02 -> head02@example.com и т.д.
UPDATE d
SET [HeadId] = (
    SELECT TOP 1 u.[Id] FROM [Users] u
    WHERE u.[Email] = CONCAT(N'head', RIGHT(d.[CodeName], 2), N'@example.com')
      AND u.[RoleId] = @RoleHead
)
FROM [Departments] d
WHERE d.[CodeName] IN (N'Department02', N'Department03', N'Department04');

-- ---------------------------------------------------------------------
-- Teachers: все 11 преподавателей
INSERT INTO [Teachers] ([UserId], [MaxStudentsLimit], [AcademicDegreeId], [AcademicTitleId], [PositionId])
SELECT
    u.[Id],
    (6 + (u.gs % 6)),
    (SELECT [Id] FROM [AcademicDegrees] WHERE [CodeName] = (CASE (u.gs % 5)
        WHEN 0 THEN N'None'
        WHEN 1 THEN N'CandidateOfTechnicalSciences'
        WHEN 2 THEN N'CandidateOfEconomicSciences'
        WHEN 3 THEN N'DoctorOfTechnicalSciences'
        ELSE N'DoctorOfEconomicSciences'
    END)),
    (SELECT [Id] FROM [AcademicTitles] WHERE [CodeName] = (CASE (u.gs % 3)
        WHEN 0 THEN N'None'
        WHEN 1 THEN N'AssociateProfessor'
        ELSE N'Professor'
    END)),
    (SELECT [Id] FROM [Positions] WHERE [CodeName] = (CASE (u.gs % 4)
        WHEN 0 THEN N'Assistant'
        WHEN 1 THEN N'SeniorLecturer'
        WHEN 2 THEN N'AssociateProfessor'
        ELSE N'Professor'
    END))
FROM (
    SELECT u.[Id], ROW_NUMBER() OVER (ORDER BY u.[Email]) AS gs
    FROM [Users] u
    WHERE u.[RoleId] = @RoleTeacher
) AS u;

-- ---------------------------------------------------------------------
-- StudyGroups: по 5 групп на кафедру + 1 для демо-студента
-- Схема номера: первая цифра = номер кафедры (1=ПМИ, 2=АСОИУ, 3=КС, 4=ДПУ),
--               вторая = курс (1), две последние = номер группы.
INSERT INTO [StudyGroups] ([CodeName])
VALUES
    (1101), (1102), (1103), (1104), (1105), (1106),  -- ПМИ (+ демо)
    (2101), (2102), (2103), (2104), (2105),           -- АСОИУ
    (3101), (3102), (3103), (3104), (3105),           -- КС
    (4101), (4102), (4103), (4104), (4105);           -- ДПУ

-- Students: 20 серийных (группы по кафедрам)
INSERT INTO [Students] ([UserId], [GroupId])
SELECT
    u.[Id],
    (SELECT [Id] FROM [StudyGroups] WHERE [CodeName] = sg.group_code)
FROM (
    SELECT u.[Id], ROW_NUMBER() OVER (ORDER BY u.[Email]) AS rn
    FROM [Users] u
    WHERE u.[RoleId] = @RoleStudent
      AND u.[Email] LIKE N'student%@example.com'
) u
JOIN (VALUES
    (1, 1101), (2, 1102), (3, 1103), (4, 1104), (5, 1105),
    (6, 2101), (7, 2102), (8, 2103), (9, 2104), (10, 2105),
    (11, 3101), (12, 3102), (13, 3103), (14, 3104), (15, 3105),
    (16, 4101), (17, 4102), (18, 4103), (19, 4104), (20, 4105)
) AS sg(pos, group_code) ON sg.pos = u.rn;

-- Students: демо-студент (группа ПМИ, запасная)
INSERT INTO [Students] ([UserId], [GroupId])
SELECT u.[Id], (SELECT [Id] FROM [StudyGroups] WHERE [CodeName] = 1106)
FROM [Users] u
WHERE u.[Email] = N'fqlfh2004@gmail.com';

-- ---------------------------------------------------------------------
-- Темы преподавателей: по 3 на каждого из 11
-- Порядок преподавателей по email (ROW_NUMBER):
--   1=Aydar.Ilin, 2=teacher01, 3=teacher02, 4=teacher03 (ПМИ)
--   5=teacher04, 6=teacher05, 7=teacher06 (АСОИУ)
--   8=teacher07, 9=teacher08 (КС)
--   10=teacher09, 11=teacher10 (ДПУ)
INSERT INTO [Topics] ([Title], [Description], [CreatorTypeId], [CreatedBy], [StatusId])
SELECT
    tt.title,
    tt.description,
    @CreatorTeacher,
    ts.[UserId],
    @TopicActive
FROM (
    SELECT u.[Id] AS [UserId], ROW_NUMBER() OVER (ORDER BY u.[Email]) AS gs
    FROM [Users] u
    WHERE u.[RoleId] = @RoleTeacher
) ts
JOIN (VALUES
    -- Ильин А.Р. (ПМИ)
    (1,  1, N'Разработка веб-сервиса для хранения и поиска научных публикаций',
            N'Проектирование RESTful API и модели данных для репозитория научных статей с поддержкой полнотекстового поиска. Апробация на корпусе публикаций конференций ВАК.'),
    (1,  2, N'Исследование алгоритмов сжатия данных без потерь на основе энтропийного кодирования',
            N'Сравнительный анализ алгоритмов Huffman, Arithmetic и ANS по скорости и степени компрессии. Реализация и тестирование на разнородных наборах данных.'),
    (1,  3, N'Построение инструментального комплекса для статического анализа программного кода',
            N'Разработка плагина обнаружения потенциальных ошибок и нарушений стиля кода методами AST-анализа. Интеграция с CI/CD-конвейером на базе GitLab.'),
    -- Смирнов А.Н. (ПМИ)
    (2,  1, N'Разработка алгоритма кластеризации данных на основе нейронных сетей',
            N'Исследование методов обучения сверточных и рекуррентных сетей для задач кластеризации неструктурированных данных. Сравнительный анализ точности и вычислительной эффективности.'),
    (2,  2, N'Применение методов машинного обучения для прогнозирования временных рядов',
            N'Разработка и сравнение моделей LSTM, GRU и Transformer для прогнозирования финансовых и метеорологических временных рядов. Оценка качества на публичных датасетах.'),
    (2,  3, N'Исследование алгоритмов поиска кратчайших путей в больших графах',
            N'Оценка производительности алгоритмов A*, Dijkstra и Bidirectional BFS на графах с миллионами вершин. Разработка параллельных версий с использованием многопоточности.'),
    -- Кузнецова О.Д. (ПМИ)
    (3,  1, N'Методы оптимизации задач линейного программирования в логистических системах',
            N'Применение симплекс-метода и метода внутренних точек к задачам оптимизации маршрутов доставки. Разработка инструментального средства с визуализацией решений.'),
    (3,  2, N'Построение системы рекомендаций на основе коллаборативной фильтрации',
            N'Разработка рекомендательной системы на основе матричной факторизации и SVD-разложения. Оценка качества рекомендаций на публичных датасетах MovieLens и Amazon.'),
    (3,  3, N'Применение генетических алгоритмов для решения задач комбинаторной оптимизации',
            N'Реализация генетического алгоритма для задачи коммивояжёра и рюкзака с настраиваемыми операторами отбора, скрещивания и мутации. Сравнение с методом имитации отжига.'),
    -- Попов М.А. (ПМИ)
    (4,  1, N'Разработка системы автоматической классификации текстовых документов',
            N'Построение конвейера NLP-обработки с токенизацией, TF-IDF векторизацией и ансамблем классификаторов. Обучение и тестирование на корпусе русскоязычных документов.'),
    (4,  2, N'Численное моделирование задач оптимального управления параметрами',
            N'Разработка численного решателя вариационных задач оптимального управления на основе метода конечных разностей. Верификация на модельных задачах с аналитическим решением.'),
    (4,  3, N'Построение предсказательных моделей на основе методов регрессионного анализа',
            N'Сравнение регрессионных моделей (линейная, полиномиальная, Ridge, Lasso, Elastic Net) для предсказания технических показателей по историческим данным производственных процессов.'),
    -- Новиков А.В. (АСОИУ)
    (5,  1, N'Разработка системы мониторинга производственных процессов на основе IoT',
            N'Разработка платформы сбора телеметрии с IoT-датчиков на базе MQTT и InfluxDB с визуализацией в Grafana. Апробация на макете производственной линии.'),
    (5,  2, N'Автоматизация бизнес-процессов предприятия с использованием RPA-технологий',
            N'Автоматизация рутинных операций документооборота и отчётности с использованием RPA-фреймворка UiPath. Оценка экономической эффективности внедрения.'),
    (5,  3, N'Проектирование распределённой системы управления складскими запасами',
            N'Реализация распределённой системы учёта и управления складом с мобильным терминалом и синхронизацией данных в реальном времени через WebSocket.'),
    -- Лебедева М.И. (АСОИУ)
    (6,  1, N'Разработка SCADA-системы для управления технологическим оборудованием цеха',
            N'Разработка системы визуализации и управления оборудованием производственного цеха на базе WinCC. Моделирование аварийных сценариев и алгоритмов аварийной защиты.'),
    (6,  2, N'Автоматизация тестирования корпоративных информационных систем',
            N'Разработка фреймворка автоматизированного тестирования REST API корпоративных систем с генерацией отчётов о покрытии и интеграцией в CI-конвейер.'),
    (6,  3, N'Проектирование микросервисной архитектуры для ERP-платформы',
            N'Проектирование микросервисной архитектуры с API Gateway, service discovery и distributed tracing. Нагрузочное тестирование и оценка горизонтальной масштабируемости.'),
    -- Соколов В.П. (АСОИУ)
    (7,  1, N'Разработка системы управления данными промышленного предприятия',
            N'Разработка хранилища данных и ETL-процессов для консолидации промышленных данных из гетерогенных источников. Построение аналитических отчётов и OLAP-кубов.'),
    (7,  2, N'Разработка системы интеллектуального управления энергопотреблением здания',
            N'Разработка системы сбора данных с датчиков и алгоритмов прогнозирования энергонагрузки здания. Оценка потенциала снижения затрат на электроэнергию.'),
    (7,  3, N'Автоматизация процессов документооборота в государственных организациях',
            N'Проектирование и внедрение системы электронного документооборота для государственного ведомства на базе платформы 1С:Документооборот. Анализ соответствия требованиям регулятора.'),
    -- Михайлов Е.А. (КС)
    (8,  1, N'Анализ и оптимизация протоколов маршрутизации в программно-определяемых сетях',
            N'Анализ производительности протоколов OSPF, EIGRP и BGP в топологиях SDN. Разработка алгоритма динамической балансировки нагрузки на базе контроллера OpenDaylight.'),
    (8,  2, N'Разработка системы обнаружения вторжений на основе анализа сетевого трафика',
            N'Построение IDS на основе методов машинного обучения с анализом сетевых потоков. Тестирование на наборах данных NSL-KDD и CICIDS2017. Сравнение с сигнатурным методом.'),
    (8,  3, N'Исследование методов обеспечения качества обслуживания в беспроводных сетях',
            N'Исследование механизмов QoS (DSCP, DiffServ, 802.11e) в гетерогенных беспроводных сетях. Моделирование в GNS3 с оценкой задержки и джиттера для мультимедиатрафика.'),
    -- Морозова Н.Г. (КС)
    (9,  1, N'Проектирование отказоустойчивой сетевой инфраструктуры для центра обработки данных',
            N'Проектирование сети ЦОД с резервированием на уровнях L2/L3, VRRP и port-channel. Расчёт метрик доступности, RTO и RPO в соответствии с требованиями Tier III.'),
    (9,  2, N'Применение технологии блокчейн для обеспечения безопасности сетевых транзакций',
            N'Разработка смарт-контракта на Ethereum для верификации целостности сетевых транзакций. Сравнение производительности с традиционными PKI-решениями по пропускной способности.'),
    (9,  3, N'Разработка системы мониторинга и управления сетевой инфраструктурой предприятия',
            N'Разработка системы SNMP/NetFlow-мониторинга с автоматической инвентаризацией и уведомлениями об инцидентах. Развёртывание на базе LibreNMS с интеграцией в ITSM-систему.'),
    -- Фёдоров П.К. (ДПУ)
    (10, 1, N'Моделирование динамических систем управления с переменной структурой',
            N'Исследование переключаемых систем управления с использованием метода функций Ляпунова. Синтез условий устойчивости и апробация на имитационной модели в Simulink.'),
    (10, 2, N'Синтез робастных регуляторов для нелинейных объектов управления',
            N'Синтез ПИД-регулятора с робастными настройками для нелинейных объектов с параметрической неопределённостью. Верификация методом μ-анализа и имитационного моделирования.'),
    (10, 3, N'Разработка системы адаптивного управления манипуляционным роботом',
            N'Разработка алгоритма адаптивного управления трёхзвенным манипулятором с компенсацией неизвестных нагрузок. Реализация на базе ROS 2 и апробация на физическом стенде.'),
    -- Волков И.Д. (ДПУ)
    (11, 1, N'Исследование устойчивости замкнутых систем управления с запаздыванием',
            N'Аналитическое и имитационное исследование устойчивости систем с чистым запаздыванием методом критериев Михайлова и частотных характеристик. Синтез корректирующих звеньев.'),
    (11, 2, N'Оптимальное управление тепловыми процессами в промышленных установках',
            N'Постановка и решение задачи оптимального управления температурным полем нагревательной камеры. Минимизация энергозатрат при соблюдении ограничений на точность регулирования.'),
    (11, 3, N'Применение нечёткой логики в системах автоматического регулирования температуры',
            N'Разработка нечёткого регулятора температуры для термической установки. Сравнение с классическим ПИД-регулятором по критериям перерегулирования, времени регулирования и интегральной ошибки.')
) AS tt(t_seq, topic_idx, title, description) ON tt.t_seq = ts.gs;

-- Темы студентов (серия, 20 чел.)
INSERT INTO [Topics] ([Title], [Description], [CreatorTypeId], [CreatedBy], [StatusId])
SELECT
    st.title,
    st.description,
    @CreatorStudent,
    u.[Id],
    @TopicInactive
FROM (
    SELECT u.[Id], ROW_NUMBER() OVER (ORDER BY u.[Email]) AS rn
    FROM [Users] u
    WHERE u.[RoleId] = @RoleStudent
      AND u.[Email] LIKE N'student%@example.com'
) u
JOIN (VALUES
    -- ПМИ (student01–05)
    (1,  N'Разработка веб-приложения для визуализации многомерных данных',
         N'Проектирование и реализация интерактивного дашборда для визуализации многомерных наборов данных с использованием D3.js и WebGL. Апробация на данных экологического мониторинга.'),
    (2,  N'Исследование методов сжатия изображений с применением вейвлет-преобразования',
         N'Сравнительный анализ алгоритмов сжатия изображений на основе дискретного вейвлет-преобразования. Реализация кодека и оценка соотношения сигнал/шум при различных степенях компрессии.'),
    (3,  N'Создание мобильного приложения для мониторинга физической активности',
         N'Разработка кроссплатформенного мобильного приложения на Flutter для отслеживания физической активности с интеграцией с фитнес-датчиками через BLE. Хранение данных в SQLite.'),
    (4,  N'Разработка системы автоматической генерации тестовых наборов данных',
         N'Разработка фреймворка генерации синтетических тестовых данных с заданными статистическими характеристиками для тестирования алгоритмов машинного обучения.'),
    (5,  N'Применение технологий дополненной реальности в учебном процессе',
         N'Разработка AR-приложения для визуализации трёхмерных математических объектов в учебном процессе на базе Unity AR Foundation. Оценка эффективности усвоения материала.'),
    -- АСОИУ (student06–10)
    (6,  N'Разработка системы учёта и контроля основных средств предприятия',
         N'Проектирование и реализация информационной системы учёта основных средств с поддержкой QR-инвентаризации и интеграцией с 1С:Бухгалтерия через REST API.'),
    (7,  N'Автоматизация составления расписания занятий в высшем учебном заведении',
         N'Разработка алгоритма автоматизированного составления расписания на основе эволюционных методов оптимизации с учётом ограничений аудиторного фонда и нагрузки преподавателей.'),
    (8,  N'Разработка системы электронного документооборота для малого предприятия',
         N'Проектирование и реализация системы электронного документооборота с маршрутизацией согласований, контролем исполнения и хранилищем документов на базе MinIO.'),
    (9,  N'Создание CRM-системы для управления взаимоотношениями с клиентами',
         N'Разработка веб-ориентированной CRM-системы с управлением воронкой продаж, историей взаимодействий и аналитическими отчётами. Интеграция с email и мессенджерами.'),
    (10, N'Разработка системы управления задачами для распределённых команд',
         N'Проектирование и реализация системы управления задачами с поддержкой Kanban-досок, уведомлений в реальном времени через WebSocket и ролевой моделью доступа.'),
    -- КС (student11–15)
    (11, N'Оптимизация конфигурации корпоративной беспроводной сети Wi-Fi',
         N'Анализ текущей топологии Wi-Fi инфраструктуры предприятия и разработка рекомендаций по оптимизации расположения точек доступа, каналов и параметров QoS для повышения пропускной способности.'),
    (12, N'Разработка VPN-решения для безопасного удалённого доступа сотрудников',
         N'Проектирование и развёртывание отказоустойчивого VPN-решения на базе WireGuard с многофакторной аутентификацией и централизованным управлением политиками доступа.'),
    (13, N'Анализ уязвимостей корпоративной сети и разработка мер защиты',
         N'Проведение аудита сетевой инфраструктуры предприятия методами пассивного и активного сканирования. Формирование реестра уязвимостей и плана мероприятий по их устранению.'),
    (14, N'Проектирование сети передачи данных для нового корпуса университета',
         N'Разработка проекта структурированной кабельной системы и активного оборудования для нового учебного корпуса с расчётом пропускной способности и планом IP-адресации.'),
    (15, N'Разработка системы IP-видеонаблюдения для объектов инфраструктуры',
         N'Проектирование и реализация системы IP-видеонаблюдения с централизованным видеосервером, детекцией движения и удалённым доступом через веб-интерфейс.'),
    -- ДПУ (student16–20)
    (16, N'Разработка системы управления автоматизированным складским комплексом',
         N'Разработка алгоритмов оптимального размещения и извлечения грузов для автоматизированного склада с управлением транспортными роботами по стратегии ABC-зонирования.'),
    (17, N'Моделирование системы управления температурным режимом теплицы',
         N'Разработка математической модели тепличного объекта и синтез ПИД-регулятора с компенсацией внешних возмущений. Имитационное моделирование в Matlab/Simulink.'),
    (18, N'Разработка алгоритма управления движением автономного мобильного робота',
         N'Разработка и реализация алгоритма навигации мобильного робота на основе SLAM с объездом препятствий. Апробация на роботизированной платформе в лабораторных условиях.'),
    (19, N'Проектирование системы автоматического контроля качества продукции',
         N'Разработка автоматизированной системы технического зрения для контроля качества продукции на производственной линии на базе нейросетевых детекторов дефектов.'),
    (20, N'Разработка системы стабилизации и управления беспилотным летательным аппаратом',
         N'Синтез алгоритма стабилизации квадрокоптера с компенсацией внешних возмущений на основе PID и LQR-регуляторов. Верификация на имитационной модели и лётных испытаниях.')
) AS st(s_seq, title, description) ON st.s_seq = u.rn;

-- Тема демо-студента
INSERT INTO [Topics] ([Title], [Description], [CreatorTypeId], [CreatedBy], [StatusId])
SELECT
    N'Разработка платформы для дистанционного обучения с адаптивным контентом',
    N'Проектирование образовательной платформы с адаптивным алгоритмом подбора учебных материалов на основе модели знаний студента. Заявки в тестовых данных не создаются.',
    @CreatorStudent,
    u.[Id],
    @TopicInactive
FROM [Users] u
WHERE u.[Email] = N'fqlfh2004@gmail.com';

-- ---------------------------------------------------------------------
-- SupervisorRequests: student01..20 ↔ teacher01..10 (по 2 студента на преподавателя)
WITH students_flow AS (
    SELECT
        s.[Id] AS [StudentId],
        ROW_NUMBER() OVER (ORDER BY su.[Email]) AS sp
    FROM [Students] s
    JOIN [Users] su ON su.[Id] = s.[UserId]
    WHERE su.[Email] LIKE N'student%@example.com'
),
teachers_flow AS (
    SELECT
        tu.[Id] AS [TeacherUserId],
        ROW_NUMBER() OVER (ORDER BY tu.[Email]) AS tpos
    FROM [Users] tu
    WHERE tu.[RoleId] = @RoleTeacher
      AND tu.[Email] LIKE N'teacher%@example.com'
)
INSERT INTO [SupervisorRequests] ([StudentId], [TeacherUserId], [StatusId], [Comment])
SELECT
    sf.[StudentId],
    tf.[TeacherUserId],
    @AppApprovedBySupervisor,
    N'Тема актуальна, научное руководство принято.'
FROM students_flow sf
JOIN teachers_flow tf ON tf.tpos = (sf.sp + 1) / 2;

-- ---------------------------------------------------------------------
-- StudentApplications (20)
WITH students_indexed AS (
    SELECT
        s.[Id] AS [StudentId],
        su.[Id] AS [StudentUserId],
        ROW_NUMBER() OVER (ORDER BY su.[Email]) AS rn
    FROM [Students] s
    JOIN [Users] su ON su.[Id] = s.[UserId]
    WHERE su.[Email] LIKE N'student%@example.com'
),
approved_requests AS (
    SELECT sr.[Id] AS [SupervisorRequestId], sr.[StudentId], sr.[TeacherUserId]
    FROM [SupervisorRequests] sr
    WHERE sr.[StatusId] = @AppApprovedBySupervisor
),
student_topics AS (
    SELECT t.[Id] AS [TopicId], t.[CreatedBy] AS [StudentUserId]
    FROM [Topics] t
    WHERE t.[CreatorTypeId] = @CreatorStudent
),
teacher_topics_ranked AS (
    SELECT
        t.[Id] AS [TopicId],
        t.[CreatedBy] AS [TeacherUserId],
        ROW_NUMBER() OVER (PARTITION BY t.[CreatedBy] ORDER BY t.[Title], t.[Id]) AS topic_rn
    FROM [Topics] t
    WHERE t.[CreatorTypeId] = @CreatorTeacher
),
app_candidates AS (
    SELECT [StudentId], [StudentUserId], rn,
           ROW_NUMBER() OVER (ORDER BY rn) AS app_seq
    FROM students_indexed
)
INSERT INTO [StudentApplications] ([StudentId], [TopicId], [SupervisorRequestId], [StatusId])
SELECT
    ac.[StudentId],
    CASE WHEN ac.app_seq <= 10 THEN st.[TopicId] ELSE tt.[TopicId] END,
    ar.[SupervisorRequestId],
    CASE WHEN ac.rn IN (2, 6, 11, 16) THEN @AppPendingHead ELSE @AppPending END
FROM app_candidates ac
JOIN approved_requests ar ON ar.[StudentId] = ac.[StudentId]
JOIN student_topics st ON st.[StudentUserId] = ac.[StudentUserId]
JOIN teacher_topics_ranked tt ON tt.[TeacherUserId] = ar.[TeacherUserId] AND tt.topic_rn = 1;

-- ---------------------------------------------------------------------
-- ApplicationActions
INSERT INTO [ApplicationActions] ([ApplicationId], [ResponsibleId], [StatusId], [Comment])
SELECT a.[Id], sr.[TeacherUserId], @ActionPending, N'Ожидает рассмотрения научным руководителем.'
FROM [StudentApplications] a
JOIN [SupervisorRequests] sr ON sr.[Id] = a.[SupervisorRequestId]
WHERE a.[StatusId] = @AppPending;

INSERT INTO [ApplicationActions] ([ApplicationId], [ResponsibleId], [StatusId], [Comment])
SELECT a.[Id], sr.[TeacherUserId], @ActionApproved, N'Одобрено научным руководителем, передано на рассмотрение кафедры.'
FROM [StudentApplications] a
JOIN [SupervisorRequests] sr ON sr.[Id] = a.[SupervisorRequestId]
WHERE a.[StatusId] = @AppPendingHead;

INSERT INTO [ApplicationActions] ([ApplicationId], [ResponsibleId], [StatusId], [Comment])
SELECT a.[Id], d.[HeadId], @ActionPending, N'Ожидает решения заведующего кафедрой.'
FROM [StudentApplications] a
JOIN [Students] s ON s.[Id] = a.[StudentId]
JOIN [Users] su ON su.[Id] = s.[UserId]
JOIN [Departments] d ON d.[Id] = su.[DepartmentId]
WHERE a.[StatusId] = @AppPendingHead;

-- ---------------------------------------------------------------------
-- ChatMessages: по 1 сообщению на заявку
INSERT INTO [ChatMessages] ([ApplicationId], [SenderId], [Content], [SentAt], [ReadAt])
SELECT
    a.[Id],
    su.[Id],
    CONCAT(N'Тестовое сообщение по заявке ', LOWER(CAST(a.[Id] AS nvarchar(36)))),
    DATEADD(MINUTE, -CAST(seq.seq_num AS int), SYSUTCDATETIME()),
    CASE
        WHEN (seq.seq_num % 2) = 0 THEN DATEADD(MINUTE, -CAST(seq.seq_num - 1 AS int), SYSUTCDATETIME())
        ELSE NULL
    END
FROM (
    SELECT [Id], ROW_NUMBER() OVER (ORDER BY [CreatedAt], [Id]) AS seq_num
    FROM [StudentApplications]
) seq
JOIN [StudentApplications] a ON a.[Id] = seq.[Id]
JOIN [Students] s ON s.[Id] = a.[StudentId]
JOIN [Users] su ON su.[Id] = s.[UserId];

-- ---------------------------------------------------------------------
-- Notifications (20)
INSERT INTO [Notifications] ([UserId], [TypeId], [Title], [Content], [IsRead], [CreatedAt])
SELECT
    u.[Id],
    (SELECT nt.[Id] FROM [NotificationTypes] nt
     WHERE nt.[CodeName] = (CASE (u.gs % 4)
        WHEN 0 THEN N'ApplicationStatusChanged'
        WHEN 1 THEN N'NewMessage'
        WHEN 2 THEN N'TopicApproved'
        ELSE N'TopicRejected'
     END)),
    CASE (u.gs % 4)
        WHEN 0 THEN N'Статус заявки изменён'
        WHEN 1 THEN N'Новое сообщение от научного руководителя'
        WHEN 2 THEN N'Тема утверждена кафедрой'
        ELSE N'Тема не принята к рассмотрению'
    END,
    CASE (u.gs % 4)
        WHEN 0 THEN N'Ваша заявка на выпускную квалификационную работу изменила статус. Перейдите в раздел «Мои заявки» для просмотра.'
        WHEN 1 THEN N'Ваш научный руководитель оставил новое сообщение. Перейдите в чат для ознакомления с комментарием.'
        WHEN 2 THEN N'Предложенная тема утверждена заведующим кафедрой. Поздравляем — можно приступать к написанию работы!'
        ELSE N'Предложенная тема не была принята к рассмотрению. Обратитесь к научному руководителю для уточнения требований к теме.'
    END,
    CASE WHEN (u.gs % 3) = 0 THEN 1 ELSE 0 END,
    DATEADD(HOUR, -CAST(u.gs AS int), SYSUTCDATETIME())
FROM (
    SELECT TOP 20 u.[Id], ROW_NUMBER() OVER (ORDER BY u.[Email]) AS gs
    FROM [Users] u
    WHERE u.[Email] NOT IN (N'Aydar.Ilin@norbit.ru', N'fqlfh2004@gmail.com')
    ORDER BY u.[Email]
) u;

-- ---------------------------------------------------------------------
-- GraduateWorks: по 1 записи на заявку
-- Название ВКР = название темы заявки
-- Годы: seq 1-10 → 2024, seq 11-20 → 2025
-- Комиссия: реальные инициалы из набора данных
INSERT INTO [GraduateWorks] (
    [ApplicationId], [StatusId], [Title], [StudentId], [TeacherId], [Year], [Grade],
    [CommissionMembers], [FilePath], [FileName], [PresentationPath], [PresentationFileName]
)
SELECT
    app.[Id],
    @GraduateWorkCompleted,
    t.[Title],
    st.[Id],
    tchr.[Id],
    CASE WHEN seq.seq_num <= 10 THEN 2024 ELSE 2025 END,
    (65 + (seq.seq_num % 31)),
    cd.members,
    CONCAT(N'vkr/', CASE WHEN seq.seq_num <= 10 THEN N'2024' ELSE N'2025' END,
           N'/work_', RIGHT(CONCAT(N'0', seq.seq_num), 2), N'/thesis.pdf'),
    CONCAT(LEFT(t.[Title], 40), N'_', RIGHT(CONCAT(N'0', seq.seq_num), 2), N'.pdf'),
    CASE WHEN (seq.seq_num % 2) = 0 THEN
        CONCAT(N'vkr/', CASE WHEN seq.seq_num <= 10 THEN N'2024' ELSE N'2025' END,
               N'/work_', RIGHT(CONCAT(N'0', seq.seq_num), 2), N'/presentation.pptx')
    ELSE NULL END,
    CASE WHEN (seq.seq_num % 2) = 0 THEN
        CONCAT(N'Презентация_', RIGHT(CONCAT(N'0', seq.seq_num), 2), N'.pptx')
    ELSE NULL END
FROM (
    SELECT app.[Id], app.[StudentId], sr.[TeacherUserId],
           ROW_NUMBER() OVER (ORDER BY app.[CreatedAt], app.[Id]) AS seq_num
    FROM [StudentApplications] app
    JOIN [SupervisorRequests] sr ON sr.[Id] = app.[SupervisorRequestId]
) seq
JOIN [StudentApplications] app ON app.[Id] = seq.[Id]
JOIN [Topics] t ON t.[Id] = app.[TopicId]
JOIN [Students] st ON st.[Id] = seq.[StudentId]
JOIN [Teachers] tchr ON tchr.[UserId] = seq.[TeacherUserId]
JOIN (VALUES
    (1,  N'Смирнов А.Н., Кузнецова О.Д., Попов М.А.'),
    (2,  N'Кузнецова О.Д., Попов М.А., Новиков А.В.'),
    (3,  N'Попов М.А., Смирнов А.Н., Лебедева М.И.'),
    (4,  N'Новиков А.В., Лебедева М.И., Соколов В.П.'),
    (5,  N'Лебедева М.И., Соколов В.П., Ильин А.Р.'),
    (6,  N'Соколов В.П., Новиков А.В., Михайлов Е.А.'),
    (7,  N'Михайлов Е.А., Морозова Н.Г., Соколов В.П.'),
    (8,  N'Морозова Н.Г., Михайлов Е.А., Фёдоров П.К.'),
    (9,  N'Фёдоров П.К., Волков И.Д., Михайлов Е.А.'),
    (10, N'Волков И.Д., Фёдоров П.К., Морозова Н.Г.'),
    (11, N'Смирнов А.Н., Новиков А.В., Рогов Е.Б.'),
    (12, N'Кузнецова О.Д., Соколов В.П., Власов К.Ю.'),
    (13, N'Попов М.А., Михайлов Е.А., Щербакова И.Б.'),
    (14, N'Новиков А.В., Морозова Н.Г., Климов О.В.'),
    (15, N'Лебедева М.И., Фёдоров П.К., Рогов Е.Б.'),
    (16, N'Соколов В.П., Волков И.Д., Власов К.Ю.'),
    (17, N'Михайлов Е.А., Смирнов А.Н., Щербакова И.Б.'),
    (18, N'Морозова Н.Г., Кузнецова О.Д., Климов О.В.'),
    (19, N'Фёдоров П.К., Попов М.А., Рогов Е.Б.'),
    (20, N'Волков И.Д., Новиков А.В., Власов К.Ю.')
) AS cd(seq_num, members) ON cd.seq_num = seq.seq_num;

-- Пересчитать счётчик ВКР у каждого преподавателя
UPDATE t
SET [GraduateWorksCount] = (SELECT COUNT(*) FROM [GraduateWorks] gw WHERE gw.[TeacherId] = t.[Id])
FROM [Teachers] t;

COMMIT TRANSACTION;
