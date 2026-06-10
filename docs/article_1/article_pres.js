// =============================================================================
// article_pres.js — презентация к конференции
// Авторы: Ильин А.А., Жугар И.К., Швагин Г.А., КНИТУ-КАИ, каф. ПМИ
// Тема: «Проектирование базы данных информационной системы выбора тем ВКР»
// Запуск: node article_pres.js  →  article_pres.pptx
// =============================================================================

const PptxGenJS = require('pptxgenjs');
const path = require('path');

const pptx = new PptxGenJS();
pptx.layout = 'LAYOUT_16x9';
pptx.title = 'Проектирование базы данных информационной системы выбора тем ВКР';
pptx.author = 'Ильин А.А., Жугар И.К., Швагин Г.А.';

// === Оформление ===============================================================
const FONT    = 'Times New Roman';
const C_BLACK = '000000';
const C_WHITE = 'FFFFFF';
const C_GRAY  = '808080';
const C_LGRAY = 'D9D9D9';

const SW = 10;
const SH = 5.63;

const IMG = {
  lcApp: path.join(__dirname, 'article_1', '1.png'),
  er:    path.join(__dirname, 'article_1', '2.png'),
};

// === Утилиты ==================================================================

function addDivider(slide, y = 0.9) {
  slide.addShape(pptx.shapes.RECTANGLE, {
    x: 0.4, y, w: SW - 0.8, h: 0.03,
    fill: { color: C_BLACK },
    line: { color: C_BLACK, width: 0 },
  });
}

function addSlideTitle(slide, title) {
  slide.addText(title, {
    x: 0.4, y: 0.12, w: SW - 0.8, h: 0.72,
    fontFace: FONT, fontSize: 22, bold: true, color: C_BLACK,
    align: 'left', valign: 'middle',
  });
  addDivider(slide);
}

function addBulletList(slide, items, opts = {}) {
  const {
    x = 0.4, y = 1.0, w = SW - 0.8, h = SH - 1.1,
    fontSize = 16, indent = 0.2, bold = false,
  } = opts;
  const rows = items.map((item) => {
    const isObj = typeof item === 'object';
    return {
      text: isObj ? item.text : item,
      options: {
        bullet: { type: 'bullet', characterCode: '2022', indent: 15 + indent * 914 },
        fontFace: FONT,
        fontSize,
        bold: isObj ? (item.bold || bold) : bold,
        color: C_BLACK,
        paraSpaceAfter: 6,
        ...(isObj ? item.options : {}),
      },
    };
  });
  slide.addText(rows, { x, y, w, h, valign: 'top' });
}

function addPageNum(slide, n, total) {
  slide.addText(`${n} / ${total}`, {
    x: SW - 1.2, y: SH - 0.35, w: 0.9, h: 0.28,
    fontFace: FONT, fontSize: 10, color: C_GRAY, align: 'right',
  });
}

// === СЛАЙДЫ ===================================================================
const TOTAL = 7;

// -----------------------------------------------------------------------------
// 1. ТИТУЛЬНЫЙ СЛАЙД
// -----------------------------------------------------------------------------
(function slide01() {
  const s = pptx.addSlide();

  s.addText([
    { text: 'МИНИСТЕРСТВО НАУКИ И ВЫСШЕГО ОБРАЗОВАНИЯ РОССИЙСКОЙ ФЕДЕРАЦИИ\n', options: { fontSize: 11, bold: false } },
    { text: 'КНИТУ-КАИ · Кафедра прикладной математики и информатики', options: { fontSize: 11, bold: false } },
  ], {
    x: 0.5, y: 0.18, w: SW - 1, h: 0.7,
    fontFace: FONT, color: C_BLACK, align: 'center',
  });

  addDivider(s, 0.95);
  addDivider(s, 0.98);

  s.addText(
    'Всероссийская молодёжная научно-практическая конференция\n«Компьютерные технологии и защита информации – 2026»',
    {
      x: 0.5, y: 1.1, w: SW - 1, h: 0.62,
      fontFace: FONT, fontSize: 13, bold: false, color: C_BLACK, align: 'center',
    }
  );

  s.addText(
    '«Проектирование базы данных информационной\nсистемы выбора тем выпускных квалификационных работ»',
    {
      x: 0.7, y: 1.85, w: SW - 1.4, h: 1.0,
      fontFace: FONT, fontSize: 20, bold: true, color: C_BLACK, align: 'center',
    }
  );

  addDivider(s, 2.96);

  s.addText([
    { text: 'Авторы:  ', options: { bold: false } },
    { text: 'Ильин А.А., Жугар И.К., Швагин Г.А.', options: { bold: true } },
  ], {
    x: 0.5, y: 3.1, w: SW - 1, h: 0.32,
    fontFace: FONT, fontSize: 14, color: C_BLACK, align: 'center',
  });

  s.addText([
    { text: 'Научный руководитель:  ', options: { bold: false } },
    { text: 'доцент каф. ПМИ Валитова Н.Л.', options: { bold: true } },
  ], {
    x: 0.5, y: 3.46, w: SW - 1, h: 0.32,
    fontFace: FONT, fontSize: 14, color: C_BLACK, align: 'center',
  });

  s.addText('Направление: 09.03.04 Программная инженерия', {
    x: 0.5, y: 3.86, w: SW - 1, h: 0.28,
    fontFace: FONT, fontSize: 12, color: C_GRAY, align: 'center',
  });

  addDivider(s, 4.25);
  addDivider(s, 4.28);

  s.addText('Казань, 2026 г.', {
    x: 0.5, y: 4.4, w: SW - 1, h: 0.35,
    fontFace: FONT, fontSize: 14, color: C_BLACK, align: 'center',
  });
})();

// -----------------------------------------------------------------------------
// 2. АКТУАЛЬНОСТЬ И ЦЕЛЬ
// -----------------------------------------------------------------------------
(function slide02() {
  const s = pptx.addSlide();
  addSlideTitle(s, '2. Актуальность и цель работы');
  addPageNum(s, 2, TOTAL);

  s.addText('Проблема', {
    x: 0.4, y: 1.05, w: SW - 0.8, h: 0.32,
    fontFace: FONT, fontSize: 16, bold: true, color: C_BLACK,
  });
  addBulletList(s, [
    'Выбор научного руководителя и темы ВКР ведётся вручную: электронная почта, личные встречи',
    'Заведующий кафедрой лишён единого представления о состоянии распределения',
    'Дублирование тем, непрозрачность процедуры, задержки и отсутствие единого архива',
  ], { y: 1.38, h: 1.35, fontSize: 15 });

  s.addText('Цель работы', {
    x: 0.4, y: 2.78, w: SW - 0.8, h: 0.32,
    fontFace: FONT, fontSize: 16, bold: true, color: C_BLACK,
  });
  s.addText(
    'Спроектировать и реализовать схему реляционной базы данных, обеспечивающей ' +
    'хранение информации о пользователях, темах, заявках студентов, сообщениях в чате ' +
    'и архиве защищённых работ с поддержкой целостности данных и встроенной бизнес-логикой.',
    {
      x: 0.55, y: 3.15, w: SW - 0.95, h: 1.0,
      fontFace: FONT, fontSize: 15, color: C_BLACK, align: 'justify',
    }
  );

  s.addText('СУБД: PostgreSQL 16  ·  ORM: Entity Framework Core  ·  API: ASP.NET Core 10', {
    x: 0.4, y: 4.28, w: SW - 0.8, h: 0.28,
    fontFace: FONT, fontSize: 13, color: C_GRAY, align: 'center',
  });
})();

// -----------------------------------------------------------------------------
// 3. ПРЕДМЕТНАЯ ОБЛАСТЬ И ЖИЗНЕННЫЙ ЦИКЛ ЗАЯВКИ
// -----------------------------------------------------------------------------
(function slide03() {
  const s = pptx.addSlide();
  addSlideTitle(s, '3. Предметная область. Жизненный цикл заявки');
  addPageNum(s, 3, TOTAL);

  addBulletList(s, [
    { text: 'Студент', bold: true },
    { text: '  выбирает тему, подаёт заявку, ведёт чат с преподавателем' },
    { text: 'Преподаватель', bold: true },
    { text: '  публикует темы, одобряет или отклоняет заявки' },
    { text: 'Заведующий кафедрой', bold: true },
    { text: '  финально утверждает переданные заявки' },
    { text: 'Администратор', bold: true },
    { text: '  ведёт учётные записи, загружает архив защищённых работ' },
  ], { x: 0.4, y: 1.0, w: 4.1, h: 3.5, fontSize: 13.5 });

  s.addImage({ path: IMG.lcApp, x: 4.6, y: 1.05, w: 5.1, h: 2.2 });

  s.addText('Рис. 1 — Диаграмма состояний заявки студента', {
    x: 4.6, y: 3.3, w: 5.1, h: 0.28,
    fontFace: FONT, fontSize: 11, color: C_GRAY, align: 'center',
  });

  addBulletList(s, [
    'Pending → ApprovedBySupervisor → PendingDepartmentHead → ApprovedByDepartmentHead',
    'В любой момент: Cancelled / RejectedBySupervisor / RejectedByDepartmentHead',
  ], { x: 4.6, y: 3.6, w: 5.1, h: 1.6, fontSize: 12 });
})();

// -----------------------------------------------------------------------------
// 4. КОНЦЕПТУАЛЬНАЯ МОДЕЛЬ (ER-ДИАГРАММА)
// -----------------------------------------------------------------------------
(function slide04() {
  const s = pptx.addSlide();
  addSlideTitle(s, '4. Концептуальная модель базы данных');
  addPageNum(s, 4, TOTAL);

  s.addImage({ path: IMG.er, x: 0.35, y: 1.0, w: 5.8, h: 4.3 });

  s.addText('17 таблиц — три группы', {
    x: 6.3, y: 1.05, w: 3.4, h: 0.32,
    fontFace: FONT, fontSize: 14, bold: true, color: C_BLACK,
  });
  addBulletList(s, [
    { text: 'Справочные (7 шт.)', bold: true },
    { text: '  UserRoles, ApplicationStatuses, TopicStatuses, AcademicDegrees и др.' },
    { text: 'Основные сущности (9 шт.)', bold: true },
    { text: '  Users, Students, Teachers, Topics, StudentApplications, ChatMessages, GraduateWorks и др.' },
    { text: 'Безопасность (1 шт.)', bold: true },
    { text: '  RefreshTokens — хранение JWT refresh-токенов' },
  ], { x: 6.3, y: 1.38, w: 3.4, h: 3.95, fontSize: 12.5 });
})();

// -----------------------------------------------------------------------------
// 5. ЦЕНТРАЛЬНАЯ СУЩНОСТЬ — StudentApplications
// -----------------------------------------------------------------------------
(function slide05() {
  const s = pptx.addSlide();
  addSlideTitle(s, '5. Логическая структура. StudentApplications');
  addPageNum(s, 5, TOTAL);

  const hOpts  = { bold: true, fontFace: FONT, fontSize: 11, color: C_BLACK, align: 'center', valign: 'middle', fill: { color: C_LGRAY } };
  const cOpts  = { fontFace: FONT, fontSize: 11, color: C_BLACK, align: 'center', valign: 'middle' };
  const lOpts  = { fontFace: FONT, fontSize: 11, color: C_BLACK, align: 'left',   valign: 'middle' };
  const border = { pt: 0.5, color: C_BLACK };

  const rows = [
    [
      { text: 'Поле',                              options: { ...hOpts, border } },
      { text: 'Тип',                               options: { ...hOpts, border } },
      { text: 'Описание',                          options: { ...hOpts, border } },
    ],
    ...[
      ['Id',                            'UUID',               'Идентификатор заявки'],
      ['StudentId',                     'UUID FK',            'Студент, подавший заявку'],
      ['TopicId',                       'UUID FK, NULL',      'Выбранная тема из каталога'],
      ['ProposedTitle',                 'CITEXT, NULL',       'Тема студента (взаимоисключает TopicId)'],
      ['StatusId',                      'UUID FK',            'Текущий статус заявки'],
      ['TeacherApprovedAt',             'TIMESTAMPTZ, NULL',  'Дата одобрения преподавателем'],
      ['TeacherRejectionReason',        'TEXT, NULL',         'Причина отклонения преподавателем'],
      ['DepartmentHeadApprovedAt',      'TIMESTAMPTZ, NULL',  'Дата утверждения заведующим'],
      ['DepartmentHeadRejectionReason', 'TEXT, NULL',         'Причина отклонения заведующим'],
      ['CancelledAt',                   'TIMESTAMPTZ, NULL',  'Дата отмены студентом'],
    ].map((row) => row.map((cell, i) => ({
      text: cell,
      options: { ...(i === 0 ? { ...lOpts, bold: true } : i === 2 ? lOpts : cOpts), border },
    }))),
  ];

  s.addTable(rows, {
    x: 0.35, y: 1.0, w: 7.1,
    colW: [2.5, 1.85, 2.75],
    rowH: 0.37,
  });

  s.addText('Ключевые правила', {
    x: 7.6, y: 1.0, w: 2.1, h: 0.32,
    fontFace: FONT, fontSize: 13, bold: true, color: C_BLACK,
  });
  addBulletList(s, [
    'CHECK: только TopicId или ProposedTitle',
    'Структура в 3НФ',
    'Временны́е метки фиксируют каждый этап',
    'Нет отдельной таблицы аудита',
  ], { x: 7.6, y: 1.35, w: 2.1, h: 3.5, fontSize: 12 });
})();

// -----------------------------------------------------------------------------
// 6. ФИЗИЧЕСКАЯ РЕАЛИЗАЦИЯ — ОГРАНИЧЕНИЯ И ИНДЕКСЫ
// -----------------------------------------------------------------------------
(function slide06() {
  const s = pptx.addSlide();
  addSlideTitle(s, '6. Физическая реализация в PostgreSQL 16');
  addPageNum(s, 6, TOTAL);

  s.addText('28 CHECK-ограничений', {
    x: 0.4, y: 1.05, w: 4.4, h: 0.32,
    fontFace: FONT, fontSize: 15, bold: true, color: C_BLACK,
  });
  addBulletList(s, [
    'Взаимоисключение TopicId и ProposedTitle',
    'Обязательность причины при отклонении заявки',
    'Запрет противоречивых решений (одновременное одобрение и отклонение)',
    'Бизнес-правила закреплены на уровне СУБД',
  ], { x: 0.4, y: 1.38, w: 4.4, h: 2.3, fontSize: 14 });

  s.addText('20 индексов', {
    x: 5.2, y: 1.05, w: 4.4, h: 0.32,
    fontFace: FONT, fontSize: 15, bold: true, color: C_BLACK,
  });
  addBulletList(s, [
    'Простые: по FK для JOIN-операций',
    'Составные: (TeacherId, Year) для фильтрации тем',
    'Частичный: только активные заявки (Status = Pending)',
    'CITEXT-индексы для регистронезависимого поиска',
  ], { x: 5.2, y: 1.38, w: 4.4, h: 2.3, fontSize: 14 });

  s.addText('Дополнительные решения', {
    x: 0.4, y: 3.75, w: SW - 0.8, h: 0.32,
    fontFace: FONT, fontSize: 15, bold: true, color: C_BLACK,
  });
  addBulletList(s, [
    'UUID-идентификаторы во всех таблицах для совместимости с распределёнными системами',
    'Тип CITEXT для регистронезависимого хранения имён и названий тем',
    'Каскадное удаление ChatMessages при удалении заявки',
  ], { x: 0.4, y: 4.08, w: SW - 0.8, h: 1.3, fontSize: 14 });
})();

// -----------------------------------------------------------------------------
// 7. ЗАКЛЮЧЕНИЕ
// -----------------------------------------------------------------------------
(function slide07() {
  const s = pptx.addSlide();
  addSlideTitle(s, '7. Заключение');
  addPageNum(s, 7, TOTAL);

  s.addText('Результаты работы:', {
    x: 0.4, y: 1.05, w: SW - 0.8, h: 0.32,
    fontFace: FONT, fontSize: 15, bold: true, color: C_BLACK,
  });
  addBulletList(s, [
    'Спроектирована реляционная БД из 17 таблиц в 3НФ для веб-сервиса выбора тем ВКР',
    'Реализована в PostgreSQL 16: 28 CHECK-ограничений закрепляют бизнес-правила на уровне СУБД',
    'Создано 20 индексов (простые, составные, частичный) для оптимизации типовых запросов',
    'Центральная сущность StudentApplications аккумулирует полный жизненный цикл заявки',
    'Схема служит источником истины для REST API на ASP.NET Core 10 с EF Core',
  ], { y: 1.4, h: 2.6, fontSize: 14.5 });

  s.addText('Практическая значимость', {
    x: 0.4, y: 4.1, w: 2.5, h: 0.32,
    fontFace: FONT, fontSize: 14, bold: true, color: C_BLACK,
  });
  s.addText(
    '— устраняет ручное управление процессом выбора тем ВКР, обеспечивает прозрачность ' +
    'на всех этапах согласования и единый архив защищённых работ',
    {
      x: 2.95, y: 4.1, w: SW - 3.35, h: 0.55,
      fontFace: FONT, fontSize: 13, color: C_BLACK, align: 'justify',
    }
  );

  s.addText('Спасибо за внимание!', {
    x: 0.4, y: 4.82, w: SW - 0.8, h: 0.55,
    fontFace: FONT, fontSize: 18, bold: true, color: C_BLACK, align: 'center',
  });
})();

// === СОХРАНЕНИЕ ==============================================================
pptx.writeFile({ fileName: path.join(__dirname, 'article_pres.pptx') })
  .then(() => console.log('✓ article_pres.pptx создан'))
  .catch((err) => { console.error('Ошибка:', err); process.exit(1); });
