import { useEffect, useRef, useState } from 'react'

/**
 * Polls an async fetcher on an interval, keeping the latest result and error in state.
 * The fetcher is held in a ref so passing an inline function doesn't restart the loop.
 */
export function usePolling<T>(fetcher: () => Promise<T>, intervalMs: number) {
  const [data, setData] = useState<T | null>(null)
  const [error, setError] = useState<unknown>(null)
  const savedFetcher = useRef(fetcher)
  savedFetcher.current = fetcher

  useEffect(() => {
    let active = true
    let timer: number | undefined

    const tick = async () => {
      try {
        const result = await savedFetcher.current()
        if (active) {
          setData(result)
          setError(null)
        }
      } catch (err) {
        if (active) {
          setError(err)
        }
      } finally {
        if (active) {
          timer = window.setTimeout(tick, intervalMs)
        }
      }
    }

    tick()

    return () => {
      active = false
      if (timer) {
        window.clearTimeout(timer)
      }
    }
  }, [intervalMs])

  return { data, error }
}
