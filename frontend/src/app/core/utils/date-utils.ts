/**
 * Преобразует строку даты (`YYYY-MM-DD`) в ISO-строку начала дня (00:00:00)
 * по локальному времени браузера.
 */
export function localDateToStartOfDayUtcIso(localDate: string): string | undefined {
  if (!localDate) return undefined;
  const date = new Date(`${localDate}T00:00:00`);
  return Number.isNaN(date.getTime()) ? undefined : date.toISOString();
}

/**
 * Преобразует строку даты (`YYYY-MM-DD`) в ISO-строку конца дня (23:59:59.999)
 * по локальному времени браузера.
 */
export function localDateToEndOfDayUtcIso(localDate: string): string | undefined {
  if (!localDate) return undefined;
  const date = new Date(`${localDate}T23:59:59.999`);
  return Number.isNaN(date.getTime()) ? undefined : date.toISOString();
}

/**
 * Возвращает диапазон дат для текущего календарного года в формате `YYYY-MM-DD`.
 */
export function currentYearDateRange(): { from: string; to: string } {
  const y = new Date().getFullYear();
  return { from: `${y}-01-01`, to: `${y}-12-31` };
}

/**
 * Возвращает диапазон дат для текущего календарного года как объекты Date.
 */
export function currentYearDateRangeDates(): { from: Date; to: Date } {
  const y = new Date().getFullYear();
  return { from: new Date(y, 0, 1), to: new Date(y, 11, 31) };
}

/**
 * Преобразует Date в ISO-строку начала дня по локальному времени.
 */
export function dateToStartOfDayIso(date: Date | null): string | undefined {
  if (!date) return undefined;
  const d = new Date(date);
  d.setHours(0, 0, 0, 0);
  return d.toISOString();
}

/**
 * Преобразует Date в ISO-строку конца дня по локальному времени.
 */
export function dateToEndOfDayIso(date: Date | null): string | undefined {
  if (!date) return undefined;
  const d = new Date(date);
  d.setHours(23, 59, 59, 999);
  return d.toISOString();
}
