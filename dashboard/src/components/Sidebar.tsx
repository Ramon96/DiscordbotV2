import { NavLink } from 'react-router-dom'
import type { ReactNode } from 'react'
import type { CurrentUser } from '../api'
import {
  ApertureLogo,
  CloseIcon,
  HomeIcon,
  LogOutIcon,
  ServerIcon,
  SwordIcon,
  TagIcon,
} from './Icons'

interface SidebarProps {
  user: CurrentUser
  onLogout: () => void
  open?: boolean
  onClose?: () => void
}

interface NavItem {
  to: string
  label: string
  icon: ReactNode
  end?: boolean
}

const mainNav: NavItem[] = [
  { to: '/', label: 'Overview', icon: <HomeIcon />, end: true },
  { to: '/runescape', label: 'RuneScape', icon: <SwordIcon /> },
]

const systemNav: NavItem[] = [
  { to: '/server', label: 'Server', icon: <ServerIcon /> },
  { to: '/changelog', label: 'Changelog', icon: <TagIcon /> },
]

function NavGroup({ label, items }: { label: string; items: NavItem[] }) {
  return (
    <div className="sidebar-group">
      <span className="sidebar-group-label">{label}</span>
      {items.map((item) => (
        <NavLink
          key={item.to}
          to={item.to}
          end={item.end}
          className={({ isActive }) => (isActive ? 'side-link active' : 'side-link')}
        >
          {item.icon}
          <span>{item.label}</span>
        </NavLink>
      ))}
    </div>
  )
}

export default function Sidebar({ user, onLogout, open, onClose }: SidebarProps) {
  const isAdmin = user.role === 'Admin'

  return (
    <aside className={open ? 'sidebar open' : 'sidebar'}>
      <div className="sidebar-brand">
        <ApertureLogo size={30} className="aperture" />
        <span className="sidebar-brand-text">
          <strong>GLaDOS</strong>
          <span>Enrichment Center</span>
        </span>
        <button className="ghost icon-btn drawer-close" aria-label="Close menu" onClick={onClose}>
          <CloseIcon size={20} />
        </button>
      </div>
      <div className="sidebar-rule" />

      <div className="core-status">
        <span className="core-status-dot" />
        <span className="core-status-text">GLaDOS Core · Online</span>
      </div>

      <nav className="sidebar-nav">
        <NavGroup label="Enrichment" items={mainNav} />
        <NavGroup label="Facility" items={systemNav} />
      </nav>

      <div className="sidebar-footer">
        <div className="user-chip">
          {user.avatarUrl ? (
            <img
              className="user-avatar"
              src={user.avatarUrl}
              alt=""
              onError={(event) => {
                event.currentTarget.style.display = 'none'
              }}
            />
          ) : (
            <span className="user-avatar" />
          )}
          <span className="user-meta">
            <span className="user-name">{user.name}</span>
            <span className={`role-pill ${isAdmin ? 'admin' : 'viewer'}`}>{user.role}</span>
          </span>
        </div>
        <button className="ghost small" onClick={onLogout}>
          <LogOutIcon size={15} /> Log out
        </button>
      </div>
    </aside>
  )
}
