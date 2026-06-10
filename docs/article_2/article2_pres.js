// =============================================================================
// article2_pres.js — презентация к конференции
// Автор: Ильин А.А., КНИТУ-КАИ, каф. ПМИ
// Тема: «Архитектура сервиса выбора тем выпускной квалификационной работы»
// Запуск: node article2_pres.js  →  article2_pres.pptx
// =============================================================================

const PptxGenJS = require('pptxgenjs');
const path = require('path');

const pptx = new PptxGenJS();
pptx.layout = 'LAYOUT_16x9';
pptx.title = 'Архитектура сервиса выбора тем выпускной квалификационной работы';
pptx.author = 'Ильин А.А.';

// === Оформление ===============================================================
const FONT    = 'Times New Roman';
const C_BLACK = '000000';
const C_WHITE = 'FFFFFF';
const C_GRAY  = '808080';
const C_LGRAY = 'D9D9D9';

const SW = 10;
const SH = 5.63;

const IMG = {
  arch:    path.join(__dirname, 'article_2', '1.png'),
  layers:  path.join(__dirname, 'article_2', '2.png'),
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
    '«Архитектура сервиса выбора тем\nвыпускной квалификационной работы»',
    {
      x: 0.7, y: 1.85, w: SW - 1.4, h: 1.0,
      fontFace: FONT, fontSize: 22, bold: true, color: C_BLACK, align: 'center',
    }
  );

  addDivider(s, 2.96);

  s.addText([
    { text: 'Обучающийся группы 4411:  ', options: { bold: false } },
    { text: 'Ильин Айдар Альбертович', options: { bold: true } },
  ], {
    x: 0.5, y: 3.1, w: SW - 1, h: 0.32,
    fontFace: FONT, fontSize: 15, color: C_BLACK, align: 'center',
  });

  s.addText([
    { text: 'Научный руководитель:  ', options: { bold: false } },
    { text: 'доцент каф. ПМИ Валитова Н.Л.', options: { bold: true } },
  ], {
    x: 0.5, y: 3.46, w: SW - 1, h: 0.32,
    fontFace: FONT, fontSize: 15, color: C_BLACK, align: 'center',
  });

  s.addText('Направление: 09.03.04 Программная инженерия · Профиль: Разработка программно-информационных систем', {
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
// 2. ЦЕЛЬ И ЗАДАЧИ
// -----------------------------------------------------------------------------
(function slide02() {
  const s = pptx.addSlide();
  addSlideTitle(s, '2. Цель и задачи работы');
  addPageNum(s, 2, TOTAL);

  s.addText('Проблема', {
    x: 0.4, y: 1.05, w: SW - 0.8, h: 0.32,
    fontFace: FONT, fontSize: 16, bold: true, color: C_BLACK,
  });
  s.addText(
    'В вузах распределение тем ВКР и научных руководителей сопровождается ' +
    'разрозненными каналами связи и слабой прозрачностью статусов. ' +
    'Целостный веб-сервис требует устойчивой архитектуры, допускающей развитие без переписывания бизнес-логики.',
    {
      x: 0.55, y: 1.38, w: SW - 0.95, h: 0.75,
      fontFace: FONT, fontSize: 15, color: C_BLACK, align: 'justify',
    }
  );

  s.addText('Цель работы', {
    x: 0.4, y: 2.2, w: SW - 0.8, h: 0.32,
    fontFace: FONT, fontSize: 16, bold: true, color: C_BLACK,
  });
  s.addText(
    'Спроектировать и обосновать архитектуру веб-сервиса для автоматизации выбора темы ' +
    'и научного руководителя ВКР с учётом ролей студента, преподавателя, заведующего кафедрой и администратора.',
    {
      x: 0.55, y: 2.55, w: SW - 0.95, h: 0.75,
      fontFace: FONT, fontSize: 15, color: C_BLACK, align: 'justify',
    }
  );

  s.addText('Задачи', {
    x: 0.4, y: 3.36, w: SW - 0.8, h: 0.32,
    fontFace: FONT, fontSize: 16, bold: true, color: C_BLACK,
  });
  addBulletList(s, [
    'Определить назначение и границы сервиса',
    'Обосновать технологический стек',
    'Описать слоистую архитектуру и декомпозицию на подсистемы',
    'Изложить ключевые потоки данных и подходы к безопасности',
  ], { y: 3.68, h: 1.6, fontSize: 14 });
})();

// -----------------------------------------------------------------------------
// 3. ТЕХНОЛОГИЧЕСКИЙ СТЕК
// -----------------------------------------------------------------------------
(function slide03() {
  const s = pptx.addSlide();
  addSlideTitle(s, '3. Технологический стек');
  addPageNum(s, 3, TOTAL);

  const col = (title, items, x, w = 2.1) => {
    s.addText(title, {
      x, y: 1.05, w, h: 0.32,
      fontFace: FONT, fontSize: 14, bold: true, color: C_BLACK, align: 'center',
    });
    s.addShape(pptx.shapes.RECTANGLE, {
      x, y: 1.37, w, h: 0.03,
      fill: { color: C_BLACK }, line: { color: C_BLACK, width: 0 },
    });
    addBulletList(s, items, { x, y: 1.42, w, h: 3.8, fontSize: 13 });
  };

  col('Клиентская часть', [
    'Angular 20',
    'TypeScript',
    'SCSS',
    'PrimeNG 20',
    'HTTP-вызовы API',
  ], 0.35);

  col('Серверная часть', [
    'ASP.NET Core 10',
    'C# 13 / .NET 10',
    'EF Core 10',
    'JWT + Refresh',
    'Swagger / OpenAPI',
  ], 2.6);

  col('Хранение данных', [
    'PostgreSQL 16',
    '  транзакции и целостность',
    'Redis 7',
    '  refresh-токены',
    'S3 / MinIO',
    '  файлы архива ВКР',
  ], 4.85, 2.3);

  col('Инфраструктура', [
    'Docker / Compose',
    'Nginx (HTTPS)',
    'SMTP (MailKit)',
    'GitHub',
  ], 7.35, 2.3);
})();

// -----------------------------------------------------------------------------
// 4. ОБЩАЯ АРХИТЕКТУРНАЯ СХЕМА
// -----------------------------------------------------------------------------
(function slide04() {
  const s = pptx.addSlide();
  addSlideTitle(s, '4. Общая архитектурная схема');
  addPageNum(s, 4, TOTAL);

  s.addImage({ path: IMG.arch, x: 0.35, y: 1.0, w: 6.0, h: 4.35 });

  s.addText('Принципы построения', {
    x: 6.55, y: 1.05, w: 3.1, h: 0.32,
    fontFace: FONT, fontSize: 14, bold: true, color: C_BLACK,
  });
  addBulletList(s, [
    { text: 'Клиент-серверная модель', bold: true },
    { text: '  SPA в браузере; бизнес-логика и доступ к данным — на сервере' },
    { text: 'Чистая архитектура', bold: true },
    { text: '  зависимости направлены к центру; домен не зависит от фреймворков' },
    { text: 'Обмен по HTTPS (JSON)', bold: true },
    { text: '  единый протокол для всех ролей' },
    { text: 'Расширяемость', bold: true },
    { text: '  интеграции (ЭИОС, LDAP) через Infrastructure без изменения ядра' },
  ], { x: 6.55, y: 1.38, w: 3.1, h: 3.95, fontSize: 12.5 });
})();

// -----------------------------------------------------------------------------
// 5. СТРУКТУРА СЕРВЕРА И ДЕКОМПОЗИЦИЯ
// -----------------------------------------------------------------------------
(function slide05() {
  const s = pptx.addSlide();
  addSlideTitle(s, '5. Структура сервера. Декомпозиция на подсистемы');
  addPageNum(s, 5, TOTAL);

  s.addImage({ path: IMG.layers, x: 0.35, y: 1.0, w: 3.5, h: 3.85 });

  s.addText('Четыре проекта', {
    x: 4.05, y: 1.0, w: 2.55, h: 0.32,
    fontFace: FONT, fontSize: 13, bold: true, color: C_BLACK,
  });
  addBulletList(s, [
    { text: 'Domain', bold: true },
    { text: '  сущности и правила; нет зависимостей от EF Core и ASP.NET Core' },
    { text: 'Application', bold: true },
    { text: '  сервисы заявок, тем, чата, уведомлений; интерфейсы репозиториев' },
    { text: 'Infrastructure', bold: true },
    { text: '  DbContext, EF Core, Redis, S3, SMTP' },
    { text: 'API', bold: true },
    { text: '  контроллеры /api/v1/..., JWT middleware, Swagger' },
  ], { x: 4.05, y: 1.33, w: 2.55, h: 3.5, fontSize: 12.5 });

  s.addText('Подсистемы', {
    x: 6.75, y: 1.0, w: 2.9, h: 0.32,
    fontFace: FONT, fontSize: 13, bold: true, color: C_BLACK,
  });
  addBulletList(s, [
    'Управление доступом и профилями',
    'Справочники',
    'Каталог тем',
    'Процесс согласования заявок',
    'Коммуникации по заявке',
    'Уведомления (in-app + email)',
    'Архив и администрирование',
  ], { x: 6.75, y: 1.33, w: 2.9, h: 3.5, fontSize: 12.5 });
})();

// -----------------------------------------------------------------------------
// 6. ПОТОКИ ДАННЫХ И БЕЗОПАСНОСТЬ
// -----------------------------------------------------------------------------
(function slide06() {
  const s = pptx.addSlide();
  addSlideTitle(s, '6. Потоки данных и безопасность');
  addPageNum(s, 6, TOTAL);

  s.addText('Ключевые потоки данных', {
    x: 0.4, y: 1.05, w: 4.6, h: 0.32,
    fontFace: FONT, fontSize: 15, bold: true, color: C_BLACK,
  });
  addBulletList(s, [
    { text: 'Заявки:', bold: true },
    { text: '  JSON → REST API → Application → транзакция в PostgreSQL' },
    { text: 'Чат (polling):', bold: true },
    { text: '  сообщения в БД; периодические запросы без WebSocket — проще прохождение через МЭ вуза' },
    { text: 'Архив:', bold: true },
    { text: '  файлы в S3/MinIO, метаданные в PostgreSQL, доступ только у администратора' },
    { text: 'Refresh-токены:', bold: true },
    { text: '  хранятся в Redis с TTL' },
  ], { x: 0.4, y: 1.38, w: 4.6, h: 3.85, fontSize: 13.5 });

  s.addText('Безопасность', {
    x: 5.2, y: 1.05, w: 4.4, h: 0.32,
    fontFace: FONT, fontSize: 15, bold: true, color: C_BLACK,
  });
  addBulletList(s, [
    'JWT-аутентификация + ротация refresh-токенов',
    'Хэширование паролей (bcrypt)',
    'Ролевой доступ на уровне API и сервисов Application',
    'Серверная валидация входных данных',
    'CORS только для доверенных источников',
    'Problem Details и журналирование ошибок',
    'Бизнес-правила в Domain/Application — тестируемы без реальной СУБД',
  ], { x: 5.2, y: 1.38, w: 4.4, h: 3.85, fontSize: 13.5 });
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
    'Спроектирована и обоснована архитектура веб-сервиса выбора тем ВКР',
    'Четырёхслойный сервер по принципам чистой архитектуры: Domain, Application, Infrastructure, API',
    'Бизнес-правила локализованы в Domain и Application — не зависят от СУБД и фреймворков',
    'Технологический стек: Angular, ASP.NET Core, PostgreSQL, Redis, S3, SMTP',
    'Интеграции (ЭИОС, LDAP) подключаются через Infrastructure без изменения ядра',
  ], { y: 1.4, h: 2.5, fontSize: 14.5 });

  s.addText('Практическая значимость', {
    x: 0.4, y: 4.0, w: 2.5, h: 0.32,
    fontFace: FONT, fontSize: 14, bold: true, color: C_BLACK,
  });
  s.addText(
    '— сокращает объём неформальной переписки при согласовании тем ВКР, предоставляет ' +
    'единое актуальное представление о статусе заявок; изменения в подсистемах локализованы ' +
    'во внешних слоях и не затрагивают доменные сценарии согласования',
    {
      x: 2.95, y: 4.0, w: SW - 3.35, h: 0.7,
      fontFace: FONT, fontSize: 13, color: C_BLACK, align: 'justify',
    }
  );

  s.addText('Спасибо за внимание!', {
    x: 0.4, y: 4.85, w: SW - 0.8, h: 0.52,
    fontFace: FONT, fontSize: 18, bold: true, color: C_BLACK, align: 'center',
  });
})();

// === СОХРАНЕНИЕ ==============================================================
pptx.writeFile({ fileName: path.join(__dirname, 'article2_pres.pptx') })
  .then(() => console.log('✓ article2_pres.pptx создан'))
  .catch((err) => { console.error('Ошибка:', err); process.exit(1); });
