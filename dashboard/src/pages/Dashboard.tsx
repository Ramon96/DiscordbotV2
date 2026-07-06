import { useEffect } from 'react'
import { ApiError, authApi, metricsApi, type CurrentUser } from '../api'
import { usePolling } from '../hooks/usePolling'
import { formatMb, formatPercent, formatUptime } from '../lib/format'
import MetricCard from '../components/MetricCard'
import MetricsCharts from '../components/MetricsCharts'

interface DashboardPageProps {
  user: CurrentUser
  onLogout: () => void
}

export default function DashboardPage({ user, onLogout }: DashboardPageProps) {
  const { data, error } = usePolling(metricsApi.get, 5000)

  useEffect(() => {
    if (error instanceof ApiError && error.status === 401) {
      onLogout()
    }
  }, [error, onLogout])

  async function handleLogout() {
    try {
      await authApi.logout()
    } finally {
      onLogout()
    }
  }

  const current = data?.current ?? null
  const host = current?.host ?? null

  return (
    <div className="shell">
      <header className="topbar">
        <span className="brand">GLaDOS Dashboard</span>
        <div className="spacer" />
        <span className="muted">{user.username}</span>
        <button className="ghost" onClick={handleLogout}>
          Log out
        </button>
      </header>

      <main className="content wide">
        {!current ? (
          <div className="card muted">Collecting metrics…</div>
        ) : (
          <>
            <section className="metric-grid">
              <MetricCard
                label="Process CPU"
                value={formatPercent(current.process.cpuPercent)}
                sub="of total cores"
              />
              <MetricCard
                label="Process memory"
                value={formatMb(current.process.workingSetMb)}
                sub={`heap ${formatMb(current.process.managedHeapMb)}`}
              />
              <MetricCard
                label="Uptime"
                value={formatUptime(current.process.uptimeSeconds)}
                sub={`${current.process.threadCount} threads`}
              />
              {host ? (
                <>
                  <MetricCard
                    label="Host CPU"
                    value={formatPercent(host.cpuPercent)}
                    sub="whole machine"
                  />
                  <MetricCard
                    label="Host memory"
                    value={`${formatMb(host.memoryUsedMb)} / ${formatMb(host.memoryTotalMb)}`}
                    sub={
                      host.memoryTotalMb > 0
                        ? formatPercent((host.memoryUsedMb / host.memoryTotalMb) * 100)
                        : undefined
                    }
                  />
                </>
              ) : (
                <MetricCard label="Host metrics" value="—" sub="Linux only" />
              )}
            </section>

            <MetricsCharts history={data?.history ?? []} />
          </>
        )}
      </main>
    </div>
  )
}
