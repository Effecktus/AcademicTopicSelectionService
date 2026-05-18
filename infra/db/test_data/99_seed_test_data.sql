-- Тестовые данные для сценариев UI/бэкенда.
-- Важно: справочники уже должны быть заполнены (01..07), таблицы созданы.
--
-- =============================================================================
-- Учётные записи (логин = Email, пароль указан ниже)
-- =============================================================================
-- Администратор:
--   z_admin@example.com                    /  TestPassword123!
-- Преподаватели (серия):
--   teacher01@example.com … teacher10@example.com   /  TestPassword123!
-- Преподаватель (кафедра ПМИ, без заявок в сиде):
--   Aydar.Ilin@norbit.ru                 /  Password123!
-- Студенты (серия):
--   student01@example.com … student20@example.com   /  TestPassword123!
-- Студент (кафедра ПМИ, без заявок в сиде):
--   fqlfh2004@gmail.com                  /  Password123!
-- Заведующие (серия, кафедры АСОИУ, КС, ДПУ):
--   head02@example.com … head04@example.com         /  TestPassword123!
-- Заведующий кафедры ПМИ:
--   effecktus@yandex.ru                  /  Password123!
-- =============================================================================
--
-- Ключевая модель:
--   - 4 кафедры; заведующий ПМИ — effecktus@yandex.ru
--   - 11 преподавателей (Aydar.Ilin 3+3+2+2=10 серийных + Aydar.Ilin@norbit.ru на ПМИ)
--   - 21 студент (20 серийных по кафедрам + fqlfh2004@gmail.com на ПМИ)
--   - у каждого преподавателя по 3 темы (33 преподавательские темы)
--   - 20 студентов student01..20: по 2 на преподавателя teacher01..10; одобренные SR + заявки на тему
--   - Aydar.Ilin@norbit.ru и fqlfh2004@gmail.com: без SupervisorRequests и без StudentApplications
--   - на каждую кафедру 1 заявка в статусе PendingDepartmentHead
--   - остальные заявки — Pending

TRUNCATE TABLE
    "GraduateWorks",
    "Notifications",
    "ChatMessages",
    "ApplicationActions",
    "StudentApplications",
    "SupervisorRequests",
    "Topics",
    "Students",
    "StudyGroups",
    "Teachers",
    "Users",
    "Departments"
RESTART IDENTITY CASCADE;

-- ---------------------------------------------------------------------
-- Departments (4)
INSERT INTO "Departments" ("CodeName", "DisplayName")
VALUES
    ('Department01', 'Прикладная математика и информатика'),
    ('Department02', 'Автоматизированные системы обработки информации и управления'),
    ('Department03', 'Компьютерные сети'),
    ('Department04', 'Динамика процессов и управления');

-- ---------------------------------------------------------------------
-- Users: преподаватели (серия, 10 чел.)
WITH teacher_data(pos, email, first_name, last_name, middle_name) AS (
    VALUES
    (1,  'teacher01@example.com', 'Алексей',   'Смирнов',    'Николаевич'),
    (2,  'teacher02@example.com', 'Ольга',     'Кузнецова',  'Дмитриевна'),
    (3,  'teacher03@example.com', 'Михаил',    'Попов',      'Андреевич'),
    (4,  'teacher04@example.com', 'Артём',     'Новиков',    'Владимирович'),
    (5,  'teacher05@example.com', 'Марина',    'Лебедева',   'Игоревна'),
    (6,  'teacher06@example.com', 'Виктор',    'Соколов',    'Петрович'),
    (7,  'teacher07@example.com', 'Евгений',   'Михайлов',   'Александрович'),
    (8,  'teacher08@example.com', 'Наталья',   'Морозова',   'Геннадьевна'),
    (9,  'teacher09@example.com', 'Павел',     'Фёдоров',    'Константинович'),
    (10, 'teacher10@example.com', 'Игорь',     'Волков',     'Дмитриевич')
)
INSERT INTO "Users" ("Email", "PasswordHash", "FirstName", "LastName", "MiddleName", "RoleId", "DepartmentId", "IsActive")
SELECT
    td.email::citext,
    crypt('TestPassword123!', gen_salt('bf', 10)),
    td.first_name,
    td.last_name,
    td.middle_name,
    (SELECT "Id" FROM "UserRoles" WHERE "CodeName" = 'Teacher' LIMIT 1),
    (SELECT "Id" FROM "Departments" ORDER BY "CodeName" OFFSET (
        CASE
            WHEN td.pos <= 3 THEN 0
            WHEN td.pos <= 6 THEN 1
            WHEN td.pos <= 8 THEN 2
            ELSE 3
        END
    ) LIMIT 1),
    TRUE
FROM teacher_data td;

-- Users: студенты (серия, 20 чел.)
WITH student_data(pos, email, first_name, last_name, middle_name) AS (
    VALUES
    (1,  'student01@example.com', 'Иван',      'Захаров',    'Алексеевич'),
    (2,  'student02@example.com', 'Анна',      'Яковлева',   'Сергеевна'),
    (3,  'student03@example.com', 'Роман',     'Гусев',      'Дмитриевич'),
    (4,  'student04@example.com', 'Мария',     'Никитина',   'Александровна'),
    (5,  'student05@example.com', 'Артём',     'Степанов',   'Павлович'),
    (6,  'student06@example.com', 'Денис',     'Орлов',      'Николаевич'),
    (7,  'student07@example.com', 'Кирилл',    'Сергеев',    'Олегович'),
    (8,  'student08@example.com', 'Екатерина', 'Кузьмина',   'Андреевна'),
    (9,  'student09@example.com', 'Максим',    'Козлов',     'Игоревич'),
    (10, 'student10@example.com', 'Антон',     'Белов',      'Владимирович'),
    (11, 'student11@example.com', 'Алексей',   'Медведев',   'Юрьевич'),
    (12, 'student12@example.com', 'Виктория',  'Фёдорова',   'Евгеньевна'),
    (13, 'student13@example.com', 'Дарья',     'Петрова',    'Михайловна'),
    (14, 'student14@example.com', 'Владимир',  'Егоров',     'Сергеевич'),
    (15, 'student15@example.com', 'Тимур',     'Макаров',    'Олегович'),
    (16, 'student16@example.com', 'Наталья',   'Борисова',   'Дмитриевна'),
    (17, 'student17@example.com', 'Дмитрий',   'Романов',    'Константинович'),
    (18, 'student18@example.com', 'Полина',    'Тихонова',   'Андреевна'),
    (19, 'student19@example.com', 'Алексей',   'Соловьёв',   'Иванович'),
    (20, 'student20@example.com', 'Евгений',   'Громов',     'Сергеевич')
)
INSERT INTO "Users" ("Email", "PasswordHash", "FirstName", "LastName", "MiddleName", "RoleId", "DepartmentId", "IsActive")
SELECT
    sd.email::citext,
    crypt('TestPassword123!', gen_salt('bf', 10)),
    sd.first_name,
    sd.last_name,
    sd.middle_name,
    (SELECT "Id" FROM "UserRoles" WHERE "CodeName" = 'Student' LIMIT 1),
    (SELECT "Id" FROM "Departments" ORDER BY "CodeName" OFFSET ((sd.pos - 1) / 5) LIMIT 1),
    TRUE
FROM student_data sd;

-- Users: заведующие кафедр АСОИУ, КС, ДПУ (head02–head04)
WITH head_data(pos, email, first_name, last_name, middle_name) AS (
    VALUES
    (2, 'head02@example.com', 'Константин', 'Власов',     'Юрьевич'),
    (3, 'head03@example.com', 'Ирина',      'Щербакова',  'Борисовна'),
    (4, 'head04@example.com', 'Олег',       'Климов',     'Валентинович')
)
INSERT INTO "Users" ("Email", "PasswordHash", "FirstName", "LastName", "MiddleName", "RoleId", "DepartmentId", "IsActive")
SELECT
    hd.email::citext,
    crypt('TestPassword123!', gen_salt('bf', 10)),
    hd.first_name,
    hd.last_name,
    hd.middle_name,
    (SELECT "Id" FROM "UserRoles" WHERE "CodeName" = 'DepartmentHead' LIMIT 1),
    (SELECT "Id" FROM "Departments" ORDER BY "CodeName" OFFSET (hd.pos - 1) LIMIT 1),
    TRUE
FROM head_data hd;

-- Users: персональные аккаунты
INSERT INTO "Users" ("Email", "PasswordHash", "FirstName", "LastName", "MiddleName", "RoleId", "DepartmentId", "IsActive")
VALUES (
    'effecktus@yandex.ru'::citext,
    crypt('Password123!', gen_salt('bf', 10)),
    'Евгений', 'Рогов', 'Борисович',
    (SELECT "Id" FROM "UserRoles" WHERE "CodeName" = 'DepartmentHead' LIMIT 1),
    (SELECT "Id" FROM "Departments" WHERE "CodeName" = 'Department01' LIMIT 1),
    TRUE
);

INSERT INTO "Users" ("Email", "PasswordHash", "FirstName", "LastName", "MiddleName", "RoleId", "DepartmentId", "IsActive")
VALUES (
    'Aydar.Ilin@norbit.ru'::citext,
    crypt('Password123!', gen_salt('bf', 10)),
    'Айдар', 'Ильин', 'Альбертович',
    (SELECT "Id" FROM "UserRoles" WHERE "CodeName" = 'Teacher' LIMIT 1),
    (SELECT "Id" FROM "Departments" WHERE "CodeName" = 'Department01' LIMIT 1),
    TRUE
);

INSERT INTO "Users" ("Email", "PasswordHash", "FirstName", "LastName", "MiddleName", "RoleId", "DepartmentId", "IsActive")
VALUES (
    'fqlfh2004@gmail.com'::citext,
    crypt('Password123!', gen_salt('bf', 10)),
    'Фёдор', 'Чернов', 'Андреевич',
    (SELECT "Id" FROM "UserRoles" WHERE "CodeName" = 'Student' LIMIT 1),
    (SELECT "Id" FROM "Departments" WHERE "CodeName" = 'Department01' LIMIT 1),
    TRUE
);

INSERT INTO "Users" ("Email", "PasswordHash", "FirstName", "LastName", "MiddleName", "RoleId", "DepartmentId", "IsActive")
VALUES (
    'z_admin@example.com'::citext,
    crypt('TestPassword123!', gen_salt('bf', 10)),
    'Николай', 'Системов', 'Администраторович',
    (SELECT "Id" FROM "UserRoles" WHERE "CodeName" = 'Admin' LIMIT 1),
    NULL,
    TRUE
);

-- Привязка заведующих к кафедрам
UPDATE "Departments" SET "HeadId" = (
    SELECT "Id" FROM "Users" WHERE "Email" = 'effecktus@yandex.ru'::citext LIMIT 1
) WHERE "CodeName" = 'Department01';

UPDATE "Departments" d
SET "HeadId" = (
    SELECT u."Id" FROM "Users" u
    WHERE u."Email" = format('head%s@example.com', regexp_replace(d."CodeName"::text, '\D', '', 'g'))::citext
      AND u."RoleId" = (SELECT "Id" FROM "UserRoles" WHERE "CodeName" = 'DepartmentHead' LIMIT 1)
    LIMIT 1
)
WHERE d."CodeName" IN ('Department02', 'Department03', 'Department04');

-- ---------------------------------------------------------------------
-- Teachers: все 11 преподавателей
INSERT INTO "Teachers" ("UserId", "MaxStudentsLimit", "AcademicDegreeId", "AcademicTitleId", "PositionId")
SELECT
    u."Id",
    (6 + (u.gs % 6)),
    (SELECT "Id" FROM "AcademicDegrees" WHERE "CodeName" = (CASE (u.gs % 5)
        WHEN 0 THEN 'None'
        WHEN 1 THEN 'CandidateOfTechnicalSciences'
        WHEN 2 THEN 'CandidateOfEconomicSciences'
        WHEN 3 THEN 'DoctorOfTechnicalSciences'
        ELSE 'DoctorOfEconomicSciences'
    END) LIMIT 1),
    (SELECT "Id" FROM "AcademicTitles" WHERE "CodeName" = (CASE (u.gs % 3)
        WHEN 0 THEN 'None'
        WHEN 1 THEN 'AssociateProfessor'
        ELSE 'Professor'
    END) LIMIT 1),
    (SELECT "Id" FROM "Positions" WHERE "CodeName" = (CASE (u.gs % 4)
        WHEN 0 THEN 'Assistant'
        WHEN 1 THEN 'SeniorLecturer'
        WHEN 2 THEN 'AssociateProfessor'
        ELSE 'Professor'
    END) LIMIT 1)
FROM (
    SELECT u."Id", row_number() OVER (ORDER BY u."Email") AS gs
    FROM "Users" u
    WHERE u."RoleId" = (SELECT "Id" FROM "UserRoles" WHERE "CodeName" = 'Teacher' LIMIT 1)
    ORDER BY u."Email"
) AS u;

-- ---------------------------------------------------------------------
-- StudyGroups: по 5 групп на кафедру + 1 для демо-студента
-- Схема номера: первая цифра = номер кафедры (1=ПМИ, 2=АСОИУ, 3=КС, 4=ДПУ),
--               вторая = курс (1), две последние = номер группы.
INSERT INTO "StudyGroups" ("CodeName")
VALUES
    (1101), (1102), (1103), (1104), (1105), (1106),  -- ПМИ (+ демо)
    (2101), (2102), (2103), (2104), (2105),           -- АСОИУ
    (3101), (3102), (3103), (3104), (3105),           -- КС
    (4101), (4102), (4103), (4104), (4105)            -- ДПУ
ON CONFLICT ("CodeName") DO NOTHING;

-- Students: 20 серийных (группы по кафедрам)
WITH student_groups(pos, group_code) AS (
    VALUES
    (1, 1101), (2, 1102), (3, 1103), (4, 1104), (5, 1105),
    (6, 2101), (7, 2102), (8, 2103), (9, 2104), (10, 2105),
    (11, 3101), (12, 3102), (13, 3103), (14, 3104), (15, 3105),
    (16, 4101), (17, 4102), (18, 4103), (19, 4104), (20, 4105)
)
INSERT INTO "Students" ("UserId", "GroupId")
SELECT
    u."Id",
    (SELECT "Id" FROM "StudyGroups" WHERE "CodeName" = sg.group_code LIMIT 1)
FROM (
    SELECT u."Id", row_number() OVER (ORDER BY u."Email") AS rn
    FROM "Users" u
    WHERE u."RoleId" = (SELECT "Id" FROM "UserRoles" WHERE "CodeName" = 'Student' LIMIT 1)
      AND u."Email"::text LIKE 'student%@example.com'
    ORDER BY u."Email"
) u
JOIN student_groups sg ON sg.pos = u.rn;

-- Students: демо-студент (группа ПМИ, запасная)
INSERT INTO "Students" ("UserId", "GroupId")
SELECT u."Id", (SELECT "Id" FROM "StudyGroups" WHERE "CodeName" = 1106 LIMIT 1)
FROM "Users" u
WHERE u."Email" = 'fqlfh2004@gmail.com'::citext;

-- ---------------------------------------------------------------------
-- Темы преподавателей: по 3 на каждого из 11
-- Порядок преподавателей по email (row_number):
--   1=Aydar.Ilin, 2=teacher01, 3=teacher02, 4=teacher03 (ПМИ)
--   5=teacher04, 6=teacher05, 7=teacher06 (АСОИУ)
--   8=teacher07, 9=teacher08 (КС)
--   10=teacher09, 11=teacher10 (ДПУ)
WITH teacher_topics(t_seq, topic_idx, title, description) AS (
    VALUES
    -- Ильин А.Р. (ПМИ)
    (1,  1, 'Разработка веб-сервиса для хранения и поиска научных публикаций',
            'Проектирование RESTful API и модели данных для репозитория научных статей с поддержкой полнотекстового поиска. Апробация на корпусе публикаций конференций ВАК.'),
    (1,  2, 'Исследование алгоритмов сжатия данных без потерь на основе энтропийного кодирования',
            'Сравнительный анализ алгоритмов Huffman, Arithmetic и ANS по скорости и степени компрессии. Реализация и тестирование на разнородных наборах данных.'),
    (1,  3, 'Построение инструментального комплекса для статического анализа программного кода',
            'Разработка плагина обнаружения потенциальных ошибок и нарушений стиля кода методами AST-анализа. Интеграция с CI/CD-конвейером на базе GitLab.'),
    -- Смирнов А.Н. (ПМИ)
    (2,  1, 'Разработка алгоритма кластеризации данных на основе нейронных сетей',
            'Исследование методов обучения сверточных и рекуррентных сетей для задач кластеризации неструктурированных данных. Сравнительный анализ точности и вычислительной эффективности.'),
    (2,  2, 'Применение методов машинного обучения для прогнозирования временных рядов',
            'Разработка и сравнение моделей LSTM, GRU и Transformer для прогнозирования финансовых и метеорологических временных рядов. Оценка качества на публичных датасетах.'),
    (2,  3, 'Исследование алгоритмов поиска кратчайших путей в больших графах',
            'Оценка производительности алгоритмов A*, Dijkstra и Bidirectional BFS на графах с миллионами вершин. Разработка параллельных версий с использованием многопоточности.'),
    -- Кузнецова О.Д. (ПМИ)
    (3,  1, 'Методы оптимизации задач линейного программирования в логистических системах',
            'Применение симплекс-метода и метода внутренних точек к задачам оптимизации маршрутов доставки. Разработка инструментального средства с визуализацией решений.'),
    (3,  2, 'Построение системы рекомендаций на основе коллаборативной фильтрации',
            'Разработка рекомендательной системы на основе матричной факторизации и SVD-разложения. Оценка качества рекомендаций на публичных датасетах MovieLens и Amazon.'),
    (3,  3, 'Применение генетических алгоритмов для решения задач комбинаторной оптимизации',
            'Реализация генетического алгоритма для задачи коммивояжёра и рюкзака с настраиваемыми операторами отбора, скрещивания и мутации. Сравнение с методом имитации отжига.'),
    -- Попов М.А. (ПМИ)
    (4,  1, 'Разработка системы автоматической классификации текстовых документов',
            'Построение конвейера NLP-обработки с токенизацией, TF-IDF векторизацией и ансамблем классификаторов. Обучение и тестирование на корпусе русскоязычных документов.'),
    (4,  2, 'Численное моделирование задач оптимального управления параметрами',
            'Разработка численного решателя вариационных задач оптимального управления на основе метода конечных разностей. Верификация на модельных задачах с аналитическим решением.'),
    (4,  3, 'Построение предсказательных моделей на основе методов регрессионного анализа',
            'Сравнение регрессионных моделей (линейная, полиномиальная, Ridge, Lasso, Elastic Net) для предсказания технических показателей по историческим данным производственных процессов.'),
    -- Новиков А.В. (АСОИУ)
    (5,  1, 'Разработка системы мониторинга производственных процессов на основе IoT',
            'Разработка платформы сбора телеметрии с IoT-датчиков на базе MQTT и InfluxDB с визуализацией в Grafana. Апробация на макете производственной линии.'),
    (5,  2, 'Автоматизация бизнес-процессов предприятия с использованием RPA-технологий',
            'Автоматизация рутинных операций документооборота и отчётности с использованием RPA-фреймворка UiPath. Оценка экономической эффективности внедрения.'),
    (5,  3, 'Проектирование распределённой системы управления складскими запасами',
            'Реализация распределённой системы учёта и управления складом с мобильным терминалом и синхронизацией данных в реальном времени через WebSocket.'),
    -- Лебедева М.И. (АСОИУ)
    (6,  1, 'Разработка SCADA-системы для управления технологическим оборудованием цеха',
            'Разработка системы визуализации и управления оборудованием производственного цеха на базе WinCC. Моделирование аварийных сценариев и алгоритмов аварийной защиты.'),
    (6,  2, 'Автоматизация тестирования корпоративных информационных систем',
            'Разработка фреймворка автоматизированного тестирования REST API корпоративных систем с генерацией отчётов о покрытии и интеграцией в CI-конвейер.'),
    (6,  3, 'Проектирование микросервисной архитектуры для ERP-платформы',
            'Проектирование микросервисной архитектуры с API Gateway, service discovery и distributed tracing. Нагрузочное тестирование и оценка горизонтальной масштабируемости.'),
    -- Соколов В.П. (АСОИУ)
    (7,  1, 'Разработка системы управления данными промышленного предприятия',
            'Разработка хранилища данных и ETL-процессов для консолидации промышленных данных из гетерогенных источников. Построение аналитических отчётов и OLAP-кубов.'),
    (7,  2, 'Разработка системы интеллектуального управления энергопотреблением здания',
            'Разработка системы сбора данных с датчиков и алгоритмов прогнозирования энергонагрузки здания. Оценка потенциала снижения затрат на электроэнергию.'),
    (7,  3, 'Автоматизация процессов документооборота в государственных организациях',
            'Проектирование и внедрение системы электронного документооборота для государственного ведомства на базе платформы 1С:Документооборот. Анализ соответствия требованиям регулятора.'),
    -- Михайлов Е.А. (КС)
    (8,  1, 'Анализ и оптимизация протоколов маршрутизации в программно-определяемых сетях',
            'Анализ производительности протоколов OSPF, EIGRP и BGP в топологиях SDN. Разработка алгоритма динамической балансировки нагрузки на базе контроллера OpenDaylight.'),
    (8,  2, 'Разработка системы обнаружения вторжений на основе анализа сетевого трафика',
            'Построение IDS на основе методов машинного обучения с анализом сетевых потоков. Тестирование на наборах данных NSL-KDD и CICIDS2017. Сравнение с сигнатурным методом.'),
    (8,  3, 'Исследование методов обеспечения качества обслуживания в беспроводных сетях',
            'Исследование механизмов QoS (DSCP, DiffServ, 802.11e) в гетерогенных беспроводных сетях. Моделирование в GNS3 с оценкой задержки и джиттера для мультимедиатрафика.'),
    -- Морозова Н.Г. (КС)
    (9,  1, 'Проектирование отказоустойчивой сетевой инфраструктуры для центра обработки данных',
            'Проектирование сети ЦОД с резервированием на уровнях L2/L3, VRRP и port-channel. Расчёт метрик доступности, RTO и RPO в соответствии с требованиями Tier III.'),
    (9,  2, 'Применение технологии блокчейн для обеспечения безопасности сетевых транзакций',
            'Разработка смарт-контракта на Ethereum для верификации целостности сетевых транзакций. Сравнение производительности с традиционными PKI-решениями по пропускной способности.'),
    (9,  3, 'Разработка системы мониторинга и управления сетевой инфраструктурой предприятия',
            'Разработка системы SNMP/NetFlow-мониторинга с автоматической инвентаризацией и уведомлениями об инцидентах. Развёртывание на базе LibreNMS с интеграцией в ITSM-систему.'),
    -- Фёдоров П.К. (ДПУ)
    (10, 1, 'Моделирование динамических систем управления с переменной структурой',
            'Исследование переключаемых систем управления с использованием метода функций Ляпунова. Синтез условий устойчивости и апробация на имитационной модели в Simulink.'),
    (10, 2, 'Синтез робастных регуляторов для нелинейных объектов управления',
            'Синтез ПИД-регулятора с робастными настройками для нелинейных объектов с параметрической неопределённостью. Верификация методом μ-анализа и имитационного моделирования.'),
    (10, 3, 'Разработка системы адаптивного управления манипуляционным роботом',
            'Разработка алгоритма адаптивного управления трёхзвенным манипулятором с компенсацией неизвестных нагрузок. Реализация на базе ROS 2 и апробация на физическом стенде.'),
    -- Волков И.Д. (ДПУ)
    (11, 1, 'Исследование устойчивости замкнутых систем управления с запаздыванием',
            'Аналитическое и имитационное исследование устойчивости систем с чистым запаздыванием методом критериев Михайлова и частотных характеристик. Синтез корректирующих звеньев.'),
    (11, 2, 'Оптимальное управление тепловыми процессами в промышленных установках',
            'Постановка и решение задачи оптимального управления температурным полем нагревательной камеры. Минимизация энергозатрат при соблюдении ограничений на точность регулирования.'),
    (11, 3, 'Применение нечёткой логики в системах автоматического регулирования температуры',
            'Разработка нечёткого регулятора температуры для термической установки. Сравнение с классическим ПИД-регулятором по критериям перерегулирования, времени регулирования и интегральной ошибки.')
)
INSERT INTO "Topics" ("Title", "Description", "CreatorTypeId", "CreatedBy", "StatusId")
SELECT
    tt.title::citext,
    tt.description,
    (SELECT "Id" FROM "TopicCreatorTypes" WHERE "CodeName" = 'Teacher' LIMIT 1),
    ts."UserId",
    (SELECT "Id" FROM "TopicStatuses" WHERE "CodeName" = 'Active' LIMIT 1)
FROM (
    SELECT u."Id" AS "UserId", row_number() OVER (ORDER BY u."Email") AS gs
    FROM "Users" u
    WHERE u."RoleId" = (SELECT "Id" FROM "UserRoles" WHERE "CodeName" = 'Teacher' LIMIT 1)
    ORDER BY u."Email"
) ts
JOIN teacher_topics tt ON tt.t_seq = ts.gs;

-- Темы студентов (серия, 20 чел.)
WITH student_topics(s_seq, title, description) AS (
    VALUES
    -- ПМИ (student01–05)
    (1,  'Разработка веб-приложения для визуализации многомерных данных',
         'Проектирование и реализация интерактивного дашборда для визуализации многомерных наборов данных с использованием D3.js и WebGL. Апробация на данных экологического мониторинга.'),
    (2,  'Исследование методов сжатия изображений с применением вейвлет-преобразования',
         'Сравнительный анализ алгоритмов сжатия изображений на основе дискретного вейвлет-преобразования. Реализация кодека и оценка соотношения сигнал/шум при различных степенях компрессии.'),
    (3,  'Создание мобильного приложения для мониторинга физической активности',
         'Разработка кроссплатформенного мобильного приложения на Flutter для отслеживания физической активности с интеграцией с фитнес-датчиками через BLE. Хранение данных в SQLite.'),
    (4,  'Разработка системы автоматической генерации тестовых наборов данных',
         'Разработка фреймворка генерации синтетических тестовых данных с заданными статистическими характеристиками для тестирования алгоритмов машинного обучения.'),
    (5,  'Применение технологий дополненной реальности в учебном процессе',
         'Разработка AR-приложения для визуализации трёхмерных математических объектов в учебном процессе на базе Unity AR Foundation. Оценка эффективности усвоения материала.'),
    -- АСОИУ (student06–10)
    (6,  'Разработка системы учёта и контроля основных средств предприятия',
         'Проектирование и реализация информационной системы учёта основных средств с поддержкой QR-инвентаризации и интеграцией с 1С:Бухгалтерия через REST API.'),
    (7,  'Автоматизация составления расписания занятий в высшем учебном заведении',
         'Разработка алгоритма автоматизированного составления расписания на основе эволюционных методов оптимизации с учётом ограничений аудиторного фонда и нагрузки преподавателей.'),
    (8,  'Разработка системы электронного документооборота для малого предприятия',
         'Проектирование и реализация системы электронного документооборота с маршрутизацией согласований, контролем исполнения и хранилищем документов на базе MinIO.'),
    (9,  'Создание CRM-системы для управления взаимоотношениями с клиентами',
         'Разработка веб-ориентированной CRM-системы с управлением воронкой продаж, историей взаимодействий и аналитическими отчётами. Интеграция с email и мессенджерами.'),
    (10, 'Разработка системы управления задачами для распределённых команд',
         'Проектирование и реализация системы управления задачами с поддержкой Kanban-досок, уведомлений в реальном времени через WebSocket и ролевой моделью доступа.'),
    -- КС (student11–15)
    (11, 'Оптимизация конфигурации корпоративной беспроводной сети Wi-Fi',
         'Анализ текущей топологии Wi-Fi инфраструктуры предприятия и разработка рекомендаций по оптимизации расположения точек доступа, каналов и параметров QoS для повышения пропускной способности.'),
    (12, 'Разработка VPN-решения для безопасного удалённого доступа сотрудников',
         'Проектирование и развёртывание отказоустойчивого VPN-решения на базе WireGuard с многофакторной аутентификацией и централизованным управлением политиками доступа.'),
    (13, 'Анализ уязвимостей корпоративной сети и разработка мер защиты',
         'Проведение аудита сетевой инфраструктуры предприятия методами пассивного и активного сканирования. Формирование реестра уязвимостей и плана мероприятий по их устранению.'),
    (14, 'Проектирование сети передачи данных для нового корпуса университета',
         'Разработка проекта структурированной кабельной системы и активного оборудования для нового учебного корпуса с расчётом пропускной способности и планом IP-адресации.'),
    (15, 'Разработка системы IP-видеонаблюдения для объектов инфраструктуры',
         'Проектирование и реализация системы IP-видеонаблюдения с централизованным видеосервером, детекцией движения и удалённым доступом через веб-интерфейс.'),
    -- ДПУ (student16–20)
    (16, 'Разработка системы управления автоматизированным складским комплексом',
         'Разработка алгоритмов оптимального размещения и извлечения грузов для автоматизированного склада с управлением транспортными роботами по стратегии ABC-зонирования.'),
    (17, 'Моделирование системы управления температурным режимом теплицы',
         'Разработка математической модели тепличного объекта и синтез ПИД-регулятора с компенсацией внешних возмущений. Имитационное моделирование в Matlab/Simulink.'),
    (18, 'Разработка алгоритма управления движением автономного мобильного робота',
         'Разработка и реализация алгоритма навигации мобильного робота на основе SLAM с объездом препятствий. Апробация на роботизированной платформе в лабораторных условиях.'),
    (19, 'Проектирование системы автоматического контроля качества продукции',
         'Разработка автоматизированной системы технического зрения для контроля качества продукции на производственной линии на базе нейросетевых детекторов дефектов.'),
    (20, 'Разработка системы стабилизации и управления беспилотным летательным аппаратом',
         'Синтез алгоритма стабилизации квадрокоптера с компенсацией внешних возмущений на основе PID и LQR-регуляторов. Верификация на имитационной модели и лётных испытаниях.')
)
INSERT INTO "Topics" ("Title", "Description", "CreatorTypeId", "CreatedBy", "StatusId")
SELECT
    st.title::citext,
    st.description,
    (SELECT "Id" FROM "TopicCreatorTypes" WHERE "CodeName" = 'Student' LIMIT 1),
    u."Id",
    (SELECT "Id" FROM "TopicStatuses" WHERE "CodeName" = 'Inactive' LIMIT 1)
FROM (
    SELECT u."Id", row_number() OVER (ORDER BY u."Email") AS rn
    FROM "Users" u
    WHERE u."RoleId" = (SELECT "Id" FROM "UserRoles" WHERE "CodeName" = 'Student' LIMIT 1)
      AND u."Email"::text LIKE 'student%@example.com'
    ORDER BY u."Email"
) u
JOIN student_topics st ON st.s_seq = u.rn;

-- Тема демо-студента
INSERT INTO "Topics" ("Title", "Description", "CreatorTypeId", "CreatedBy", "StatusId")
SELECT
    'Разработка платформы для дистанционного обучения с адаптивным контентом'::citext,
    'Проектирование образовательной платформы с адаптивным алгоритмом подбора учебных материалов на основе модели знаний студента. Заявки в тестовых данных не создаются.',
    (SELECT "Id" FROM "TopicCreatorTypes" WHERE "CodeName" = 'Student' LIMIT 1),
    u."Id",
    (SELECT "Id" FROM "TopicStatuses" WHERE "CodeName" = 'Inactive' LIMIT 1)
FROM "Users" u
WHERE u."Email" = 'fqlfh2004@gmail.com'::citext;

-- ---------------------------------------------------------------------
-- SupervisorRequests: student01..20 ↔ teacher01..10 (по 2 студента на преподавателя)
WITH students_flow AS (
    SELECT
        s."Id" AS "StudentId",
        row_number() OVER (ORDER BY su."Email") AS sp
    FROM "Students" s
    JOIN "Users" su ON su."Id" = s."UserId"
    WHERE su."Email"::text LIKE 'student%@example.com'
),
teachers_flow AS (
    SELECT
        tu."Id" AS "TeacherUserId",
        row_number() OVER (ORDER BY tu."Email") AS tpos
    FROM "Users" tu
    WHERE tu."RoleId" = (SELECT "Id" FROM "UserRoles" WHERE "CodeName" = 'Teacher' LIMIT 1)
      AND tu."Email"::text LIKE 'teacher%@example.com'
)
INSERT INTO "SupervisorRequests" ("StudentId", "TeacherUserId", "StatusId", "Comment")
SELECT
    sf."StudentId",
    tf."TeacherUserId",
    (SELECT "Id" FROM "ApplicationStatuses" WHERE "CodeName" = 'ApprovedBySupervisor' LIMIT 1),
    'Тема актуальна, научное руководство принято.'
FROM students_flow sf
JOIN teachers_flow tf ON tf.tpos = (sf.sp + 1) / 2;

-- ---------------------------------------------------------------------
-- StudentApplications (20)
WITH students_indexed AS (
    SELECT
        s."Id" AS "StudentId",
        su."Id" AS "StudentUserId",
        row_number() OVER (ORDER BY su."Email") AS rn
    FROM "Students" s
    JOIN "Users" su ON su."Id" = s."UserId"
    WHERE su."Email"::text LIKE 'student%@example.com'
),
approved_requests AS (
    SELECT sr."Id" AS "SupervisorRequestId", sr."StudentId", sr."TeacherUserId"
    FROM "SupervisorRequests" sr
    WHERE sr."StatusId" = (SELECT "Id" FROM "ApplicationStatuses" WHERE "CodeName" = 'ApprovedBySupervisor' LIMIT 1)
),
student_topics AS (
    SELECT t."Id" AS "TopicId", t."CreatedBy" AS "StudentUserId"
    FROM "Topics" t
    WHERE t."CreatorTypeId" = (SELECT "Id" FROM "TopicCreatorTypes" WHERE "CodeName" = 'Student' LIMIT 1)
),
teacher_topics_ranked AS (
    SELECT
        t."Id" AS "TopicId",
        t."CreatedBy" AS "TeacherUserId",
        row_number() OVER (PARTITION BY t."CreatedBy" ORDER BY t."Title", t."Id") AS topic_rn
    FROM "Topics" t
    WHERE t."CreatorTypeId" = (SELECT "Id" FROM "TopicCreatorTypes" WHERE "CodeName" = 'Teacher' LIMIT 1)
),
app_candidates AS (
    SELECT "StudentId", "StudentUserId", rn,
           row_number() OVER (ORDER BY rn) AS app_seq
    FROM students_indexed
)
INSERT INTO "StudentApplications" ("StudentId", "TopicId", "SupervisorRequestId", "StatusId")
SELECT
    ac."StudentId",
    CASE WHEN ac.app_seq <= 10 THEN st."TopicId" ELSE tt."TopicId" END,
    ar."SupervisorRequestId",
    CASE
        WHEN ac.rn IN (2, 6, 11, 16)
            THEN (SELECT "Id" FROM "ApplicationStatuses" WHERE "CodeName" = 'PendingDepartmentHead' LIMIT 1)
        ELSE
            (SELECT "Id" FROM "ApplicationStatuses" WHERE "CodeName" = 'Pending' LIMIT 1)
    END
FROM app_candidates ac
JOIN approved_requests ar ON ar."StudentId" = ac."StudentId"
JOIN student_topics st ON st."StudentUserId" = ac."StudentUserId"
JOIN teacher_topics_ranked tt ON tt."TeacherUserId" = ar."TeacherUserId" AND tt.topic_rn = 1;

-- ---------------------------------------------------------------------
-- ApplicationActions
INSERT INTO "ApplicationActions" ("ApplicationId", "ResponsibleId", "StatusId", "Comment")
SELECT
    a."Id",
    sr."TeacherUserId",
    (SELECT "Id" FROM "ApplicationActionStatuses" WHERE "CodeName" = 'Pending' LIMIT 1),
    'Ожидает рассмотрения научным руководителем.'
FROM "StudentApplications" a
JOIN "SupervisorRequests" sr ON sr."Id" = a."SupervisorRequestId"
WHERE a."StatusId" = (SELECT "Id" FROM "ApplicationStatuses" WHERE "CodeName" = 'Pending' LIMIT 1);

INSERT INTO "ApplicationActions" ("ApplicationId", "ResponsibleId", "StatusId", "Comment")
SELECT
    a."Id",
    sr."TeacherUserId",
    (SELECT "Id" FROM "ApplicationActionStatuses" WHERE "CodeName" = 'Approved' LIMIT 1),
    'Одобрено научным руководителем, передано на рассмотрение кафедры.'
FROM "StudentApplications" a
JOIN "SupervisorRequests" sr ON sr."Id" = a."SupervisorRequestId"
WHERE a."StatusId" = (SELECT "Id" FROM "ApplicationStatuses" WHERE "CodeName" = 'PendingDepartmentHead' LIMIT 1);

INSERT INTO "ApplicationActions" ("ApplicationId", "ResponsibleId", "StatusId", "Comment")
SELECT
    a."Id",
    d."HeadId",
    (SELECT "Id" FROM "ApplicationActionStatuses" WHERE "CodeName" = 'Pending' LIMIT 1),
    'Ожидает решения заведующего кафедрой.'
FROM "StudentApplications" a
JOIN "Students" s ON s."Id" = a."StudentId"
JOIN "Users" su ON su."Id" = s."UserId"
JOIN "Departments" d ON d."Id" = su."DepartmentId"
WHERE a."StatusId" = (SELECT "Id" FROM "ApplicationStatuses" WHERE "CodeName" = 'PendingDepartmentHead' LIMIT 1);

-- ---------------------------------------------------------------------
-- ChatMessages: по 1 сообщению на заявку (не изменяем по просьбе)
INSERT INTO "ChatMessages" ("ApplicationId", "SenderId", "Content", "SentAt", "ReadAt")
SELECT
    a."Id",
    su."Id",
    format('Тестовое сообщение по заявке %s', a."Id"::text),
    (CURRENT_TIMESTAMP - make_interval(mins => seq.seq_num::int)),
    CASE
        WHEN (seq.seq_num % 2) = 0 THEN (CURRENT_TIMESTAMP - make_interval(mins => (seq.seq_num - 1)::int))
        ELSE NULL
    END
FROM (
    SELECT "Id", "StudentId", row_number() OVER (ORDER BY "CreatedAt", "Id") AS seq_num
    FROM "StudentApplications"
) seq
JOIN "StudentApplications" a ON a."Id" = seq."Id"
JOIN "Students" s ON s."Id" = a."StudentId"
JOIN "Users" su ON su."Id" = s."UserId";

-- ---------------------------------------------------------------------
-- Notifications (20)
INSERT INTO "Notifications" ("UserId", "TypeId", "Title", "Content", "IsRead", "CreatedAt")
SELECT
    u."Id",
    (SELECT nt."Id" FROM "NotificationTypes" nt
     WHERE nt."CodeName" = (CASE (u.gs % 4)
        WHEN 0 THEN 'ApplicationStatusChanged'
        WHEN 1 THEN 'NewMessage'
        WHEN 2 THEN 'TopicApproved'
        ELSE 'TopicRejected'
     END) LIMIT 1),
    CASE (u.gs % 4)
        WHEN 0 THEN 'Статус заявки изменён'
        WHEN 1 THEN 'Новое сообщение от научного руководителя'
        WHEN 2 THEN 'Тема утверждена кафедрой'
        ELSE 'Тема не принята к рассмотрению'
    END,
    CASE (u.gs % 4)
        WHEN 0 THEN 'Ваша заявка на выпускную квалификационную работу изменила статус. Перейдите в раздел «Мои заявки» для просмотра.'
        WHEN 1 THEN 'Ваш научный руководитель оставил новое сообщение. Перейдите в чат для ознакомления с комментарием.'
        WHEN 2 THEN 'Предложенная тема утверждена заведующим кафедрой. Поздравляем — можно приступать к написанию работы!'
        ELSE 'Предложенная тема не была принята к рассмотрению. Обратитесь к научному руководителю для уточнения требований к теме.'
    END,
    CASE WHEN (u.gs % 3) = 0 THEN TRUE ELSE FALSE END,
    (CURRENT_TIMESTAMP - make_interval(hours => u.gs::int))
FROM (
    SELECT u."Id", row_number() OVER (ORDER BY u."Email") AS gs
    FROM "Users" u
    WHERE u."Email" NOT IN (
        'Aydar.Ilin@norbit.ru'::citext,
        'fqlfh2004@gmail.com'::citext
    )
    ORDER BY u."Email"
    LIMIT 20
) u;

-- ---------------------------------------------------------------------
-- GraduateWorks: по 1 записи на заявку
-- Название ВКР = название темы заявки
-- Годы: seq 1-10 → 2024, seq 11-20 → 2025
-- Комиссия: реальные инициалы из набора данных
WITH commission_data(seq_num, members) AS (
    VALUES
    (1,  'Смирнов А.Н., Кузнецова О.Д., Попов М.А.'),
    (2,  'Кузнецова О.Д., Попов М.А., Новиков А.В.'),
    (3,  'Попов М.А., Смирнов А.Н., Лебедева М.И.'),
    (4,  'Новиков А.В., Лебедева М.И., Соколов В.П.'),
    (5,  'Лебедева М.И., Соколов В.П., Ильин А.Р.'),
    (6,  'Соколов В.П., Новиков А.В., Михайлов Е.А.'),
    (7,  'Михайлов Е.А., Морозова Н.Г., Соколов В.П.'),
    (8,  'Морозова Н.Г., Михайлов Е.А., Фёдоров П.К.'),
    (9,  'Фёдоров П.К., Волков И.Д., Михайлов Е.А.'),
    (10, 'Волков И.Д., Фёдоров П.К., Морозова Н.Г.'),
    (11, 'Смирнов А.Н., Новиков А.В., Рогов Е.Б.'),
    (12, 'Кузнецова О.Д., Соколов В.П., Власов К.Ю.'),
    (13, 'Попов М.А., Михайлов Е.А., Щербакова И.Б.'),
    (14, 'Новиков А.В., Морозова Н.Г., Климов О.В.'),
    (15, 'Лебедева М.И., Фёдоров П.К., Рогов Е.Б.'),
    (16, 'Соколов В.П., Волков И.Д., Власов К.Ю.'),
    (17, 'Михайлов Е.А., Смирнов А.Н., Щербакова И.Б.'),
    (18, 'Морозова Н.Г., Кузнецова О.Д., Климов О.В.'),
    (19, 'Фёдоров П.К., Попов М.А., Рогов Е.Б.'),
    (20, 'Волков И.Д., Новиков А.В., Власов К.Ю.')
)
INSERT INTO "GraduateWorks" (
    "ApplicationId",
    "Title",
    "StudentId",
    "TeacherId",
    "Year",
    "Grade",
    "CommissionMembers",
    "FilePath",
    "FileName",
    "PresentationPath",
    "PresentationFileName"
)
SELECT
    app."Id",
    t."Title",
    st."Id",
    tchr."Id",
    CASE WHEN seq.seq_num <= 10 THEN 2024 ELSE 2025 END,
    (65 + (seq.seq_num % 31)),
    cd.members,
    format('vkr/%s/work_%s/thesis.pdf',
           CASE WHEN seq.seq_num <= 10 THEN '2024' ELSE '2025' END,
           lpad(seq.seq_num::text, 2, '0')),
    format('%s_%s.pdf', left(t."Title"::text, 40), lpad(seq.seq_num::text, 2, '0')),
    CASE WHEN (seq.seq_num % 2) = 0 THEN
        format('vkr/%s/work_%s/presentation.pptx',
               CASE WHEN seq.seq_num <= 10 THEN '2024' ELSE '2025' END,
               lpad(seq.seq_num::text, 2, '0'))
    ELSE NULL END,
    CASE WHEN (seq.seq_num % 2) = 0 THEN
        format('Презентация_%s.pptx', lpad(seq.seq_num::text, 2, '0'))
    ELSE NULL END
FROM (
    SELECT app."Id", app."StudentId", sr."TeacherUserId",
           row_number() OVER (ORDER BY app."CreatedAt", app."Id") AS seq_num
    FROM "StudentApplications" app
    JOIN "SupervisorRequests" sr ON sr."Id" = app."SupervisorRequestId"
) seq
JOIN "StudentApplications" app ON app."Id" = seq."Id"
JOIN "Topics" t ON t."Id" = app."TopicId"
JOIN "Students" st ON st."Id" = seq."StudentId"
JOIN "Teachers" tchr ON tchr."UserId" = seq."TeacherUserId"
JOIN commission_data cd ON cd.seq_num = seq.seq_num;
