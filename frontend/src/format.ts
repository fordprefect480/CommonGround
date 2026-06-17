/**
 * Formats a membership price (in cents) as a dollar string: whole dollars as `$25`,
 * otherwise `$25.50`.
 */
export function formatMembershipPrice(cents: number): string {
  const dollars = cents / 100
  return dollars % 1 === 0 ? `$${dollars}` : `$${dollars.toFixed(2)}`
}
