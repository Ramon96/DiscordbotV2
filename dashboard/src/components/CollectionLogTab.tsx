import { useEffect, useState } from 'react'
import { ApiError, playersApi, type CollectionLogResponse } from '../api'

interface CollectionLogTabProps {
  playerId: string
  onUnauthorized: () => void
}

const ICON_BASE = 'https://static.runelite.net/cache/item/icon'

export default function CollectionLogTab({ playerId, onUnauthorized }: CollectionLogTabProps) {
  const [data, setData] = useState<CollectionLogResponse | null>(null)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    let active = true
    playersApi
      .collectionLog(playerId)
      .then((result) => {
        if (active) {
          setData(result)
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
        setError('Failed to load collection log.')
      })

    return () => {
      active = false
    }
  }, [playerId, onUnauthorized])

  if (error) {
    return <p className="muted">{error}</p>
  }

  if (!data) {
    return <p className="muted">Loading collection log…</p>
  }

  if (data.count === 0) {
    return <p className="muted">No collection log items tracked yet.</p>
  }

  return (
    <>
      <p className="muted collog-count">{data.count.toLocaleString()} items</p>
      <div className="collog-grid">
        {data.items.map((item) => (
          <div key={item.itemId} className="collog-item" title={item.name ?? `Item ${item.itemId}`}>
            <img
              src={`${ICON_BASE}/${item.itemId}.png`}
              alt={item.name ?? String(item.itemId)}
              loading="lazy"
              onError={(event) => {
                event.currentTarget.style.visibility = 'hidden'
              }}
            />
            <span className="collog-name">{item.name ?? `#${item.itemId}`}</span>
          </div>
        ))}
      </div>
    </>
  )
}
