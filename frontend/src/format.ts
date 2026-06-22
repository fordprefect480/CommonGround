/**
 * Formats a price (in cents) as a dollar string: whole dollars as `$25`,
 * otherwise `$25.50`.
 */
export function formatPrice(cents: number): string {
  const dollars = cents / 100
  return dollars % 1 === 0 ? `$${dollars}` : `$${dollars.toFixed(2)}`
}

/** @deprecated Use {@link formatPrice}. */
export const formatMembershipPrice = formatPrice

/**
 * Financial-year label (e.g. `2026/27`) for the FY containing `date`. The
 * Australian financial year runs 1 July – 30 June, so July onwards belongs to
 * the year starting that July.
 */
export function financialYearLabel(date: Date): string {
  const startYear = date.getMonth() >= 6 ? date.getFullYear() : date.getFullYear() - 1
  return `${startYear}/${String(startYear + 1).slice(-2)}`
}

/**
 * The financial year a membership is paid up to, derived from its paid-through
 * date (the 1 July boundary). Steps back a day so the boundary lands inside the
 * year actually covered rather than the next one - e.g. a paid-through of
 * 1 July 2027 yields `2026/27`.
 */
export function membershipPaidThroughFyLabel(paidThroughIso: string): string {
  const d = new Date(paidThroughIso)
  d.setDate(d.getDate() - 1)
  return financialYearLabel(d)
}
