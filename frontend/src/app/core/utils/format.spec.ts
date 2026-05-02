import { describe, expect, it } from 'vitest';

import { formatIsoDate } from './format';

describe('formatIsoDate', () => {
  it('formats DateOnly strings without shifting the calendar day by timezone', () => {
    expect(formatIsoDate('2025-05-02')).toBe('May 02, 2025');
  });

  it('formats DateOnly prefixes from ISO datetime strings as the same calendar day', () => {
    expect(formatIsoDate('2025-05-02T00:00:00')).toBe('May 02, 2025');
  });

  it('returns invalid date strings unchanged', () => {
    expect(formatIsoDate('not-a-date')).toBe('not-a-date');
  });
});
