import { useEffect, useState } from 'react'
import {
  CartesianGrid,
  Line,
  LineChart,
  ResponsiveContainer,
  Tooltip,
  XAxis,
  YAxis,
} from 'recharts'
import { ApiError, playersApi } from '../api'
import { formatCompact, formatDate } from '../lib/format'

export interface HistorySelection {
  kind: 'skill' | 'boss'
  name: string
}

interface PlayerHistoryModalProps {
  playerId: string
  selection: HistorySelection
  onClose: () => void
  onUnauthorized: () => void
}

export default function PlayerHistoryModal({
  playerId,
  selection,
  onClose,
  onUnauthorized,
}: PlayerHistoryModalProps) {
  const [points, setPoints] = useState<{ date: string; value: number }[] | null>(null)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    let active = true
    setPoints(null)
    setError(null)

    const load =
      selection.kind === 'skill'
        ? playersApi
            .skillHistory(playerId, selection.name)
            .then((data) => data.map((point) => ({ date: formatDate(point.date), value: point.experience })))
        : playersApi
            .bossHistory(playerId, selection.name)
            .then((data) => data.map((point) => ({ date: formatDate(point.date), value: point.score })))

    load
      .then((mapped) => {
        if (active) {
          setPoints(mapped)
        }
      })
      .catch((err) => {
        if (!active) {
          return
        }
        if (err instanceof ApiError && err.status === 401) {
          onUnauthorized()
          return
        }
        setError('Failed to load history.')
      })

    return () => {
      active = false
    }
  }, [playerId, selection, onUnauthorized])

  const unit = selection.kind === 'skill' ? 'XP' : 'kills'
  const heading = selection.kind === 'skill' ? 'XP over time' : 'kill count over time'

  return (
    <div className="modal-backdrop" onClick={onClose}>
      <div className="modal" onClick={(event) => event.stopPropagation()}>
        <div className="modal-header">
          <h3>
            {selection.name} · {heading}
          </h3>
          <button className="ghost small" onClick={onClose}>
            Close
          </button>
        </div>

        {error ? (
          <p className="muted">{error}</p>
        ) : !points ? (
          <p className="muted">Loading…</p>
        ) : points.length < 2 ? (
          <p className="muted">Not enough history yet — snapshots are captured daily.</p>
        ) : (
          <ResponsiveContainer width="100%" height={260}>
            <LineChart data={points} margin={{ top: 8, right: 12, bottom: 0, left: 8 }}>
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
                formatter={(value) => `${formatCompact(value as number)} ${unit}`}
              />
              <Line type="monotone" dataKey="value" stroke="#5b8cff" dot={false} isAnimationActive={false} />
            </LineChart>
          </ResponsiveContainer>
        )}
      </div>
    </div>
  )
}
