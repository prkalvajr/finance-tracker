// Default to USD until/unless multi-currency lands.
const CURRENCY_FORMAT = new Intl.NumberFormat('en-US', {
  style: 'currency',
  currency: 'USD'
});

const DATE_FORMAT = new Intl.DateTimeFormat('en-US', {
  year: 'numeric',
  month: 'short',
  day: '2-digit'
});

export function formatCurrency(amount: number): string {
  return CURRENCY_FORMAT.format(amount);
}

export function formatIsoDate(iso: string): string {
  // Accepts both DateOnly ("2026-04-30") and full ISO datetimes.
  const parsed = new Date(iso);
  if (Number.isNaN(parsed.getTime())) {
    return iso;
  }
  return DATE_FORMAT.format(parsed);
}

// Returns a YYYY-MM-DD string for the given Date in local time.
// Used to seed the create-transaction form with today's date.
export function toDateOnlyString(d: Date = new Date()): string {
  const yyyy = d.getFullYear();
  const mm = String(d.getMonth() + 1).padStart(2, '0');
  const dd = String(d.getDate()).padStart(2, '0');
  return `${yyyy}-${mm}-${dd}`;
}
