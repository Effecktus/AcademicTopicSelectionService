// =============================================================================
// ilyin_pres.js — презентация к защите ВКР
// Ильин Айдар Альбертович, гр. 4411, КНИТУ-КАИ, каф. ПМИ
// Тема: «Разработка прототипа сервиса по выбору научного руководителя
//        для выпускных квалификационных работ в высшем учебном заведении»
// Запуск: node ilyin_pres.js  →  ilyin_pres.pptx
// =============================================================================

const PptxGenJS = require('pptxgenjs');
const path = require('path');

const pptx = new PptxGenJS();
pptx.layout = 'LAYOUT_16x9';
pptx.title = 'Разработка прототипа сервиса по выбору научного руководителя для ВКР';
pptx.author = 'Ильин А.А.';

// === Оформление ===============================================================
const FONT      = 'Times New Roman';
const C_BLACK   = '000000';
const C_WHITE   = 'FFFFFF';
const C_GRAY    = '808080';
const C_LGRAY   = 'D9D9D9';

// Slide 16:9 → 10 × 5.63 дюймов
const SW = 10;   // slide width
const SH = 5.63; // slide height

// Пути к изображениям (относительно папки docs/)
const IMG = {
  arch:     path.join(__dirname, 'media',  'Architecture.png'),
  ucd:      path.join(__dirname, 'ucd_atss.drawio.png'),
  lcReq:    path.join(__dirname, 'lc_supervisor_request.drawio.png'),
  lcApp:    path.join(__dirname, 'lc_student_application.drawio.png'),
};

// === Утилиты ==================================================================

// Горизонтальный разделитель под заголовком слайда
function addDivider(slide, y = 0.9) {
  slide.addShape(pptx.shapes.RECTANGLE, {
    x: 0.4, y, w: SW - 0.8, h: 0.03,
    fill: { color: C_BLACK },
    line: { color: C_BLACK, width: 0 },
  });
}

// Заголовок контентного слайда
function addSlideTitle(slide, title) {
  slide.addText(title, {
    x: 0.4, y: 0.12, w: SW - 0.8, h: 0.72,
    fontFace: FONT, fontSize: 22, bold: true, color: C_BLACK,
    align: 'left', valign: 'middle',
  });
  addDivider(slide);
}

// Маркированный список (массив строк или объектов { text, options })
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

// Номер слайда
function addPageNum(slide, n, total) {
  slide.addText(`${n} / ${total}`, {
    x: SW - 1.2, y: SH - 0.35, w: 0.9, h: 0.28,
    fontFace: FONT, fontSize: 10, color: C_GRAY, align: 'right',
  });
}

// === СЛАЙДЫ ===================================================================
const TOTAL = 15;

// -----------------------------------------------------------------------------
// 1. ТИТУЛЬНЫЙ СЛАЙД
// -----------------------------------------------------------------------------
(function slide01() {
  const s = pptx.addSlide();

  // Шапка: вуз и кафедра
  s.addText([
    { text: 'МИНИСТЕРСТВО НАУКИ И ВЫСШЕГО ОБРАЗОВАНИЯ РОССИЙСКОЙ ФЕДЕРАЦИИ\n', options: { fontSize: 11, bold: false } },
    { text: 'КНИТУ-КАИ · Кафедра прикладной математики и информатики', options: { fontSize: 11, bold: false } },
  ], {
    x: 0.5, y: 0.18, w: SW - 1, h: 0.7,
    fontFace: FONT, color: C_BLACK, align: 'center',
  });

  addDivider(s, 0.95);
  addDivider(s, 0.98);

  // Тип работы
  s.addText('ВЫПУСКНАЯ КВАЛИФИКАЦИОННАЯ РАБОТА БАКАЛАВРА', {
    x: 0.5, y: 1.1, w: SW - 1, h: 0.45,
    fontFace: FONT, fontSize: 16, bold: true, color: C_BLACK, align: 'center',
  });

  // Тема
  s.addText(
    '«Разработка прототипа сервиса по выбору научного руководителя\n' +
    'для выпускных квалификационных работ в высшем учебном заведении»',
    {
      x: 0.7, y: 1.65, w: SW - 1.4, h: 1.1,
      fontFace: FONT, fontSize: 20, bold: true, color: C_BLACK, align: 'center',
    }
  );

  addDivider(s, 2.85);

  // Сведения об авторе и руководителе
  s.addText([
    { text: 'Обучающийся группы 4411:  ', options: { bold: false } },
    { text: 'Ильин Айдар Альбертович', options: { bold: true } },
  ], {
    x: 0.5, y: 3.0, w: SW - 1, h: 0.35,
    fontFace: FONT, fontSize: 15, color: C_BLACK, align: 'center',
  });

  s.addText([
    { text: 'Научный руководитель:  ', options: { bold: false } },
    { text: 'доцент каф. ПМИ Медведева С.Н.', options: { bold: true } },
  ], {
    x: 0.5, y: 3.4, w: SW - 1, h: 0.35,
    fontFace: FONT, fontSize: 15, color: C_BLACK, align: 'center',
  });

  s.addText('Направление: 09.03.04 Программная инженерия · Профиль: Разработка программно-информационных систем', {
    x: 0.5, y: 3.85, w: SW - 1, h: 0.32,
    fontFace: FONT, fontSize: 12, color: C_GRAY, align: 'center',
  });

  addDivider(s, 4.25);
  addDivider(s, 4.28);

  s.addText('Казань, 2026 г.', {
    x: 0.5, y: 4.38, w: SW - 1, h: 0.35,
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

  s.addText('Актуальность', {
    x: 0.4, y: 1.05, w: SW - 0.8, h: 0.35,
    fontFace: FONT, fontSize: 17, bold: true, color: C_BLACK,
  });
  addBulletList(s, [
    'Процесс выбора научного руководителя и темы ВКР ведётся вручную: бумажные заявления, электронная почта, таблицы',
    'Отсутствует единый регламентированный цифровой процесс согласования',
    'Разрозненность данных и невозможность отслеживания статуса заявки в реальном времени',
  ], { y: 1.38, h: 1.3, fontSize: 15 });

  s.addText('Цель работы', {
    x: 0.4, y: 2.72, w: SW - 0.8, h: 0.35,
    fontFace: FONT, fontSize: 17, bold: true, color: C_BLACK,
  });
  s.addText(
    'Повышение эффективности процесса организации ВКР за счёт разработки прототипа ' +
    'автоматизированной программно-информационной системы выбора научного руководителя и темы ВКР ' +
    'в высшем учебном заведении.',
    {
      x: 0.55, y: 3.07, w: SW - 0.95, h: 0.9,
      fontFace: FONT, fontSize: 15, color: C_BLACK, align: 'justify',
    }
  );

  s.addText('Объект: процессы организации выбора научного руководителя и темы ВКР', {
    x: 0.4, y: 4.1, w: SW - 0.8, h: 0.28,
    fontFace: FONT, fontSize: 13, color: C_GRAY,
  });
  s.addText('Предмет: методы и программные средства автоматизации согласования заявок с ролевой моделью и многоэтапным ЖЦ', {
    x: 0.4, y: 4.38, w: SW - 0.8, h: 0.28,
    fontFace: FONT, fontSize: 13, color: C_GRAY,
  });
})();

// -----------------------------------------------------------------------------
// 3. ЗАДАЧИ
// -----------------------------------------------------------------------------
(function slide03() {
  const s = pptx.addSlide();
  addSlideTitle(s, '3. Задачи работы');
  addPageNum(s, 3, TOTAL);

  addBulletList(s, [
    'Анализ предметной области и формирование требований к автоматизации',
    'Сравнительный анализ существующих систем управления учебным процессом',
    'Формулировка функциональных и нефункциональных требований',
    'Выбор и обоснование технологического стека',
    'Проектирование архитектуры по принципам Clean Architecture',
    'Разработка реляционной модели данных PostgreSQL',
    'Реализация алгоритма согласования заявок (двухэтапный ЖЦ)',
    'Разработка модуля коммуникации (чат в рамках заявки)',
    'Реализация ролевой модели доступа (студент, преподаватель, зав. кафедрой, администратор)',
    'Реализация подсистемы уведомлений с дублированием по e-mail',
    'Тестирование: бизнес-логика, разграничение прав, параллельные операции',
  ], { y: 1.0, fontSize: 14.5 });
})();

// -----------------------------------------------------------------------------
// 4. ПРЕДМЕТНАЯ ОБЛАСТЬ — ПРОБЛЕМЫ
// -----------------------------------------------------------------------------
(function slide04() {
  const s = pptx.addSlide();
  addSlideTitle(s, '4. Предметная область. Выявленные проблемы');
  addPageNum(s, 4, TOTAL);

  addBulletList(s, [
    { text: 'Отсутствие единого регламентированного процесса', bold: true },
    { text: '      Нет явных статусов заявки и единой точки контроля для всех участников' },
    { text: 'Ручной учёт и бумажный документооборот', bold: true },
    { text: '      Высок риск конфликтов при одновременном выборе одной темы разными студентами' },
    { text: 'Ограниченная коммуникация в рамках заявки', bold: true },
    { text: '      Обсуждение ведётся в мессенджерах, история не привязана к заявке' },
    { text: 'Многократное оформление бумажных заявлений', bold: true },
    { text: '      Любой отказ требует повторного оформления' },
    { text: 'Затруднённое формирование отчётности', bold: true },
    { text: '      Данные разрозненны — сводная аналитика требует ручной агрегации' },
  ], { y: 1.0, fontSize: 14 });
})();

// -----------------------------------------------------------------------------
// 5. АНАЛИЗ АНАЛОГОВ — ТАБЛИЦА
// -----------------------------------------------------------------------------
(function slide05() {
  const s = pptx.addSlide();
  addSlideTitle(s, '5. Анализ существующих решений');
  addPageNum(s, 5, TOTAL);

  const hOpts  = { bold: true, fontFace: FONT, fontSize: 12, color: C_BLACK, align: 'center', valign: 'middle', fill: { color: C_LGRAY } };
  const cOpts  = { fontFace: FONT, fontSize: 12, color: C_BLACK, align: 'center', valign: 'middle' };
  const lOpts  = { fontFace: FONT, fontSize: 12, color: C_BLACK, align: 'left',   valign: 'middle' };
  const border = { pt: 0.5, color: C_BLACK };

  const rows = [
    [
      { text: 'Критерий',                                   options: { ...hOpts, border } },
      { text: 'Moodle',                                     options: { ...hOpts, border } },
      { text: 'ELMA365 /\nБитрикс24',                      options: { ...hOpts, border } },
      { text: '1С:\nУниверситет',                          options: { ...hOpts, border } },
      { text: 'Разраба-\nтываемая',                        options: { ...hOpts, border } },
    ],
    ...[
      ['Каталог тем и преподавателей',               '±', '−', '±', '+'],
      ['Регламентированный процесс заявки',           '−', '+', '±', '+'],
      ['Чат в рамках заявки',                         '±', '±', '−', '+'],
      ['Роли студент / преподаватель / зав. каф.',   '±', '+', '+', '+'],
      ['Архив защищённых ВКР',                       '−', '−', '±', '+'],
      ['Аналитика и экспорт',                        '±', '+', '+', '+'],
      ['Специализация под задачу ВКР',               '−', '−', '±', '+'],
      ['Простота внедрения в вузе',                  '±', '−', '−', '+'],
    ].map((row) => row.map((cell, i) => ({
      text: cell,
      options: { ...(i === 0 ? lOpts : cOpts), border,
        color: cell === '+' ? '006400' : cell === '−' ? 'CC0000' : C_BLACK,
      },
    }))),
  ];

  s.addTable(rows, {
    x: 0.35, y: 1.0, w: SW - 0.7,
    colW: [3.2, 1.3, 1.5, 1.4, 1.5],
    rowH: 0.4,
  });

  s.addText('«+» — удовлетворён; «±» — частично / при доработке; «−» — не предусмотрен', {
    x: 0.35, y: 5.2, w: SW - 0.7, h: 0.28,
    fontFace: FONT, fontSize: 11, color: C_GRAY, align: 'left',
  });
})();

// -----------------------------------------------------------------------------
// 6. ТРЕБОВАНИЯ К СИСТЕМЕ
// -----------------------------------------------------------------------------
(function slide06() {
  const s = pptx.addSlide();
  addSlideTitle(s, '6. Требования к системе');
  addPageNum(s, 6, TOTAL);

  s.addText('Функциональные требования', {
    x: 0.4, y: 1.05, w: 4.5, h: 0.32,
    fontFace: FONT, fontSize: 15, bold: true, color: C_BLACK,
  });
  addBulletList(s, [
    '4 роли: студент, преподаватель, зав. кафедрой, администратор',
    'Каталог преподавателей с лимитом и фильтрацией тем ВКР',
    'ЖЦ запроса на руководство (4 статуса)',
    'ЖЦ заявки на тему ВКР (6 статусов + журнал)',
    'Чат в рамках заявки (студент ↔ преподаватель)',
    'Архив ВКР с загрузкой и presigned-ссылками',
    'Уведомления: in-app + email',
    'Аналитика и экспорт (Excel / CSV)',
  ], { x: 0.4, y: 1.35, w: 4.6, h: 3.0, fontSize: 13 });

  s.addText('Нефункциональные требования', {
    x: 5.2, y: 1.05, w: 4.5, h: 0.32,
    fontFace: FONT, fontSize: 15, bold: true, color: C_BLACK,
  });
  addBulletList(s, [
    'Безопасность: JWT + refresh-ротация, bcrypt, rate-limit',
    'Масштабируемость: stateless-серверная часть',
    'Сопровождаемость: Clean Architecture, тесты',
    'Переносимость: Docker-контейнеры',
    'Доступность: адаптивный UI, актуальные браузеры',
  ], { x: 5.2, y: 1.35, w: 4.5, h: 3.0, fontSize: 13 });
})();

// -----------------------------------------------------------------------------
// 7. ТЕХНОЛОГИЧЕСКИЙ СТЕК
// -----------------------------------------------------------------------------
(function slide07() {
  const s = pptx.addSlide();
  addSlideTitle(s, '7. Технологический стек');
  addPageNum(s, 7, TOTAL);

  const col = (title, items, x) => {
    s.addText(title, {
      x, y: 1.05, w: 2.1, h: 0.32,
      fontFace: FONT, fontSize: 14, bold: true, color: C_BLACK, align: 'center',
    });
    s.addShape(pptx.shapes.RECTANGLE, { x, y: 1.37, w: 2.1, h: 0.03, fill: { color: C_BLACK }, line: { color: C_BLACK, width: 0 } });
    addBulletList(s, items, { x, y: 1.42, w: 2.1, h: 3.8, fontSize: 13 });
  };

  col('Серверная часть', [
    'ASP.NET Core 10',
    'C# 13 / .NET 10',
    'EF Core 10',
    'JWT + Refresh',
    'MailKit',
  ], 0.35);

  col('Клиентская часть', [
    'Angular 20',
    'TypeScript',
    'PrimeNG 20',
    'Angular Signals',
  ], 2.6);

  col('Хранение данных', [
    'PostgreSQL 16',
    'Redis 7',
    'MinIO / AWS S3',
  ], 4.85);

  col('Инфраструктура', [
    'Docker / Compose',
    'Nginx (HTTPS)',
    'GitHub',
  ], 7.1);
})();

// -----------------------------------------------------------------------------
// 8. АРХИТЕКТУРА СИСТЕМЫ
// -----------------------------------------------------------------------------
(function slide08() {
  const s = pptx.addSlide();
  addSlideTitle(s, '8. Архитектура системы');
  addPageNum(s, 8, TOTAL);

  s.addImage({ path: IMG.arch, x: 0.35, y: 1.0, w: 5.8, h: 4.4 });

  addBulletList(s, [
    { text: 'Domain', bold: true },
    { text: '  Сущности и бизнес-правила, нет зависимостей' },
    { text: 'Application', bold: true },
    { text: '  Use cases (CQRS), интерфейсы репозиториев' },
    { text: 'Infrastructure', bold: true },
    { text: '  EF Core, Redis, MinIO, SMTP' },
    { text: 'API', bold: true },
    { text: '  Контроллеры, JWT Middleware, DI-конфигурация' },
  ], { x: 6.3, y: 1.05, w: 3.4, h: 4.3, fontSize: 13 });
})();

// -----------------------------------------------------------------------------
// 9. ДИАГРАММА ВАРИАНТОВ ИСПОЛЬЗОВАНИЯ
// -----------------------------------------------------------------------------
(function slide09() {
  const s = pptx.addSlide();
  addSlideTitle(s, '9. Диаграмма вариантов использования');
  addPageNum(s, 9, TOTAL);

  s.addImage({ path: IMG.ucd, x: 0.35, y: 1.0, w: 9.3, h: 4.4 });
})();

// -----------------------------------------------------------------------------
// 10. ЖИЗНЕННЫЙ ЦИКЛ — ЗАПРОС НА РУКОВОДСТВО
// -----------------------------------------------------------------------------
(function slide10() {
  const s = pptx.addSlide();
  addSlideTitle(s, '10. Жизненный цикл: запрос на научное руководство');
  addPageNum(s, 10, TOTAL);

  s.addImage({ path: IMG.lcReq, x: 0.35, y: 1.0, w: 6.2, h: 4.3 });

  addBulletList(s, [
    { text: 'Статусы', bold: true },
    { text: '  Pending' },
    { text: '  ApprovedBySupervisor' },
    { text: '  RejectedBySupervisor' },
    { text: '  Cancelled' },
    { text: 'Ключевые правила', bold: true },
    { text: '  Одобрение атомарно отменяет все прочие запросы студента' },
    { text: '  Отклонение требует обязательного комментария' },
    { text: '  Отмена студентом — только из Pending' },
  ], { x: 6.7, y: 1.05, w: 3.0, h: 4.3, fontSize: 13 });
})();

// -----------------------------------------------------------------------------
// 11. ЖИЗНЕННЫЙ ЦИКЛ — ЗАЯВКА НА ТЕМУ ВКР
// -----------------------------------------------------------------------------
(function slide11() {
  const s = pptx.addSlide();
  addSlideTitle(s, '11. Жизненный цикл: заявка на тему ВКР');
  addPageNum(s, 11, TOTAL);

  s.addImage({ path: IMG.lcApp, x: 0.35, y: 1.0, w: 6.2, h: 4.3 });

  addBulletList(s, [
    { text: 'Статусы', bold: true },
    { text: '  Pending' },
    { text: '  PendingDepartmentHead' },
    { text: '  ApprovedByDepartmentHead' },
    { text: '  RejectedBySupervisor' },
    { text: '  RejectedByDepartmentHead' },
    { text: '  Cancelled' },
    { text: 'Чат', bold: true },
    { text: '  Открыт до терминального статуса заявки' },
  ], { x: 6.7, y: 1.05, w: 3.0, h: 4.3, fontSize: 13 });
})();

// -----------------------------------------------------------------------------
// 12. ER-ДИАГРАММА / СХЕМА БД
// -----------------------------------------------------------------------------
(function slide12() {
  const s = pptx.addSlide();
  addSlideTitle(s, '12. Схема базы данных');
  addPageNum(s, 12, TOTAL);

  // Заглушка для ER-диаграммы
  s.addShape(pptx.shapes.RECTANGLE, {
    x: 0.35, y: 1.05, w: 5.8, h: 4.3,
    fill: { color: C_LGRAY },
    line: { color: C_BLACK, pt: 1 },
  });
  s.addText('[ ER-диаграмма ]', {
    x: 0.35, y: 2.7, w: 5.8, h: 0.6,
    fontFace: FONT, fontSize: 16, color: C_GRAY, align: 'center',
  });

  s.addText('Основные сущности', {
    x: 6.3, y: 1.05, w: 3.4, h: 0.32,
    fontFace: FONT, fontSize: 14, bold: true, color: C_BLACK,
  });
  addBulletList(s, [
    'Users (Student / Teacher / DepartmentHead / Admin)',
    'Departments (кафедры)',
    'Topics (темы ВКР)',
    'SupervisorRequests (запросы на руководство)',
    'StudentApplications (заявки на тему)',
    'ApplicationStatusLogs (журнал переходов)',
    'ChatMessages (сообщения чата)',
    'GraduateWorks (архив ВКР)',
    'Notifications (уведомления)',
  ], { x: 6.3, y: 1.38, w: 3.4, h: 3.95, fontSize: 12.5 });
})();

// -----------------------------------------------------------------------------
// 13. ИНТЕРФЕЙС СИСТЕМЫ
// -----------------------------------------------------------------------------
(function slide13() {
  const s = pptx.addSlide();
  addSlideTitle(s, '13. Интерфейс системы');
  addPageNum(s, 13, TOTAL);

  // Два заглушки-прямоугольника
  const ph = (x, y, label) => {
    s.addShape(pptx.shapes.RECTANGLE, {
      x, y, w: 4.4, h: 2.8,
      fill: { color: C_LGRAY },
      line: { color: C_BLACK, pt: 0.75 },
    });
    s.addText(label, {
      x, y: y + 1.1, w: 4.4, h: 0.6,
      fontFace: FONT, fontSize: 13, color: C_GRAY, align: 'center',
    });
  };

  ph(0.35, 1.05, '[ Скриншот: каталог преподавателей ]');
  ph(5.25, 1.05, '[ Скриншот: страница заявки + чат ]');

  s.addText('Скриншоты добавить вручную', {
    x: 0.35, y: 4.0, w: SW - 0.7, h: 0.28,
    fontFace: FONT, fontSize: 11, color: C_GRAY, align: 'center',
  });
})();

// -----------------------------------------------------------------------------
// 14. ТЕСТИРОВАНИЕ
// -----------------------------------------------------------------------------
(function slide14() {
  const s = pptx.addSlide();
  addSlideTitle(s, '14. Тестирование');
  addPageNum(s, 14, TOTAL);

  s.addText('Состав тестов', {
    x: 0.4, y: 1.05, w: SW - 0.8, h: 0.32,
    fontFace: FONT, fontSize: 15, bold: true, color: C_BLACK,
  });
  addBulletList(s, [
    'Unit-тесты бизнес-логики: переходы ЖЦ запроса на руководство и заявки на тему',
    'Unit-тесты разграничения прав: запрещённые действия по ролям',
    'Integration-тесты: полный сценарий от регистрации заявки до утверждения',
    'Тесты параллельных операций: конкурентные заявки на одну тему',
    'Тесты подсистемы уведомлений: фоновая очередь, stub-SMTP',
  ], { y: 1.4, fontSize: 14 });

  s.addText('Результаты', {
    x: 0.4, y: 3.45, w: SW - 0.8, h: 0.32,
    fontFace: FONT, fontSize: 15, bold: true, color: C_BLACK,
  });
  addBulletList(s, [
    'Все тесты проходят успешно (CI: зелёный статус)',
    'Покрытие бизнес-логики Application-слоя: [[N]]%',
    'Зафиксировано и устранено [[N]] дефектов в ходе тестирования',
  ], { y: 3.78, h: 1.6, fontSize: 14 });
})();

// -----------------------------------------------------------------------------
// 15. ЗАКЛЮЧЕНИЕ
// -----------------------------------------------------------------------------
(function slide15() {
  const s = pptx.addSlide();
  addSlideTitle(s, '15. Заключение');
  addPageNum(s, 15, TOTAL);

  s.addText('В результате выполнения работы:', {
    x: 0.4, y: 1.05, w: SW - 0.8, h: 0.32,
    fontFace: FONT, fontSize: 15, bold: true, color: C_BLACK,
  });
  addBulletList(s, [
    'Разработан прототип системы AcademicTopicSelectionService, автоматизирующей выбор научного руководителя и темы ВКР',
    'Реализован двухэтапный процесс согласования (преподаватель → заведующий кафедрой) с журналом статусов',
    'Обеспечена ролевая модель доступа для 4 типов пользователей',
    'Реализован чат в рамках заявки, подсистема уведомлений и архив защищённых работ',
    'Система развёртывается как набор Docker-контейнеров с Nginx и HTTPS',
    'Бизнес-логика покрыта автоматизированными тестами',
  ], { y: 1.4, h: 2.5, fontSize: 14 });

  s.addText('Научная новизна', {
    x: 0.4, y: 4.0, w: 1.8, h: 0.32,
    fontFace: FONT, fontSize: 14, bold: true, color: C_BLACK,
  });
  s.addText(
    '— специализированное решение, объединяющее каталог тем и преподавателей, ' +
    'регламентированный ЖЦ заявки, чат и архив ВКР в одном веб-сервисе',
    {
      x: 2.2, y: 4.0, w: SW - 2.6, h: 0.55,
      fontFace: FONT, fontSize: 13, color: C_BLACK, align: 'justify',
    }
  );

  s.addText('Спасибо за внимание!', {
    x: 0.4, y: 4.7, w: SW - 0.8, h: 0.55,
    fontFace: FONT, fontSize: 18, bold: true, color: C_BLACK, align: 'center',
  });
})();

// === СОХРАНЕНИЕ ==============================================================
pptx.writeFile({ fileName: path.join(__dirname, 'ilyin_pres.pptx') })
  .then(() => console.log('✓ ilyin_pres.pptx создан'))
  .catch((err) => { console.error('Ошибка:', err); process.exit(1); });
