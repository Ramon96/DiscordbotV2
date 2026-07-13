export interface CurrentUser {
  id: string
  name: string
  avatarUrl: string | null
  role: string
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
  logout: () => request<void>('/api/auth/logout', { method: 'POST' }),
  // Login is a full-page redirect into the Discord OAuth flow, not a fetch.
  loginUrl: '/api/auth/login',
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
  logEntries: number
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

export interface PlayerBoss {
  activityId: number
  name: string
  score: number
  rank: number
}

export interface PlayerDetail {
  userId: string
  username: string
  skills: PlayerSkill[]
  xpHistory: XpPoint[]
  bosses: PlayerBoss[]
}

export interface BossPoint {
  date: string
  score: number
  rank: number
}

export const playersApi = {
  list: () => request<PlayerSummary[]>('/api/players'),
  get: (id: string) => request<PlayerDetail>(`/api/players/${id}`),
  skillHistory: (id: string, name: string) =>
    request<XpPoint[]>(`/api/players/${id}/skill-history?name=${encodeURIComponent(name)}`),
  bossHistory: (id: string, name: string) =>
    request<BossPoint[]>(`/api/players/${id}/boss-history?name=${encodeURIComponent(name)}`),
  collectionLog: (id: string) => request<CollectionLogResponse>(`/api/players/${id}/collection-log`),
}

export interface CollectionLogItem {
  itemId: number
  name: string | null
}

export interface CollectionLogResponse {
  count: number
  items: CollectionLogItem[]
}

export interface ChangelogEntry {
  number: number
  title: string
  mergedAt: string
  author: string
  url: string
}

export const changelogApi = {
  get: () => request<ChangelogEntry[]>('/api/changelog'),
}

export interface CommandOption {
  name: string
  description: string
  type: string
  required: boolean
}

export interface CommandInfo {
  name: string
  description: string
  options: CommandOption[]
}

export const commandsApi = {
  get: () => request<CommandInfo[]>('/api/commands'),
}

export interface ShirtlessPost {
  imageUrl: string
  postedAt: string
  taggedUsername: string | null
}

export const overviewApi = {
  shirtless: (limit = 12) => request<ShirtlessPost[]>(`/api/overview/shirtless?limit=${limit}`),
}
