import { useEffect } from 'react'
import { Link, useOutletContext } from 'react-router-dom'
import { ApiError, playersApi } from '../api'
import type { DashboardOutletContext } from '../components/DashboardLayout'
import { usePolling } from '../hooks/usePolling'
import { formatCompact } from '../lib/format'
import PageHeader from '../components/PageHeader'

export default function PlayersPage() {
  const { onLogout } = useOutletContext<DashboardOutletContext>()
  const { data, error } = usePolling(playersApi.list, 60000)

  useEffect(() => {
    if (error instanceof ApiError && error.status === 401) {
      onLogout()
    }
  }, [error, onLogout])

  return (
    <div>
      <PageHeader
        title="RuneScape"
        subtitle="Clan hiscores — ranked by total level. Click a player for skills, bosses and charts."
      />

      {!data ? (
        <div className="card muted">Loading players…</div>
      ) : data.length === 0 ? (
        <div className="card muted">No tracked players yet.</div>
      ) : (
        <div className="card players-table">
          <table>
            <thead>
              <tr>
                <th className="rank">#</th>
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
                    <Link to={`/runescape/${player.userId}`} className="player-link">
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
      )}
    </div>
  )
}
