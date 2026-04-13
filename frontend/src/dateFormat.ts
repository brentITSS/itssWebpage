const UK_DATE_LOCALE = 'en-GB';

export const formatDateUk = (value: string | Date | null | undefined): string => {
  if (!value) return '—';

  const date = value instanceof Date ? value : new Date(value);
  if (Number.isNaN(date.getTime())) return '—';

  return date.toLocaleDateString(UK_DATE_LOCALE);
};

export const formatDateTimeUk = (value: string | Date | null | undefined): string => {
  if (!value) return '—';

  const date = value instanceof Date ? value : new Date(value);
  if (Number.isNaN(date.getTime())) return '—';

  return date.toLocaleString(UK_DATE_LOCALE, {
    day: '2-digit',
    month: '2-digit',
    year: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
  });
};
