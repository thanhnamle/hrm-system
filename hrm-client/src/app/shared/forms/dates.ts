/**
 * Today's date as the API expects a DateOnly ("2026-11-16"), in the user's local time zone.
 * toISOString() would give the UTC date, which is yesterday for part of the day in UTC+7.
 */
export const todayIso = (): string => toIsoDate(new Date());

export const toIsoDate = (date: Date): string =>
  `${date.getFullYear()}-${String(date.getMonth() + 1).padStart(2, '0')}-${String(date.getDate()).padStart(2, '0')}`;
