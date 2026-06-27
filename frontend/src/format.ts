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

/**
 * Whether a membership with the given paid-through date is paid up for the
 * financial year that ends at `renewalTargetIso` (the next renewal boundary the
 * server reports). True when coverage extends past the START of that year, so a
 * member covered through to the year's end counts as paid even if their
 * paid-through instant sits a little before the exact 1 July boundary. Compared
 * against the year start (not the end) this stays robust to that off-by-hours
 * boundary; comparing against the end marked fully-covered members as unpaid.
 */
export function isPaidForRenewalYear(paidThroughIso: string | null, renewalTargetIso: string): boolean {
  if (!paidThroughIso) return false
  const paid = new Date(paidThroughIso).getTime()
  if (Number.isNaN(paid)) return false
  const yearStart = new Date(renewalTargetIso)
  yearStart.setUTCFullYear(yearStart.getUTCFullYear() - 1)
  return paid > yearStart.getTime()
}
