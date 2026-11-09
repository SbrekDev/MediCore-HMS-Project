import { describe, it, expect } from 'vitest';
import {
  toDayRangeStrings,
  toLocalInstantString,
} from '../features/booking/components/bookingSchema';

describe('bookingSchema', () => {
  it('formats a local date as an instant that always carries an explicit offset', () => {
    const instant = toLocalInstantString(new Date(2026, 11, 15, 9, 30, 0));

    expect(instant).toMatch(/^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}[+-]\d{2}:\d{2}$/);
    expect(instant.startsWith('2026-12-15T09:30:00')).toBe(true);
  });

  it('derives a day range that starts and ends at local midnight of consecutive days', () => {
    const { from, to } = toDayRangeStrings('2026-12-15');

    // A range built by adding a fixed 24h span would drift here in any zone with a
    // DST transition; building the next day from calendar parts keeps it on midnight.
    expect(from.startsWith('2026-12-15T00:00:00')).toBe(true);
    expect(to.startsWith('2026-12-16T00:00:00')).toBe(true);
    expect(from).toMatch(/[+-]\d{2}:\d{2}$/);
    expect(to).toMatch(/[+-]\d{2}:\d{2}$/);
  });

  it('crosses a month and a year boundary without losing a day', () => {
    expect(toDayRangeStrings('2026-12-31').to.startsWith('2027-01-01T00:00:00')).toBe(true);
    expect(toDayRangeStrings('2027-02-28').to.startsWith('2027-03-01T00:00:00')).toBe(true);
  });
});
