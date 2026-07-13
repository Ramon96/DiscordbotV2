import { useEffect } from 'react'
import { Link, useOutletContext } from 'react-router-dom'
import { ApiError, metricsApi, overviewApi, statsApi } from '../api'
import type { DashboardOutletContext } from '../components/DashboardLayout'
import { usePolling } from '../hooks/usePolling'
import { formatCompact, formatRelative, formatUptime } from '../lib/format'
import MetricCard from '../components/MetricCard'
import {
  ApertureLogo,
  ClockIcon,
  FileTextIcon,
  ServerIcon,
  SwordIcon,
  TagIcon,
  UsersIcon,
} from '../components/Icons'

export default function OverviewPage() {
  const { onLogout, user } = useOutletContext<DashboardOutletContext>()
  const { data: stats, error: statsError } = usePolling(statsApi.get, 30000)
  const { data: metrics, error: metricsError } = usePolling(metricsApi.get, 15000)
  const { data: shirtless } = usePolling(overviewApi.shirtless, 120000)

  useEffect(() => {
    const err = statsError ?? metricsError
    if (err instanceof ApiError && err.status === 401) {
      onLogout()
    }
  }, [statsError, metricsError, onLogout])

  const uptime = metrics?.current?.process.uptimeSeconds

  return (
    <div>
      <section className="hero">
        <ApertureLogo size={52} className="aperture" />
        <div>
          <h1>Welcome back to the Enrichment Center, {user.name.split(/\s+/)[0]}.</h1>
          <p>Your window into GLaDOS — the clan hiscores, the server, and everything she's been up to. For science.</p>
        </div>
      </section>

      <section className="metric-grid">
        <MetricCard
          label="Tracked players"
          value={stats ? stats.trackedUsers.toLocaleString() : '—'}
          sub="on the clan hiscores"
          icon={<UsersIcon size={16} />}
        />
        <MetricCard
          label="Log entries"
          value={stats ? formatCompact(stats.logEntries) : '—'}
          sub="captured by GLaDOS"
          icon={<FileTextIcon size={16} />}
        />
        <MetricCard
          label="Uptime"
          value={uptime != null ? formatUptime(uptime) : '—'}
          sub="since last deploy"
          icon={<ClockIcon size={16} />}
        />
      </section>

      <h2 className="section-title">Jump in</h2>
      <section className="shortcut-grid">
        <Link to="/runescape" className="card shortcut">
          <span className="shortcut-icon">
            <SwordIcon size={22} />
          </span>
          <span className="shortcut-body">
            <strong>RuneScape</strong>
            <span>Clan hiscores, XP charts, bosses & collection logs</span>
          </span>
        </Link>
        <Link to="/server" className="card shortcut">
          <span className="shortcut-icon">
            <ServerIcon size={22} />
          </span>
          <span className="shortcut-body">
            <strong>Server</strong>
            <span>Live metrics, background jobs and logs</span>
          </span>
        </Link>
        <Link to="/changelog" className="card shortcut">
          <span className="shortcut-icon">
            <TagIcon size={22} />
          </span>
          <span className="shortcut-body">
            <strong>Changelog</strong>
            <span>What's new — every shipped change</span>
          </span>
        </Link>
      </section>

      <h2 className="section-title">Shirtless Old Man Gallery</h2>
      {shirtless && shirtless.length > 0 ? (
        <section className="gallery-grid">
          {shirtless.map((post) => (
            <a
              key={`${post.imageUrl}-${post.postedAt}`}
              href={post.imageUrl}
              target="_blank"
              rel="noreferrer"
              className="card gallery-item"
            >
              <img
                src={post.imageUrl}
                alt="A shirtless old man"
                loading="lazy"
                onError={(event) => {
                  const card = event.currentTarget.closest('.gallery-item') as HTMLElement | null
                  if (card) {
                    card.style.display = 'none'
                  }
                }}
              />
              <span className="gallery-caption">
                {post.taggedUsername ? `@${post.taggedUsername}` : 'someone'} · {formatRelative(post.postedAt)}
              </span>
            </a>
          ))}
        </section>
      ) : (
        <p className="gallery-empty">
          No shirtless old men on file yet — GLaDOS posts one every Monday. Check back soon. 👴
        </p>
      )}
    </div>
  )
}
