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
import { compactAxisFormatter, formatDate, formatFull } from '../lib/format'
import TimeRangeButtons from './TimeRangeButtons'
import { filterByRange, type TimeRange } from '../lib/range'

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
  const [range, setRange] = useState<TimeRange>('month')

  useEffect(() => {
    let active = true
    setPoints(null)
    setError(null)

    const load =
      selection.kind === 'skill'
        ? playersApi
            .skillHistory(playerId, selection.name)
            .then((data) => data.map((point) => ({ date: point.date, value: point.experience })))
        : playersApi
            .bossHistory(playerId, selection.name)
            .then((data) => data.map((point) => ({ date: point.date, value: point.score })))

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
  const chartData = points
    ? filterByRange(points, range).map((point) => ({ date: formatDate(point.date), value: point.value }))
    : []

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

        <div className="range-row">
          <TimeRangeButtons value={range} onChange={setRange} />
        </div>

        {error ? (
          <p className="muted">{error}</p>
        ) : !points ? (
          <p className="muted">Loading…</p>
        ) : chartData.length < 2 ? (
          <p className="muted">Not enough history in this range yet — snapshots are captured daily.</p>
        ) : (
          <ResponsiveContainer width="100%" height={260}>
            <LineChart data={chartData} margin={{ top: 8, right: 12, bottom: 0, left: 8 }}>
              <CartesianGrid stroke="var(--border)" strokeDasharray="3 3" />
              <XAxis dataKey="date" tick={{ fill: 'var(--muted)', fontSize: 11 }} minTickGap={30} />
              <YAxis
                domain={['auto', 'auto']}
                tick={{ fill: 'var(--muted)', fontSize: 11 }}
                width={56}
                tickFormatter={compactAxisFormatter(chartData.map((point) => point.value))}
              />
              <Tooltip
                contentStyle={{
                  background: 'var(--surface)',
                  border: '1px solid var(--border)',
                  borderRadius: 8,
                  color: 'var(--text)',
                }}
                formatter={(value) => `${formatFull(value as number)} ${unit}`}
              />
              <Line type="monotone" dataKey="value" stroke="#5b8cff" dot={false} isAnimationActive={false} />
            </LineChart>
          </ResponsiveContainer>
        )}
      </div>
    </div>
  )
}
