const MONTH_MS = 30 * 24 * 60 * 60 * 1000

export function isOlderThanAMonth(ev: { startUtc: string; endUtc?: string | null }, now: number): boolean {
  const finished = new Date(ev.endUtc ?? ev.startUtc).getTime()
  return finished < now - MONTH_MS
}
