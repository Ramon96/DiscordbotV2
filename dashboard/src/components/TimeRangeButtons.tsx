import type { TimeRange } from '../lib/range'

const RANGES: { value: TimeRange; label: string }[] = [
  { value: 'week', label: 'Week' },
  { value: 'month', label: 'Month' },
  { value: 'year', label: 'Year' },
  { value: 'all', label: 'All' },
]

interface TimeRangeButtonsProps {
  value: TimeRange
  onChange: (range: TimeRange) => void
}

export default function TimeRangeButtons({ value, onChange }: TimeRangeButtonsProps) {
  return (
    <div className="range-buttons">
      {RANGES.map((range) => (
        <button
          key={range.value}
          className={value === range.value ? 'active' : ''}
          onClick={() => onChange(range.value)}
        >
          {range.label}
        </button>
      ))}
    </div>
  )
}
