import type { CSSProperties } from 'react'
import {
  CartesianGrid,
  Line,
  LineChart,
  ResponsiveContainer,
  Tooltip,
  XAxis,
  YAxis,
} from 'recharts'
import type { MetricsSnapshot } from '../api'
import { formatClock } from '../lib/format'

interface MetricsChartsProps {
  history: MetricsSnapshot[]
}

const tooltipStyle: CSSProperties = {
  background: 'var(--surface)',
  border: '1px solid var(--border)',
  borderRadius: 8,
  color: 'var(--text)',
}

const axisTick = { fill: 'var(--muted)', fontSize: 11 }

export default function MetricsCharts({ history }: MetricsChartsProps) {
  const data = history.map((sample) => ({
    time: formatClock(sample.timestamp),
    processCpu: sample.process.cpuPercent,
    hostCpu: sample.host?.cpuPercent ?? null,
    processMem: sample.process.workingSetMb,
    hostMem: sample.host?.memoryUsedMb ?? null,
  }))

  return (
    <div className="chart-grid">
      <div className="card chart">
        <h3>CPU %</h3>
        <ResponsiveContainer width="100%" height={220}>
          <LineChart data={data} margin={{ top: 8, right: 12, bottom: 0, left: -16 }}>
            <CartesianGrid stroke="var(--border)" strokeDasharray="3 3" />
            <XAxis dataKey="time" tick={axisTick} minTickGap={40} />
            <YAxis domain={[0, 100]} tick={axisTick} />
            <Tooltip contentStyle={tooltipStyle} />
            <Line type="monotone" dataKey="processCpu" name="Process" stroke="#5b8cff" dot={false} isAnimationActive={false} />
            <Line type="monotone" dataKey="hostCpu" name="Host" stroke="#4ade80" dot={false} isAnimationActive={false} connectNulls />
          </LineChart>
        </ResponsiveContainer>
      </div>

      <div className="card chart">
        <h3>Memory (MB)</h3>
        <ResponsiveContainer width="100%" height={220}>
          <LineChart data={data} margin={{ top: 8, right: 12, bottom: 0, left: -8 }}>
            <CartesianGrid stroke="var(--border)" strokeDasharray="3 3" />
            <XAxis dataKey="time" tick={axisTick} minTickGap={40} />
            <YAxis tick={axisTick} />
            <Tooltip contentStyle={tooltipStyle} />
            <Line type="monotone" dataKey="processMem" name="Process" stroke="#5b8cff" dot={false} isAnimationActive={false} />
            <Line type="monotone" dataKey="hostMem" name="Host used" stroke="#4ade80" dot={false} isAnimationActive={false} connectNulls />
          </LineChart>
        </ResponsiveContainer>
      </div>
    </div>
  )
}
