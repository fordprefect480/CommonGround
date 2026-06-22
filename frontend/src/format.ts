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
