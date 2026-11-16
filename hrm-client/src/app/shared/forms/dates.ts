/**
 * Today's date as the API expects a DateOnly ("2026-11-16"), in the user's local time zone.
 * toISOString() would give the UTC date, which is yesterday for part of the day in UTC+7.
 */
export const todayIso = (): string => toIsoDate(new Date());

/**
 * Monday–Friday days from start to end inclusive ("2026-11-16" strings), the same rule as the
 * API's WorkingDays.Count. Used only to preview a request; the API computes the real value.
 */
export function workingDays(start: string, end: string): number {
  const day = new Date(`${start}T00:00:00`);
  const last = new Date(`${end}T00:00:00`);
  let count = 0;
  while (day <= last) {
    if (day.getDay() !== 0 && day.getDay() !== 6) count++;
    day.setDate(day.getDate() + 1);
  }
  return count;
}

export const toIsoDate = (date: Date): string =>
  `${date.getFullYear()}-${String(date.getMonth() + 1).padStart(2, '0')}-${String(date.getDate()).padStart(2, '0')}`;
