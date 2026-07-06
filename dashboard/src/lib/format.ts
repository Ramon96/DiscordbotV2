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
