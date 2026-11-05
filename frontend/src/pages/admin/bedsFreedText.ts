function joinWithAmpersand(items: string[]): string {
  return items.length <= 1 ? items.join('') : `${items.slice(0, -1).join(', ')} & ${items[items.length - 1]}`
}

export function bedsFreedText(bedLabels: string[] | null): string {
  if (bedLabels === null) return 'Any garden bed they hold will be freed up for someone else.'
  if (bedLabels.length === 0) return 'They do not hold any garden beds.'
  const noun = bedLabels.length === 1 ? 'Garden bed' : 'Garden beds'
  return `${noun} ${joinWithAmpersand(bedLabels)} will be freed up for someone else.`
}
