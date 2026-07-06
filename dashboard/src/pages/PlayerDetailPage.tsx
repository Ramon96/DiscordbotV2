import { useEffect, useState } from 'react'
import { Link, useOutletContext, useParams } from 'react-router-dom'
import {
  CartesianGrid,
  Line,
  LineChart,
  ResponsiveContainer,
  Tooltip,
  XAxis,
  YAxis,
} from 'recharts'
import { ApiError, playersApi, type PlayerDetail } from '../api'
import type { DashboardOutletContext } from '../components/DashboardLayout'
import { formatCompact, formatDate } from '../lib/format'
import { levelProgress, xpToNextLevel } from '../lib/osrs'

export default function PlayerDetailPage() {
  const { id } = useParams<{ id: string }>()
  const { onLogout } = useOutletContext<DashboardOutletContext>()
  const [player, setPlayer] = useState<PlayerDetail | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [view, setView] = useState<'skills' | 'bosses'>('skills')

  useEffect(() => {
    if (!id) {
      return
    }

    let active = true
    playersApi
      .get(id)
      .then((data) => {
        if (active) {
          setPlayer(data)
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
        setError(err instanceof ApiError && err.status === 404 ? 'Player not found.' : 'Failed to load player.')
      })

    return () => {
      active = false
    }
  }, [id, onLogout])

  if (error) {
    return (
      <div className="player-detail">
        <Link to="/players" className="player-link">
          ← Players
        </Link>
        <div className="card muted">{error}</div>
      </div>
    )
  }

  if (!player) {
    return <div className="card muted">Loading…</div>
  }

  const overall = player.skills.find((skill) => skill.name === 'Overall')
  const chartData = player.xpHistory.map((point) => ({
    date: formatDate(point.date),
    xp: point.experience,
  }))

  return (
    <div className="player-detail">
      <div className="player-header">
        <Link to="/players" className="player-link">
          ← Players
        </Link>
        <h2>{player.username}</h2>
        {overall && (
          <span className="muted">
            Total level {overall.level.toLocaleString()} · {formatCompact(overall.experience)} XP
          </span>
        )}
      </div>

      <div className="card chart">
        <h3>Total XP over time</h3>
        {chartData.length < 2 ? (
          <p className="muted">Not enough history yet — snapshots are captured daily.</p>
        ) : (
          <ResponsiveContainer width="100%" height={240}>
            <LineChart data={chartData} margin={{ top: 8, right: 12, bottom: 0, left: 8 }}>
              <CartesianGrid stroke="var(--border)" strokeDasharray="3 3" />
              <XAxis dataKey="date" tick={{ fill: 'var(--muted)', fontSize: 11 }} minTickGap={30} />
              <YAxis
                tick={{ fill: 'var(--muted)', fontSize: 11 }}
                width={48}
                tickFormatter={(value) => formatCompact(value as number)}
              />
              <Tooltip
                contentStyle={{
                  background: 'var(--surface)',
                  border: '1px solid var(--border)',
                  borderRadius: 8,
                  color: 'var(--text)',
                }}
                formatter={(value) => `${formatCompact(value as number)} XP`}
              />
              <Line type="monotone" dataKey="xp" stroke="#5b8cff" dot={false} isAnimationActive={false} />
            </LineChart>
          </ResponsiveContainer>
        )}
      </div>

      <div className="card">
        <div className="tab-toggle">
          <button className={view === 'skills' ? 'active' : ''} onClick={() => setView('skills')}>
            Skills
          </button>
          <button className={view === 'bosses' ? 'active' : ''} onClick={() => setView('bosses')}>
            Bosses
          </button>
        </div>

        {view === 'skills' ? (
          <div className="skills-grid">
            {player.skills
              .filter((skill) => skill.name !== 'Overall')
              .map((skill) => {
                const toNext = xpToNextLevel(skill.level, skill.experience)
                const progress = levelProgress(skill.level, skill.experience)
                return (
                  <div key={skill.skillId} className="skill">
                    <div className="skill-top">
                      <span className="skill-name">{skill.name}</span>
                      <span className="skill-level">{skill.level}</span>
                    </div>
                    <div className="skill-meta">
                      <span className="muted">{formatCompact(skill.experience)} xp</span>
                      <span className="muted">
                        {toNext != null ? `${formatCompact(toNext)} to go` : 'maxed'}
                      </span>
                    </div>
                    {progress != null && (
                      <div className="skill-bar">
                        <div className="skill-bar-fill" style={{ width: `${progress * 100}%` }} />
                      </div>
                    )}
                  </div>
                )
              })}
          </div>
        ) : player.bosses.length === 0 ? (
          <p className="muted">No boss kills on the hiscores yet.</p>
        ) : (
          <div className="skills-grid">
            {player.bosses.map((boss) => (
              <div key={boss.activityId} className="skill">
                <div className="skill-top">
                  <span className="skill-name">{boss.name}</span>
                  <span className="skill-level">{boss.score.toLocaleString()}</span>
                </div>
                <div className="skill-meta">
                  <span className="muted">kills</span>
                  <span className="muted">rank {boss.rank.toLocaleString()}</span>
                </div>
              </div>
            ))}
          </div>
        )}
      </div>
    </div>
  )
}
