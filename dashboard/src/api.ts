export interface CurrentUser {
  username: string
}

export class ApiError extends Error {
  constructor(public status: number) {
    super(`Request failed with status ${status}`)
  }
}

async function request<T>(path: string, init?: RequestInit): Promise<T> {
  const response = await fetch(path, {
    credentials: 'include',
    headers: { 'Content-Type': 'application/json' },
    ...init,
  })

  if (!response.ok) {
    throw new ApiError(response.status)
  }

  const text = await response.text()
  return (text ? JSON.parse(text) : undefined) as T
}

export const authApi = {
  me: () => request<CurrentUser>('/api/auth/me'),
  login: (username: string, password: string) =>
    request<CurrentUser>('/api/auth/login', {
      method: 'POST',
      body: JSON.stringify({ username, password }),
    }),
  logout: () => request<void>('/api/auth/logout', { method: 'POST' }),
}

export interface ProcessMetrics {
  cpuPercent: number
  workingSetMb: number
  managedHeapMb: number
  threadCount: number
  uptimeSeconds: number
}

export interface HostMetrics {
  cpuPercent: number
  memoryUsedMb: number
  memoryTotalMb: number
}

export interface MetricsSnapshot {
  timestamp: string
  process: ProcessMetrics
  host: HostMetrics | null
}

export interface MetricsResponse {
  current: MetricsSnapshot | null
  history: MetricsSnapshot[]
}

export const metricsApi = {
  get: () => request<MetricsResponse>('/api/metrics'),
}

export interface LogEntry {
  id: number
  timestamp: string
  level: string
  message: string
  exception: string | null
  sourceContext: string | null
}

export interface LogQuery {
  level?: string
  search?: string
  limit?: number
}

export const logsApi = {
  get: (query: LogQuery) => {
    const params = new URLSearchParams()
    if (query.level) {
      params.set('level', query.level)
    }
    if (query.search) {
      params.set('search', query.search)
    }
    params.set('limit', String(query.limit ?? 200))
    return request<LogEntry[]>(`/api/logs?${params.toString()}`)
  },
}

export interface JobStatistics {
  succeeded: number
  failed: number
  processing: number
  enqueued: number
  scheduled: number
}

export interface RecurringJobSummary {
  id: string
  cron: string | null
  lastExecution: string | null
  nextExecution: string | null
  lastJobState: string | null
}

export interface JobsResponse {
  statistics: JobStatistics
  recurring: RecurringJobSummary[]
}

export interface StatsResponse {
  trackedUsers: number
  priceSnapshots: number
  logEntries: number
  latestPriceAt: string | null
}

export const jobsApi = {
  get: () => request<JobsResponse>('/api/jobs'),
  trigger: (id: string) =>
    request<void>(`/api/jobs/${encodeURIComponent(id)}/trigger`, { method: 'POST' }),
}

export const statsApi = {
  get: () => request<StatsResponse>('/api/stats'),
}

export interface PlayerSummary {
  userId: string
  username: string
  totalLevel: number
  totalXp: number
  rank: number
  weeklyXpGain: number | null
}

export interface PlayerSkill {
  skillId: number
  name: string
  level: number
  experience: number
  rank: number
}

export interface XpPoint {
  date: string
  experience: number
  level: number
}

export interface PlayerDetail {
  userId: string
  username: string
  skills: PlayerSkill[]
  xpHistory: XpPoint[]
}

export const playersApi = {
  list: () => request<PlayerSummary[]>('/api/players'),
  get: (id: string) => request<PlayerDetail>(`/api/players/${id}`),
}
