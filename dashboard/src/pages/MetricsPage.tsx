import { useEffect } from 'react'
import { useOutletContext } from 'react-router-dom'
import { ApiError, metricsApi } from '../api'
import type { DashboardOutletContext } from '../components/DashboardLayout'
import { usePolling } from '../hooks/usePolling'
import { formatMb, formatPercent, formatUptime } from '../lib/format'
import MetricCard from '../components/MetricCard'
import MetricsCharts from '../components/MetricsCharts'

export default function MetricsPage() {
  const { onLogout } = useOutletContext<DashboardOutletContext>()
  const { data, error } = usePolling(metricsApi.get, 5000)

  useEffect(() => {
    if (error instanceof ApiError && error.status === 401) {
      onLogout()
    }
  }, [error, onLogout])

  const current = data?.current ?? null
  const host = current?.host ?? null

  if (!current) {
    return <div className="card muted">Collecting metrics…</div>
  }

  return (
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
  )
}
