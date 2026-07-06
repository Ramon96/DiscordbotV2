import { type FormEvent, useEffect, useState } from 'react'
import { useOutletContext } from 'react-router-dom'
import { ApiError, logsApi, type LogEntry } from '../api'
import type { DashboardOutletContext } from '../components/DashboardLayout'
import { formatClock } from '../lib/format'

const LEVELS = ['', 'Information', 'Warning', 'Error', 'Fatal', 'Debug']

export default function LogsPage() {
  const { onLogout } = useOutletContext<DashboardOutletContext>()
  const [level, setLevel] = useState('')
  const [searchInput, setSearchInput] = useState('')
  const [appliedSearch, setAppliedSearch] = useState('')
  const [reloadKey, setReloadKey] = useState(0)
  const [logs, setLogs] = useState<LogEntry[]>([])
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    let active = true
    setLoading(true)
    setError(null)

    logsApi
      .get({ level: level || undefined, search: appliedSearch || undefined, limit: 200 })
      .then((items) => {
        if (active) {
          setLogs(items)
        }
      })
      .catch((err) => {
        if (!active) {
          return
        }
        if (err instanceof ApiError && err.status === 401) {
          onLogout()
          return
        }
        setError('Failed to load logs.')
      })
      .finally(() => {
        if (active) {
          setLoading(false)
        }
      })

    return () => {
      active = false
    }
  }, [level, appliedSearch, reloadKey, onLogout])

  function handleSubmit(event: FormEvent) {
    event.preventDefault()
    setAppliedSearch(searchInput)
    setReloadKey((key) => key + 1)
  }

  return (
    <div className="logs">
      <form className="logs-filters" onSubmit={handleSubmit}>
        <select value={level} onChange={(event) => setLevel(event.target.value)}>
          {LEVELS.map((option) => (
            <option key={option} value={option}>
              {option === '' ? 'All levels' : option}
            </option>
          ))}
        </select>
        <input
          placeholder="Search message…"
          value={searchInput}
          onChange={(event) => setSearchInput(event.target.value)}
        />
        <button type="submit" disabled={loading}>
          {loading ? 'Loading…' : 'Refresh'}
        </button>
      </form>

      {error && <p className="error">{error}</p>}

      <div className="card logs-table">
        {logs.length === 0 && !loading ? (
          <p className="muted">No log entries.</p>
        ) : (
          <table>
            <thead>
              <tr>
                <th>Time</th>
                <th>Level</th>
                <th>Source</th>
                <th>Message</th>
              </tr>
            </thead>
            <tbody>
              {logs.map((log) => (
                <tr key={log.id}>
                  <td className="nowrap">{formatClock(log.timestamp)}</td>
                  <td>
                    <span className={`badge badge-${log.level.toLowerCase()}`}>{log.level}</span>
                  </td>
                  <td className="source">{shortSource(log.sourceContext)}</td>
                  <td className="msg">
                    {log.message}
                    {log.exception && <pre className="exception">{log.exception}</pre>}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </div>
    </div>
  )
}

function shortSource(source: string | null): string {
  if (!source) {
    return '—'
  }
  const parts = source.split('.')
  return parts[parts.length - 1]
}
