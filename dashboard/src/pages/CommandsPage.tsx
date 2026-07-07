import { useEffect } from 'react'
import { useOutletContext } from 'react-router-dom'
import { ApiError, commandsApi } from '../api'
import type { DashboardOutletContext } from '../components/DashboardLayout'
import { usePolling } from '../hooks/usePolling'
import PageHeader from '../components/PageHeader'

export default function CommandsPage() {
  const { onLogout } = useOutletContext<DashboardOutletContext>()
  const { data, error } = usePolling(commandsApi.get, 300000)

  useEffect(() => {
    if (error instanceof ApiError && error.status === 401) {
      onLogout()
    }
  }, [error, onLogout])

  return (
    <div>
      <PageHeader
        title="Commands"
        subtitle="Every slash command GLaDOS understands, straight from the bot."
      />

      {error && !data ? (
        <div className="card muted">Couldn’t load the command list.</div>
      ) : !data ? (
        <div className="card muted">Loading commands…</div>
      ) : data.length === 0 ? (
        <div className="card muted">No commands are registered.</div>
      ) : (
        <div className="command-list">
          {data.map((command) => (
            <div key={command.name} className="card command-card">
              <div className="command-name mono">/{command.name}</div>
              <p className="command-desc">{command.description}</p>
              {command.options.length > 0 && (
                <ul className="command-options">
                  {command.options.map((option) => (
                    <li key={option.name} className="command-option">
                      <span className="command-option-name mono">{option.name}</span>
                      <span className={`command-option-tag ${option.required ? 'required' : ''}`}>
                        {option.required ? 'required' : 'optional'}
                      </span>
                      <span className="command-option-desc">{option.description}</span>
                    </li>
                  ))}
                </ul>
              )}
            </div>
          ))}
        </div>
      )}
    </div>
  )
}
