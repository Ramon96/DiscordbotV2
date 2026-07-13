export function formatPercent(value: number): string {
  return `${value.toFixed(1)}%`
}

export function formatMb(mb: number): string {
  if (mb >= 1024) {
    return `${(mb / 1024).toFixed(1)} GB`
  }
  return `${Math.round(mb)} MB`
}

export function formatUptime(seconds: number): string {
  const days = Math.floor(seconds / 86400)
  const hours = Math.floor((seconds % 86400) / 3600)
  const minutes = Math.floor((seconds % 3600) / 60)

  if (days > 0) {
    return `${days}d ${hours}h`
  }
  if (hours > 0) {
    return `${hours}h ${minutes}m`
  }
  return `${minutes}m`
}

export function formatClock(iso: string): string {
  return new Date(iso).toLocaleTimeString([], {
    hour: '2-digit',
    minute: '2-digit',
    second: '2-digit',
  })
}

export function formatRelative(iso: string | null | undefined): string {
  if (!iso) {
    return '—'
  }

  const target = new Date(iso).getTime()
  if (Number.isNaN(target)) {
    return '—'
  }

  const diffMs = target - Date.now()
  const value = describeDuration(Math.abs(diffMs) / 1000)

  if (value === 'now') {
    return 'just now'
  }
  return diffMs > 0 ? `in ${value}` : `${value} ago`
}

const compactFormatter = new Intl.NumberFormat('en', {
  notation: 'compact',
  maximumFractionDigits: 1,
})

export function formatCompact(value: number): string {
  return compactFormatter.format(value)
}

// Exact value with thousands separators, e.g. 13138492 -> "13,138,492". Used in chart tooltips
// where the precise number matters.
export function formatFull(value: number): string {
  return Math.round(value).toLocaleString('en')
}

// Builds a Y-axis tick formatter whose decimal precision resolves the *range* of the data, so a
// rising line over a narrow band (e.g. 13.05M–13.14M) doesn't collapse every tick to "13.1M".
// A fixed 1-decimal compact format can't tell those apart; here we widen the precision only as much
// as the span needs.
export function compactAxisFormatter(values: number[]): (value: number) => string {
  const finite = values.filter((value) => Number.isFinite(value))
  if (finite.length === 0) {
    return formatCompact
  }

  const min = Math.min(...finite)
  const max = Math.max(...finite)
  const magnitude = Math.max(Math.abs(min), Math.abs(max))
  const divisor = magnitude >= 1e9 ? 1e9 : magnitude >= 1e6 ? 1e6 : magnitude >= 1e3 ? 1e3 : 1
  const suffix = magnitude >= 1e9 ? 'B' : magnitude >= 1e6 ? 'M' : magnitude >= 1e3 ? 'K' : ''

  const span = max - min
  let decimals = 1
  if (span > 0) {
    // Roughly the value between adjacent gridlines (~4 intervals), in the chosen unit.
    const tickStep = span / 4 / divisor
    decimals = Math.min(4, Math.max(0, Math.ceil(-Math.log10(tickStep))))
  }

  return (value: number) => `${(value / divisor).toFixed(decimals)}${suffix}`
}

export function formatDate(iso: string): string {
  return new Date(iso).toLocaleDateString([], { month: 'short', day: 'numeric' })
}

function describeDuration(seconds: number): string {
  if (seconds < 45) {
    return 'now'
  }
  const minutes = Math.round(seconds / 60)
  if (minutes < 60) {
    return `${minutes}m`
  }
  const hours = Math.round(minutes / 60)
  if (hours < 24) {
    return `${hours}h`
  }
  return `${Math.round(hours / 24)}d`
}
