import { useEffect } from 'react'
import { useOutletContext } from 'react-router-dom'
import { ApiError, changelogApi } from '../api'
import type { DashboardOutletContext } from '../components/DashboardLayout'
import { usePolling } from '../hooks/usePolling'
import { formatDate, formatRelative } from '../lib/format'
import PageHeader from '../components/PageHeader'

export default function ChangelogPage() {
  const { onLogout } = useOutletContext<DashboardOutletContext>()
  const { data, error } = usePolling(changelogApi.get, 300000)

  useEffect(() => {
    if (error instanceof ApiError && error.status === 401) {
      onLogout()
    }
  }, [error, onLogout])

  return (
    <div>
      <PageHeader title="Changelog" subtitle="Every change shipped to GLaDOS, newest first." />

      {error && !data ? (
        <div className="card muted">Couldn’t load the changelog (GitHub unavailable).</div>
      ) : !data ? (
        <div className="card muted">Loading changelog…</div>
      ) : data.length === 0 ? (
        <div className="card muted">No merged pull requests found.</div>
      ) : (
        <div className="card changelog">
          <ul className="changelog-list">
            {data.map((entry) => (
              <li key={entry.number} className="changelog-item">
                <a href={entry.url} target="_blank" rel="noreferrer" className="changelog-title">
                  {entry.title}
                </a>
                <div className="changelog-meta muted">
                  #{entry.number} · merged {formatRelative(entry.mergedAt)} ({formatDate(entry.mergedAt)}) · {entry.author}
                </div>
              </li>
            ))}
          </ul>
        </div>
      )}
    </div>
  )
}
