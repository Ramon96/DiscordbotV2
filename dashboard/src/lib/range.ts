export type TimeRange = 'week' | 'month' | 'year' | 'all'

const RANGE_DAYS: Record<Exclude<TimeRange, 'all'>, number> = {
  week: 7,
  month: 31,
  year: 366,
}

/** Keep only points within the selected range (by ISO `date`); 'all' returns everything. */
export function filterByRange<T extends { date: string }>(points: T[], range: TimeRange): T[] {
  if (range === 'all') {
    return points
  }
  const cutoff = Date.now() - RANGE_DAYS[range] * 86_400_000
  return points.filter((point) => new Date(point.date).getTime() >= cutoff)
}
