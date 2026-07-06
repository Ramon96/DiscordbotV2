import { useCallback, useEffect, useState } from 'react'
import { useOutletContext } from 'react-router-dom'
import { ApiError, jobsApi, statsApi } from '../api'
import type { DashboardOutletContext } from '../components/DashboardLayout'
import { usePolling } from '../hooks/usePolling'
import { formatRelative } from '../lib/format'
import MetricCard from '../components/MetricCard'

export default function JobsPage() {
  const { onLogout } = useOutletContext<DashboardOutletContext>()
  const { data: jobs, error } = usePolling(jobsApi.get, 10000)
  const { data: stats } = usePolling(statsApi.get, 15000)
  const [triggering, setTriggering] = useState<string | null>(null)

  useEffect(() => {
    if (error instanceof ApiError && error.status === 401) {
      onLogout()
    }
  }, [error, onLogout])

  const handleTrigger = useCallback(
    async (id: string) => {
      setTriggering(id)
      try {
        await jobsApi.trigger(id)
      } catch (err) {
        if (err instanceof ApiError && err.status === 401) {
          onLogout()
        }
      } finally {
        setTriggering(null)
      }
    },
    [onLogout],
  )

  if (!jobs) {
    return <div className="card muted">Loading jobs…</div>
  }

  return (
    <div className="jobs">
      <section className="metric-grid">
        <MetricCard label="Succeeded" value={jobs.statistics.succeeded.toLocaleString()} />
        <MetricCard label="Failed" value={jobs.statistics.failed.toLocaleString()} />
        <MetricCard label="Processing" value={jobs.statistics.processing.toLocaleString()} />
        <MetricCard label="Enqueued" value={jobs.statistics.enqueued.toLocaleString()} />
        <MetricCard label="Scheduled" value={jobs.statistics.scheduled.toLocaleString()} />
      </section>

      <div className="card jobs-table">
        <table>
          <thead>
            <tr>
              <th>Job</th>
              <th>Schedule</th>
              <th>Last run</th>
              <th>Next run</th>
              <th />
            </tr>
          </thead>
          <tbody>
            {jobs.recurring.map((job) => (
              <tr key={job.id}>
                <td className="job-id">{job.id}</td>
                <td className="mono">{job.cron ?? '—'}</td>
                <td className="nowrap">
                  {formatRelative(job.lastExecution)}
                  {job.lastJobState && (
                    <span className={`badge badge-${stateClass(job.lastJobState)}`}>
                      {job.lastJobState}
                    </span>
                  )}
                </td>
                <td className="nowrap">{formatRelative(job.nextExecution)}</td>
                <td className="nowrap">
                  <button
                    className="ghost small"
                    disabled={triggering === job.id}
                    onClick={() => handleTrigger(job.id)}
                  >
                    {triggering === job.id ? 'Running…' : 'Run now'}
                  </button>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      {stats && (
        <section className="metric-grid">
          <MetricCard label="Tracked OSRS users" value={stats.trackedUsers.toLocaleString()} />
          <MetricCard label="Price snapshots" value={stats.priceSnapshots.toLocaleString()} />
          <MetricCard label="Log entries" value={stats.logEntries.toLocaleString()} />
          <MetricCard label="Prices updated" value={formatRelative(stats.latestPriceAt)} />
        </section>
      )}
    </div>
  )
}

function stateClass(state: string): string {
  switch (state.toLowerCase()) {
    case 'succeeded':
      return 'succeeded'
    case 'failed':
      return 'error'
    case 'processing':
      return 'information'
    default:
      return 'debug'
  }
}
