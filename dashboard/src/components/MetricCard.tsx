interface MetricCardProps {
  label: string
  value: string
  sub?: string
}

export default function MetricCard({ label, value, sub }: MetricCardProps) {
  return (
    <div className="card metric">
      <span className="metric-label">{label}</span>
      <span className="metric-value">{value}</span>
      {sub && <span className="metric-sub">{sub}</span>}
    </div>
  )
}
