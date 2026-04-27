import {
  currentYearDateRange,
  localDateToEndOfDayUtcIso,
  localDateToStartOfDayUtcIso,
} from './date-utils';

describe('date-utils', () => {
  describe('localDateToStartOfDayUtcIso', () => {
    it('returns undefined for empty string', () => {
      expect(localDateToStartOfDayUtcIso('')).toBeUndefined();
    });

    it('returns undefined for invalid date', () => {
      expect(localDateToStartOfDayUtcIso('not-a-date')).toBeUndefined();
    });

    it('returns ISO string for valid calendar day', () => {
      const iso = localDateToStartOfDayUtcIso('2024-06-15');
      expect(iso).toBeDefined();
      if (!iso) {
        throw new Error('expected ISO string');
      }
      expect(iso).toMatch(/^\d{4}-\d{2}-\d{2}T/);
      expect(new Date(iso).toISOString()).toBe(iso);
    });
  });

  describe('localDateToEndOfDayUtcIso', () => {
    it('returns undefined for empty string', () => {
      expect(localDateToEndOfDayUtcIso('')).toBeUndefined();
    });

    it('returns undefined for invalid date', () => {
      expect(localDateToEndOfDayUtcIso('invalid')).toBeUndefined();
    });

    it('returns ISO after start of same calendar day', () => {
      const start = localDateToStartOfDayUtcIso('2024-06-15');
      const end = localDateToEndOfDayUtcIso('2024-06-15');
      expect(start).toBeDefined();
      expect(end).toBeDefined();
      if (!start || !end) {
        throw new Error('expected ISO strings');
      }
      expect(new Date(end).getTime()).toBeGreaterThan(new Date(start).getTime());
    });
  });

  describe('currentYearDateRange', () => {
    it('returns January 1 through December 31 of current year', () => {
      const y = new Date().getFullYear();
      const range = currentYearDateRange();
      expect(range.from).toBe(`${y}-01-01`);
      expect(range.to).toBe(`${y}-12-31`);
    });
  });
});
