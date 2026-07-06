import { useEffect } from 'react'
import { Link, useOutletContext } from 'react-router-dom'
import { ApiError, playersApi } from '../api'
import type { DashboardOutletContext } from '../components/DashboardLayout'
import { usePolling } from '../hooks/usePolling'
import { formatCompact } from '../lib/format'

export default function PlayersPage() {
  const { onLogout } = useOutletContext<DashboardOutletContext>()
  const { data, error } = usePolling(playersApi.list, 60000)

  useEffect(() => {
    if (error instanceof ApiError && error.status === 401) {
      onLogout()
    }
  }, [error, onLogout])

  if (!data) {
    return <div className="card muted">Loading players…</div>
  }

  if (data.length === 0) {
    return <div className="card muted">No tracked players yet.</div>
  }

  return (
    <div className="card players-table">
      <table>
        <thead>
          <tr>
            <th>#</th>
            <th>Player</th>
            <th>Total level</th>
            <th>Total XP</th>
            <th>This week</th>
          </tr>
        </thead>
        <tbody>
          {data.map((player, index) => (
            <tr key={player.userId}>
              <td className="rank">{index + 1}</td>
              <td>
                <Link to={`/players/${player.userId}`} className="player-link">
                  {player.username}
                </Link>
              </td>
              <td>{player.totalLevel.toLocaleString()}</td>
              <td>{formatCompact(player.totalXp)}</td>
              <td className={player.weeklyXpGain && player.weeklyXpGain > 0 ? 'gain' : 'muted'}>
                {player.weeklyXpGain != null ? `+${formatCompact(player.weeklyXpGain)}` : '—'}
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  )
}
