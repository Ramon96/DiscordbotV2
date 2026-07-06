import type { ReactNode } from 'react'

interface MetricCardProps {
  label: string
  value: string
  sub?: string
  icon?: ReactNode
}

export default function MetricCard({ label, value, sub, icon }: MetricCardProps) {
  return (
    <div className="card metric">
      <span className="metric-top">
        {icon}
        <span className="metric-label">{label}</span>
      </span>
      <span className="metric-value">{value}</span>
      {sub && <span className="metric-sub">{sub}</span>}
    </div>
  )
}
