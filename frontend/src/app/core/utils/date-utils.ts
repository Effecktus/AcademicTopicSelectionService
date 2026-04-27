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
